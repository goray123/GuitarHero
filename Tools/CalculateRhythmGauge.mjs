import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');
const rules = [
  { name: 'Low', min: 0, speed: 1, damage: 1, decay: 12.5 },
  { name: 'Normal', min: 250, speed: 1.25, damage: 1, decay: 25 },
  { name: 'High', min: 500, speed: 1.5, damage: 1.25, decay: 50 },
  { name: 'FEVER', min: 750, speed: 2, damage: 1.5, decay: 100 },
];
const parameters = { baseGain: 30, enhancedGainMultiplier: 2, noteGainMultiplier: 0.5, maximum: 1000 };
const prefabText = read('Assets/Prefabs/Player.prefab');
const prefabGuid = read('Assets/Prefabs/Player.prefab.meta').match(/^guid:\s*(\S+)/m)[1];
const attackFields = [
  'basicWindupTime', 'basicActiveTime', 'basicRecoveryTime', 'basicContactDelay',
  'thirdWindupTime', 'thirdActiveTime', 'thirdLandingActiveTime',
  'thirdRecoveryTime', 'thirdContactDelay', 'thirdHopHeight', 'thirdAttackFallSpeed',
];

function component(className, fields) {
  const blocks = prefabText.split(/(?=^--- !u!)/m);
  const block = blocks.find(item => item.includes('Assembly-CSharp::' + className));
  if (!block) throw new Error('Missing prefab component: ' + className);
  const id = block.match(/^--- !u!\d+ &(\d+)/m)[1];
  const values = {};
  for (const field of fields) {
    const match = block.match(new RegExp('^  ' + field + ':\\s*([-+0-9.eE]+)\\s*$', 'm'));
    if (!match || !Number.isFinite(Number(match[1]))) throw new Error('Missing numeric field: ' + field);
    values[field] = Number(match[1]);
  }
  return { id, values };
}

const attackComponent = component('PlayerAttack', attackFields);
const stopComponent = component('PlayerHitStop', ['basicDuration', 'thirdDuration']);
const fixedStepMatch = read('ProjectSettings/TimeManager.asset').match(/Fixed Timestep:\s*([0-9.]+)/);
const fixedStep = Number(fixedStepMatch[1]);

function profile(scenePath) {
  const text = read(scenePath);
  const attack = { ...attackComponent.values };
  const hitStop = { ...stopComponent.values };
  const overridePattern = /- target: \{fileID:\s*(\d+), guid:\s*([a-f0-9]+), type:\s*\d+\}\r?\n\s+propertyPath:\s*(\S+)\r?\n\s+value:\s*([^\r\n]*)/g;
  for (const match of text.matchAll(overridePattern)) {
    if (match[2] !== prefabGuid) continue;
    const target = match[1] === attackComponent.id ? attack : match[1] === stopComponent.id ? hitStop : null;
    if (target && Object.hasOwn(target, match[3])) {
      const value = Number(match[4].trim());
      if (!Number.isFinite(value)) throw new Error('Invalid scene override: ' + match[3]);
      target[match[3]] = value;
    }
  }
  return { scene: scenePath, attack, hitStop };
}

const profiles = [profile('Assets/Scenes/Main.unity'), profile('Assets/Scenes/City01_SilentBlock.unity')];
function stage(gauge) {
  return rules.findLastIndex(rule => gauge >= rule.min - 1e-9);
}
function ceilStep(value) {
  return Math.ceil((value - 1e-10) / fixedStep) * fixedStep;
}

// Continuous estimate: standing three-hit combo with all hits landing on one target.
// Third attack uses windup + fall + landing-active + recovery, without adding the
// original active time again. Height / fall speed is an approximation, not a physics replay.
// Quantized estimate rounds attack completion, windup, fall and landing tail to 20 ms.
// Hit-stop duration is not divided by attack speed. Input, display-frame and collision
// latency are not simulated. These are analytical projections, not Unity measurements.
function timings(p, speed, model) {
  const a = p.attack;
  const fall = a.thirdHopHeight / a.thirdAttackFallSpeed;
  const basicRaw = (a.basicWindupTime + a.basicActiveTime + a.basicRecoveryTime) / speed;
  const thirdWindup = a.thirdWindupTime / speed;
  const thirdTail = (a.thirdLandingActiveTime + a.thirdRecoveryTime) / speed;
  const quantized = model === 'quantized';
  const basic = quantized ? ceilStep(basicRaw) : basicRaw;
  const third = quantized
    ? ceilStep(thirdWindup) + ceilStep(fall) + ceilStep(thirdTail)
    : thirdWindup + fall + thirdTail;
  const basicContact = a.basicWindupTime / speed + Math.min(a.basicContactDelay / speed, a.basicActiveTime / speed * 0.5);
  const thirdContact = thirdWindup + Math.max(fall, Math.min(a.thirdContactDelay / speed, a.thirdActiveTime / speed * 0.5));
  return {
    durations: [basic, basic, third],
    contacts: [basicContact, basicContact, thirdContact].map((contact, i) =>
      Math.min(quantized ? ceilStep(contact) : contact, [basic, basic, third][i])),
    stops: [p.hitStop.basicDuration, p.hitStop.basicDuration, p.hitStop.thirdDuration],
  };
}

function rates(p, model, attackUptime = 1, gain = parameters.baseGain) {
  return rules.map(rule => {
    const t = timings(p, rule.speed, model);
    const attackGameSeconds = t.durations.reduce((sum, value) => sum + value, 0);
    const gameSeconds = attackGameSeconds / attackUptime;
    const stopSeconds = t.stops.reduce((sum, value) => sum + value, 0);
    const realSeconds = gameSeconds + stopSeconds;
    return {
      stage: rule.name,
      speed: rule.speed,
      comboGameSeconds: attackGameSeconds,
      comboRealSeconds: attackGameSeconds + stopSeconds,
      hitsPerRealSecond: 3 / realSeconds,
      grossGainPerRealSecond: 3 * gain / realSeconds,
      decayPerRealSecond: rule.decay * gameSeconds / realSeconds,
      netGainPerRealSecond: (3 * gain - rule.decay * gameSeconds) / realSeconds,
      // Alternative clock: decay also runs during hit stop.
      netWithUnscaledDecay: 3 * gain / realSeconds - rule.decay,
      minimumAttackUptime: rule.decay * attackGameSeconds / (3 * gain),
    };
  });
}

function simulate(p, {
  model = 'continuous', attackUptime = 1, targets = 1, gain = parameters.baseGain,
  startGauge = 0, realLimit = 120,
} = {}) {
  let gauge = startGauge, gameTime = 0, realTime = 0, hits = 0, comboIndex = 0;
  let comboGameSeconds = 0;
  const first = {};
  const firstAfterHit = {};
  const events = [];
  const snapshots = [];
  const occupancy = Object.fromEntries(rules.map(rule => [rule.name, 0]));
  const attackStartCounts = Object.fromEntries(rules.map(rule => [rule.name, 0]));
  let firstFeverAttackRealSeconds = null;
  let firstFeverExit = null;
  let everFever = gauge >= 750;
  if (everFever) first.FEVER = 0;
  for (const rule of rules) if (gauge >= rule.min && rule.min > 0) first[rule.name] = 0;

  function advance(delta) {
    let left = delta;
    if (left > 1e-9 && everFever && firstFeverExit === null && gauge <= 750 + 1e-9)
      firstFeverExit = realTime;
    while (left > 1e-9) {
      if (gauge < 1e-9) {
        occupancy.Low += left;
        gauge = 0; gameTime += left; realTime += left; left = 0;
        break;
      }
      // Descending at an exact boundary immediately enters the lower interval.
      let index = rules.findLastIndex(rule => gauge > rule.min + 1e-9);
      if (index < 0) index = 0;
      const rule = rules[index];
      const toBoundary = (gauge - rule.min) / rule.decay;
      const used = Math.min(left, toBoundary);
      occupancy[rule.name] += used;
      gauge = Math.max(0, gauge - rule.decay * used);
      gameTime += used; realTime += used; left -= used;
      if (everFever && firstFeverExit === null && gauge <= 750 + 1e-9)
        firstFeverExit = realTime;
      if (used < 1e-12) throw new Error('Decay integration made no progress');
    }
  }

  while (realTime < realLimit && hits < 10000) {
    const rule = rules[stage(gauge)];
    attackStartCounts[rule.name] += 1;
    if (rule.name === 'FEVER' && firstFeverAttackRealSeconds === null)
      firstFeverAttackRealSeconds = realTime;
    const t = timings(p, rule.speed, model);
    const duration = t.durations[comboIndex];
    const contact = t.contacts[comboIndex];
    comboGameSeconds += duration;
    advance(contact);
    const before = gauge;
    gauge = Math.min(parameters.maximum, gauge + gain * targets);
    hits += 1;
    const event = { hit: hits, realSeconds: realTime, gameSeconds: gameTime, before, after: gauge, attackSpeed: rule.speed };
    events.push(event);
    for (const threshold of rules.slice(1)) {
      if (gauge >= threshold.min && first[threshold.name] === undefined) {
        first[threshold.name] = realTime;
        firstAfterHit[threshold.name] = hits;
      }
    }
    if (gauge >= 750) everFever = true;
    // Hit stop pauses both gameplay and gauge decay.
    occupancy[rules[stage(gauge)].name] += t.stops[comboIndex];
    realTime += t.stops[comboIndex];
    advance(duration - contact);
    comboIndex = (comboIndex + 1) % 3;
    if (comboIndex === 0) {
      advance(comboGameSeconds * (1 / attackUptime - 1));
      comboGameSeconds = 0;
      snapshots.push({ realSeconds: realTime, gameSeconds: gameTime, gauge });
    }
  }
  return {
    model, attackUptime, targets, gain, firstThresholdRealSeconds: first,
    firstThresholdHitNumber: firstAfterHit, firstFeverExitRealSeconds: firstFeverExit,
    firstFeverDurationRealSeconds: first.FEVER === undefined || firstFeverExit === null
      ? null : firstFeverExit - first.FEVER,
    finalGauge: gauge, simulatedRealSeconds: realTime, hits, occupancy,
    attackStartCounts, firstFeverAttackRealSeconds,
    firstFeverEvent: events.find(event => event.after >= 750) ?? null,
    eventsNearFirstFever: first.FEVER === undefined ? [] :
      events.filter(event => event.realSeconds >= first.FEVER - 1 && event.realSeconds <= first.FEVER + 5),
    comboSamples: snapshots.filter((_, index) => index % 5 === 0),
  };
}

const reports = profiles.map(p => ({
  ...p,
  continuousRates: rates(p, 'continuous'),
  quantizedRates: rates(p, 'quantized'),
  uptimeRates: [0.5, 0.75, 1].map(attackUptime => ({
    attackUptime, rates: rates(p, 'continuous', attackUptime),
  })),
  simulations: ['continuous', 'quantized'].flatMap(model =>
    [1, 0.75, 0.5].map(attackUptime => simulate(p, { model, attackUptime }))),
  twoTargetSimulation: simulate(p, { targets: 2 }),
  feverStartedAt1000: ['continuous', 'quantized'].map(model =>
    simulate(p, { model, startGauge: 1000, realLimit: 30 })),
}));

let lastActiveScene = null;
const sceneSetup = path.join(root, 'Library/LastSceneManagerSetup.txt');
if (fs.existsSync(sceneSetup)) {
  const setup = fs.readFileSync(sceneSetup, 'utf8');
  lastActiveScene = setup.match(/- path:\s*([^\r\n]+)\r?\n\s+isLoaded:\s*1\r?\n\s+isActive:\s*1/)?.[1] ?? null;
}
const report = {
  sourceDocument: 'https://docs.google.com/document/d/1tj7w9QywV0V6_dDQxrGJIo9g6Vg4bA6ol6bAeSSuZLc/edit',
  assumptions: {
    timeBase: 'Decay uses scaled gameplay time; hit stop pauses decay',
    speedApplication: 'Snapshot speed at attack start; divide attack sections and contact delays by speed',
    targets: 'One target, successful damage once per attack; no incoming damage, skill or enhanced attacks',
    uptime: 'Fraction of gameplay time spent attacking; downtime is inserted after each full combo',
    landing: 'Standing third attack adds height / fall speed; quantized is a separate 20 ms rounding approximation',
    notMeasured: 'Not a Unity Play-mode measurement; terrain, collision and display-frame delays can change results',
  },
  fixedStep, lastActiveScene, rules, parameters, profiles: reports,
};
const outputIndex = process.argv.indexOf('--output');
if (outputIndex !== -1) {
  const outputPath = path.resolve(process.argv[outputIndex + 1]);
  fs.mkdirSync(path.dirname(outputPath), { recursive: true });
  fs.writeFileSync(outputPath, JSON.stringify(report, null, 2) + '\n');
} else {
  process.stdout.write(JSON.stringify(report, null, 2) + '\n');
}
