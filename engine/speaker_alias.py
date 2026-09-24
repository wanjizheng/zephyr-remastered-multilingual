"""Build scene-scoped names without replacing the actor's visual data."""
import copy
import struct
from UnityPy.files.ObjectReader import ObjectReader
from catalog import Catalog

def add_unit_aliases(env, aliases):
    for spec in aliases:
        source = next(o for o in env.objects if o.assets_file.name == spec['sf'] and o.path_id == spec['source_pid'])
        original = source.read_typetree()
        if original['m_Name'] != spec['source_name'] or spec['pid'] in source.assets_file.objects:
            raise ValueError('Speaker alias source or object identity mismatch')
        tree = copy.deepcopy(original)
        tree['m_Name'] = spec['name']
        for key in ('dialogueName', 'korName', 'engName'):
            tree[key] = spec[key]
        clone = ObjectReader(source.assets_file, source.reader, spec['pid'], source.type_id,
            source.serialized_type, source.class_id, source.type, source.byte_start,
            source.byte_size, source.is_destroyed, source.is_stripped, data=source.get_raw_data())
        clone.save_typetree(tree)
        source.assets_file.objects[spec['pid']] = clone
        bundle = next(o for o in source.assets_file.objects.values() if o.type.name == 'AssetBundle')
        bt = bundle.read_typetree()
        entry = next(e for e in bt['m_Container'] if e[0] == spec['source_internal'])
        if any(e[0] == spec['internal'] for e in bt['m_Container']):
            raise ValueError('Duplicate speaker alias container')
        refs = copy.deepcopy(bt['m_PreloadTable'][entry[1]['preloadIndex']:entry[1]['preloadIndex']+entry[1]['preloadSize']])
        for ref in refs:
            if ref['m_FileID'] == 0 and ref['m_PathID'] == source.path_id:
                ref['m_PathID'] = spec['pid']
        alias = list(copy.deepcopy(entry))
        alias[0] = spec['internal']
        alias[1]['preloadIndex'] = len(bt['m_PreloadTable'])
        alias[1]['asset']['m_PathID'] = spec['pid']
        bt['m_PreloadTable'].extend(refs)
        bt['m_Container'].append(tuple(alias))
        bundle.save_typetree(bt)

def add_sprite_aliases(env, aliases):
    """Alias packed sprites while sharing the original texture and render data."""
    if not aliases:
        return
    bundle=next(o for o in env.objects if o.type.name=='AssetBundle')
    tree=bundle.read_typetree()
    for spec in aliases:
        entries=[entry for entry in tree['m_Container'] if entry[0]==spec['source_internal']]
        if not entries or any(e[0]==spec['internal'] for e in tree['m_Container']):
            raise ValueError('Invalid sprite alias container')
        remap={}
        for old_id,new_id in spec['sprite_ids']:
            source=bundle.assets_file.objects[old_id]
            if source.type.name!='Sprite' or new_id in bundle.assets_file.objects:
                raise ValueError('Invalid sprite alias object')
            st=source.read_typetree()
            if not st['m_Name'].startswith(spec['source_prefix']):
                raise ValueError('Unexpected sprite alias name')
            st['m_Name']=spec['prefix']+st['m_Name'][len(spec['source_prefix']):]
            clone=ObjectReader(source.assets_file,source.reader,new_id,source.type_id,
                source.serialized_type,source.class_id,source.type,source.byte_start,
                source.byte_size,source.is_destroyed,source.is_stripped,data=source.get_raw_data())
            clone.save_typetree(st);source.assets_file.objects[new_id]=clone;remap[old_id]=new_id
        ranges={}
        for entry in entries:
            record=copy.deepcopy(entry[1]);key=(record['preloadIndex'],record['preloadSize'])
            if key not in ranges:
                refs=copy.deepcopy(tree['m_PreloadTable'][key[0]:key[0]+key[1]])
                for ref in refs:
                    if ref['m_FileID']==0 and ref['m_PathID'] in remap:ref['m_PathID']=remap[ref['m_PathID']]
                ranges[key]=len(tree['m_PreloadTable']);tree['m_PreloadTable'].extend(refs)
            record['preloadIndex']=ranges[key]
            ref=record['asset']
            if ref['m_FileID']==0 and ref['m_PathID'] in remap:ref['m_PathID']=remap[ref['m_PathID']]
            tree['m_Container'].append((spec['internal'],record))
    bundle.save_typetree(tree)

def add_catalog_aliases(raw, aliases):
    """Append location/key records, preserving all original catalog records."""
    if not aliases:
        return raw
    raw = bytearray(raw)
    catalog = Catalog(raw)
    key_offset, = catalog.values(8)
    keys = catalog.array(key_offset, 2)
    def append(data):
        offset = len(raw); raw.extend(data); return offset
    def string(value):
        data = value.encode('ascii')
        append(struct.pack('<I', len(data)))
        return append(data)
    locations = {loc for _, array in keys for (loc,) in catalog.array(array)}
    names = {}
    for loc in sorted(locations):
        names.setdefault(catalog.string(catalog.values(loc,7)[0], '/'),[]).append(loc)
    for alias in aliases:
        if alias['key'] in names:
            raise ValueError('Duplicate speaker alias address')
        source_locs = names[alias['source_key']]
        source_key = next(key for key, arr in keys if {v[0] for v in catalog.array(arr)} == set(source_locs) and
            catalog.string(catalog.values(catalog.values(key,2)[1],2)[0], '/') == alias['source_key'])
        key_type, _ = catalog.values(source_key, 2)
        name_offset=string(alias['key']);internal_offset=string(alias['internal'])
        added=[]
        for source_loc in source_locs:
            values=list(catalog.values(source_loc,7))
            values[0]=name_offset;values[1]=internal_offset
            added.append(append(struct.pack('<7I',*values)))
        # String object data is (string offset, separator char). A plain string
        # has no segmented chain and therefore does not use the separator.
        key_data = append(struct.pack('<2I', name_offset, 0))
        key_object = append(struct.pack('<2I', key_type, key_data))
        append(struct.pack('<I', 4*len(added)))
        location_array = append(struct.pack('<'+'I'*len(added), *added))
        keys.append((key_object, location_array))
        names[alias['key']] = added
    append(struct.pack('<I', len(keys)*8))
    key_offset = append(b''.join(struct.pack('<2I', *entry) for entry in keys))
    struct.pack_into('<I', raw, 8, key_offset)
    return raw
