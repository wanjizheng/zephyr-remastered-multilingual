"""Create licensed static font resources from authenticated local templates."""
import copy
import hashlib
from UnityPy.files.ObjectReader import ObjectReader
from patch_data import apply_ops, texture_pixels


def add_font_aliases(env, aliases, payload):
    for spec in aliases:
        sf = next(o.assets_file for o in env.objects if o.assets_file.name == spec['sf'])
        remap = {s['source_pid']: s['pid'] for s in spec['objects']}
        if len(remap) != 3 or len(set(remap.values())) != 3:
            raise ValueError('Invalid font resource identities')
        for edit in spec['objects']:
            source = sf.objects[edit['source_pid']]
            if edit['pid'] in sf.objects or hashlib.sha256(source.get_raw_data()).hexdigest() != edit['before_raw_sha256']:
                raise ValueError('Font template identity or version mismatch')
            if source.type.name not in ('MonoBehaviour', 'Material', 'Texture2D'):
                raise ValueError('Invalid font template type')
            tree = apply_ops(copy.deepcopy(source.read_typetree()), edit['ops'])
            if edit.get('texture'):
                tree['image data'] = texture_pixels(bytes(tree['m_Width'] * tree['m_Height']), tree['m_Width'], tree['m_Height'], edit['texture'], payload)
            clone = ObjectReader(sf, source.reader, edit['pid'], source.type_id,
                                 source.serialized_type, source.class_id, source.type,
                                 source.byte_start, source.byte_size, source.is_destroyed,
                                 source.is_stripped, data=source.get_raw_data())
            clone.save_typetree(tree)
            sf.objects[edit['pid']] = clone
        bundle = next(o for o in sf.objects.values() if o.type.name == 'AssetBundle')
        bt = bundle.read_typetree()
        entries = [e for e in bt['m_Container'] if e[0] == spec['source_internal'] and e[1]['asset']['m_PathID'] in remap]
        if len(entries) != 3 or any(e[0] == spec['internal'] for e in bt['m_Container']):
            raise ValueError('Invalid font container')
        ranges = {}
        for entry in entries:
            record = copy.deepcopy(entry[1])
            key = record['preloadIndex'], record['preloadSize']
            if key not in ranges:
                refs = copy.deepcopy(bt['m_PreloadTable'][key[0]:key[0]+key[1]])
                for ref in refs:
                    if ref['m_FileID'] == 0 and ref['m_PathID'] in remap:
                        ref['m_PathID'] = remap[ref['m_PathID']]
                ranges[key] = len(bt['m_PreloadTable'])
                bt['m_PreloadTable'].extend(refs)
            record['preloadIndex'] = ranges[key]
            record['asset']['m_PathID'] = remap[record['asset']['m_PathID']]
            bt['m_Container'].append((spec['internal'], record))
        bundle.save_typetree(bt)
