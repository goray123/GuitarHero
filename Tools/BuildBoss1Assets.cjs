const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const root = path.resolve(__dirname, '..');
const read = file => fs.readFileSync(path.join(root,file),'utf8').replace(/\r\n/g,'\n');
const hash = name => crypto.createHash('sha256').update('GuitarHero.Boss1.runtime.v1/'+name).digest('hex').slice(0,32);
function write(file, content) {
  const output = path.join(root,file);
  fs.mkdirSync(path.dirname(output),{recursive:true});
  if (!fs.existsSync(output) || fs.readFileSync(output,'utf8')!==content) fs.writeFileSync(output,content);
}
function meta(file, importer='DefaultImporter', body='') {
  const output=path.join(root,file+'.meta');
  if (fs.existsSync(output)) {
    const existing=read(file+'.meta').match(/^guid: (\w+)/m);
    if (existing && existing[1]!==hash(file)) throw new Error('Unexpected existing GUID: '+file);
  }
  write(file+'.meta','fileFormatVersion: 2\nguid: '+hash(file)+'\n'+importer+':\n  externalObjects: {}\n'+body+'  userData: \n  assetBundleName: \n  assetBundleVariant: \n');
}
const codeFiles = [
  'Assets/Scripts/Characters/Boss1/Boss1CombatModel.cs',
  'Assets/Scripts/Characters/Boss1/Boss1SpriteSet.cs',
  'Assets/Scripts/Characters/Boss1/Boss1MissileView.cs',
  'Assets/Scripts/Characters/Boss1/Boss1Controller.cs',
  'Assets/Scripts/Characters/Boss1/Boss1Arena.cs',
  'Assets/Scripts/Player/PlayerHealth.cs'
];
write('Assets/Scripts/Characters/Boss1.meta','fileFormatVersion: 2\nguid: '+hash('Assets/Scripts/Characters/Boss1')+
  '\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n');
for(const file of codeFiles) meta(file,'MonoImporter','  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n');

const manifest=JSON.parse(read('Assets/Characters/Boss1/boss1-sprites.json'));
const frameRows={idle:'Idle',walk:'Walk',lowSweep:'LowSweep',midPunch:'MidPunch',overheadSlam:'OverheadSlam',
  boosterDash:'BoosterDash',rearMissile:'RearMissile',missileBarrage:'MissileBarrage',hit:'Hit',death:'Death',
  missileFlight:'MissileFlight',missileExplosion:'MissileExplosion'};
const yamlHeader='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n';
const scriptGuid=file=>hash('Assets/Scripts/'+file);
let spriteAsset=yamlHeader+'--- !u!114 &11400000\nMonoBehaviour:\n'+
  '  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n'+
  '  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n'+
  '  m_Script: {fileID: 11500000, guid: '+scriptGuid('Characters/Boss1/Boss1SpriteSet.cs')+', type: 3}\n'+
  '  m_Name: Boss1Sprites\n  m_EditorClassIdentifier: Assembly-CSharp::Boss1SpriteSet\n';
for(const [field,animation] of Object.entries(frameRows)) {
  const sheet=manifest.sheets.find(s=>s.frames.some(f=>f.animation===animation));
  const frames=sheet.frames.filter(f=>f.animation===animation).sort((a,b)=>a.frame-b.frame);
  if(frames.length!==8) throw new Error('Expected 8 frames for '+animation);
  spriteAsset+='  '+field+':\n'+frames.map(f=>'  - {fileID: '+f.internalID+', guid: '+sheet.guid+', type: 3}\n').join('');
}
const spriteAssetPath='Assets/Characters/Boss1/Boss1Sprites.asset';
write(spriteAssetPath,spriteAsset);
meta(spriteAssetPath,'NativeFormatImporter','  mainObjectFileID: 11400000\n');

const playerPrefabPath='Assets/Prefabs/Player.prefab';
let playerPrefab=read(playerPrefabPath);
const playerRoot=playerPrefab.match(/--- !u!1 &(\d+)\n/)[1];
const playerGuid=read(playerPrefabPath+'.meta').match(/^guid: (\w+)/m)[1];
function block(text,type) {
  const value=text.match(new RegExp('--- !u!'+type+' &[0-9-]+\\n[\\s\\S]*?(?=--- !u!|$)'));
  if(!value) throw new Error('Missing prefab block '+type);
  return value[0];
}
function replaceBlockIdentity(text,type,newId,goId) {
  return text.replace(new RegExp('^--- !u!'+type+' &[0-9-]+'), '--- !u!'+type+' &'+newId)
    .replace(/^  m_GameObject: \{fileID: \d+\}/m,'  m_GameObject: {fileID: '+goId+'}');
}
function gameObject(id,name,components) {
  return '--- !u!1 &'+id+'\nGameObject:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n'+
    '  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  serializedVersion: 6\n  m_Component:\n'+
    components.map(id=>'  - component: {fileID: '+id+'}\n').join('')+
    '  m_Layer: 0\n  m_Name: '+name+'\n  m_TagString: Untagged\n  m_Icon: {fileID: 0}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 1\n';
}
function transform(id,goId,parent=0,children=[],scale=1,rootOrder=0) {
  return '--- !u!4 &'+id+'\nTransform:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n'+
    '  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: '+goId+'}\n'+
    '  serializedVersion: 2\n  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n  m_LocalPosition: {x: 0, y: 0, z: 0}\n'+
    '  m_LocalScale: {x: '+scale+', y: '+scale+', z: 1}\n  m_ConstrainProportionsScale: 0\n'+
    (children.length?'  m_Children:\n'+children.map(c=>'  - {fileID: '+c+'}\n').join(''):'  m_Children: []\n')+
    '  m_Father: {fileID: '+parent+'}\n  m_RootOrder: '+rootOrder+'\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n';
}
function mono(id,goId,className,file,fields) {
  return '--- !u!114 &'+id+'\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n'+
    '  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: '+goId+'}\n'+
    '  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: '+scriptGuid(file)+', type: 3}\n'+
    '  m_Name:\n  m_EditorClassIdentifier: Assembly-CSharp::'+className+'\n'+fields;
}
const body=replaceBlockIdentity(block(playerPrefab,50),50,1002,1000).replace('  m_Mass: 1\n','  m_Mass: 12\n');
const collider=replaceBlockIdentity(block(playerPrefab,61),61,1003,1000)
  .replace(/^  m_Offset: .+$/m,'  m_Offset: {x: 0, y: 1.1135}')
  .replace(/^  m_Size: .+$/m,'  m_Size: {x: 1.441, y: 2.227}');
const idleSheet=manifest.sheets.find(s=>s.rowNames.includes('Idle'));
const idleFrame=idleSheet.frames.find(f=>f.animation==='Idle' && f.frame===0);
const renderer=replaceBlockIdentity(block(playerPrefab,212),212,2002,2000)
  .replace(/^  m_SortingOrder: \d+$/m,'  m_SortingOrder: 5')
  .replace(/^  m_Sprite: .+$/m,'  m_Sprite: {fileID: '+idleFrame.internalID+', guid: '+idleSheet.guid+', type: 3}');
const tuningSource=read('Assets/Scripts/Characters/Boss1/Boss1CombatModel.cs');
const tuning=[...tuningSource.matchAll(/    public float (\w+) = ([0-9.]+)f;/g)];
let fields='  sprites: {fileID: 11400000, guid: '+hash(spriteAssetPath)+', type: 2}\n  visual: {fileID: 2002}\n'+
  '  target: {fileID: 0}\n  autoBegin: 1\n  settings:\n'+tuning.map(m=>'    '+m[1]+': '+m[2]+'\n').join('')+
  '  arenaLeft: -13\n  arenaRight: 13\n  drawAttackGizmos: 1\n';
const prefab=yamlHeader+gameObject(1000,'Boss1',[1001,1002,1003,11400001])+
  transform(1001,1000,0,[2001])+body+collider+
  mono(11400001,1000,'Boss1Controller','Characters/Boss1/Boss1Controller.cs',fields)+
  gameObject(2000,'Visual',[2001,2002])+transform(2001,2000,1001,[],1.35)+renderer;
const prefabPath='Assets/Prefabs/Boss1.prefab';
write(prefabPath,prefab); meta(prefabPath,'PrefabImporter');

const healthGuid=scriptGuid('Player/PlayerHealth.cs');
if(!playerPrefab.includes('guid: '+healthGuid)) {
  const healthId=114000040001;
  playerPrefab=playerPrefab.replace(/(  m_Component:\n[\s\S]*?)(?=  m_Layer:)/,
    '$1  - component: {fileID: '+healthId+'}\n');
  playerPrefab+=mono(healthId,playerRoot,'PlayerHealth','Player/PlayerHealth.cs',
    '  maxHealth: 100\n  invulnerabilityTime: 0.2\n');
  write(playerPrefabPath,playerPrefab);
}
const scenePath='Assets/Scenes/Boss1_Arena.unity';
const skylineGuid=read('Assets/Environment/City01/CitySkyline.png.meta').match(/^guid: (\w+)/m)[1];
const scene=read('Assets/Scenes/SampleScene.unity')+'\n'+
  gameObject(801000,'Boss1 Encounter',[801001,801002])+transform(801001,801000,0,[],1,2)+
  mono(801002,801000,'Boss1Arena','Characters/Boss1/Boss1Arena.cs',
    '  playerPrefab: {fileID: '+playerRoot+', guid: '+playerGuid+', type: 3}\n'+
    '  bossPrefab: {fileID: 11400001, guid: '+hash(prefabPath)+', type: 3}\n'+
    '  skyline: {fileID: 21300000, guid: '+skylineGuid+', type: 3}\n'+
    '  leftEdge: -13\n  rightEdge: 13\n  enableTestKeys: 1\n');
write(scenePath,scene); meta(scenePath);
const buildPath='ProjectSettings/EditorBuildSettings.asset';
let builds=read(buildPath);
if(!builds.includes('path: '+scenePath)) {
  builds=builds.replace('  m_configObjects:\n',
    '  - enabled: 1\n    path: '+scenePath+'\n    guid: '+hash(scenePath)+'\n  m_configObjects:\n');
  write(buildPath,builds);
}
console.log(JSON.stringify({scene:scenePath,prefab:prefabPath,spriteSet:spriteAssetPath,
  animations:Object.keys(frameRows).length,frames:96,tuningFields:tuning.length,playerHealth:true},null,2));

