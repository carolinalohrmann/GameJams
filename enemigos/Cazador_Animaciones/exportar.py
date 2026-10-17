from PIL import Image,ImageDraw,ImageChops
from pathlib import Path
import json,zipfile
root=Path('Cazador_Animaciones')
for name,cols,rows,ms,loop in [('Idle',2,2,180,True),('Caminar',3,2,120,True),('Disparar',2,2,100,False),('Muerte',3,2,150,False)]:
 folder=root/name;im=Image.open(folder/(name+'_original.png')).convert('RGBA');parts=[]
 for i in range(cols*rows):
  x=i%cols;y=i//cols;c=im.crop((round(x*im.width/cols),round(y*im.height/rows),round((x+1)*im.width/cols),round((y+1)*im.height/rows)));m=c.getchannel('A').point(lambda a:255 if a>=128 else 0);protected=Image.new('L',m.size)
  edges=[(x,0) for x in range(m.width)]+[(x,m.height-1) for x in range(m.width)]+[(0,y) for y in range(m.height)]+[(m.width-1,y) for y in range(m.height)]
  total=m.histogram()[255]
  for pt in edges:
   if not m.getpixel(pt) or protected.getpixel(pt):continue
   test=m.copy();ImageDraw.floodfill(test,pt,0);diff=ImageChops.subtract(m,test)
   if diff.histogram()[255]<total*0.08:m=test
   else:protected=ImageChops.lighter(protected,diff)
  b=m.getbbox();assert b;parts.append(c.crop(b))
 scale=min(200/max(p.height for p in parts),224/max(p.width for p in parts));frames=[]
 for i,p in enumerate(parts):
  p=p.resize((round(p.width*scale),round(p.height*scale)),Image.Resampling.NEAREST);f=Image.new('RGBA',(256,256));f.paste(p,((256-p.width)//2,236-p.height));f.save(folder/'cuadros'/f'{name}_{i:03d}.png');frames.append(f)
 sheet=Image.new('RGBA',(cols*256,rows*256))
 for i,f in enumerate(frames):sheet.paste(f,((i%cols)*256,(i//cols)*256))
 sheet.save(folder/(name+'_spritesheet.png'));gs=[]
 for f in frames:
  q=f.convert('RGB').quantize(colors=255,dither=Image.Dither.NONE);q.paste(255,mask=f.getchannel('A').point(lambda a:255 if a<128 else 0));gs.append(q)
 durations=[ms]*len(gs)
 if not loop:durations[-1]=650
 gs[0].save(folder/(name+'.gif'),save_all=True,append_images=gs[1:],duration=durations,transparency=255,disposal=2,optimize=False,**({'loop':0} if loop else {}))
 print(name,'OK')
preview=Image.new('RGB',(1536,1128),(190,201,214));d=ImageDraw.Draw(preview)
for row,name in enumerate(['Idle','Caminar','Disparar','Muerte']):
 folder=root/name;meta=json.loads((folder/'animacion.json').read_text());ps=sorted((folder/'cuadros').glob('*.png'));assert len(ps)==meta['cuadros'];d.text((5,row*282+5),name,fill='black')
 for i,p in enumerate(ps):
  with Image.open(p) as f:assert f.size==(256,256);preview.paste(f,(i*256,row*282+24),f)
 with Image.open(folder/(name+'.gif')) as g:assert g.n_frames==meta['cuadros']
preview.save(root/'Vista_general.png')
with zipfile.ZipFile('Cazador_Animaciones_Unity.zip','w',zipfile.ZIP_DEFLATED) as z:
 for p in root.rglob('*'):
  if p.is_file():z.write(p,p.as_posix())
with zipfile.ZipFile('Cazador_Animaciones_Unity.zip') as z:assert z.testzip() is None
