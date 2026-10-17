from pathlib import Path
from PIL import Image,ImageDraw,ImageChops
import numpy as np,json,math,zipfile
def clean_edges(f):
 m=f.getchannel('A');protected=Image.new('L',m.size);total=m.histogram()[255]
 if not total:return f
 edges=[(x,0) for x in range(m.width)]+[(x,m.height-1) for x in range(m.width)]+[(0,y) for y in range(m.height)]+[(m.width-1,y) for y in range(m.height)]
 for pt in edges:
  if not m.getpixel(pt) or protected.getpixel(pt):continue
  test=m.copy();ImageDraw.floodfill(test,pt,0);diff=ImageChops.subtract(m,test)
  if diff.histogram()[255]<total*0.15:m=test
  else:protected=ImageChops.lighter(protected,diff)
 f.putalpha(m);return f
out=Path('Disparos_para_Unity');out.mkdir(exist_ok=True)
im=Image.open('disparos.gif');frames=[];times=[]
for i in range(im.n_frames):
 im.seek(i);a=np.array(im.convert('RGBA'));a[np.all(a[:,:,:3]==(31,16,42),axis=2)]=0;frames.append(Image.fromarray(a));times.append(im.info.get('duration',100))
entries=[]
for row in range(8):
 for col in range(16):
  if row==0 and col in (5,6):continue
  box=(col*48,row*48,(col+3)*48 if row==0 and col==4 else (col+1)*48,(row+1)*48)
  fs=[clean_edges(f.crop(box)) for f in frames]
  if not any(f.getbbox() for f in fs):continue
  index=len(entries)+1;name=f'Efecto_{index:03d}';folder=out/name;fd=folder/'cuadros';fd.mkdir(parents=True,exist_ok=True)
  for i,f in enumerate(fs):f.save(fd/f'{name}_{i:03d}.png')
  w,h=fs[0].size;sheet=Image.new('RGBA',(w*5,h*4))
  for i,f in enumerate(fs):sheet.paste(f,((i%5)*w,(i//5)*h))
  sheet.save(folder/(name+'_spritesheet.png'))
  gs=[]
  for f in fs:
   q=f.convert('RGB').quantize(colors=255,dither=Image.Dither.NONE);q.paste(255,mask=f.getchannel('A').point(lambda a:255 if a==0 else 0));gs.append(q)
  gs[0].save(folder/(name+'.gif'),save_all=True,append_images=gs[1:],duration=times,loop=0,transparency=255,disposal=2,optimize=False)
  meta={'nombre':name,'cuadros':len(fs),'ancho_cuadro':w,'alto_cuadro':h,'columnas':5,'filas':4,'duraciones_ms':times,'fps':10,'bucle_vista_previa':True,'fila_origen':row+1,'columna_origen':col+1,'pivote_normalizado':[0.5,0.5]}
  (folder/'animacion.json').write_text(json.dumps(meta,indent=2),encoding='utf-8');entries.append(meta)
cols=12;cw=128;ch=122;catalog=Image.new('RGB',(cols*cw,math.ceil(len(entries)/cols)*ch),(72,79,94));d=ImageDraw.Draw(catalog)
for i,e in enumerate(entries):
 folder=out/e['nombre'];best=None;count=-1
 for f in sorted((folder/'cuadros').glob('*.png')):
  pic=Image.open(f).convert('RGBA');n=sum(1 for a in pic.getchannel('A').getdata() if a)
  if n>count:best=pic.copy();count=n
 best.thumbnail((112,90),Image.Resampling.NEAREST)
 if best.width<=56 and best.height<=45:best=best.resize((best.width*2,best.height*2),Image.Resampling.NEAREST)
 x=(i%cols)*cw;y=(i//cols)*ch;catalog.paste(best,(x+(cw-best.width)//2,y+8+(90-best.height)//2),best);d.text((x+18,y+103),e['nombre'],fill='white')
catalog.save(out/'Catalogo_efectos.png');(out/'catalogo.json').write_text(json.dumps(entries,indent=2),encoding='utf-8')
print('Efectos:',len(entries),'Cuadros:',sum(e['cuadros'] for e in entries),'Tiempos:',times)
