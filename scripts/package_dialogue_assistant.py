"""Private, complete review package. Never publish or overwrite earlier archives."""
import argparse, datetime, hashlib, json, zipfile
from pathlib import Path

p=argparse.ArgumentParser()
p.add_argument('--binary',type=Path,required=True)
p.add_argument('--evidence',type=Path,required=True)
p.add_argument('--probe',type=Path,required=True)
p.add_argument('--output',type=Path,required=True)
p.add_argument('--installer',type=Path)
a=p.parse_args();root=Path(__file__).resolve().parents[1]
files={}
def add(path,name):files[name]=path.read_bytes()
add(a.binary,'ZephyrDialogueAssistant.exe')
add(root/'docs/DIALOGUE_ASSISTANT.md','先读我.md')
for name in ['LICENSE','THIRD_PARTY_NOTICES.md','README.md','DEVELOPMENT.md','dependency-versions.json','requirements.txt','release-public.pem','.gitignore','CHANGELOG.md','DATA_PROVENANCE.md']:
    add(root/name,'source/'+name)
for folder in ['app','tests','scripts','docs','.github','licenses','engine']:
    for path in (root/folder).rglob('*'):
        if not path.is_file() or any(x in {'bin','obj','__pycache__'} for x in path.relative_to(root/folder).parts):continue
        add(path,'source/'+path.relative_to(root).as_posix())
if a.installer:
    for path in a.installer.rglob('*'):
        if path.is_file():add(path,'Zephyr-Chinese-Patcher/'+path.relative_to(a.installer).as_posix())
for path in a.evidence.iterdir():
    if path.is_file() and path.suffix.lower() in {'.json','.jsonl','.md','.txt','.png','.log'}:add(path,'evidence/'+path.name)
for name in ['read_snapshot.py','read_snapshot.ps1','watch_dialogue.py','inspect_state.py','verify_capture.py',
             'READ_FIRST.md','LIVE_VALIDATION.md','RESTART_VALIDATION.md','BALLOON_VALIDATION.md']:
    add(a.probe/name,'probe/'+name)
for folder in ['static','captures']:
    for path in (a.probe/folder).iterdir():
        if path.is_file() and path.suffix in {'.txt','.json'}:add(path,'probe/'+folder+'/'+path.name)
files['SHA256SUMS.txt']=''.join(hashlib.sha256(data).hexdigest()+'  '+name+'\n' for name,data in files.items()).encode('utf8')
a.output.mkdir(parents=True,exist_ok=True)
archive=a.output/('Zephyr-Dialogue-Assistant-preview-'+datetime.datetime.now().strftime('%Y%m%d-%H%M%S')+'.zip')
with zipfile.ZipFile(archive,'x',zipfile.ZIP_DEFLATED) as z:
    for name,data in files.items():z.writestr(name,data)
with zipfile.ZipFile(archive) as z:
    assert z.testzip() is None
    assert set(z.namelist())==set(files)
    for name,data in files.items():assert hashlib.sha256(z.read(name)).digest()==hashlib.sha256(data).digest()
report={'archive':str(archive),'files':len(files),'sha256':hashlib.sha256(archive.read_bytes()).hexdigest(),'all_members_verified':True}
archive.with_suffix('.verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
print(json.dumps(report,ensure_ascii=False))
