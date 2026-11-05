const fs = require('node:fs');
const sharp = require('C:/Users/HAJE/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');

async function inspect(file, columns = 8, rows = 8, threshold = 32) {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const { width, height, channels } = info;
  let transparent = 0, opaque = 0, partial = 0;
  for (let i = 3; i < data.length; i += channels) {
    if (data[i] === 0) transparent++;
    else if (data[i] === 255) opaque++;
    else partial++;
  }
  const cells = [];
  for (let row = 0; row < rows; row++) {
    for (let column = 0; column < columns; column++) {
      const x0 = Math.round(column * width / columns), x1 = Math.round((column + 1) * width / columns);
      const y0 = Math.round(row * height / rows), y1 = Math.round((row + 1) * height / rows);
      let minX = x1, maxX = -1, minY = y1, maxY = -1, pixels = 0;
      for (let y = y0; y < y1; y++) for (let x = x0; x < x1; x++) {
        if (data[(y * width + x) * channels + 3] >= threshold) {
          minX = Math.min(minX, x); maxX = Math.max(maxX, x);
          minY = Math.min(minY, y); maxY = Math.max(maxY, y); pixels++;
        }
      }
      cells.push({ row, column, rect: { x: x0, y: y0, width: x1 - x0, height: y1 - y0 },
        bounds: maxX < 0 ? null : { x: minX - x0, y: minY - y0, width: maxX - minX + 1, height: maxY - minY + 1 },
        margin: maxX < 0 ? null : { left: minX - x0, right: x1 - maxX - 1, top: minY - y0, bottom: y1 - maxY - 1 }, pixels });
    }
  }
  return { file, width, height, channels, columns, rows, transparent, opaque, partial,
    transparentRatio: transparent / (width * height), cells };
}

module.exports = { inspect };
if (require.main === module) inspect(process.argv[2], Number(process.argv[3] || 8), Number(process.argv[4] || 8))
  .then(result => { console.log(JSON.stringify(result, null, 2)); })
  .catch(error => { console.error(error); process.exitCode = 1; });
