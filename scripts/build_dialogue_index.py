"""Build locator-only index, omitting all dialogue/source text."""
import argparse, hashlib, json
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('registry',type=Path);a=p.parse_args()
root=Path(__file__).resolve().parents[1]
rows={}
for line in a.registry.open(encoding='utf-8'):
    row=json.loads(line)
    if not row['key'].startswith('@k') or not row['object_name'].startswith('art_'):continue
    if row['bundle'].startswith('fieldtext.re_'):prefix='text.re/art_'
    elif row['bundle'].startswith('fieldtext.ko_'):prefix='text.ko/art_'
    else:continue
    key=prefix+'|'+row['key']
    value={a:row[b] for a,b in [('Bundle','bundle'),('SerializedFile','serialized_file'),
       ('ObjectPathId','object_path_id'),('ObjectName','object_name'),('FieldPath','field_path'),
       ('RecordId','record_id'),('SourceHash','source_hash')]}
    if key in rows and rows[key]!=value:raise ValueError('Ambiguous locator: '+key)
    rows[key]=value
out=root/'app/Dialogue/ResourceIndex.json'
out.write_text(json.dumps(rows,ensure_ascii=False,separators=(',',':')),encoding='utf8')
(root/'docs/dialogue-index-provenance.json').write_text(json.dumps({
 'registry_sha256':hashlib.sha256(a.registry.read_bytes()).hexdigest(),
 'index_sha256':hashlib.sha256(out.read_bytes()).hexdigest(),'entries':len(rows),
 'contains_dialogue_text':False,'mapping_status':'registry coordinates, not per-install runtime asset proof'
},indent=2),encoding='utf8')
print('Locator entries:',len(rows))
