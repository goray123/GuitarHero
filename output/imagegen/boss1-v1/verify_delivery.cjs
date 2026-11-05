const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const { inspect } = require('./inspect_sprite_sheet.cjs');
const root = path.resolve(__dirname, '../../..');
const assetRoot = path.join(root, 'Assets/Characters/Boss1');
async function main() {
  const manifest = JSON.parse(fs.readFileSync(path.join(assetRoot,'boss1-sprites.json'),'utf8'));
  const names = new Set(), ids = new Set(), results = [];
  for (const sheet of manifest.sheets) {
    const png = path.join(assetRoot,'Sprites',sheet.file);
    const meta = fs.readFileSync(png+'.meta','utf8');
    if (!meta.includes('guid: '+sheet.guid+'\n') || !meta.includes('  spriteMode: 2\n') ||
        !meta.includes('  alphaIsTransparency: 1\n') || !meta.includes('  spritePixelsToUnits: 100\n') ||
        !meta.includes('  spriteMeshType: 0\n') || /textureCompression: [1-9]/.test(meta)) throw new Error('Importer settings mismatch');
    if ((meta.match(/^      name: /gm)||[]).length !== sheet.frames.length) throw new Error('Sprite count mismatch');
    for (const frame of sheet.frames) {
      if (names.has(frame.name) || ids.has(frame.internalID)) throw new Error('Duplicate frame identity');
      names.add(frame.name); ids.add(frame.internalID);
      const block = '      name: '+frame.name+'\n      rect:\n        serializedVersion: 2\n'+
        '        x: '+frame.rect.x+'\n        y: '+frame.rect.y+'\n        width: 256\n        height: 256\n'+
        '      alignment: 9\n      pivot: {x: '+frame.pivot.x+', y: '+frame.pivot.y+'}';
      if (!meta.includes(block) || !meta.includes('      internalID: '+frame.internalID+'\n') ||
          !meta.includes('      '+frame.name+': '+frame.internalID+'\n')) throw new Error('Sprite range/ID mismatch: '+frame.name);
    }
    const inspected = await inspect(png,8,sheet.rows,1);
    const minimumAlphaMargin = Math.min(...inspected.cells.flatMap(c=>Object.values(c.margin)));
    if (minimumAlphaMargin < 1 || inspected.cells.some(c=>!c.bounds) || inspected.width!==sheet.width || inspected.height!==sheet.height) throw new Error('Pixel inspection failed');
    results.push({file:sheet.file,frames:sheet.frames.length,width:inspected.width,height:inspected.height,
      minimumMarginIncludingFaintAlpha:minimumAlphaMargin,fullyTransparentRatio:inspected.transparentRatio,
      pngSha256:crypto.createHash('sha256').update(fs.readFileSync(png)).digest('hex'),metadataChecked:true});
  }
  if (names.size!==96) throw new Error('Expected 96 unique frames');
  const report = {verified:true,totalFrames:names.size,unityEditorRun:false,sheets:results};
  fs.writeFileSync(path.join(__dirname,'delivery-verification.json'),JSON.stringify(report,null,2)+'\n');
  console.log(JSON.stringify(report,null,2));
}
main().catch(e=>{console.error(e);process.exitCode=1;});

