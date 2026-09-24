"""Guard alias lookup keys and preservation of existing catalog records."""
import struct
import sys
import unittest
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'engine'))
from catalog import Catalog
from speaker_alias import add_catalog_aliases

class SpeakerCatalogTests(unittest.TestCase):
    def fixture(self):
        raw=bytearray(struct.pack('<3I',0x0DE38942,2,0))
        def append(data):
            off=len(raw);raw.extend(data);return off
        def text(s):
            data=s.encode();append(struct.pack('<I',len(data)));return append(data)
        # Segmented keys are reconstructed using '/', not '.' or concatenation.
        first=append(struct.pack('<2I',text('unit.re'),0xffffffff))
        second=append(struct.pack('<2I',text('chc_00chri1f'),first))|0x40000000
        internal=text('Assets/Data/UnitData/re/chc_00chri1f.asset')
        loc=append(struct.pack('<7I',second,internal,text('AssetProvider'),0xffffffff,0,0xffffffff,123))
        key_data=append(struct.pack('<2I',second,ord('/')))
        key=append(struct.pack('<2I',999,key_data))
        append(struct.pack('<I',4));arr=append(struct.pack('<I',loc))
        append(struct.pack('<I',8));keys=append(struct.pack('<2I',key,arr))
        struct.pack_into('<I',raw,8,keys)
        return raw,loc
    def test_alias_preserves_original_location_and_dependency(self):
        original,loc=self.fixture()
        alias=dict(source_key='unit.re/chc_00chri1f',key='unit.re/chc_00chri9f',internal='Assets/Data/UnitData/re/chc_00chri9f.asset')
        result=add_catalog_aliases(original,[alias]);c=Catalog(result)
        self.assertEqual(result[:8],original[:8])
        self.assertEqual(result[12:len(original)],original[12:])
        entries=c.array(c.values(8)[0],2);self.assertEqual(len(entries),2)
        obj,arr=entries[1];_,data=c.values(obj,2);key,sep=c.values(data,2)
        self.assertEqual(c.string(key,chr(sep)),alias['key'])
        newloc=c.array(arr)[0][0];values=c.values(newloc,7)
        self.assertEqual(c.string(values[1],'/'),alias['internal'])
        self.assertEqual(values[2:],c.values(loc,7)[2:])
        with self.assertRaises(ValueError):add_catalog_aliases(result,[alias])
    def test_missing_source_fails_without_modifying_input(self):
        raw,_=self.fixture();before=bytes(raw)
        with self.assertRaises(KeyError):add_catalog_aliases(raw,[dict(source_key='missing',key='new',internal='new')])
        self.assertEqual(bytes(raw),before)

if __name__=='__main__':unittest.main()
