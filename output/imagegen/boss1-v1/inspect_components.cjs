const sharp = require('C:/Users/HAJE/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
async function components(file, threshold = 32) {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const { width, height } = info, labels = new Int32Array(width * height), stack = new Int32Array(width * height), result = [];
  let id = 0;
  for (let start = 0; start < labels.length; start++) {
    if (labels[start] || data[start * 4 + 3] < threshold) continue;
    id++; let head = 0, tail = 1, minX = width, minY = height, maxX = -1, maxY = -1, sumX = 0, sumY = 0;
    stack[0] = start; labels[start] = id;
    while (head < tail) {
      const p = stack[head++], x = p % width, y = Math.floor(p / width);
      minX = Math.min(minX, x); maxX = Math.max(maxX, x); minY = Math.min(minY, y); maxY = Math.max(maxY, y); sumX += x; sumY += y;
      for (let oy = -1; oy <= 1; oy++) for (let ox = -1; ox <= 1; ox++) {
        const nx = x + ox, ny = y + oy;
        if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
        const n = ny * width + nx;
        if (!labels[n] && data[n * 4 + 3] >= threshold) { labels[n] = id; stack[tail++] = n; }
      }
    }
    result.push({ id, pixels: tail, x: minX, y: minY, width: maxX - minX + 1, height: maxY - minY + 1, centerX: sumX / tail, centerY: sumY / tail });
  }
  return { data, info, labels, components: result.sort((a, b) => b.pixels - a.pixels) };
}
module.exports = { components };
if (require.main === module) components(process.argv[2], Number(process.argv[3] || 32)).then(r=>console.log(JSON.stringify({ width:r.info.width,height:r.info.height,count:r.components.length,major:r.components.filter(c=>c.pixels>=1000),minorLargest:r.components.filter(c=>c.pixels<1000).slice(0,8) })));
