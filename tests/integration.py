"""Run only with an isolated original-game fixture. Never point this at a live game."""
import argparse,json,shutil,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'engine'))
from main import Patcher,write
from resources import safe,sha

def run(fixture,work):
 if work.exists():raise ValueError('Use a new, empty test output directory')
 game=work/'game';shutil.copytree(fixture,game)
 p=Patcher(game,state_home=work/'state');baseline=p.actual();checks=[]
 # The fixture contains no running process; leave the player's separate game alone.
 p.stopped=lambda:None
 assert p.status()['status']=='original';checks.append('original recognized')
 save=game/'test-save.bin';save.write_bytes(b'untouched save sentinel')
 assert p.install()['status']=='installed';installed=p.actual();checks.append('all rebuilt files match release hashes')
 old_state=p.state();old_state['version']='0.0.9';write(p.statefile,old_state)
 assert p.status()['status']=='update_available'
 assert p.install()['status']=='installed' and p.actual()==installed
 p.verify_original();checks.append('managed upgrade reuses intact original backup')
 assert not hasattr(p,'restore') and not hasattr(p,'force_restore')
 for row in p.files:shutil.copy2(safe(p.original,row['path']),safe(game,row['path']))
 assert p.status()['status']=='original' and p.actual()==baseline;checks.append('Steam restore fixture recognized; manual local restore actions absent')
 stage=next((p.home/'s').iterdir())
 next_state=dict(version=p.patch['version'],installed_files=installed)
 try:p.transaction(stage,installed,next_state,fail_after=3)
 except RuntimeError:pass
 else:raise AssertionError('failure injection did not fail')
 assert p.actual()==baseline and not p.journalfile.exists();checks.append('mid-install exception rolls back all files')
 # Emulate an abrupt process exit, bypassing the exception handler.
 rollback=p.home/'transactions'/'crash-fixture'/'rollback'
 entries=[]
 for r in p.files:
  dest=safe(rollback,r['path']);dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(safe(game,r['path']),dest)
  entries.append(dict(path=r['path'],before=baseline[r['path']],after=installed[r['path']]))
 write(p.journalfile,dict(rollback=str(rollback.relative_to(p.home)),entries=entries,previous_state=p.state()))
 first=p.files[0]['path'];shutil.copy2(safe(stage,first),safe(game,first))
 assert p.status()['status']=='recovery_required'
 p=Patcher(game,state_home=work/'state');p.stopped=lambda:None
 assert p.recover()['status']=='original' and p.actual()==baseline;checks.append('fresh process journal recovery restores baseline')
 target=safe(game,first);old=target.read_bytes();target.write_bytes(old+b'changed')
 assert p.status()['status']=='unsupported_or_modified'
 try:p.install()
 except ValueError:pass
 else:raise AssertionError('modified game accepted')
 assert target.read_bytes()==old+b'changed';target.write_bytes(old);checks.append('modified game rejected without overwrite')
 try:safe(game,'../escape')
 except ValueError:pass
 else:raise AssertionError('path escape accepted')
 checks.append('path traversal rejected')
 bad=work/'bad-payload';bad.mkdir();(bad/'patch.json.gz').write_bytes(b'tampered')
 try:Patcher(game,payload=bad,state_home=work/'state')
 except ValueError:pass
 else:raise AssertionError('tampered payload accepted')
 checks.append('tampered payload rejected')
 # An updated game must not be overwritten from an old local backup.
 target=safe(game,first);original_bytes=target.read_bytes();target.write_bytes(original_bytes+b'edited')
 exe=game/'ZephyrRemastered.exe';exe_bytes=exe.read_bytes();exe.write_bytes(exe_bytes+b'new Steam version')
 assert p.status()['status']=='unsupported_or_modified'
 try:p.install()
 except ValueError:pass
 else:raise AssertionError('updated game accepted for install')
 assert target.read_bytes()==original_bytes+b'edited' and exe.read_bytes()==exe_bytes+b'new Steam version'
 target.write_bytes(original_bytes);exe.write_bytes(exe_bytes)
 checks.append('updated game is not overwritten from local backup')
 # Failure recovery must restore the exact modified baseline, including absence.
 target.unlink();missing_baseline=p.actual()
 try:p.transaction(p.original,baseline,dict(version=None,installed_files={}),fail_after=3)
 except RuntimeError:pass
 else:raise AssertionError('transaction interruption fixture did not fail')
 assert p.actual()==missing_baseline and not target.exists()
 target.write_bytes(original_bytes);checks.append('interrupted recovery preserves missing-file pre-operation state')
 saved_game=p.game;fake=work/'steamapps/common/TestGame';fake.mkdir(parents=True)
 manifest=work/'steamapps/appmanifest_5099430.acf'
 manifest.write_text('"appid" "5099430" "installdir" "TestGame" "buildid" "999999"',encoding='utf8')
 p.game=fake;assert p.installed_build()=='999999'
 manifest.write_text('"appid" "5099430" "installdir" "OtherGame" "buildid" "999999"',encoding='utf8')
 assert p.installed_build() is None;p.game=saved_game
 checks.append('Steam build detection validates app id and selected installation')
 assert save.read_bytes()==b'untouched save sentinel';checks.append('save sentinel untouched')
 report=dict(passed=True,checks=checks,files=len(p.files),original_hashes=baseline,installed_hashes=installed)
 write(work/'report.json',report);print(json.dumps(dict(passed=True,checks=checks),indent=2))

if __name__=='__main__':
 ap=argparse.ArgumentParser();ap.add_argument('--fixture',type=Path,required=True);ap.add_argument('--work',type=Path,required=True);a=ap.parse_args();run(a.fixture,a.work)
