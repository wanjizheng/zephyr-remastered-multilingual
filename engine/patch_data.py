"""Apply declarative edits. Official resource bytes are read only from the user's files."""
import copy,hashlib,json,zlib,zipfile,functools
@functools.lru_cache(maxsize=4)
def glyph_archive(path):return zipfile.ZipFile(path)
def digest(b):return hashlib.sha256(b).hexdigest()
def assign(tree,path,value):
 if not path:return copy.deepcopy(value)
 key=path[0]
 if isinstance(tree,tuple):
  items=list(tree);items[key]=assign(items[key],path[1:],value);return tuple(items)
 tree[key]=assign(tree[key],path[1:],value);return tree
def apply_ops(tree,ops):
 for op in ops:
  path=op['path'];parent=tree
  for key in path[:-1]:parent=parent[key]
  key=path[-1]
  if op['kind']=='set':tree=assign(tree,path,op['value'])
  elif op['kind']=='keyed':
   original=parent[key];result=[]
   for item in op['items']:
    if 'original' in item:
     value=copy.deepcopy(original[item['original']]);apply_ops(value,item.get('ops',[]));result.append(value)
    else:result.append(copy.deepcopy(item['new']))
   parent[key]=result
  else:raise ValueError('Unsupported edit operation')
 return tree
def texture_pixels(original,width,height,recipe,payload):
 if recipe.get('format')=='rgba32_overlay':
  out=bytearray(width*height*4);ow=recipe['original_width'];oh=recipe['original_height']
  assert len(original)==ow*oh*4 and ow<=width and oh<=height
  for y in range(oh):out[y*width*4:y*width*4+ow*4]=original[y*ow*4:(y+1)*ow*4]
  for tile in recipe['tiles']:
   assert tile['kind']=='licensed'
   x,y,w,h=tile['to'];assert 0<=x and x+w<=width and 0<=y and y+h<=height
   name=tile['sha256']+'.z';blob=(payload/'glyphs'/name).read_bytes() if (payload/'glyphs').exists() else glyph_archive(str(payload/('auto-glyphs.zip' if tile.get('archive')=='auto' else 'glyphs.zip'))).read(name)
   raw=zlib.decompress(blob);assert digest(raw)==tile['sha256'] and len(raw)==w*h
   for i in range(h):
    row=bytearray(b'\xff'*(w*4));row[3::4]=raw[i*w:(i+1)*w];start=((y+i)*width+x)*4;out[start:start+w*4]=row
  assert digest(out)==recipe['pixel_sha256'],'RGBA font reconstruction mismatch'
  return bytes(out)
 out=bytearray(width*height)
 for tile in recipe['tiles']:
  x,y,w,h=tile['to'];kind=tile['kind']
  if kind=='original':
   sx,sy=tile['from'];ow=recipe['original_width'];data=b''.join(original[(sy+i)*ow+sx:(sy+i)*ow+sx+w] for i in range(h))
  elif kind=='licensed':
   name=tile['sha256']+'.z';blob=(payload/'glyphs'/name).read_bytes() if (payload/'glyphs').exists() else glyph_archive(str(payload/('auto-glyphs.zip' if tile.get('archive')=='auto' else 'glyphs.zip'))).read(name)
   raw=zlib.decompress(blob);assert digest(raw)==tile['sha256'];assert len(raw)==w*h;data=raw
  else:raise ValueError('Unknown pixel source')
  assert 0<=x and x+w<=width and 0<=y and y+h<=height
  for i in range(h):out[(y+i)*width+x:(y+i)*width+x+w]=data[i*w:(i+1)*w]
 assert digest(out)==recipe['pixel_sha256'],'Font reconstruction mismatch'
 return bytes(out)
