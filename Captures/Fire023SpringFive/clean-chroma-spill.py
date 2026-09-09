from pathlib import Path
from PIL import Image
import json
root=Path(__file__).resolve().parents[2]
results=[]
for p in sorted((root/'Assets/Art/Generated/Environment/Fire/OnDemand/fire023-spring-five').glob('*.png')):
 im=Image.open(p).convert('RGBA');pix=im.load();count=0
 for y in range(im.height):
  for x in range(im.width):
   r,g,b,a=pix[x,y]
   if a and min(r,b)>g+8:
    spill=min(r,b)-g
    pix[x,y]=(r-spill,g,b-spill,a);count+=1
 if count:im.save(p)
 results.append({'file':p.name,'despilled_pixels':count})
(root/'Captures/Fire023SpringFive/despill-audit.json').write_text(json.dumps(results,indent=2));print(results)
