"""Exercise three language switches and byte-exact restore on a fixture."""
import argparse,json,shutil,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'engine'))
from main import Patcher

def make(game,state,language):
 p=Patcher(game,state_home=state,language=language)
 p.stopped=lambda:None
 return p

def run(fixture,work):
 if work.exists(): raise ValueError('Use a new, empty test output directory')
 game=work/'game'; shutil.copytree(fixture,game); state=work/'state'
 hans=make(game,state,'zh-Hans'); baseline=hans.actual()
 assert hans.status()['status']=='original'
 assert hans.install()['status']=='installed'
 assert hans.state()['language']=='zh-Hans'
 hans_hashes=hans.actual()
 hant=make(game,state,'zh-Hant')
 assert hant.status()['status']=='update_available'
 assert hant.install()['status']=='installed'
 assert hant.state()['language']=='zh-Hant'
 hant_hashes=hant.actual(); assert hant_hashes != hans_hashes
 english=make(game,state,'en')
 assert english.status()['status']=='update_available'
 assert english.install()['status']=='installed'
 assert english.state()['language']=='en'
 english_hashes=english.actual();assert english_hashes!=hans_hashes and english_hashes!=hant_hashes
 hans=make(game,state,'zh-Hans')
 assert hans.status()['status']=='update_available'
 assert hans.install()['status']=='installed' and hans.actual()==hans_hashes
 assert not hasattr(hans,'restore') and not hasattr(hans,'force_restore')
 for row in hans.files:shutil.copy2(hans.original/row['path'],game/row['path'])
 assert hans.status()['status']=='original' and hans.actual()==baseline
 report={'passed':True,'checks':['original recognized','Simplified install','Traditional switch from Simplified','English switch from Traditional','Simplified switch from English','Steam restore fixture recognized byte-exactly','manual local restore commands absent'], 'files':len(baseline)}
 (work/'report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
 print(json.dumps(report,ensure_ascii=False,indent=2))

if __name__=='__main__':
 ap=argparse.ArgumentParser(); ap.add_argument('--fixture',type=Path,required=True); ap.add_argument('--work',type=Path,required=True); a=ap.parse_args(); run(a.fixture,a.work)
