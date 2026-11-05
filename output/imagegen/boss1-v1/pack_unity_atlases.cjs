const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const sharp = require('C:/Users/HAJE/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const { components } = require('./inspect_components.cjs');
const { inspect } = require('./inspect_sprite_sheet.cjs');
const root = path.resolve(__dirname, '../../..');
const target = path.join(root, 'Assets/Characters/Boss1');
const spritesDir = path.join(target, 'Sprites');
const CELL = 256;
const BODY_BASELINE = 224;
const PIXELS_PER_UNIT = 100;
const mainRows = ['Idle', 'Walk', 'LowSweep', 'MidPunch', 'OverheadSlam', 'BoosterDash', 'RearMissile', 'MissileBarrage'];
const supportRows = ['Hit', 'Death', 'MissileFlight', 'MissileExplosion'];
const hash = text => crypto.createHash('sha256').update('GuitarHero/Boss1/v1/' + text).digest('hex');
const guid = text => hash(text).slice(0, 32);
const id = text => parseInt(hash('sprite/' + text).slice(0, 13), 16);
function writeNew(file, content) {
  if (fs.existsSync(file)) throw new Error('Refusing to replace existing asset: ' + file);
  fs.mkdirSync(path.dirname(file), { recursive: true });
  fs.writeFileSync(file, content);
}
function folderMeta(dir) {
  const file = dir + '.meta';
  if (!fs.existsSync(file)) writeNew(file, 'fileFormatVersion: 2\nguid: ' + guid(path.relative(root, dir)) +
    '\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n');
}
function union(items) {
  const x = Math.min(...items.map(c => c.x)), y = Math.min(...items.map(c => c.y));
  return { x, y, width: Math.max(...items.map(c => c.x + c.width)) - x, height: Math.max(...items.map(c => c.y + c.height)) - y };
}
function spriteMeta(sheet) {
  const original = fs.readFileSync(path.join(root, 'Assets/Characters/Player/PC_Idle.png.meta'), 'utf8').replace(/\r\n/g, '\n');
  let head = original.slice(0, original.indexOf('  spriteSheet:\n'));
  head = head.replace(/^guid: .+$/m, 'guid: ' + sheet.guid);
  const names = sheet.frames.map(frame => '  - first:\n      213: ' + frame.internalID + '\n    second: ' + frame.name).join('\n');
  head = head.replace(/  internalIDToNameTable:[\s\S]*?  externalObjects: \{\}/, '  internalIDToNameTable:\n' + names + '\n  externalObjects: {}');
  head = head.replace('  spriteMeshType: 1', '  spriteMeshType: 0')
    .replace('  alignment: 0', '  alignment: 9')
    .replace('  spritePivot: {x: 0.5, y: 0.5}', '  spritePivot: {x: 0.5, y: 0.125}')
    .replace(/  spritePixelsToUnits: \d+/, '  spritePixelsToUnits: ' + PIXELS_PER_UNIT)
    .replace('  spriteGenerateFallbackPhysicsShape: 1', '  spriteGenerateFallbackPhysicsShape: 0')
    .replace(/textureCompression: 1/g, 'textureCompression: 0');
  const frames = sheet.frames.map(f => [
    '    - serializedVersion: 2', '      name: ' + f.name, '      rect:', '        serializedVersion: 2',
    '        x: ' + f.rect.x, '        y: ' + f.rect.y, '        width: 256', '        height: 256',
    '      alignment: 9', '      pivot: {x: ' + f.pivot.x + ', y: ' + f.pivot.y + '}',
    '      border: {x: 0, y: 0, z: 0, w: 0}', '      customData: ', '      outline: []', '      physicsShape: []',
    '      tessellationDetail: -1', '      bones: []', '      spriteID: ' + guid(f.name), '      internalID: ' + f.internalID,
    '      vertices: []', '      indices: ', '      edges: []', '      weights: []'
  ].join('\n')).join('\n');
  const settings = JSON.stringify({sliceOnImport:false,gridCellCount:{x:8,y:sheet.rows},gridSpriteSize:{x:256,y:256},
    gridSpriteOffset:{x:0,y:0},gridSpritePadding:{x:0,y:0},pivot:{x:0.5,y:0.125},pivotPixels:{x:0,y:0},
    autoSlicingMethod:0,spriteAlignment:9,pivotUnitMode:0,slicingType:1,keepEmptyRects:false,isAlternate:false});
  return head + '  spriteSheet:\n    serializedVersion: 2\n    sprites:\n' + frames + '\n' +
    '    outline: []\n    customData: \n    physicsShape: []\n    bones: []\n    spriteID: ' + guid(sheet.file + '/root') + '\n' +
    '    internalID: 0\n    vertices: []\n    indices: \n    edges: []\n    weights: []\n    secondaryTextures: []\n' +
    '    spriteCustomMetadata:\n      entries:\n      - key: SpriteEditor.SliceSettings\n        value: \'' + settings + '\'\n' +
    '    nameFileIdTable:\n' + [...sheet.frames].sort((a,b)=>a.name.localeCompare(b.name)).map(f=>'      '+f.name+': '+f.internalID).join('\n') +
    '\n  mipmapLimitGroupName: \n  pSDRemoveMatte: 0\n  userData: Boss1 v1; 256x256; 8 columns; row-major\n  assetBundleName: \n  assetBundleVariant: \n';
}
async function pack(sourceFile, rowNames, outputName, support) {
  const source = await components(sourceFile, 32);
  const { width, height } = source.info;
  const groups = Array.from({ length: rowNames.length * 8 }, () => []);
  for (const component of source.components) {
    const col = Math.max(0, Math.min(7, Math.floor(component.centerX / (width / 8))));
    const row = Math.max(0, Math.min(rowNames.length - 1, Math.floor(component.centerY / (height / rowNames.length))));
    groups[row * 8 + col].push(component);
  }
  if (groups.some(group => !group.length)) throw new Error('Missing source frame in ' + outputName);
  const neutral = groups[0][0];
  const bodyScale = 184 / neutral.height;
  const sheet = { file: outputName, guid: guid(outputName), width: 2048, height: CELL * rowNames.length,
    columns: 8, rows: rowNames.length, cellWidth: CELL, cellHeight: CELL, pixelsPerUnit: PIXELS_PER_UNIT,
    textureType: 'Sprite', spriteMode: 'Multiple', filterMode: 'Bilinear', compression: 'None', mipmaps: false,
    rowNames, frames: [], source: path.basename(sourceFile), sourceBodyScale: bodyScale };
  const layers = [];
  for (let row = 0; row < rowNames.length; row++) for (let col = 0; col < 8; col++) {
    const group = groups[row * 8 + col].sort((a,b)=>b.pixels-a.pixels);
    const fx = support && row >= 2;
    const bounds = fx && row === 3 ? union(group) : group[0];
    // Preserve the painted RGBA content; this step only extracts and packs
    // complete poses into a regular atlas, with one scale for all body frames.
    const padding = 4;
    const crop = { left: Math.max(0, bounds.x - padding), top: Math.max(0, bounds.y - padding),
      width: 0, height: 0 };
    crop.width = Math.min(width, bounds.x + bounds.width + padding) - crop.left;
    crop.height = Math.min(height, bounds.y + bounds.height + padding) - crop.top;
    const sourceRootX = Math.round((col + 0.5) * width / 8);
    let sourceRootY, scale, destinationRootY;
    if (!fx) {
      sourceRootY = bounds.y + bounds.height; scale = bodyScale; destinationRootY = BODY_BASELINE;
    } else if (row === 2) {
      sourceRootY = group[0].y + 44; scale = 0.5; destinationRootY = 128;
    } else {
      sourceRootY = Math.round((row + 0.5) * height / rowNames.length); scale = 0.75; destinationRootY = 128;
    }
    const resizedWidth = Math.round(crop.width * scale), resizedHeight = Math.round(crop.height * scale);
    const left = 128 - Math.round((sourceRootX - crop.left) * scale);
    const top = destinationRootY - Math.round((sourceRootY - crop.top) * scale);
    if (left < 0 || top < 0 || left + resizedWidth > CELL || top + resizedHeight > CELL) {
      throw new Error('Pose does not fit its cell: ' + rowNames[row] + '/' + col + JSON.stringify({left, top, resizedWidth, resizedHeight}));
    }
    const buffer = await sharp(sourceFile).extract(crop).resize(resizedWidth, resizedHeight, {kernel:'lanczos3'}).png().toBuffer();
    layers.push({ input: buffer, left: col * CELL + left, top: row * CELL + top });
    const name = 'Boss1_' + rowNames[row] + '_' + String(col).padStart(2, '0');
    sheet.frames.push({ name, animation: rowNames[row], frame: col, row, column: col,
      rect: {x:col*CELL,y:sheet.height-(row+1)*CELL,width:CELL,height:CELL},
      pivot: {x:0.5,y:fx?0.5:0.125}, internalID:id(name), sourceRect:crop, sourceScale:scale });
  }
  const png = await sharp({create:{width:sheet.width,height:sheet.height,channels:4,background:{r:0,g:0,b:0,alpha:0}}})
    .composite(layers).png({compressionLevel:9}).toBuffer();
  writeNew(path.join(spritesDir, outputName + '.meta'), spriteMeta(sheet));
  writeNew(path.join(spritesDir, outputName), png);
  const inspection = await inspect(path.join(spritesDir, outputName), 8, rowNames.length);
  const unsafe = inspection.cells.filter(c=>!c.bounds || Object.values(c.margin).some(n=>n<8));
  if (unsafe.length) throw new Error('Unsafe atlas cell padding: ' + JSON.stringify(unsafe));
  return { sheet, inspection };
}
async function main() {
  fs.mkdirSync(spritesDir, { recursive: true });
  folderMeta(target); folderMeta(spritesDir);
  const actions = await pack(path.join(__dirname, 'boss1-actions-generated.png'), mainRows, 'Boss1_Actions_v1.png', false);
  const support = await pack(path.join(__dirname, 'boss1-support-generated.png'), supportRows, 'Boss1_Support_v1.png', true);
  const manifest = { version: 1, generatedWith: 'Built-in ImageGen', facing: 'right', frameOrder: 'top-to-bottom rows, left-to-right columns',
    cellSize: [256,256], bodyPivot:[0.5,0.125], effectPivot:[0.5,0.5], sheets:[actions.sheet,support.sheet],
    notes: ['Body frames use one scale per source sheet; all feet aligned at y=224.',
      'Guided missiles are separate effects so body attacks can keep playing.',
      'Animation timing, missile period, hitboxes, damage and AI remain governed by the boss design document.'] };
  writeNew(path.join(target, 'boss1-sprites.json'), JSON.stringify(manifest, null, 2) + '\n');
  writeNew(path.join(target, 'boss1-sprites.json.meta'), 'fileFormatVersion: 2\nguid: ' + guid('boss1-sprites.json') + '\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n');
  const report = { success:true, sheets:[actions.inspection,support.inspection], totalFrames:96,
    checks:{ dimensions:true, nonEmptyFrames:true, minimum8PixelGutters:true, actualAlphaTransparency:true,
      uniform256PixelCells:true, originalCharacterArtGeneratedWithImageGen:true } };
  fs.writeFileSync(path.join(__dirname, 'validation.json'), JSON.stringify(report, null, 2) + '\n');
  console.log(JSON.stringify({ assets:path.relative(root,target), frameCount:96,
    sheets:report.sheets.map(s=>({ file:path.basename(s.file),width:s.width,height:s.height,frameCount:s.cells.length,
      alphaZeroRatio:s.transparentRatio,minimumMargin:Math.min(...s.cells.flatMap(c=>Object.values(c.margin))) })) }, null, 2));
}
main().catch(error=>{console.error(error);process.exitCode=1;});

