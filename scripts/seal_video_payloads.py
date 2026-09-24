"""Seal native CRCs and exact output hashes, then promote local payloads."""
import argparse,gzip,hashlib,json,shutil,struct,sys
from pathlib import Path
import spooky,UnityPy
from UnityPy.helpers.ResourceReader import get_resource_data
P=Path(__file__).resolve().parents[1];sys.path.insert(0,str(P/'engine'))
from resources import sha
from catalog import Catalog
ap=argparse.ArgumentParser();ap.add_argument('--work',type=Path,required=True);a=ap.parse_args();w=a.work
crcs={str(Path(path).resolve()):(digest,int(crc)) for path,digest,crc,method in [line.split('\t') for line in (w/'native_crc_sha_evidence.tsv').read_text(encoding='utf-8-sig').splitlines()]}
pins={};report={}
for lang in ['zh-Hans','zh-Hant','en']:
 payload=w/'payload'/lang;stage=w/'staged'/lang;patch=json.loads((payload/'patch.unsealed.json').read_text(encoding='utf8'))
 item=next(r for r in patch['files'] if r['kind']=='video_bundle');bundle=stage/item['path'];digest,crc=crcs[str(bundle.resolve())];assert sha(bundle)==digest==item['after_sha256'];item['native_crc']=crc
 # Fresh bundle readback: exact selected streams; all other serialized objects untouched.
 old=UnityPy.load(str(w/'original'/item['path']));new=UnityPy.load(str(bundle));oo={(o.assets_file.name,o.path_id):o for o in old.objects};nn={(o.assets_file.name,o.path_id):o for o in new.objects};assert oo.keys()==nn.keys()
 edits={(e['sf'],e['pid']):e for e in item['videos']}
 for key,obj in nn.items():
  if key not in edits:assert obj.get_raw_data()==oo[key].get_raw_data();continue
  e=edits[key];tree=obj.read_typetree();before=oo[key].read_typetree();ext=tree['m_ExternalResources']
  assert {k:v for k,v in tree.items() if k!='m_ExternalResources'}=={k:v for k,v in before.items() if k!='m_ExternalResources'}
  assert ext['m_Source']==before['m_ExternalResources']['m_Source']
  assert hashlib.sha256(get_resource_data(ext['m_Source'],obj.assets_file,ext['m_Offset'],ext['m_Size'])).hexdigest()==e['sha256']
 for name,reader in old.file.files.items():
  if name.endswith('.resource'):assert new.file.files[name].bytes[:len(reader.bytes)]==reader.bytes
 catrow=next(r for r in patch['files'] if r['kind']=='catalog');cat=stage/catrow['path'];raw=bytearray(cat.read_bytes());matches=[e for e in Catalog(raw).bundles() if Path(e['internal_id'].replace('\\','/')).name==bundle.name];assert len(matches)==1;e=matches[0]
 struct.pack_into('<I',raw,e['crc_offset'],crc);struct.pack_into('<I',raw,e['size_offset'],bundle.stat().st_size);cat.write_bytes(raw);catrow['after_sha256']=sha(cat)
 hashrow=next(r for r in patch['files'] if r['kind']=='catalog_hash');hp=stage/hashrow['path'];hp.write_text(spooky.hash128(bytes(raw)).to_bytes(16,'little').hex(),encoding='ascii');hashrow['after_sha256']=sha(hp)
 for r in patch['files']:assert sha(stage/r['path'])==r['after_sha256']
 (payload/'patch.json.gz').write_bytes(gzip.compress(json.dumps(patch,ensure_ascii=False,separators=(',',':')).encode(),mtime=0))
 names={'patch.json.gz','glyphs.zip'}
 if (payload/'auto-glyphs.zip').exists():names.add('auto-glyphs.zip')
 names.update(e['payload'] for f in patch['files'] for e in f.get('videos',[]))
 pins[lang]={name:sha(payload/name) for name in sorted(names)}
 report[lang]=dict(movie_sha256=digest,native_crc=crc,embedded_videos_exact=True,untouched_objects_exact=True,original_resource_prefix_exact=True,all_staged_files_sha256_match=True,english_060_original=lang=='en',runtime_playback_verified=False)
# Promote only after every language is sealed successfully; retain pre-change payloads.
for lang in pins:
 backup=w/'previous_payload'/lang;shutil.copytree(P/'payload'/lang,backup)
 for name in pins[lang]:
  dest=P/'payload'/lang/name;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(w/'payload'/lang/name,dest)
shutil.copy2(P/'engine/trusted_payload.py',w/'trusted_payload.previous.py')
(P/'engine/trusted_payload.py').write_text('"""Pinned payloads, including local video inputs, sealed after native CRC readback."""\nPAYLOADS = '+repr(pins)+'\n',encoding='utf8')
(w/'verification.json').write_text(json.dumps(report,indent=2),encoding='utf8');print(json.dumps(report,indent=2))
