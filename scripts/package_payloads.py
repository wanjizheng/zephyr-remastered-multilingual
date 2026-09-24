"""Seal local video inputs and copy only authenticated release payloads."""
import argparse,gzip,hashlib,json,runpy,shutil,sys
from pathlib import Path
P=Path(__file__).resolve().parents[1];sys.path.insert(0,str(P/'engine'))
from video_crypto import encrypt_video,decrypt_video
def sha(data):return hashlib.sha256(data).hexdigest()
def seal():
 pins=runpy.run_path(str(P/'engine/trusted_payload.py'))['PAYLOADS']
 compatibility=json.loads((P/'scripts/video_compatibility.json').read_text(encoding='utf8'))
 updates=[]
 for lang,files in pins.items():
  root=P/'payload'/lang
  for name,h in files.items():
   if sha((root/name).read_bytes())!=h:raise ValueError('Payload hash mismatch: '+lang+'/'+name)
  patch=json.loads(gzip.decompress((root/'patch.json.gz').read_bytes()))
  required=compatibility['109']['languages'][lang]['sha256']
  targets=[e for f in patch['files'] for e in f.get('videos',[]) if e.get('media_index')==109]
  if len(targets)!=1 or targets[0]['sha256']!=required:
   raise ValueError('109 '+lang+' 未使用已认可的兼容视频；请先更新并封存视频 payload，不能发布旧编码。')
  changed=False
  for item in patch['files']:
   for edit in item.get('videos',[]):
    old=edit['payload']
    if old not in files:raise ValueError('Video absent from pinned manifest: '+old)
    data=(root/old).read_bytes()
    if edit.get('encoding')=='aes-256-gcm-v1':decrypt_video(data,edit['sha256']);continue
    if edit.get('encoding'):raise ValueError('Unknown video encoding')
    if sha(data)!=edit['sha256']:raise ValueError('Video mismatch')
    encrypted=encrypt_video(data,edit['sha256'])
    if decrypt_video(encrypted,edit['sha256'])!=data:raise ValueError('Video roundtrip mismatch')
    name=str(Path(old).with_suffix('.zvenc')).replace('\\','/')
    (root/name).write_bytes(encrypted);files.pop(old,None);files[name]=sha(encrypted)
    edit.update(payload=name,encoding='aes-256-gcm-v1');changed=True
  if changed:
   encoded=gzip.compress(json.dumps(patch,ensure_ascii=False,separators=(',',':')).encode(),mtime=0)
   updates.append((root/'patch.json.gz',encoded));files['patch.json.gz']=sha(encoded)
 # All language inputs and decrypted content validated before publishing manifests.
 for path,data in updates:path.write_bytes(data)
 (P/'engine/trusted_payload.py').write_text('"""Pinned authenticated release payloads."""\nPAYLOADS = '+repr(pins)+'\n',encoding='utf8')
 return pins
def copy_release(destination):
 pins=seal();destination=Path(destination)
 for lang,files in pins.items():
  for name,expected in files.items():
   if name.lower().endswith(('.webm','.mp4','.mkv')):raise ValueError('Unencrypted video in release manifest')
   root=(P/'payload'/lang).resolve();src=(root/name).resolve()
   if not src.is_relative_to(root):raise ValueError('Invalid payload path')
   dst=destination/lang/name;dst.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(src,dst)
   if sha(dst.read_bytes())!=expected:raise ValueError('Release copy mismatch')
 if any(p.suffix.lower() in ('.webm','.mp4','.mkv') for p in destination.rglob('*')):raise ValueError('Loose video found in release')
if __name__=='__main__':
 ap=argparse.ArgumentParser();ap.add_argument('--output',type=Path);a=ap.parse_args()
 if a.output:copy_release(a.output)
 else:seal()
 print('Encrypted video payloads verified; no loose videos selected for release.')
