# Boss 1 v1 — ImageGen prompts

Mode: built-in ImageGen; transparent_background: true.

Character artwork and effects were generated with ImageGen. Complete poses were then extracted and packed into regular Unity sprite cells; no background color was painted over the artwork.

## Actions

```text
Use case: style-transfer / stylized-concept.
Asset type: production-oriented Unity 2D boss CHARACTER ANIMATION SPRITE SHEET, transparent RGBA PNG.
Input image 1 is the character identity and armor reference ONLY. Transform this existing concept into clean side-view game animation sprites. Preserve the same stocky heavy charcoal armored humanoid mech, enormous armored fists, TWO red rectangular shoulder/back missile pods, cyan circuit seams, magenta-red narrow visor, chunky boots. Simplify fine scratches for crisp readable 2D game art. Camera: locked orthographic side-view with slight three-quarter exposure, character consistently FACING RIGHT. Never show front turnaround or rear turnaround.
Canvas and slicing are mandatory: exactly 2048 x 2048 pixels, exactly EIGHT COLUMNS and EIGHT ROWS, 64 individual frames. Each invisible grid cell is exactly 256 x 256. Absolutely NO labels, numbers, text, grid lines, borders, ground line, ground shadows, color panels, backgrounds or checkerboard painted into the image. ACTUAL transparent alpha background around every character. All 64 cells occupied by exactly ONE full-body character pose. Do not merge adjacent cells.
Alignment: same camera scale, armor proportions and light direction in every cell; body root centered at x=128 in each cell, feet on local y=224 baseline. Keep the whole character and each attack INSIDE its own cell with at least 12px transparent padding. Idle robot body from y=48 to y=224; highest raised fist or missile pod stays above y=12. Punch may extend right within x=244. Do NOT let any arm, rocket, effect or shadow touch neighboring cells. Feet/body root stays in place even for walking/dash: game code handles translation. NO floating detached body pieces.
Eight animation rows, read left to right, all with 8 coherent sequential animation poses:
ROW 1 / top: IDLE breathing loop; planted feet, gentle weight and shoulder movement; eyes/cyan lights subtly pulse; first and last poses match closely.
ROW 2: WALK in place loop, deliberate heavy mechanical steps, alternating planted feet; same height and root location, no travel across cells.
ROW 3: LOW SWEEP first combo hit. Frames 1-3 pull a huge fist back and crouch clearly; frames 4-5 sweep the fist forward close to floor below knee height; frames 6-8 recover. Clearly a LOW attack to jump over, not a straight mid punch.
ROW 4: MIDDLE PUNCH second combo hit. Frames 1-3 opposite fist drawn back at chest; frames 4-5 extend long horizontal punch right at torso height; frames 6-8 retract. Leave obvious space below extended arm for the player to slide.
ROW 5: TWO-FIST OVERHEAD SLAM third combo hit. Frames 1-3 both enormous fists lift HIGH ABOVE HEAD; frames 4-5 both arms slam down on floor IN FRONT TO THE RIGHT; frames 6-8 crouched recovery pulling fists off floor. Clear very large silhouette change; not a punch. No separate scenery or giant explosion.
ROW 6: BOOSTER DASH in place. Frames 1-3 lower body and lean toward right, gathering tiny orange booster glow behind at left; frames 4-6 low forward dash posture with small contained orange booster jets; frames 7-8 brake and recover. Do NOT shift the root across the cell.
ROW 7: REAR MISSILE attack. Body facing right throughout, feet planted; back red pods pivot toward LEFT/up-left (behind robot), small contained launch flashes in frames 4-5, recoil and close in 6-8. No traveling detached rockets on this character sheet.
ROW 8 / bottom: PERIODIC MISSILE BARRAGE. Feet planted, arms open outward, twin red pods lift upward/open to prepare in frames 1-3; alternate contained launch flashes/recoil frames 4-6; lower/vent/cool down frames 7-8. This is a distinct periodic full-body attack, NOT a phase transformation or third phase.
Visual style: crisp dark outline, hand-painted cel-shaded 2D action game sprites, restrained cyan and red highlights, strong readable silhouettes. Identical character construction and two pods across ALL cells. No blur, no ghost images, no multiple copies in a cell. Priority: accurate eight-by-eight regular grid, true transparent alpha, consistent scale and baseline, then clear animation poses.
```

## Support

```text
Use case: style-transfer.
Asset type: TRANSPARENT RGBA Unity 2D GAME SPRITE SUPPORT SHEET, companion to the provided boss action sprite sheet.
Image 1 is the original concept identity reference. Image 2 is the ALREADY CHOSEN sprite style and proportions: match image 2 exactly. Same heavy short charcoal armored humanoid robot, twin red shoulder/back missile pods, huge fists, cyan circuits, narrow magenta visor. Locked orthographic side view, slight three-quarter exposure, consistently facing RIGHT.
Layout: exactly EIGHT COLUMNS and FOUR ROWS = 32 distinct isolated animation frames; requested canvas 2048 x 1024, 256 x 256 cells. Every row has eight frames, read left to right. Generous transparent gutters. ALL content must stay comfortably inside its own invisible cell; no frame overlaps any adjacent frame, no body cut off. No letters, numbers, captions, labels, borders, visible grid, background, ground shadow, scene, or checkerboard. Background must be TRUE alpha transparency. This is a usable sprite sheet, not an infographic or a concept turnaround.
ROW 1, HIT REACTION, full-body robot: 1 neutral, 2 flinch head and torso backward, 3 strongest backward recoil with arms pulled back, 4 recoil begins releasing, 5 lower guard, 6 return guard, 7 settle, 8 same neutral as first. Feet remain planted on identical baseline, stable body scale across every frame. No missing limbs, no loose parts.
ROW 2, DEATH, full-body robot: 1 damaged upright slumped, 2 knees bend and visor dims, 3 lowers to knees, 4 pitches forward onto fists, 5 collapses toward the ground, 6 fallen armored body, 7 settles, 8 final inert compact fallen body with visor/cyan lights off. Keep body side-facing RIGHT; final frame is a complete corpse silhouette within the cell. Same two red pods through all eight frames. No large explosion obscuring the corpse, no disappearing robot.
ROW 3, MISSILE FLIGHT effect ONLY, NO ROBOT: a single identical compact red-and-charcoal rocket pointing VERTICALLY UP, cyan trim, orange exhaust underneath. All eight frames keep rocket center and scale EXACTLY fixed; animate only the exhaust flicker. Rocket should be approximately one quarter of a standing robot's height. One projectile per cell, no multiple rockets, no scene or robot. This projectile will be rotated by Unity and reused for homing/aim-locked, rear-fired and barrage missiles.
ROW 4, MISSILE EXPLOSION effect ONLY, NO ROBOT: eight coherent sequential centered explosion frames: 1 tiny bright impact spark, 2 white-hot orange core expands, 3 peak orange-red fireball, 4 round fiery blast expanding, 5 smaller flames and gray smoke, 6 dissipating smoke fragments, 7 faint dispersed smoke, 8 very faint residual sparks/smoke (keep visible so frame is not blank). Every effect centered at exactly the same position, no sideways drifting, no ground debris or background.
Rendering: match chosen sheet's crisp hand-painted 2D cel-shaded game sprite rendering, dark outline, restrained red and cyan accents, consistent camera and anatomy. No photorealism, no blur or ghost images. Full-body row frames share a ground baseline at 224/256. Projectiles and explosions use the center of each cell. True clean alpha transparency with no colored backdrop.
```

