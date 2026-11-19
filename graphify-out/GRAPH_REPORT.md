# Graph Report - GuitarHero  (2026-11-19)

## Corpus Check
- 66 files · ~53,971 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 2307 file(s) not represented in the graph (top: .meta 1244, .asset 1002, .anim 28)
- WARNING: 2307 of 2486 files seen (93%) have no supported extractor and are missing from this graph entirely. It may look complete while representing only part of the codebase.

## Summary
- 1420 nodes · 2651 edges · 122 communities (61 shown, 61 thin omitted)
- Extraction: 96% EXTRACTED · 4% INFERRED · 0% AMBIGUOUS · INFERRED: 105 edges (avg confidence: 0.87)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `8b86c611`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- TrainingDummy
- Boss1MissileArt
- Collider2D
- 보스 1 기획서
- unityengine
- Boss1MissileBarrage
- com.unity.2d.common@12.0.4
- com.unity.modules.unitywebrequest@1.0.0
- com.unity.modules.jsonserialize@1.0.0
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
- Boss1RearMissiles
- PlayerAttack
- com.unity.modules.audio@1.0.0
- SpriteRenderer
- com.unity.modules.uielements@1.0.0
- PlayerRhythmGauge
- 리듬 게이지 기획문서
- com.unity.modules.imageconversion@1.0.0
- com.unity.modules.physics@1.0.0
- com.unity.ide.rider
- BossMeleeTimeline
- Boss1ProximityAttack.Tick
- Boss1Backstep
- Boss1Health
- com.unity.2d.tilemap@1.0.0
- Boss1BoosterFlameVisual
- Boss1BoostStep
- PlayerRhythmGaugeUI
- manifest.json
- dependencies
- com.unity.modules.imgui@1.0.0
- MonoBehaviour
- PlayerHealth
- com.unity.modules.physics2d@1.0.0
- PlayerChordStrike
- PlayerHitAudio
- com.unity.collections@2.4.3
- FlameState
- com.unity.modules.subsystems@1.0.0
- HealthBarUI
- 보스 1 구현 스펙 (초안)
- Boss1SlamSpriteVisual
- PlayerHitStop
- Boss1AttackHitboxes
- Sprite
- NozzleUse
- Boss1BoostRushVisual
- Pattern
- Phase
- CombatFeelValidation.CheckInPlayMode
- dependencies
- Phase
- com.unity.ide.visualstudio
- Phase
- ExclusiveAction
- Transform
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
- Stage
- 보스 1 GPT 이미지 프롬프트
- 보스 1 아트 디렉션
- 패턴 상세
- Stage
- 타격감 튜닝
- Q: 이펙터 시스템 우리 설계된게 뭐가 잇었지
- com.unity.modules.ai

## God Nodes (most connected - your core abstractions)
1. `Boss1ProximityAttack` - 80 edges
2. `PlayerMovement` - 70 edges
3. `PlayerAttack` - 59 edges
4. `Boss1AimedMissile` - 53 edges
5. `Boss1BoosterFlameVisual` - 53 edges
6. `Boss1LeapSlam` - 47 edges
7. `Boss1RearMissiles` - 47 edges
8. `Boss1BoostRush` - 44 edges
9. `TrainingDummy` - 38 edges
10. `PlayerRhythmGauge` - 35 edges

## Surprising Connections (you probably didn't know these)
- `현재 값` --references--> `AttackHit`  [INFERRED]
  Docs/위키/타격감 튜닝.md → Assets/Scripts/Combat/AttackHit.cs
- `공중·방향 공격 및 키 설정` --references--> `PlayerAttack`  [INFERRED]
  Docs/위키/타격감 튜닝.md → Assets/Scripts/Player/PlayerAttack.cs
- `로비 구현` --references--> `CityCameraFollow`  [INFERRED]
  Docs/위키/로비 구현.md → Assets/Scripts/Environment/CityCameraFollow.cs
- `2. 기존 코드와의 연결점` --references--> `TrainingDummy`  [INFERRED]
  Docs/위키/보스 1 구현 스펙.md → Assets/Scripts/Characters/TrainingDummy.cs
- `2. 기존 코드와의 연결점` --references--> `ZombieMovement`  [INFERRED]
  Docs/위키/보스 1 구현 스펙.md → Assets/Scripts/Characters/ZombieMovement.cs

## Import Cycles
- None detected.

## Communities (122 total, 61 thin omitted)

### Community 0 - "TrainingDummy"
Cohesion: 0.06
Nodes (14): BodyFront, TrainingDummy, CurrentHealth, IsDead, IsPlayingHit, MaxHealth, ZombieMovement, LobbyInteraction (+6 more)

### Community 1 - "Boss1MissileArt"
Cohesion: 0.15
Nodes (6): Boss1MissileArt, ActiveBursts, Burst, HasArt, HasExplosionArt, HasMissileArt

### Community 2 - "Collider2D"
Cohesion: 0.08
Nodes (18): AttackHit, ComboStep, Damage, Direction, DisplayPoint, Point, ChordNoteProjectile, Damageable (+10 more)

### Community 3 - "보스 1 기획서"
Cohesion: 0.18
Nodes (11): 2페이즈 가속, 개요, 공통 전투 규칙, 반격 창 설계 원칙, 보스 1 기획서, 외형, 이동: 부스트 스텝 (신규), 패턴 수치 초안 (+3 more)

### Community 4 - "unityengine"
Cohesion: 0.09
Nodes (3): 게임의 정체성 v20261110, **기타 히어로**, **총 기획 문서**

### Community 5 - "Boss1MissileBarrage"
Cohesion: 0.19
Nodes (6): Boss1MissileBarrage, CurrentStage, H, IsActive, Rocket, ShotsFired

### Community 6 - "com.unity.2d.common@12.0.4"
Cohesion: 0.06
Nodes (37): dependencies, depth, source, url, version, dependencies, depth, source (+29 more)

### Community 7 - "com.unity.modules.unitywebrequest@1.0.0"
Cohesion: 0.06
Nodes (34): dependencies, depth, source, version, dependencies, depth, source, version (+26 more)

### Community 8 - "com.unity.modules.jsonserialize@1.0.0"
Cohesion: 0.06
Nodes (40): dependencies, depth, source, url, version, dependencies, dependencies, depth (+32 more)

### Community 9 - "PlayerMovement"
Cohesion: 0.12
Nodes (7): PlayerMovement, IsAttacking, IsCrouching, IsOnGround, IsSliding, IsUsingSkill, SlideColliderHeight

### Community 10 - "com.unity.render-pipelines.core"
Cohesion: 0.07
Nodes (29): depth, source, version, dependencies, depth, source, version, dependencies (+21 more)

### Community 11 - "Boss1BoostRush"
Cohesion: 0.10
Nodes (17): Boss1BoostRush, BlocksActions, CooldownRemaining, DesiredSpeed, Facing, Hitbox, HitboxActive, PoseHeight (+9 more)

### Community 12 - "Boss1AimedMissile"
Cohesion: 0.10
Nodes (17): Boss1AimedMissile, AimEnd, AimOrigin, AimVisible, Clock, CurrentPhase, Direction, ExplosionPoint (+9 more)

### Community 13 - "CalculateRhythmGauge.mjs"
Cohesion: 0.09
Nodes (21): attackComponent, attackFields, ceilStep(), fixedStep, fixedStepMatch, outputIndex, parameters, prefabText (+13 more)

### Community 14 - "Boss1ProximityAttack"
Cohesion: 0.08
Nodes (24): Boss1ProximityAttack, AttackSerial, BarrageActive, BarragePending, BodyBusy, EffectiveWindupMultiplier, ExclusiveActionPending, Facing (+16 more)

### Community 15 - "com.unity.nuget.mono-cecil@1.11.5"
Cohesion: 0.08
Nodes (26): dependencies, depth, source, url, version, dependencies, depth, source (+18 more)

### Community 16 - "Boss1LeapSlam"
Cohesion: 0.09
Nodes (20): Boss1LeapSlam, CurrentPhase, H, IsBusy, LandingPoint, MarkerVisible, Phase, Airborne (+12 more)

### Community 18 - "packages-lock.json"
Cohesion: 0.09
Nodes (24): dependencies, depth, source, url, version, dependencies, depth, source (+16 more)

### Community 19 - "com.unity.modules.imgui@1.0.0"
Cohesion: 0.11
Nodes (18): dependencies, depth, source, version, dependencies, depth, source, version (+10 more)

### Community 20 - "Boss1RearMissiles"
Cohesion: 0.12
Nodes (13): Boss1RearMissiles, CooldownRemaining, Exploding, Facing, FlightProgress, HasLiveMissiles, InFlight, IsBusy (+5 more)

### Community 21 - "PlayerAttack"
Cohesion: 0.14
Nodes (6): PlayerAttack, CurrentAttackDamageMultiplier, CurrentAttackSpeedMultiplier, IsAttacking, IsSliding, IsUsingSkill

### Community 22 - "com.unity.modules.audio@1.0.0"
Cohesion: 0.09
Nodes (24): dependencies, depth, source, version, dependencies, depth, source, version (+16 more)

### Community 24 - "com.unity.modules.uielements@1.0.0"
Cohesion: 0.09
Nodes (22): dependencies, depth, source, url, version, dependencies, depth, source (+14 more)

### Community 25 - "PlayerRhythmGauge"
Cohesion: 0.08
Nodes (25): PlayerRhythmGauge, AttackDamageMultiplier, AttackSpeedMultiplier, CurrentGauge, DecayPerSecond, LifeVersion, NormalizedGauge, Stage (+17 more)

### Community 26 - "리듬 게이지 기획문서"
Cohesion: 0.10
Nodes (21): 0에서 시작하는 첫 진입 시간, 10 조정 검토 사항, 11 기능 검증 항목, 1 기획 의도와 구현 범위, 2 단계와 능력치, 3 게이지 획득과 피격, 4 공격 능력치 적용, 5 현재 프로젝트에서 확인한 공격 값 (+13 more)

### Community 27 - "com.unity.modules.imageconversion@1.0.0"
Cohesion: 0.12
Nodes (16): dependencies, depth, source, version, dependencies, depth, source, version (+8 more)

### Community 28 - "com.unity.modules.physics@1.0.0"
Cohesion: 0.08
Nodes (26): dependencies, depth, source, version, dependencies, depth, source, version (+18 more)

### Community 29 - "com.unity.ide.rider"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.ide.rider

### Community 30 - "BossMeleeTimeline"
Cohesion: 0.11
Nodes (9): BossMeleeTimeline, Active, Attack, CurrentStage, Elapsed, Recovery, StageProgress, Total (+1 more)

### Community 32 - "Boss1Backstep"
Cohesion: 0.16
Nodes (9): Boss1Backstep, CooldownRemaining, CurrentPhase, DesiredSpeed, EndPoint, Facing, IsBusy, Progress (+1 more)

### Community 33 - "Boss1Health"
Cohesion: 0.14
Nodes (6): Boss1Health, CurrentHealth, IsDead, IsPhaseTransitionPending, IsPhaseTwo, MaxHealth

### Community 34 - "com.unity.2d.tilemap@1.0.0"
Cohesion: 0.12
Nodes (18): dependencies, depth, dependencies, depth, source, url, version, source (+10 more)

### Community 35 - "Boss1BoosterFlameVisual"
Cohesion: 0.12
Nodes (15): Boss1BoosterFlameVisual, BoundExhaustDirection, CurrentBodySprite, CurrentNozzleUses, CurrentState, ExhaustDirection, FlameRenderer, HasCurrentNozzleBinding (+7 more)

### Community 36 - "Boss1BoostStep"
Cohesion: 0.29
Nodes (5): Boss1BoostStep, CurrentPhase, DesiredSpeed, Facing, IsBusy

### Community 38 - "manifest.json"
Cohesion: 0.12
Nodes (15): com.unity.2d.sprite@1.0.0, com.unity.ai.inference@2.6.1, com.unity.modules.terrain@1.0.0, com.unity.modules.unitywebrequesttexture@1.0.0, com.unity.modules.vectorgraphics@1.0.0, com.unity.modules.wind@1.0.0, com.unity.pipeline@1.0.0, com.unity.2d.sprite (+7 more)

### Community 39 - "dependencies"
Cohesion: 0.12
Nodes (16): dependencies, com.unity.2d.tilemap@1.0.0, com.unity.2d.tooling@1.0.4, com.unity.modules.accessibility@1.0.0, com.unity.modules.androidjni@1.0.0, com.unity.modules.imageconversion@1.0.0, com.unity.modules.screencapture@1.0.0, com.unity.modules.unitywebrequestwww@1.0.0 (+8 more)

### Community 40 - "com.unity.modules.imgui@1.0.0"
Cohesion: 0.08
Nodes (25): dependencies, depth, source, version, dependencies, depth, source, version (+17 more)

### Community 41 - "MonoBehaviour"
Cohesion: 0.18
Nodes (3): CityCameraFollow, CityParallax, CityStageFlow

### Community 42 - "PlayerHealth"
Cohesion: 0.20
Nodes (4): PlayerHealth, CurrentHealth, IsDead, MaxHealth

### Community 43 - "com.unity.modules.physics2d@1.0.0"
Cohesion: 0.17
Nodes (12): dependencies, depth, source, url, version, dependencies, depth, source (+4 more)

### Community 46 - "com.unity.collections@2.4.3"
Cohesion: 0.17
Nodes (12): dependencies, depth, source, url, version, depth, source, url (+4 more)

### Community 47 - "FlameState"
Cohesion: 0.18
Nodes (11): FlameState, BackstepMoving, BackstepWindup, Hidden, LeapAirborne, LeapWindup, Pursuit, RushMoving (+3 more)

### Community 48 - "com.unity.modules.subsystems@1.0.0"
Cohesion: 0.18
Nodes (11): dependencies, depth, source, version, dependencies, depth, source, version (+3 more)

### Community 52 - "보스 1 구현 스펙 (초안)"
Cohesion: 0.22
Nodes (9): 1. 범위, 3. 구성 요소 (새 파일 6개, `Assets/Scripts/Boss/`), 4. 거리 단위 H (실측), 5. 조정 가능한 수치 (전부 인스펙터), 6. 동작 규칙 요약 (구현 기준), 7. 검증, 8. 결정 사항 (2026-11-12, 기획서보다 우선), 가정 (구현하면서 다르면 알려주세요) (+1 more)

### Community 57 - "NozzleUse"
Cohesion: 0.29
Nodes (7): NozzleUse, Backstep, Leap, None, Pursuit, Rush, Step

### Community 59 - "Pattern"
Cohesion: 0.29
Nodes (7): Pattern, Backstep, Leap, Melee, None, RearMissiles, Rush

### Community 60 - "Phase"
Cohesion: 0.33
Nodes (6): Phase, Exploding, Flying, Idle, Locked, Tracking

### Community 62 - "dependencies"
Cohesion: 0.09
Nodes (22): dependencies, depth, source, url, version, dependencies, depth, source (+14 more)

### Community 63 - "Phase"
Cohesion: 0.40
Nodes (5): Phase, Idle, Moving, Preparing, Resting

### Community 64 - "com.unity.ide.visualstudio"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.ide.visualstudio

### Community 65 - "Phase"
Cohesion: 0.40
Nodes (5): Phase, Idle, Moving, Settling, Windup

### Community 66 - "ExclusiveAction"
Cohesion: 0.50
Nodes (4): ExclusiveAction, Barrage, None, PhaseTransition

### Community 68 - "Canvas 체력바"
Cohesion: 0.50
Nodes (3): Canvas 체력바, 동작, 조정

### Community 113 - "Stage"
Cohesion: 0.40
Nodes (5): Stage, Active, Idle, Recovery, Windup

### Community 114 - "보스 1 GPT 이미지 프롬프트"
Cohesion: 0.22
Nodes (9): 0. 준비, 1. 시드 콘셉트 (그림체 결정), 2. 시드 다듬기 (필요할 때만), 3. 리그용 부위 분리, 4. 애니메이션 참고 포즈 (선택), 5. 이펙트, 6. 저장과 기록, 보스 1 GPT 이미지 프롬프트 (+1 more)

### Community 115 - "보스 1 아트 디렉션"
Cohesion: 0.22
Nodes (9): 1. 게임 프레임, 2. 비주얼 시스템, 3. 제작 방식 (제안), 4. 기술 계약, 5. 애니메이션 목록 (패턴 타이밍 기준), 6. 이펙트 목록, 7. 진행 순서, 8. 출처 기록 (+1 more)

### Community 116 - "패턴 상세"
Cohesion: 0.25
Nodes (8): 도약 내려찍기 (신규), 미사일 난사, 보스 3타, 부스터 돌진, 부스터 백스텝 (신규), 조준 고정 미사일, 패턴 상세, 후방 미사일

### Community 117 - "Stage"
Cohesion: 0.33
Nodes (6): Stage, Draining, Firing, Preparing, Recovery, Waiting

### Community 119 - "타격감 튜닝"
Cohesion: 0.33
Nodes (5): 공중·방향 공격 및 키 설정, 자동 검증, 타격감 튜닝, 플레이 확인 순서, 현재 값

### Community 120 - "Q: 이펙터 시스템 우리 설계된게 뭐가 잇었지"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: 이펙터 시스템 우리 설계된게 뭐가 잇었지, Source Nodes

### Community 121 - "com.unity.modules.ai"
Cohesion: 0.40
Nodes (5): dependencies, depth, source, version, com.unity.modules.ai

## Knowledge Gaps
- **216 isolated node(s):** `CurrentHealth`, `IsDead`, `IsPlayingHit`, `MaxHealth`, `CurrentGauge` (+211 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 634 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **61 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `dependencies` connect `dependencies` to `com.unity.2d.common@12.0.4`, `com.unity.modules.unitywebrequest@1.0.0`, `com.unity.modules.jsonserialize@1.0.0`, `com.unity.render-pipelines.core`, `com.unity.nuget.mono-cecil@1.11.5`, `packages-lock.json`, `com.unity.modules.imgui@1.0.0`, `com.unity.modules.audio@1.0.0`, `com.unity.modules.uielements@1.0.0`, `com.unity.modules.imageconversion@1.0.0`, `com.unity.modules.physics@1.0.0`, `com.unity.ide.rider`, `com.unity.2d.tilemap@1.0.0`, `com.unity.modules.imgui@1.0.0`, `com.unity.modules.physics2d@1.0.0`, `com.unity.collections@2.4.3`, `com.unity.modules.subsystems@1.0.0`, `com.unity.ide.visualstudio`, `com.unity.modules.ai`?**
  _High betweenness centrality (0.074) - this node is a cross-community bridge._
- **What connects `CurrentHealth`, `IsDead`, `IsPlayingHit` to the rest of the system?**
  _216 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `TrainingDummy` be split into smaller, more focused modules?**
  _Cohesion score 0.05894736842105263 - nodes in this community are weakly interconnected._
- **Why does `Boss1ProximityAttack` connect `Boss1ProximityAttack` to `TrainingDummy`, `Boss1MissileArt`, `Collider2D`, `Boss1MissileBarrage`, `Boss1BoostRush`, `Boss1AimedMissile`, `Boss1LeapSlam`, `Boss1RearMissiles`, `SpriteRenderer`, `BossMeleeTimeline`, `Boss1ProximityAttack.Tick`, `Boss1Backstep`, `Boss1Health`, `Boss1BoosterFlameVisual`, `Boss1BoostStep`, `MonoBehaviour`, `Boss1SlamSpriteVisual`, `Boss1AttackHitboxes`, `Boss1BoostRushVisual`, `Pattern`, `ExclusiveAction`, `PlayerHitCameraShake`?**
  _High betweenness centrality (0.067) - this node is a cross-community bridge._
- **Should `Collider2D` be split into smaller, more focused modules?**
  _Cohesion score 0.08250355618776671 - nodes in this community are weakly interconnected._
- **Why does `PlayerRhythmGauge` connect `PlayerRhythmGauge` to `TrainingDummy`, `Collider2D`, `unityengine`, `PlayerRhythmGaugeUI`, `MonoBehaviour`, `PlayerAttack`, `SpriteRenderer`?**
  _High betweenness centrality (0.049) - this node is a cross-community bridge._
- **Should `unityengine` be split into smaller, more focused modules?**
  _Cohesion score 0.0919661733615222 - nodes in this community are weakly interconnected._