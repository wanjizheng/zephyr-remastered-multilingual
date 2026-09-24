"""Rebuild supported resources using local originals and independently licensed glyphs."""
import copy,json,hashlib,struct,sys
from pathlib import Path
import UnityPy
import spooky
from patch_data import apply_ops,texture_pixels
from catalog import Catalog
from video_resources import rebuild_videos
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def safe(root,relative):
 root=Path(root).resolve();path=(root/relative).resolve()
 if not path.is_relative_to(root) or path==root:raise ValueError('路径超出游戏目录')
 return path
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
   if src.suffix=='.bundle':env.save(pack='lz4',out_path=str(dst.parent))
   else:dst.write_bytes(env.file.save())
  elif item['kind']=='video_bundle':rebuild_videos(src,dst,item,payload)
  elif item['kind']=='bytes':
   raw=bytearray(src.read_bytes())
   for edit in item['edits']:
    a=edit['offset'];b=a+edit['old_length'];replacement=edit['text'].encode('utf8')
    assert len(replacement)==b-a and hashlib.sha256(raw[a:b]).hexdigest()==edit['expected_sha256'];raw[a:b]=replacement
   dst.write_bytes(raw)
  elif item['kind']=='catalog':
   raw=bytearray(src.read_bytes());entries=Catalog(raw).bundles()
   for bundle in patch['files']:
    if not bundle['path'].endswith('.bundle'):continue
    output=safe(out,bundle['path']);e=next(e for e in entries if Path(e['internal_id'].replace('\\','/')).name==output.name)
    assert sha(output)==bundle['after_sha256'];struct.pack_into('<I',raw,e['crc_offset'],bundle['native_crc']);struct.pack_into('<I',raw,e['size_offset'],output.stat().st_size)
   dst.write_bytes(raw)
  elif item['kind']=='catalog_hash':dst.write_text(spooky.hash128(dst.with_name('catalog.bin').read_bytes()).to_bytes(16,'little').hex(),encoding='ascii')
  else:raise ValueError('未知资源操作')
  if verify_output and sha(dst)!=item['after_sha256']:raise ValueError('生成结果与发行版本不一致：'+dst.name)
