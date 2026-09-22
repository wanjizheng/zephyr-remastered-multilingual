"""Derive review text from the exact installer payload operations, never translate anew."""
import argparse, gzip, hashlib, json, runpy
from pathlib import Path
root=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--payload',type=Path,default=root/'payload');args=p.parse_args()
index=json.loads((root/'app/Dialogue/ResourceIndex.json').read_text('utf8'))
coordinates={(r['Bundle'],r['SerializedFile'],r['ObjectPathId'],r['FieldPath']):key for key,r in index.items()}
catalogs={}
trusted=runpy.run_path(str(root/'engine/trusted_payload.py'))['PAYLOADS']
for language in ['zh-Hans','zh-Hant','en']:
    path=args.payload/language/'patch.json.gz';data=path.read_bytes();patch=json.loads(gzip.decompress(data))
    assert hashlib.sha256(data).hexdigest()==trusted[language]['patch.json.gz'], 'Untrusted payload: '+language
    assert patch['language']==language
    rows={}
    for f in patch['files']:
        for obj in f.get('changes',[]):
            for op in obj.get('ops',[]):
                parts=op.get('path',[])
                if op['kind']!='set' or len(parts)!=3 or parts[0]!='TextList' or parts[2]!='Text':continue
                key=coordinates.get((Path(f['path']).name,obj['sf'],str(obj['pid']),'/'+('/'.join(map(str,parts)))))
                if key:
                    assert key not in rows
                    rows[key]=op['value']
    catalogs[language]={'Version':patch['version'],'PayloadHash':hashlib.sha256(data).hexdigest(),'Lines':rows}
    print(language,len(rows),'lines')
(root/'app/Dialogue/Catalogs.json').write_text(json.dumps(catalogs,ensure_ascii=False,separators=(',',':')),encoding='utf8')
