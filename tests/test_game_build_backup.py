"""Regression for a Steam update with the previous build's managed backup."""
import hashlib,sys,tempfile,unittest
from pathlib import Path
from unittest.mock import patch
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'engine'))
from main import Patcher,write
from resources import sha,safe

class GameUpdateTests(unittest.TestCase):
 def fixture(self,root):
  p=Patcher.__new__(Patcher);p.game=root/'game';p.game.mkdir();p.home=root/'state';p.original=p.home/'original';p.original.mkdir(parents=True)
  p.statefile=p.home/'state.json';p.journalfile=p.home/'transaction.json';p.language='en';p.stopped=lambda:None
  name='ZephyrRemastered_Data/level0';target=safe(p.game,name);target.parent.mkdir();target.write_bytes(b'new original');old=safe(p.original,name);old.parent.mkdir();old.write_bytes(b'old original')
  (p.game/'ZephyrRemastered.exe').write_bytes(b'stable launcher');(p.game/'GameAssembly.dll').write_bytes(b'new assembly')
  p.files=[dict(path=name,before_sha256=sha(target),after_sha256=hashlib.sha256(b'new localization').hexdigest())];p.names={name}
  p.patch=dict(files=p.files,exe_sha256=sha(p.game/'ZephyrRemastered.exe'),dependencies=[dict(path='GameAssembly.dll',sha256=sha(p.game/'GameAssembly.dll'))],game_build='new');p.payload=root
  write(p.statefile,dict(version='old',language='en',installed_files={name:hashlib.sha256(b'old localization').hexdigest()}))
  return p
 def test_updated_original_rotates_previous_backup(self):
  with tempfile.TemporaryDirectory() as tmp:
   p=self.fixture(Path(tmp));self.assertEqual(p.status()['status'],'original')
   def rebuild(original,out,data,payload):
    p.verify_original();dest=safe(out,p.files[0]['path']);dest.parent.mkdir(parents=True);dest.write_bytes(b'new localization')
   with patch('main.rebuild',rebuild):self.assertEqual(p.install()['status'],'installed')
   self.assertEqual(safe(p.original,p.files[0]['path']).read_bytes(),b'new original')
   archived=list((p.home/'b').glob('*/ZephyrRemastered_Data/level0'));self.assertTrue(any(f.read_bytes()==b'old original' for f in archived))
 def test_native_update_rejected_even_with_unchanged_launcher(self):
  with tempfile.TemporaryDirectory() as tmp:
   p=self.fixture(Path(tmp));(p.game/'GameAssembly.dll').write_bytes(b'future assembly')
   self.assertEqual(p.status()['status'],'unsupported_or_modified')
   with self.assertRaises(ValueError):p.install()
   self.assertEqual(safe(p.original,p.files[0]['path']).read_bytes(),b'old original')
   self.assertEqual(safe(p.game,p.files[0]['path']).read_bytes(),b'new original')

if __name__=='__main__':unittest.main()
