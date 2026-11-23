# Graph Report - GuitarHero  (2026-11-23)

## Corpus Check
- 67 files · ~55,809 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 2311 file(s) not represented in the graph (top: .meta 1247, .asset 1002, .anim 28)
- WARNING: 2311 of 2492 files seen (93%) have no supported extractor and are missing from this graph entirely. It may look complete while representing only part of the codebase.

## Summary
- 1452 nodes · 2714 edges · 120 communities (59 shown, 61 thin omitted)
- Extraction: 96% EXTRACTED · 4% INFERRED · 0% AMBIGUOUS · INFERRED: 109 edges (avg confidence: 0.87)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `731c2c78`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- CombatFeelValidation.CheckInPlayMode
- Boss1RearMissiles
- AttackHit
- 보스 1 기획서
- Boss1MissileBarrage
- com.unity.2d.common@12.0.4
- com.unity.modules.unitywebrequest@1.0.0
- com.unity.mathematics@1.2.6
- PlayerMovement
- com.unity.render-pipelines.core
- Boss1BoostRush
- Boss1AimedMissile
- CalculateRhythmGauge.mjs
- Boss1ProximityAttack
- com.unity.nuget.mono-cecil@1.11.5
- Boss1LeapSlam
- Vector2
- packages-lock.json
- com.unity.modules.imgui@1.0.0
- TrainingDummy
- PlayerAttack
- com.unity.modules.audio@1.0.0
- com.unity.modules.imgui@1.0.0
- com.unity.modules.uielements@1.0.0
- com.unity.modules.jsonserialize@1.0.0
- 리듬 게이지 기획문서
- com.unity.modules.imageconversion@1.0.0
- com.unity.modules.physics@1.0.0
- com.unity.ide.rider
- BossMeleeTimeline
- Boss1Backstep
- com.unity.2d.tilemap@1.0.0
- Boss1BoosterFlameVisual
- Boss1BoostStep
- PlayerHealth
- manifest.json
- dependencies
- com.unity.test-framework@1.4.6
- CityCameraFollow
- Rigidbody2D
- com.unity.modules.physics2d@1.0.0
- PlayerChordStrike
- LobbyInteraction
- FlameState
- com.unity.modules.subsystems@1.0.0
- BossHitboxEditor
- 기타 히어로 (GuitarHero)
- 보스 1 GPT 이미지 프롬프트
- 보스 1 구현 스펙 (초안)
- Collider2D
- 보스 1 아트 디렉션
- BoxCollider2D
- RushStage
- NozzleUse
- Boss1BoostRushVisual
- Pattern
- 패턴 상세
- ChordNoteProjectile
- dependencies
- Sprite
- com.unity.ide.visualstudio
- com.unity.collab-proxy
- ExclusiveAction
- PlayerRhythmGauge.cs
- Canvas 체력바
- com.unity.2d.animation@13.0.6
- com.unity.2d.aseprite@3.0.2
- com.unity.2d.psdimporter@12.0.2
- com.unity.2d.spriteshape@13.0.0
- com.unity.2d.tilemap.extras@6.0.3
- com.unity.ai.assistant@2.21.0-pre.1
- com.unity.collab-proxy@2.13.6
- com.unity.ide.rider@3.0.40
- com.unity.ide.visualstudio@2.0.26
- com.unity.inputsystem@1.20.0
- com.unity.modules.adaptiveperformance@1.0.0
- com.unity.modules.ai@1.0.0
- com.unity.modules.animation@1.0.0
- com.unity.modules.assetbundle@1.0.0
- com.unity.modules.audio@1.0.0
- com.unity.modules.cloth@1.0.0
- com.unity.modules.director@1.0.0
- com.unity.modules.imgui@1.0.0
- com.unity.modules.jsonserialize@1.0.0
- com.unity.modules.particlesystem@1.0.0
- com.unity.modules.physics@1.0.0
- com.unity.modules.physics2d@1.0.0
- com.unity.modules.terrainphysics@1.0.0
- com.unity.modules.tilemap@1.0.0
- com.unity.modules.ui@1.0.0
- com.unity.modules.uielements@1.0.0
- com.unity.modules.umbra@1.0.0
- com.unity.modules.unityanalytics@1.0.0
- com.unity.modules.unitywebrequest@1.0.0
- com.unity.modules.unitywebrequestassetbundle@1.0.0
- com.unity.modules.unitywebrequestaudio@1.0.0
- com.unity.modules.vehicles@1.0.0
- com.unity.modules.video@1.0.0
- com.unity.modules.vr@1.0.0
- com.unity.modules.xr@1.0.0
- com.unity.multiplayer.center@1.0.1
- com.unity.render-pipelines.universal@17.3.0
- com.unity.timeline@1.8.13
- com.unity.ugui@2.0.0
- com.unity.visualscripting@1.9.12
- PlayerHitCameraShake
- LobbyValidation.Run
- 게임의 정체성 v20261110.md
- Stage
- Q: 이펙터 시스템 우리 설계된게 뭐가 잇었지

## God Nodes (most connected - your core abstractions)
1. `Boss1ProximityAttack` - 84 edges
2. `PlayerMovement` - 74 edges
3. `PlayerAttack` - 61 edges
4. `Boss1LeapSlam` - 50 edges
5. `Boss1BoosterFlameVisual` - 50 edges
6. `Boss1AimedMissile` - 49 edges
7. `Boss1BoostRush` - 47 edges
8. `Boss1RearMissiles` - 46 edges
9. `TrainingDummy` - 37 edges
10. `Boss1AttackHitboxes` - 36 edges

## Surprising Connections (you probably didn't know these)
- `공중·방향 공격 및 키 설정` --references--> `PlayerAttack`  [INFERRED]
  Docs/위키/타격감 튜닝.md → Assets/Scripts/Player/PlayerAttack.cs
- `로비 구현` --references--> `TrainingDummy`  [INFERRED]
  Docs/위키/로비 구현.md → Assets/Scripts/Characters/TrainingDummy.cs
- `로비 구현` --references--> `CityCameraFollow`  [INFERRED]
  Docs/위키/로비 구현.md → Assets/Scripts/Environment/CityCameraFollow.cs
- `2. 기존 코드와의 연결점` --references--> `HealthBarUI`  [INFERRED]
  Docs/위키/보스 1 구현 스펙.md → Assets/Scripts/Characters/HealthBarUI.cs
- `2. 기존 코드와의 연결점` --references--> `ZombieMovement`  [INFERRED]
  Docs/위키/보스 1 구현 스펙.md → Assets/Scripts/Characters/ZombieMovement.cs

## Import Cycles
- None detected.

## Communities (120 total, 61 thin omitted)

### Community 1 - "Boss1RearMissiles"
Cohesion: 0.05
Nodes (22): Boss1MissileArt, ActiveBursts, Burst, HasArt, HasExplosionArt, HasMissileArt, Boss1PrototypeModel, Boss1RearMissiles (+14 more)

### Community 2 - "AttackHit"
Cohesion: 0.13
Nodes (11): AttackHit, ComboStep, Damage, Direction, DisplayPoint, Point, 공중·방향 공격 및 키 설정, 자동 검증 (+3 more)

### Community 3 - "보스 1 기획서"
Cohesion: 0.18
Nodes (11): 2페이즈 가속, 개요, 공통 전투 규칙, 반격 창 설계 원칙, 보스 1 기획서, 외형, 이동: 부스트 스텝 (신규), 패턴 수치 초안 (+3 more)

### Community 5 - "Boss1MissileBarrage"
Cohesion: 0.21
Nodes (6): Boss1MissileBarrage, CurrentStage, H, IsActive, Rocket, ShotsFired

### Community 6 - "com.unity.2d.common@12.0.4"
Cohesion: 0.05
Nodes (49): dependencies, depth, source, url, version, dependencies, depth, source (+41 more)

### Community 7 - "com.unity.modules.unitywebrequest@1.0.0"
Cohesion: 0.08
Nodes (29): dependencies, depth, source, version, dependencies, depth, source, version (+21 more)

### Community 8 - "com.unity.mathematics@1.2.6"
Cohesion: 0.10
Nodes (21): dependencies, depth, source, url, version, dependencies, depth, source (+13 more)

### Community 9 - "PlayerMovement"
Cohesion: 0.10
Nodes (7): PlayerMovement, IsAttacking, IsCrouching, IsOnGround, IsSliding, IsUsingSkill, SlideColliderHeight

### Community 10 - "com.unity.render-pipelines.core"
Cohesion: 0.07
Nodes (34): depth, source, version, depth, source, version, dependencies, depth (+26 more)

### Community 11 - "Boss1BoostRush"
Cohesion: 0.13
Nodes (11): Boss1BoostRush, BlocksActions, CooldownRemaining, DesiredSpeed, Facing, Hitbox, HitboxActive, PoseHeight (+3 more)

### Community 12 - "Boss1AimedMissile"
Cohesion: 0.06
Nodes (24): Boss1AimedMissile, AimEnd, AimOrigin, AimVisible, Clock, CurrentPhase, Direction, ExplosionPoint (+16 more)

### Community 13 - "CalculateRhythmGauge.mjs"
Cohesion: 0.09
Nodes (21): attackComponent, attackFields, ceilStep(), fixedStep, fixedStepMatch, outputIndex, parameters, prefabText (+13 more)

### Community 14 - "Boss1ProximityAttack"
Cohesion: 0.06
Nodes (26): Boss1ProximityAttack, AttackSerial, BarrageActive, BarragePending, BodyBusy, EffectiveWindupMultiplier, ExclusiveActionPending, Facing (+18 more)

### Community 15 - "com.unity.nuget.mono-cecil@1.11.5"
Cohesion: 0.08
Nodes (26): dependencies, depth, source, url, version, dependencies, depth, source (+18 more)

### Community 16 - "Boss1LeapSlam"
Cohesion: 0.09
Nodes (21): Boss1LeapSlam, CurrentPhase, H, IsBusy, LandingPoint, MarkerVisible, Phase, Airborne (+13 more)

### Community 18 - "packages-lock.json"
Cohesion: 0.08
Nodes (24): dependencies, depth, source, url, version, dependencies, depth, source (+16 more)

### Community 19 - "com.unity.modules.imgui@1.0.0"
Cohesion: 0.11
Nodes (18): dependencies, depth, source, version, dependencies, depth, source, version (+10 more)

### Community 20 - "TrainingDummy"
Cohesion: 0.05
Nodes (29): TrainingDummy, CurrentHealth, IsDead, IsPlayingHit, MaxHealth, PlayerHitStop, IsStopped, PlayerRhythmGauge (+21 more)

### Community 21 - "PlayerAttack"
Cohesion: 0.12
Nodes (7): PlayerAttack, CurrentAttackDamageMultiplier, CurrentAttackSpeedMultiplier, IsAttacking, IsBasicComboAttack, IsSliding, IsUsingSkill

### Community 22 - "com.unity.modules.audio@1.0.0"
Cohesion: 0.09
Nodes (24): dependencies, depth, source, version, dependencies, depth, source, version (+16 more)

### Community 23 - "com.unity.modules.imgui@1.0.0"
Cohesion: 0.08
Nodes (25): dependencies, depth, source, version, dependencies, depth, source, version (+17 more)

### Community 24 - "com.unity.modules.uielements@1.0.0"
Cohesion: 0.12
Nodes (16): dependencies, depth, source, url, version, depth, source, version (+8 more)

### Community 25 - "com.unity.modules.jsonserialize@1.0.0"
Cohesion: 0.12
Nodes (16): dependencies, depth, source, version, dependencies, depth, source, version (+8 more)

### Community 26 - "리듬 게이지 기획문서"
Cohesion: 0.10
Nodes (21): 0에서 시작하는 첫 진입 시간, 10 조정 검토 사항, 11 기능 검증 항목, 1 기획 의도와 구현 범위, 2 단계와 능력치, 3 게이지 획득과 피격, 4 공격 능력치 적용, 5 현재 프로젝트에서 확인한 공격 값 (+13 more)

### Community 27 - "com.unity.modules.imageconversion@1.0.0"
Cohesion: 0.12
Nodes (17): dependencies, depth, source, version, dependencies, dependencies, depth, source (+9 more)

### Community 28 - "com.unity.modules.physics@1.0.0"
Cohesion: 0.10
Nodes (21): dependencies, depth, source, version, dependencies, depth, source, version (+13 more)

### Community 29 - "com.unity.ide.rider"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.ide.rider

### Community 30 - "BossMeleeTimeline"
Cohesion: 0.10
Nodes (14): BossMeleeTimeline, Active, Attack, CurrentStage, Elapsed, Recovery, Stage, Active (+6 more)

### Community 32 - "Boss1Backstep"
Cohesion: 0.12
Nodes (14): Boss1Backstep, CooldownRemaining, CurrentPhase, DesiredSpeed, EndPoint, Facing, IsBusy, Phase (+6 more)

### Community 34 - "com.unity.2d.tilemap@1.0.0"
Cohesion: 0.12
Nodes (17): dependencies, depth, dependencies, depth, source, url, version, source (+9 more)

### Community 35 - "Boss1BoosterFlameVisual"
Cohesion: 0.12
Nodes (15): Boss1BoosterFlameVisual, BoundExhaustDirection, CurrentBodySprite, CurrentNozzleUses, CurrentState, ExhaustDirection, FlameRenderer, HasCurrentNozzleBinding (+7 more)

### Community 36 - "Boss1BoostStep"
Cohesion: 0.13
Nodes (10): Boss1BoostStep, CurrentPhase, DesiredSpeed, Facing, IsBusy, Phase, Idle, Moving (+2 more)

### Community 37 - "PlayerHealth"
Cohesion: 0.05
Nodes (15): Boss1Health, CurrentHealth, IsDead, IsPhaseTransitionPending, IsPhaseTwo, MaxHealth, HealthBarUI, DamagePopup (+7 more)

### Community 38 - "manifest.json"
Cohesion: 0.12
Nodes (15): com.unity.2d.sprite@1.0.0, com.unity.ai.inference@2.6.1, com.unity.modules.terrain@1.0.0, com.unity.modules.unitywebrequesttexture@1.0.0, com.unity.modules.vectorgraphics@1.0.0, com.unity.modules.wind@1.0.0, com.unity.pipeline@1.0.0, com.unity.2d.sprite (+7 more)

### Community 39 - "dependencies"
Cohesion: 0.12
Nodes (16): dependencies, com.unity.2d.tilemap@1.0.0, com.unity.2d.tooling@1.0.4, com.unity.modules.accessibility@1.0.0, com.unity.modules.androidjni@1.0.0, com.unity.modules.imageconversion@1.0.0, com.unity.modules.screencapture@1.0.0, com.unity.modules.unitywebrequestwww@1.0.0 (+8 more)

### Community 40 - "com.unity.test-framework@1.4.6"
Cohesion: 0.17
Nodes (13): dependencies, depth, dependencies, depth, source, url, version, source (+5 more)

### Community 43 - "com.unity.modules.physics2d@1.0.0"
Cohesion: 0.15
Nodes (13): dependencies, depth, source, url, version, dependencies, depth, source (+5 more)

### Community 46 - "LobbyInteraction"
Cohesion: 0.22
Nodes (4): LobbyInteraction, 로비 구현, 미정 사항, 배경 에셋 출처와 생성 프롬프트

### Community 47 - "FlameState"
Cohesion: 0.18
Nodes (11): FlameState, BackstepMoving, BackstepWindup, Hidden, LeapAirborne, LeapWindup, Pursuit, RushMoving (+3 more)

### Community 48 - "com.unity.modules.subsystems@1.0.0"
Cohesion: 0.18
Nodes (11): dependencies, depth, source, version, dependencies, depth, source, version (+3 more)

### Community 49 - "BossHitboxEditor"
Cohesion: 0.28
Nodes (3): BossHitboxEditor, Boxes, RadiusField

### Community 50 - "기타 히어로 (GuitarHero)"
Cohesion: 0.15
Nodes (10): Damageable, 검증, 게임 디자인 핵심 (구현 판단의 기준), 기술 스택, 기타 히어로 (GuitarHero), 기획 문서 위치, 작업 시 주의, 코드 컨벤션 (기존 코드 기준) (+2 more)

### Community 51 - "보스 1 GPT 이미지 프롬프트"
Cohesion: 0.22
Nodes (9): 0. 준비, 1. 시드 콘셉트 (그림체 결정), 2. 시드 다듬기 (필요할 때만), 3. 리그용 부위 분리, 4. 애니메이션 참고 포즈 (선택), 5. 이펙트, 6. 저장과 기록, 보스 1 GPT 이미지 프롬프트 (+1 more)

### Community 52 - "보스 1 구현 스펙 (초안)"
Cohesion: 0.22
Nodes (9): 1. 범위, 3. 구성 요소 (새 파일 6개, `Assets/Scripts/Boss/`), 4. 거리 단위 H (실측), 5. 조정 가능한 수치 (전부 인스펙터), 6. 동작 규칙 요약 (구현 기준), 7. 검증, 8. 결정 사항 (2026-11-12, 기획서보다 우선), 가정 (구현하면서 다르면 알려주세요) (+1 more)

### Community 54 - "보스 1 아트 디렉션"
Cohesion: 0.22
Nodes (9): 1. 게임 프레임, 2. 비주얼 시스템, 3. 제작 방식 (제안), 4. 기술 계약, 5. 애니메이션 목록 (패턴 타이밍 기준), 6. 이펙트 목록, 7. 진행 순서, 8. 출처 기록 (+1 more)

### Community 55 - "BoxCollider2D"
Cohesion: 0.18
Nodes (4): Boss1AttackHitboxes, ActiveAttack, BodyFront, StandingColliderHeight

### Community 56 - "RushStage"
Cohesion: 0.33
Nodes (6): RushStage, Braking, Idle, Interval, Moving, Windup

### Community 57 - "NozzleUse"
Cohesion: 0.29
Nodes (7): NozzleUse, Backstep, Leap, None, Pursuit, Rush, Step

### Community 59 - "Pattern"
Cohesion: 0.29
Nodes (7): Pattern, Backstep, Leap, Melee, None, RearMissiles, Rush

### Community 60 - "패턴 상세"
Cohesion: 0.25
Nodes (8): 도약 내려찍기 (신규), 미사일 난사, 보스 3타, 부스터 돌진, 부스터 백스텝 (신규), 조준 고정 미사일, 패턴 상세, 후방 미사일

### Community 62 - "dependencies"
Cohesion: 0.10
Nodes (21): dependencies, depth, source, version, dependencies, depth, source, version (+13 more)

### Community 64 - "com.unity.ide.visualstudio"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.ide.visualstudio

### Community 65 - "com.unity.collab-proxy"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.collab-proxy

### Community 66 - "ExclusiveAction"
Cohesion: 0.50
Nodes (4): ExclusiveAction, Barrage, None, PhaseTransition

### Community 67 - "PlayerRhythmGauge.cs"
Cohesion: 0.40
Nodes (4): RhythmHitKind, Basic, Enhanced, Note

### Community 68 - "Canvas 체력바"
Cohesion: 0.50
Nodes (3): Canvas 체력바, 동작, 조정

### Community 114 - "게임의 정체성 v20261110.md"
Cohesion: 0.50
Nodes (3): 게임의 정체성 v20261110, **기타 히어로**, **총 기획 문서**

### Community 117 - "Stage"
Cohesion: 0.33
Nodes (6): Stage, Draining, Firing, Preparing, Recovery, Waiting

### Community 120 - "Q: 이펙터 시스템 우리 설계된게 뭐가 잇었지"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: 이펙터 시스템 우리 설계된게 뭐가 잇었지, Source Nodes

## Knowledge Gaps
- **222 isolated node(s):** `Idle`, `Tracking`, `Locked`, `Flying`, `Exploding` (+217 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 647 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **61 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Boss1ProximityAttack` connect `Boss1ProximityAttack` to `Boss1Backstep`, `Boss1RearMissiles`, `ExclusiveAction`, `Boss1BoosterFlameVisual`, `Boss1BoostStep`, `PlayerHealth`, `Boss1MissileBarrage`, `Rigidbody2D`, `Boss1BoostRush`, `Boss1AimedMissile`, `Boss1LeapSlam`, `PlayerHitCameraShake`, `BossHitboxEditor`, `Collider2D`, `BoxCollider2D`, `Boss1BoostRushVisual`, `Pattern`, `Boss1ProximityAttack.Tick`?**
  _High betweenness centrality (0.070) - this node is a cross-community bridge._
- **What connects `Idle`, `Tracking`, `Locked` to the rest of the system?**
  _222 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Boss1RearMissiles` be split into smaller, more focused modules?**
  _Cohesion score 0.05160628844839371 - nodes in this community are weakly interconnected._
- **Why does `dependencies` connect `dependencies` to `com.unity.2d.common@12.0.4`, `com.unity.modules.unitywebrequest@1.0.0`, `com.unity.mathematics@1.2.6`, `com.unity.render-pipelines.core`, `com.unity.nuget.mono-cecil@1.11.5`, `packages-lock.json`, `com.unity.modules.imgui@1.0.0`, `com.unity.modules.audio@1.0.0`, `com.unity.modules.imgui@1.0.0`, `com.unity.modules.uielements@1.0.0`, `com.unity.modules.jsonserialize@1.0.0`, `com.unity.modules.imageconversion@1.0.0`, `com.unity.modules.physics@1.0.0`, `com.unity.ide.rider`, `com.unity.2d.tilemap@1.0.0`, `com.unity.test-framework@1.4.6`, `com.unity.modules.physics2d@1.0.0`, `com.unity.modules.subsystems@1.0.0`, `com.unity.ide.visualstudio`, `com.unity.collab-proxy`?**
  _High betweenness centrality (0.060) - this node is a cross-community bridge._
- **Should `AttackHit` be split into smaller, more focused modules?**
  _Cohesion score 0.1323529411764706 - nodes in this community are weakly interconnected._
- **Why does `PlayerMovement` connect `PlayerMovement` to `CombatFeelValidation.CheckInPlayMode`, `Boss1RearMissiles`, `AttackHit`, `unityengine`, `Boss1MissileBarrage`, `Boss1BoostRush`, `Boss1AimedMissile`, `Boss1LeapSlam`, `TrainingDummy`, `PlayerAttack`, `Boss1Backstep`, `Boss1BoostStep`, `PlayerHealth`, `Rigidbody2D`, `PlayerChordStrike`, `Collider2D`, `BoxCollider2D`, `PlayerHitCameraShake`, `LobbyValidation.Run`?**
  _High betweenness centrality (0.049) - this node is a cross-community bridge._
- **Should `com.unity.2d.common@12.0.4` be split into smaller, more focused modules?**
  _Cohesion score 0.04591836734693878 - nodes in this community are weakly interconnected._