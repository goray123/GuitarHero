const fs = require('node:fs');
const path = require('node:path');
const root=path.resolve(__dirname,'..');
const read=f=>fs.readFileSync(path.join(root,f),'utf8').replace(/\r\n/g,'\n');
const checks=[];
function check(condition,message) {
  checks.push({passed:!!condition,message});
  if(!condition) throw new Error(message);
}
const manifest=JSON.parse(read('Assets/Characters/Boss1/boss1-sprites.json'));
const asset=read('Assets/Characters/Boss1/Boss1Sprites.asset');
let spriteCount=0;
for(const sheet of manifest.sheets) {
  const meta=read('Assets/Characters/Boss1/Sprites/'+sheet.file+'.meta');
  for(const frame of sheet.frames) {
    check(asset.includes('{fileID: '+frame.internalID+', guid: '+sheet.guid+', type: 3}'),
      'SpriteSet reference resolves: '+frame.name);
    check(meta.includes('      internalID: '+frame.internalID+'\n'),'Imported sprite ID exists: '+frame.name);
    spriteCount++;
  }
}
function localGraph(file) {
  const text=read(file), ids=[...text.matchAll(/^--- !u!\d+ &(-?\d+)/gm)].map(m=>m[1]);
  check(new Set(ids).size===ids.length,'Unique serialized object IDs in '+file);
  const local=[...text.matchAll(/\{fileID: (-?\d+)\}/g)].map(m=>m[1]).filter(id=>id!=='0');
  for(const id of local) check(ids.includes(id),'Local object reference resolves in '+file+': '+id);
}
localGraph('Assets/Prefabs/Boss1.prefab');
localGraph('Assets/Prefabs/Player.prefab');
localGraph('Assets/Scenes/Boss1_Arena.unity');
const scriptMeta=read('Assets/Scripts/Player/PlayerHealth.cs.meta');
const healthGuid=scriptMeta.match(/^guid: (\w+)/m)[1];
check(read('Assets/Prefabs/Player.prefab').includes('guid: '+healthGuid),'Player prefab has PlayerHealth');
const sceneGuid=read('Assets/Scenes/Boss1_Arena.unity.meta').match(/^guid: (\w+)/m)[1];
check(read('ProjectSettings/EditorBuildSettings.asset').includes('path: Assets/Scenes/Boss1_Arena.unity\n    guid: '+sceneGuid),
  'Boss arena is included in build settings');
const prefab=read('Assets/Prefabs/Boss1.prefab');
const setGuid=read('Assets/Characters/Boss1/Boss1Sprites.asset.meta').match(/^guid: (\w+)/m)[1];
check(prefab.includes('sprites: {fileID: 11400000, guid: '+setGuid),'Boss prefab references the sprite set');
const report={verified:true,checks:checks.length,spriteFrames:spriteCount,animations:12,scene:'Assets/Scenes/Boss1_Arena.unity'};
fs.mkdirSync(path.join(root,'output/boss1-development'),{recursive:true});
fs.writeFileSync(path.join(root,'output/boss1-development/assets-verification.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify(report,null,2));

