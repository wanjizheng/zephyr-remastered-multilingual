"""Version-locked, journaled local patcher. No uploads and no game launching."""
import argparse,contextlib,gzip,hashlib,json,os,re,shutil,subprocess,sys,uuid
from pathlib import Path
from resources import rebuild,safe,sha
from trusted_payload import PAYLOADS
VERSION='0.4.1'
WORK_DIRS={'backup':'b','staging':'s','rollback':'t','verification':'v'}
def workdir(home,kind,token=None):
 if token is not None:return home/WORK_DIRS[kind]/token
 for _ in range(10):
  candidate=home/WORK_DIRS[kind]/uuid.uuid4().hex[:16]
  if not candidate.exists():return candidate
 raise ValueError('无法建立唯一的工作目录，请稍后重试。')
def read(p):return json.loads(p.read_text(encoding='utf8'))
def write(p,value):
 p.parent.mkdir(parents=True,exist_ok=True);tmp=p.with_suffix(p.suffix+'.new');tmp.write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8');os.replace(tmp,p)
def payload_default():
 base=Path(sys.executable).parent if getattr(sys,'frozen',False) else Path(__file__).resolve().parents[1]
 for candidate in (base/'payload',base.parent/'payload'):
  if candidate.is_dir():return candidate
 return base/'payload'
class Patcher:
 def __init__(self,game,payload=None,state_home=None,language='zh-Hans'):
  self.game=Path(game).resolve();self.language=language
  if language not in PAYLOADS:raise ValueError('不支持的汉化语言。')
  root=Path(payload or payload_default()).resolve();self.payload=(root/language if (root/language).is_dir() else root).resolve()
  for n,h in PAYLOADS[language].items():
   if sha(self.payload/n)!=h:raise ValueError('汉化数据损坏，请重新下载官方发布包。')
  self.patch=json.loads(gzip.decompress((self.payload/'patch.json.gz').read_bytes()));self.files=self.patch['files'];self.names={r['path'] for r in self.files}
  for name in self.names:
   if not name.startswith('ZephyrRemastered_Data/') or any(x in Path(name).parts for x in ['..']):raise ValueError('非法资源路径')
   safe(self.game,name)
  key=hashlib.sha256(str(self.game).casefold().encode()).hexdigest()[:24]
  home=Path(state_home or Path(os.environ['LOCALAPPDATA'])/'ZephyrChinesePatcher');self.home=home.resolve()/key;self.original=self.home/'original';self.statefile=self.home/'state.json';self.journalfile=self.home/'transaction.json'
 def guard_game(self):
  exe=self.game/'ZephyrRemastered.exe'
  if not exe.is_file() or sha(exe)!=self.patch['exe_sha256']:raise ValueError('请选择受支持的《西风狂诗曲重制版》游戏目录。')
  for r in self.patch['dependencies']:
   if not safe(self.game,r['path']).is_file() or sha(safe(self.game,r['path']))!=r['sha256']:raise ValueError('游戏依赖文件版本不匹配，请先通过 Steam 验证完整性。')
 def stopped(self):
  # A process in another installation is also a conservative stop condition.
  result=subprocess.run(['tasklist','/FI','IMAGENAME eq ZephyrRemastered.exe','/FO','CSV','/NH'],capture_output=True,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0),check=True)
  if b'zephyrremastered.exe' in result.stdout.lower():raise ValueError('请先保存并正常退出游戏，再操作汉化工具。')
 def state(self):return read(self.statefile) if self.statefile.exists() else None
 def actual(self):return {r['path']:sha(safe(self.game,r['path'])) if safe(self.game,r['path']).is_file() else None for r in self.files}
 def installed_build(self):
  manifest=self.game.parent.parent/'appmanifest_5099430.acf'
  if self.game.parent.name.casefold()!='common' or not manifest.is_file():return None
  text=manifest.read_text(encoding='utf8')
  def value(key):
   match=re.search(r'"'+key+r'"\s+"([^"]+)"',text)
   return match.group(1) if match else None
  if value('appid')!='5099430' or value('installdir')!=self.game.name:return None
  return value('buildid')
 def backup_rows(self):
  # Only hash-verified original files from this install path are eligible.
  return [r for r in self.files if safe(self.original,r['path']).is_file() and sha(safe(self.original,r['path']))==r['before_sha256']]
 def status(self):
  if not (self.game/'ZephyrRemastered.exe').is_file():raise ValueError('请选择包含 ZephyrRemastered.exe 的游戏目录。')
  compatible=True;reason=None
  try:self.guard_game()
  except ValueError as ex:compatible=False;reason=str(ex)
  actual=self.actual();state=self.state();before={r['path']:r['before_sha256'] for r in self.files};after={r['path']:r['after_sha256'] for r in self.files}
  managed=(state or {}).get('installed_files',{})
  matches_managed=bool(managed) and all(actual.get(k)==v for k,v in managed.items()) and all(actual[k]==before[k] for k in actual.keys()-managed.keys())
  if self.journalfile.exists():kind='recovery_required'
  elif not compatible:kind='unsupported_or_modified'
  elif actual==before:kind='original'
  elif matches_managed:
   kind='installed' if state.get('version')==VERSION and actual==after else 'update_available'
  elif actual==after:kind='unmanaged_localization'
  else:kind='unsupported_or_modified'
  backups=self.backup_rows()
  return dict(status=kind,version=VERSION,language=self.language,installed_language=(state or {}).get('language'),installed_version=(state or {}).get('version'),game_build=self.patch['game_build'],installed_build=self.installed_build(),compatibility_reason=reason,can_install=kind in ['original','update_available','installed'],backup_files=len(backups),backup_complete=len(backups)==len(self.files),backup_available=bool(backups))
 @contextlib.contextmanager
 def locked(self):
  import msvcrt
  self.home.mkdir(parents=True,exist_ok=True)
  with (self.home/'operation.lock').open('a+b') as f:
   f.seek(0);f.write(b'0');f.flush();f.seek(0)
   try:msvcrt.locking(f.fileno(),msvcrt.LK_NBLCK,1)
   except OSError:raise ValueError('另一个汉化操作正在进行，请稍后重试。')
   try:yield
   finally:f.seek(0);msvcrt.locking(f.fileno(),msvcrt.LK_UNLCK,1)
 def copy_checked(self,source,target,expected):
  if sha(source)!=expected:raise ValueError('待写入文件校验失败')
  target.parent.mkdir(parents=True,exist_ok=True);temp=target.with_name(target.name+'.zephyr-patcher.tmp')
  # Only the current transaction owns this narrowly named temporary path.
  if temp.exists():raise ValueError('发现未完成的临时文件，请先恢复上次操作')
  shutil.copy2(source,temp)
  if sha(temp)!=expected:raise ValueError('复制校验失败')
  os.replace(temp,target)
  if sha(target)!=expected:raise ValueError('写入后的文件校验失败')
 def verify_original(self):
  for r in self.files:
   if sha(safe(self.original,r['path']))!=r['before_sha256']:raise ValueError('原版备份缺失或校验失败，已停止操作。')
  for r in self.patch['dependencies']:
   if sha(safe(self.original,r['path']))!=r['sha256']:raise ValueError('原版依赖备份不完整。')
 def _recover(self):
  self.stopped()
  if not (self.game/'ZephyrRemastered.exe').is_file():raise ValueError('游戏目录不存在')
  if not self.journalfile.exists():return
  tx=read(self.journalfile);rollback=safe(self.home,tx['rollback']);entries=tx['entries']
  if not {r['path'] for r in entries}.issubset(self.names) or not entries:raise ValueError('恢复记录不匹配')
  for r in entries:
   p=safe(self.game,r['path'])
   if (sha(p) if p.is_file() else None) not in [r['before'],r['after']]:raise ValueError('操作中断后游戏文件又发生变化；为保护新版文件，未自动回滚。请联系维护者。')
   if r['before'] is not None and sha(safe(rollback,r['path']))!=r['before']:raise ValueError('事务备份损坏')
  for r in entries:
   self.stopped();p=safe(self.game,r['path']);tmp=p.with_name(p.name+'.zephyr-patcher.tmp')
   if tmp.exists():tmp.unlink()
   if r['before'] is None:p.unlink(missing_ok=True)
   elif not p.is_file() or sha(p)!=r['before']:self.copy_checked(safe(rollback,r['path']),p,r['before'])
  if tx['previous_state'] is None:
   if self.statefile.exists():self.statefile.unlink()
  else:write(self.statefile,tx['previous_state'])
  self.journalfile.unlink()
 def recover(self):
  with self.locked():self._recover()
  return self.status()
 def transaction(self,source,desired,next_state,fail_after=None,selected=None):
  self.stopped();before=self.actual();rollback=workdir(self.home,'rollback')
  entries=[dict(path=r['path'],before=before[r['path']],after=desired[r['path']]) for r in (selected if selected is not None else self.files)]
  for r in entries:
   if r['before'] is not None:
    p=safe(rollback,r['path']);p.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(safe(self.game,r['path']),p)
    if sha(p)!=r['before']:raise ValueError('安装前备份失败')
  write(self.journalfile,dict(rollback=str(rollback.relative_to(self.home)),entries=entries,previous_state=self.state()))
  try:
   for i,r in enumerate(entries):
    self.stopped();p=safe(self.game,r['path'])
    if (sha(p) if p.is_file() else None)!=r['before']:raise ValueError('安装期间游戏文件发生变化')
    self.copy_checked(safe(source,r['path']),p,r['after'])
    if fail_after is not None and i+1==fail_after:raise RuntimeError('Injected interruption for recovery testing')
   if self.actual()!=desired:raise ValueError('安装后总体验证失败')
   write(self.statefile,next_state);self.journalfile.unlink()
  except Exception:
   self._recover();raise
 def install(self):
  with self.locked():
   self.stopped();self.guard_game()
   if self.journalfile.exists():raise ValueError('请先点击恢复上次操作')
   state=self.status()
   if state['status']=='installed':return state
   if state['status'] not in ['original','update_available']:raise ValueError('当前文件不是受支持的原版或本工具管理的汉化版，请先用 Steam 验证文件。')
   if not self.original.exists():
    if state['status']!='original':raise ValueError('缺少原版备份，无法更新')
    temporary=workdir(self.home,'backup')
    for r in self.files+self.patch['dependencies']:
     src=safe(self.game,r['path']);dst=safe(temporary,r['path']);dst.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(src,dst)
    temporary.rename(self.original)
   # A newer payload may add a previously untouched resource to an old backup.
   for r in self.files:
    dst=safe(self.original,r['path'])
    if not dst.exists():
     src=safe(self.game,r['path'])
     if not src.is_file() or sha(src)!=r['before_sha256']:raise ValueError('新增资源缺少原版备份，请先通过 Steam 验证完整性。')
     self.copy_checked(src,dst,r['before_sha256'])
   self.verify_original();stage=workdir(self.home,'staging');rebuild(self.original,stage,self.patch,self.payload)
   desired={r['path']:r['after_sha256'] for r in self.files};next_state=dict(version=VERSION,language=self.language,game_build=self.patch['game_build'],installed_files=desired)
   self.transaction(stage,desired,next_state)
  return self.status()
 def verify(self):
  """Read-only game validation and full reconstruction, useful for release QA."""
  with self.locked():
   self.guard_game()
   if self.status()['status']!='original':raise ValueError('离线验证需要受支持的原版文件')
   before=self.actual();folder=workdir(self.home,'verification')
   original=folder/'o'
   for r in self.files+self.patch['dependencies']:
    dst=safe(original,r['path']);dst.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(safe(self.game,r['path']),dst)
   rebuild(original,folder/'r',self.patch,self.payload)
   if self.actual()!=before:raise ValueError('验证期间源文件发生变化')
  return dict(status='verified',files=len(self.files),game_files_changed=False)
def main():
 for stream in (sys.stdout,sys.stderr):
  if hasattr(stream,'reconfigure'):stream.reconfigure(encoding='utf8')
 ap=argparse.ArgumentParser();ap.add_argument('action',choices=['status','install','recover','verify']);ap.add_argument('--game',required=True);ap.add_argument('--payload');ap.add_argument('--language',choices=PAYLOADS);args=ap.parse_args()
 try:
  p=Patcher(args.game,args.payload,language=args.language or 'zh-Hans');result=getattr(p,args.action)();print(json.dumps(dict(ok=True,**result),ensure_ascii=False))
 except Exception as ex:print(json.dumps(dict(ok=False,error=str(ex)),ensure_ascii=False));sys.exit(1)
if __name__=='__main__':main()
