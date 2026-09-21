"""The v0.2.0 rollback name exceeded the default Windows MAX_PATH limit."""
import gzip
import json
import sys
from pathlib import Path, PureWindowsPath

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'engine'))
from main import workdir

patch = json.loads(gzip.decompress((ROOT / 'payload/zh-Hans/patch.json.gz').read_bytes()))
longest = max((row['path'] for row in patch['files']), key=len)
home = PureWindowsPath('C:/Users/Administrator/AppData/Local/ZephyrChinesePatcher') / ('a' * 24)
old_token = 'b' * 32
token = 'b' * 16
old_rollback = home / 'transactions' / old_token / 'rollback' / longest
paths = {
    'rollback': workdir(home, 'rollback', token) / longest,
    'staging': workdir(home, 'staging', token) / longest,
    'initial_backup': workdir(home, 'backup', token) / longest,
    'verification_original': workdir(home, 'verification', token) / 'o' / longest,
    'verification_rebuilt': workdir(home, 'verification', token) / 'r' / longest,
}
assert len(str(old_rollback)) >= 260
assert all(len(str(path)) < 260 for path in paths.values()), paths
print(json.dumps({'old_rollback_length': len(str(old_rollback)),
                  'new_path_lengths': {key: len(str(path)) for key, path in paths.items()},
                  'max_path_limit': 260}, indent=2))
