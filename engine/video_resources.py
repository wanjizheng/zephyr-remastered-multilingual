"""Append localized WebM streams to a pristine movie bundle."""
import hashlib
from pathlib import Path
import UnityPy
from UnityPy.streams import EndianBinaryReader
from video_crypto import decrypt_video

def rebuild_videos(src, dst, item, payload):
 env=UnityPy.load(str(src))
 objects={(o.assets_file.name,o.path_id):o for o in env.objects}
 streams={}
 for edit in item['videos']:
  obj=objects[(edit['sf'],edit['pid'])]
  if obj.type.name!='VideoClip' or hashlib.sha256(obj.get_raw_data()).hexdigest()!=edit['before_raw_sha256']:
   raise ValueError('视频资源版本不匹配')
  tree=obj.read_typetree();external=tree['m_ExternalResources']
  name=Path(external['m_Source'].replace('\\','/')).name
  if name not in env.file.files:raise ValueError('视频数据流不存在')
  if name not in streams:streams[name]=bytearray(env.file.files[name].bytes)
  data=streams[name];start=external['m_Offset'];size=external['m_Size']
  if hashlib.sha256(data[start:start+size]).hexdigest()!=edit['original_video_sha256']:
   raise ValueError('原始视频校验失败')
  root=Path(payload).resolve();path=(root/edit['payload']).resolve()
  if not path.is_relative_to(root):raise ValueError('非法视频数据路径')
  video=path.read_bytes()
  if edit.get('encoding')=='aes-256-gcm-v1':video=decrypt_video(video,edit['sha256'])
  elif edit.get('encoding'):raise ValueError('未知视频编码')
  if hashlib.sha256(video).hexdigest()!=edit['sha256']:raise ValueError('本地化视频校验失败')
  external['m_Offset']=len(data);external['m_Size']=len(video);data.extend(video)
  obj.save_typetree(tree)
 for name,data in streams.items():
  reader=EndianBinaryReader(bytes(data));reader.flags=env.file.files[name].flags;env.file.files[name]=reader
 dst.write_bytes(env.file.save(packer='lz4'))
