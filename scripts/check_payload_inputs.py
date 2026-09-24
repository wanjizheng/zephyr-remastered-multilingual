"""Build-time integrity gate for every pinned language payload."""
import sys,hashlib
from pathlib import Path
P=Path(__file__).resolve().parents[1];sys.path.insert(0,str(P/'engine'))
from trusted_payload import PAYLOADS
for lang,files in PAYLOADS.items():
 for name,expected in files.items():
  path=P/'payload'/lang/name
  if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest()!=expected:
   raise SystemExit('Missing or changed payload: '+lang+'/'+name)
print('All language payload inputs present and hash-verified.')
