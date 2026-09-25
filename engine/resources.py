"""Rebuild supported resources using local originals and independently licensed glyphs."""
import copy,json,hashlib,struct,sys
from pathlib import Path
import UnityPy
import spooky
from patch_data import apply_ops,texture_pixels
from catalog import Catalog
from video_resources import rebuild_videos
from speaker_alias import add_unit_aliases,add_sprite_aliases,add_catalog_aliases
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def safe(root,relative):
 root=Path(root).resolve();path=(root/relative).resolve()
 if not path.is_relative_to(root) or path==root:raise ValueError('路径超出游戏目录')
 return path
def replace_metadata_literals(raw,edits):
 """Replace IL2CPP UTF-8 literals while retaining every metadata section offset."""
 table,table_bytes,count=struct.unpack_from('<iii',raw,8)
 data,data_bytes,_=struct.unpack_from('<iii',raw,20)
 offsets=list(struct.unpack_from('<'+str(count)+'I',raw,table))
 if table_bytes!=count*4 or offsets[0]!=0 or offsets[-1]!=data_bytes:
  raise ValueError('Unknown metadata literal layout')
 replacements={}
 for edit in edits:
  index=edit['index']
  if not 0<=index<count-1 or index in replacements:raise ValueError('Invalid literal index')
  old=raw[data+offsets[index]:data+offsets[index+1]]
  if hashlib.sha256(old).hexdigest()!=edit['expected_sha256']:raise ValueError('Metadata literal changed')
  replacements[index]=edit['text'].encode('utf8')
 parts=[];new_offsets=[0]
 for index in range(count-1):
  part=replacements.get(index,raw[data+offsets[index]:data+offsets[index+1]])
  parts.append(part);new_offsets.append(new_offsets[-1]+len(part))
 joined=b''.join(parts)
 if len(joined)>data_bytes:raise ValueError('Metadata literal pool overflow')
 result=bytearray(raw)
 result[data:data+data_bytes]=joined+b'\0'*(data_bytes-len(joined))
 struct.pack_into('<'+str(count)+'I',result,table,*new_offsets)
 if len(result)!=len(raw):raise ValueError('Metadata size changed')
 for index in range(count-1):
  actual=result[data+new_offsets[index]:data+new_offsets[index+1]]
  expected=replacements.get(index,raw[data+offsets[index]:data+offsets[index+1]])
  if actual!=expected:raise ValueError('Metadata literal readback failed')
 return result
def rebuild(original,out,patch,payload,verify_output=True):
 cache={}
 def schema(spec):
  key=spec['file']
  if key not in cache:cache[key]=UnityPy.load(str(safe(original,key)))
  return next(o.serialized_type.node for o in cache[key].objects if o.assets_file.name==spec['sf'] and o.path_id==spec['pid'])
 for i,item in enumerate(patch['files']):
  src=safe(original,item['path']);dst=safe(out,item['path']);dst.parent.mkdir(parents=True,exist_ok=True)
  if sha(src)!=item['before_sha256']:raise ValueError('原版文件校验失败：'+Path(item['path']).name)
  progress_label={'zh-Hans':'正在准备资源','zh-Hant':'正在準備資源','en':'Preparing resources'}.get(patch.get('language'),'正在准备资源')
  print('%s %d/%d'%(progress_label,i+1,len(patch['files'])),file=sys.stderr,flush=True)
  if item['kind']=='unity':
   env=UnityPy.load(str(src));objects={(o.assets_file.name,o.path_id):o for o in env.objects}
   for edit in item['changes']:
    obj=objects[(edit['sf'],edit['pid'])]
    if hashlib.sha256(obj.get_raw_data()).hexdigest()!=edit['before_raw_sha256']:raise ValueError('资源对象版本不匹配')
    node=schema(edit['schema']) if edit['schema'] else obj.serialized_type.node;tree=obj.read_typetree(nodes=node);pixels=None
    if edit['texture']:
     pixels=obj.read().get_image_data()
    tree=apply_ops(tree,edit['ops'])
    if pixels is not None:tree['image data']=texture_pixels(pixels,tree['m_Width'],tree['m_Height'],edit['texture'],payload)
    obj.save_typetree(tree,nodes=node)
   add_unit_aliases(env,item.get('speaker_aliases',[]))
   add_sprite_aliases(env,item.get('sprite_aliases',[]))
   if src.suffix=='.bundle':env.save(pack='lz4',out_path=str(dst.parent))
   else:dst.write_bytes(env.file.save())
  elif item['kind']=='video_bundle':rebuild_videos(src,dst,item,payload)
  elif item['kind']=='bytes':
   raw=bytearray(src.read_bytes())
   for edit in item['edits']:
    a=edit['offset'];b=a+edit['old_length'];replacement=edit['text'].encode('utf8')
    assert len(replacement)==b-a and hashlib.sha256(raw[a:b]).hexdigest()==edit['expected_sha256'];raw[a:b]=replacement
   if item.get('literal_edits'):raw=replace_metadata_literals(raw,item['literal_edits'])
   dst.write_bytes(raw)
  elif item['kind']=='catalog':
   raw=bytearray(src.read_bytes());entries=Catalog(raw).bundles()
   for bundle in patch['files']:
    if not bundle['path'].endswith('.bundle'):continue
    output=safe(out,bundle['path']);e=next(e for e in entries if Path(e['internal_id'].replace('\\','/')).name==output.name)
    assert sha(output)==bundle['after_sha256'];struct.pack_into('<I',raw,e['crc_offset'],bundle['native_crc']);struct.pack_into('<I',raw,e['size_offset'],output.stat().st_size)
   raw=add_catalog_aliases(raw,item.get('speaker_aliases',[]));dst.write_bytes(raw)
  elif item['kind']=='catalog_hash':dst.write_text(spooky.hash128(dst.with_name('catalog.bin').read_bytes()).to_bytes(16,'little').hex(),encoding='ascii')
  else:raise ValueError('未知资源操作')
  if verify_output and sha(dst)!=item['after_sha256']:raise ValueError('生成结果与发行版本不一致：'+dst.name)
