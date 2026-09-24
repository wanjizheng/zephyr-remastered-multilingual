"""Prepare local movie payloads and isolated candidates; no game writes."""
import argparse,copy,gzip,hashlib,json,shutil,sys
from pathlib import Path
import UnityPy
P=Path(__file__).resolve().parents[1];sys.path.insert(0,str(P/'engine'))
from resources import rebuild,sha
from video_resources import rebuild_videos
ap=argparse.ArgumentParser();ap.add_argument('--video-dir',type=Path,required=True);ap.add_argument('--catalog',type=Path,required=True);ap.add_argument('--original',type=Path,required=True);ap.add_argument('--movie-bundle',type=Path,required=True);ap.add_argument('--work',type=Path,required=True);a=ap.parse_args()
assert not a.work.exists();a.work.mkdir(parents=True)
catalog=json.loads(a.catalog.read_text(encoding='utf8'));rows={r['media_index']:r for r in catalog if r['media_index'] in [60,83,109]}
bundle=rows[60]['bundle'];relative='ZephyrRemastered_Data/StreamingAssets/aa/StandaloneWindows64/'+bundle
assert sha(a.movie_bundle)==rows[60]['source_sha256']
original=a.work/'original';original.mkdir()
# A local copy keeps all preparation independent of live game files and backups.
patches={lang:json.loads(gzip.decompress((P/'payload'/lang/'patch.json.gz').read_bytes())) for lang in ['zh-Hans','zh-Hant','en']}
for patch in patches.values():
 for item in patch['files']+patch['dependencies']:
  src=a.movie_bundle if item['path']==relative else a.original/item['path'];dst=original/item['path'];expected=item.get('before_sha256',item.get('sha256'))
  assert sha(src)==expected,(src,expected)
  if not dst.exists():dst.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(src,dst)
dst=original/relative;dst.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(a.movie_bundle,dst)
env=UnityPy.load(str(dst));objects={(o.assets_file.name,o.path_id):o for o in env.objects}
requests=[]
for lang,patch in patches.items():
 candidate=a.work/'staged'/lang;payload=a.work/'payload'/lang;payload.mkdir(parents=True)
 for name in ['patch.json.gz','glyphs.zip','auto-glyphs.zip']:
  src=P/'payload'/lang/name
  if src.exists():shutil.copy2(src,payload/name)
 if (P/'payload'/lang/'videos').exists():shutil.copytree(P/'payload'/lang/'videos',payload/'videos')
 # Reconstruct the existing accepted payload before extending it.
 rebuild(original,candidate,patch,payload)
 edits=[]
 for index in [60,83,109]:
  # 060 already has English lettering; rebuilding from pristine restores it on language switches.
  if index==60 and lang=='en':continue
  r=rows[index];obj=objects[(r['serialized_file'],int(r['path_id']))]
  name=f'{index:03d}.{lang}.webm';src=a.video_dir/name;dest=payload/'videos'/name;dest.parent.mkdir(exist_ok=True);shutil.copy2(src,dest)
  edits.append(dict(sf=r['serialized_file'],pid=int(r['path_id']),media_index=index,before_raw_sha256=hashlib.sha256(obj.get_raw_data()).hexdigest(),original_video_sha256=sha(a.video_dir/f'{index:03d}.webm'),payload='videos/'+name,sha256=sha(src)))
 item=dict(path=relative,kind='video_bundle',before_sha256=sha(a.movie_bundle),videos=edits)
 out=candidate/relative;rebuild_videos(original/relative,out,item,payload);item['after_sha256']=sha(out)
 patch['files']=[r for r in patch['files'] if r['path']!=relative]
 patch['files'].insert(next(i for i,r in enumerate(patch['files']) if r['kind']=='catalog'),item)
 patch['version']='0.4.7';patch['video_localization']={'indices':[60,83,109],'english_060':'original'}
 (payload/'patch.unsealed.json').write_text(json.dumps(patch,ensure_ascii=False,indent=2),encoding='utf8')
 requests.append(str(out.resolve()));print('Prepared',lang,item['after_sha256'],flush=True)
(a.work/'crc.request').write_text('\n'.join(requests)+'\n',encoding='utf8')
