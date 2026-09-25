"""Prepare the three language payloads for native New Game prompts, offline only."""
import gzip,hashlib,json,runpy,sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'engine'))
from resources import rebuild,replace_metadata_literals,sha

REL='ZephyrRemastered_Data/il2cpp_data/Metadata/global-metadata.dat'
ORIGINAL=ROOT/'private/review-r2-original'
SOURCE={
 21167:'아니오',21171:'예',
 21181:'이전 클리어 정보({0}회 클리어)를 이어받아/n새 게임을 시작하겠습니까?',
 21182:'이전 클리어 정보를 이어받지 않고/n새 게임을 시작합니다.',
 21184:'저장된 ELD, 아이템, 장비 정보를 이어받아/n새 게임을 시작합니다.',
 21196:'처음부터 게임을 시작합니다.',
 21199:'최근 저장된 지점부터 이어서 진행합니다.',
}
TEXT={
 'zh-Hans':{
  21167:'否',21171:'是',
  21181:'是否继承此前的通关记录（已通关{0}次），/n开始新游戏？',
  21182:'不继承此前的通关记录，/n从头开始新游戏。',
  21184:'继承已保存的ELD、物品和装备，/n开始新游戏。',
  21196:'从头开始新游戏。',21199:'从最近的存档继续游戏。',
 },
 'zh-Hant':{
  21167:'否',21171:'是',
  21181:'是否繼承此前的通關記錄（已通關{0}次），/n開始新遊戲？',
  21182:'不繼承此前的通關記錄，/n從頭開始新遊戲。',
  21184:'繼承已儲存的ELD、物品和裝備，/n開始新遊戲。',
  21196:'從頭開始新遊戲。',21199:'從最近的存檔繼續遊戲。',
 },
 'en':{
  21167:'No',21171:'Yes',
  21181:'Carry over your clear data ({0} completions)/nand start a new game?',
  21182:'Start a new game/nwithout carrying over clear data.',
  21184:'Start a new game with saved ELD,/nitems, and equipment.',
  21196:'Start a new game from the beginning.',
  21199:'Continue from the most recent save.',
 },
}
def digest(data):return hashlib.sha256(data).hexdigest()
def main():
 original=(ORIGINAL/REL).read_bytes()
 table,table_bytes,count=__import__('struct').unpack_from('<iii',original,8)
 data,data_bytes,_=__import__('struct').unpack_from('<iii',original,20)
 offsets=__import__('struct').unpack_from('<'+str(count)+'I',original,table)
 for index,value in SOURCE.items():
  actual=original[data+offsets[index]:data+offsets[index+1]].decode('utf8')
  if actual!=value:raise ValueError(f'Original literal {index} changed: {actual!r}')
 trust=runpy.run_path(str(ROOT/'engine/trusted_payload.py'))['PAYLOADS']
 report={}
 prepared=[]
 for lang,translations in TEXT.items():
  path=ROOT/'payload'/lang/'patch.json.gz'
  compressed=path.read_bytes()
  if digest(compressed)!=trust[lang]['patch.json.gz']:raise ValueError('Payload trust mismatch: '+lang)
  patch=json.loads(gzip.decompress(compressed))
  item=next(x for x in patch['files'] if x['path']==REL)
  edits=[{'index':index,'expected_sha256':digest(SOURCE[index].encode('utf8')),'text':value}
         for index,value in sorted(translations.items())]
  item['literal_edits']=edits
  raw=bytearray(original)
  for edit in item['edits']:
   start=edit['offset'];end=start+edit['old_length']
   replacement=edit['text'].encode('utf8')
   if digest(raw[start:end])!=edit['expected_sha256'] or len(replacement)!=end-start:
    raise ValueError('Existing metadata edit changed: '+lang)
   raw[start:end]=replacement
  output=replace_metadata_literals(raw,edits)
  item['after_sha256']=digest(output)
  if item['before_sha256']!=digest(original):raise ValueError('Original metadata hash changed')
  encoded=gzip.compress(json.dumps(patch,ensure_ascii=False,separators=(',',':')).encode('utf8'),mtime=0)
  prepared.append((lang,path,encoded,item['after_sha256'],edits))
 for lang,path,encoded,expected,edits in prepared:
  path.write_bytes(encoded)
  trust[lang]['patch.json.gz']=digest(encoded)
  report[lang]={'payload_sha256':digest(encoded),'metadata_sha256':expected,'literals':edits}
 (ROOT/'engine/trusted_payload.py').write_text('"""Release payload digests, verified by isolated rebuild."""\nPAYLOADS = '+repr(trust)+'\n',encoding='utf8')
 print(json.dumps(report,ensure_ascii=False,indent=2))
if __name__=='__main__':main()
