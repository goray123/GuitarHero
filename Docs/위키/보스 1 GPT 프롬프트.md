# 보스 1 GPT 이미지 프롬프트

ChatGPT에서 보스 1 아트를 만들 때 쓰는 프롬프트 모음이다. 기준은 [보스 1 아트 디렉션](보스%201%20아트%20디렉션.md).
프롬프트는 이미지 모델이 더 정확하게 따르도록 영어로 쓰고, 설명은 한국어로 붙였다.

## 0. 준비

1. ChatGPT에서 **프로젝트**를 하나 만든다 (예: `보스 1 아트`). 아래 "프로젝트 지침"을 프로젝트 설정의 지침 칸에 붙여 넣는다.
   프로젝트를 쓰면 대화를 새로 열어도 스타일 규칙이 유지된다.
2. 프로젝트 파일에 **그림체 참고 이미지**를 올린다.
   - `Assets/Characters/Zombie/Sprites/Idle/Idle_00.png` (외곽선·셀 셰이딩 스타일 기준)
3. **한 단계 = 한 대화**로 진행한다. 시드를 승인한 뒤에는 항상 **승인한 시드 이미지를 다시 첨부**해서 시작한다.
   직전 결과물을 기준으로 계속 고치면 디자인이 조금씩 틀어진다.

### 프로젝트 지침 (그대로 붙여 넣기)

```text
You are producing 2D game art for a side-view action boss-rush game in Unity.
The character is "Boss 1": a dark, heavily armored humanoid combat robot.

STYLE LOCKS (never change these):
- Strict side view, facing RIGHT, unless the request says otherwise.
- Bold dark outline (#0B0D12, about 3px at 1024px), two-tone cel shading, minimal gradients.
- Light comes from the top-left in every image.
- Palette roles: armor base #2A2F3A, armor shadow #161A22, armor highlight #5B6577,
  glowing cyan-teal circuit lines #2EF2E0 in seams and joints, red sensor and missile launcher #FF2E3C,
  thruster flame #FF8A2A to #FFE07A (only when the thrusters fire).
- Matte metal armor with chamfered angular plates, very few scratches or rivets.
- Match the outlined cel-shaded illustration style of the uploaded zombie reference.

DESIGN LOCKS (keep identical across all images once a design is approved):
- Top-heavy inverted-triangle build: huge chest and shoulders, short sturdy legs.
- Two enormous fists, each about the size of the torso. They are the main silhouette.
- A large red missile-launcher box on the back that rises above the head.
- Two thruster nozzles on the lower back.
- Small head with a single glowing red mono-eye sensor.

OUTPUT RULES:
- Transparent background, single subject, whole subject visible with margin, nothing cropped.
- No text, letters, logos, numbers, watermarks, signatures, UI, frames, floor, scenery or extra characters.
- No weapons, wings or capes.
- When I attach an approved image, treat it as the design source of truth and change only what I ask.
```

## 1. 시드 콘셉트 (그림체 결정)

**목표:** 오른쪽을 보는 측면 대기 자세 1장. 이 그림이 이후 모든 작업의 기준이 된다.
같은 프롬프트로 3~4번 생성하고, 아래 "승인 체크"로 하나를 고른다.

```text
Create a concept image of Boss 1.

SUBJECT: Boss 1 standing in a calm, heavy idle pose, full body, strict side view facing right.
The giant fists hang low near the ground. The red missile launcher on its back rises above the head.
The two back thrusters are visible but OFF (no flame).

SCALE: The robot must read as a huge boss, about 3.5 times the height of a normal human.
Keep the head small so the body feels massive.

GAME-SCALE READ: Even when shrunk to 150px tall, the fists, the red launcher and the red eye
must still be recognizable from the silhouette alone.

TECHNICAL: Square 1024x1024, transparent background, feet resting about 60px above the bottom edge,
body horizontally centered, generous margin on all sides.
```

**변형용 한 줄 추가** (후보끼리 차이를 주고 싶을 때 끝에 붙인다)
- `Variation: blockier, more industrial armor, like heavy construction machinery.`
- `Variation: sleeker military armor with more cyan circuit lines.`
- `Variation: the fists are even larger and the legs even shorter.`

**승인 체크**
- [ ] 정확한 측면(3/4 시점이 아님), 오른쪽을 본다
- [ ] 작게 줄여도 주먹·붉은 발사기·붉은 눈이 실루엣으로 읽힌다
- [ ] 배경이 실제로 투명하다 (회색·흰색 체크무늬가 그려진 가짜 투명이 아님)
- [ ] 글자·로고·무늬 같은 문자가 없다
- [ ] 다리가 단순하고, 디테일은 주먹 > 발사기 > 몸통 순으로 많다

승인한 그림은 `Assets/Characters/Boss1/Concept/Boss1_Seed.png`로 저장한다.

## 2. 시드 다듬기 (필요할 때만)

승인 후보가 거의 맞는데 일부만 고치고 싶을 때. **시드 이미지를 첨부**하고 쓴다.

```text
Edit the attached approved Boss 1 image.
CHANGE ONLY: [고칠 점을 한 가지만, 예: make the missile launcher box 20% larger]
KEEP EXACTLY: pose, proportions, palette, outline weight, lighting, facing, framing and transparent background.
```

한 번에 한 가지만 고친다. 여러 개를 동시에 바꾸면 고치지 않은 부분도 같이 변한다.

## 3. 리그용 부위 분리

**목표:** 2D Animation 컷아웃 리그에 쓸 부위를 따로따로 그린다. **시드 이미지를 첨부**하고, 부위 묶음마다 대화를 나눠도 된다.

```text
Using the attached approved Boss 1 image as the exact design reference, create a parts sheet
for a 2D cut-out animation rig.

Draw each part SEPARATELY, not touching, spread out in a grid on a transparent background,
all at the same scale as the reference, same side view facing right, same palette and outline:
1. head (with the red mono-eye)
2. torso / chest armor (without arms, without the launcher)
3. missile launcher box (closed)
4. thruster pack with two nozzles (no flame)
5. upper arm - far side
6. forearm - far side
7. fist - far side (closed)
8. upper arm - near side
9. forearm - near side
10. fist - near side (closed)
11. thigh - far side
12. shin and foot - far side
13. thigh - near side
14. shin and foot - near side
15. pelvis

RIG RULES: Each limb part ends in a rounded overlap at the joint so it can rotate without gaps.
Hidden areas that will be covered by other parts must still be fully painted.
No labels or numbers in the image.
```

부위가 많아서 한 장에 잘 안 나오면 둘로 나눈다.
- 1차: 1~4번 + 15번 (몸통 쪽)
- 2차: 5~14번 (팔·다리)

**추가 상태 부위** (같은 방식, 시드 첨부)

```text
Using the attached approved Boss 1 image as the exact design reference, draw these extra part states
separately on a transparent background, same scale, side view facing right:
1. missile launcher box with its front hatch OPEN, showing missile tips inside
2. missile launcher box tilted back about 30 degrees, hatch open
3. a small missile pod on the side of the head, open, for an aimed shot
4. fist - near side, open hand with spread armored fingers
5. head with the red eye glowing brighter and a red light bloom
No labels or numbers.
```

**확인할 것:** 모든 부위의 크기 비율이 시드와 같은지, 외곽선 두께가 같은지, 색이 시드와 같은지.
저장 위치: `Assets/Characters/Boss1/Rig/` (부위별 PNG 또는 레이어로 합친 PSB)

## 4. 애니메이션 참고 포즈 (선택)

실제 애니메이션은 Unity에서 리그로 만든다. 이 단계는 **포즈 참고용**이라 게임에 들어가지 않는다.
**시드 이미지를 첨부**하고, 아래 `[포즈]`만 바꿔 가며 쓴다.

```text
Using the attached approved Boss 1 image as the exact design reference, draw Boss 1 in this key pose:
[포즈]
Same design, palette, outline and scale. Side view facing right. Transparent background. No motion lines, no text.
```

| 클립 | `[포즈]`에 넣을 문장 |
|---|---|
| 3타 1타 예고 | `winding up a low sweep: crouched low, the near arm pulled far back behind the body, fist near knee height` |
| 3타 1타 판정 | `mid low horizontal sweep: the near fist swinging forward just above the ground, body twisted forward` |
| 3타 2타 예고 | `pulling back for a straight punch: the far arm drawn back behind the shoulder, chest turned` |
| 3타 2타 판정 | `fully extended straight punch at chest height, the far arm locked straight forward` |
| 3타 3타 예고 | `both fists raised high above the head, preparing a double slam` |
| 3타 3타 판정 | `both fists slammed into the ground in front, body bent forward, fists half buried` |
| 돌진 예고 | `crouched very low, leaning forward like a sprinter, thrusters glowing orange as they charge up` |
| 돌진 이동 | `charging forward shoulder-first, low body, thrusters firing long bright flames backward` |
| 도약 예고 | `deep squat, knees bent hard, fists on the ground, thrusters charging` |
| 도약 체공 | `airborne, both fists pulled together above the head, legs tucked, thrusters firing downward` |
| 도약 착지 | `landed with both fists driven into the ground, knees bent, heavy impact pose` |
| 백스텝 | `leaning back and sliding backward, arms raised in guard, thrusters firing forward to push it back` |
| 후방 미사일 | `standing firm, the missile launcher tilted back with its hatch open, firing toward the rear` |
| 미사일 난사 | `standing wide with both arms spread open, the launcher hatch fully open, missiles launching upward` |
| 사망 | `collapsed onto its knees, slumping forward, the red eye and cyan circuits dark` |

## 5. 이펙트

시드는 첨부하지 않는다. 대신 **팔레트 규칙만** 지키게 한다.

```text
Create a 2D game effect sprite on a transparent background, side view, cel-shaded with a bold dark outline
where it makes sense, matching Boss 1's palette. Single effect only, centered, no text.
EFFECT: [이펙트]
```

| 파일 | `[이펙트]` |
|---|---|
| `Boss1_FX_BoosterFlame` | `a horizontal strip of 4 thruster flame states for a back-mounted rocket nozzle pointing left: 1) small glowing ember charge-up, 2) short burst, 3) full long jet flame, 4) fading puff. Colors #FF8A2A to #FFE07A with a white-hot core. Evenly spaced, same nozzle position in each` |
| `Boss1_FX_Missile` | `a small missile flying to the right: red #FF2E3C body, dark gunmetal fins, short orange exhaust flame` |
| `Boss1_FX_Explosion` | `a horizontal strip of 5 explosion frames from flash to smoke, round, red-orange fire with dark smoke, evenly spaced` |
| `Boss1_FX_Shockwave` | `a low, wide ground shockwave wave traveling right, cyan-teal #2EF2E0 energy with white edges, very flat and wide` |
| `Boss1_FX_ImpactDust` | `a ground impact dust burst spreading left and right, grey-brown dust chunks and a few cyan sparks` |

## 6. 저장과 기록

- 파일을 받으면 PNG 그대로 `Assets/Characters/Boss1/` 아래 해당 폴더에 넣는다. 이름은 [에셋 목록](보스%201%20에셋%20목록.json)의 `path`를 따른다.
- **가짜 투명**을 조심한다. 흰 배경이나 체크무늬가 그려져 나오면 ChatGPT에 `Remove the background so it is truly transparent.`로 다시 요청한다.
- 받은 프롬프트(위 문장 그대로)와 날짜를 에셋 목록의 `source.prompt_or_source_record`에 남긴다.
  `source.kind`는 `ai-generated`, `url_or_tool`은 `ChatGPT (GPT Image)`로 적는다.
- 넣은 뒤 Claude에게 "보스 1 시드 검수해 줘"라고 하면 알파·크기·색 수 검사, 비교 시트 생성, 에셋 목록 기록을 이어서 한다.
