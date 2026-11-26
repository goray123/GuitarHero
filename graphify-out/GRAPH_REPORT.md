# Graph Report - GuitarHero  (2026-11-26)

## Corpus Check
- 84 files · ~63,412 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 2323 file(s) not represented in the graph (top: .meta 1257, .asset 1002, .anim 28)
- WARNING: 2323 of 2525 files seen (92%) have no supported extractor and are missing from this graph entirely. It may look complete while representing only part of the codebase.

## Summary
- 1586 nodes · 3002 edges · 126 communities (67 shown, 59 thin omitted)
- Extraction: 96% EXTRACTED · 4% INFERRED · 0% AMBIGUOUS · INFERRED: 118 edges (avg confidence: 0.87)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `a51197fe`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- com.unity.modules.physics@1.0.0
- Boss1MissileArt
- AttackHit
- 보스 1 기획서
- unityengine
- Boss1MissileBarrage
- Boss1SlamSpriteVisual
- com.unity.modules.imageconversion@1.0.0
- com.unity.modules.jsonserialize@1.0.0
- PlayerMovement
- com.unity.render-pipelines.core
- Boss1BoostRush
- Boss1AimedMissile
- CalculateRhythmGauge.mjs
- Boss1ProximityAttack
- com.unity.nuget.mono-cecil@1.11.5
- Boss1LeapSlam
- CombatFeelValidation.CheckInPlayMode
- packages-lock.json
- PlayerRhythmGauge
- PlayerAttack
- com.unity.modules.audio@1.0.0
- com.unity.ide.rider
- com.unity.modules.uielements@1.0.0
- Boss1RearMissiles
- 리듬 게이지 기획문서
- com.unity.2d.sprite@1.0.0
- dependencies
- Rigidbody2D
- BossMeleeTimeline
- SpriteRenderer
- Boss1Backstep
- PlayerChordStrike
- com.unity.2d.tilemap@1.0.0
- Boss1BoosterFlameVisual
- HealthBarUI
- PlayerHitAudio
- manifest.json
- dependencies
- com.unity.modules.jsonserialize@1.0.0
- com.unity.modules.screencapture@1.0.0
- Boss1Health
- TrainingDummy
- BossHitboxEditor
- com.unity.modules.physics2d@1.0.0
- Boss1BoostStep
- RushStage
- Vector2
- PlayerHealth
- 타격감 튜닝
- PlayerHitEffects
- 보스 1 구현 스펙 (초안)
- PlayerHitStop
- PlayerDamageEffects
- BoxCollider2D
- Q: 전체적으로 보스패턴에 몸통 판정이 어떻게 들어가있는지 설명해줘
- com.unity.ide.visualstudio
- Q: 보스가 떨어질 때 영화 같은 검은 띠, 큰 이름, 카메라 줌과 쉐이크로 등장 연출을 만들고 싶은데 어떤 것을 추가하면 좋을까
- Pattern
- com.unity.collections@2.4.3
- Q: 전체적으로 모든 패턴의 후딜을 20% 증가시켜주고, 미사일 난사 패턴에서 미사일이 도착지점에서만 피격 판정이 있게 해줘.
- com.unity.modules.terrain@1.0.0
- PlayerRhythmGaugeUI
- ExclusiveAction
- Phase
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
- MonoBehaviour
- Q: 지금 C 대쉬가 평타를 캔슬할 수 있는지 확인해줘
- List
- com.unity.modules.unitywebrequest@1.0.0
- Stage
- PlayerDamageEffectsValidation.CheckInPlayMode
- Q: 내 깃 확인하고 문제해결해서 메인에 푸시해줘
- Q: 이펙터 시스템 우리 설계된게 뭐가 잇었지
- com.unity.modules.subsystems@1.0.0
- Q: 몸통 관련 판정을 그냥 다 빼줘.
- Q: 보스가 왼쪽을 볼 때 히트박스의 몸 쪽 경계를 조정할 수 없는 이유와 수정
- Q: 보스의 패턴 사이사이 간격이 늘어났으면 좋겠어. 넘 짧다. 지금

## God Nodes (most connected - your core abstractions)
1. `Boss1ProximityAttack` - 71 edges
2. `PlayerAttack` - 58 edges
3. `PlayerMovement` - 56 edges
4. `Boss1BoosterFlameVisual` - 50 edges
5. `Boss1AimedMissile` - 47 edges
6. `Boss1LeapSlam` - 46 edges
7. `Boss1BoostRush` - 45 edges
8. `Boss1RearMissiles` - 42 edges
9. `TrainingDummy` - 33 edges
10. `PlayerHealth` - 32 edges

## Surprising Connections (you probably didn't know these)
- `현재 값` --references--> `AttackHit`  [INFERRED]
  Docs/위키/타격감 튜닝.md → Assets/Scripts/Combat/AttackHit.cs
- `공중·방향 공격 및 키 설정` --references--> `PlayerAttack`  [INFERRED]
  Docs/위키/타격감 튜닝.md → Assets/Scripts/Player/PlayerAttack.cs
- `배경·셔터 에셋 출처와 기술 설정` --references--> `ArenaShutter`  [INFERRED]
  Docs/위키/보스 1 씬 진입.md → Assets/Scripts/Environment/ArenaShutter.cs
- `코드 컨벤션 (기존 코드 기준)` --references--> `Damageable`  [INFERRED]
  CLAUDE.md → Assets/Scripts/Combat/Damageable.cs
- `2. 기존 코드와의 연결점` --references--> `Damageable`  [INFERRED]
  Docs/위키/보스 1 구현 스펙.md → Assets/Scripts/Combat/Damageable.cs

## Import Cycles
- None detected.

## Communities (126 total, 59 thin omitted)

### Community 0 - "com.unity.modules.physics@1.0.0"
Cohesion: 0.10
Nodes (21): dependencies, depth, source, version, dependencies, depth, source, version (+13 more)

### Community 1 - "Boss1MissileArt"
Cohesion: 0.16
Nodes (6): Boss1MissileArt, ActiveBursts, Burst, HasArt, HasExplosionArt, HasMissileArt

### Community 2 - "AttackHit"
Cohesion: 0.07
Nodes (18): AttackHit, ComboStep, Damage, Direction, DisplayPoint, Point, ChordNoteProjectile, Damageable (+10 more)

### Community 3 - "보스 1 기획서"
Cohesion: 0.05
Nodes (37): 2페이즈 가속, 개요, 공통 전투 규칙, 도약 내려찍기 (신규), 미사일 난사, 반격 창 설계 원칙, 보스 1 기획서, 보스 3타 (+29 more)

### Community 4 - "unityengine"
Cohesion: 0.11
Nodes (3): 게임의 정체성 v20261110, **기타 히어로**, **총 기획 문서**

### Community 5 - "Boss1MissileBarrage"
Cohesion: 0.18
Nodes (6): Boss1MissileBarrage, CurrentStage, H, IsActive, Rocket, ShotsFired

### Community 7 - "com.unity.modules.imageconversion@1.0.0"
Cohesion: 0.12
Nodes (16): dependencies, depth, source, version, dependencies, depth, source, version (+8 more)

### Community 8 - "com.unity.modules.jsonserialize@1.0.0"
Cohesion: 0.07
Nodes (32): dependencies, depth, source, url, version, dependencies, depth, source (+24 more)

### Community 9 - "PlayerMovement"
Cohesion: 0.12
Nodes (7): PlayerMovement, IsAttacking, IsCrouching, IsOnGround, IsSliding, IsUsingSkill, SlideColliderHeight

### Community 10 - "com.unity.render-pipelines.core"
Cohesion: 0.07
Nodes (29): depth, source, version, dependencies, depth, source, version, dependencies (+21 more)

### Community 11 - "Boss1BoostRush"
Cohesion: 0.13
Nodes (11): Boss1BoostRush, BlocksActions, CooldownRemaining, DesiredSpeed, Facing, Hitbox, HitboxActive, PoseHeight (+3 more)

### Community 12 - "Boss1AimedMissile"
Cohesion: 0.10
Nodes (17): Boss1AimedMissile, AimEnd, AimOrigin, AimVisible, Clock, CurrentPhase, Direction, ExplosionPoint (+9 more)

### Community 13 - "CalculateRhythmGauge.mjs"
Cohesion: 0.09
Nodes (21): attackComponent, attackFields, ceilStep(), fixedStep, fixedStepMatch, outputIndex, parameters, prefabText (+13 more)

### Community 14 - "Boss1ProximityAttack"
Cohesion: 0.07
Nodes (26): Boss1ProximityAttack, AttackSerial, BarrageActive, BarragePending, BodyBusy, EffectiveWindupMultiplier, ExclusiveActionPending, Facing (+18 more)

### Community 15 - "com.unity.nuget.mono-cecil@1.11.5"
Cohesion: 0.08
Nodes (26): dependencies, depth, source, url, version, dependencies, depth, source (+18 more)

### Community 16 - "Boss1LeapSlam"
Cohesion: 0.09
Nodes (21): Boss1LeapSlam, CurrentPhase, H, IsBusy, LandingPoint, MarkerVisible, Phase, Airborne (+13 more)

### Community 18 - "CombatFeelValidation.CheckInPlayMode"
Cohesion: 0.11
Nodes (3): Boss1EncounterValidation, CombatFeelValidation, RhythmGaugeValidation

### Community 19 - "packages-lock.json"
Cohesion: 0.11
Nodes (20): dependencies, depth, source, version, dependencies, depth, source, version (+12 more)

### Community 20 - "PlayerRhythmGauge"
Cohesion: 0.08
Nodes (24): PlayerRhythmGauge, AttackDamageMultiplier, AttackSpeedMultiplier, CurrentGauge, DecayPerSecond, LifeVersion, NormalizedGauge, Stage (+16 more)

### Community 21 - "PlayerAttack"
Cohesion: 0.13
Nodes (7): PlayerAttack, CurrentAttackDamageMultiplier, CurrentAttackSpeedMultiplier, IsAttacking, IsBasicComboAttack, IsSliding, IsUsingSkill

### Community 22 - "com.unity.modules.audio@1.0.0"
Cohesion: 0.09
Nodes (24): dependencies, depth, source, version, dependencies, depth, source, version (+16 more)

### Community 23 - "com.unity.ide.rider"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.ide.rider

### Community 24 - "com.unity.modules.uielements@1.0.0"
Cohesion: 0.09
Nodes (22): dependencies, depth, source, url, version, dependencies, depth, source (+14 more)

### Community 25 - "Boss1RearMissiles"
Cohesion: 0.13
Nodes (13): Boss1RearMissiles, CooldownRemaining, Exploding, Facing, FlightProgress, HasLiveMissiles, InFlight, IsBusy (+5 more)

### Community 26 - "리듬 게이지 기획문서"
Cohesion: 0.10
Nodes (21): 0에서 시작하는 첫 진입 시간, 10 조정 검토 사항, 11 기능 검증 항목, 1 기획 의도와 구현 범위, 2 단계와 능력치, 3 게이지 획득과 피격, 4 공격 능력치 적용, 5 현재 프로젝트에서 확인한 공격 값 (+13 more)

### Community 27 - "com.unity.2d.sprite@1.0.0"
Cohesion: 0.06
Nodes (37): dependencies, depth, source, url, version, dependencies, depth, source (+29 more)

### Community 28 - "dependencies"
Cohesion: 0.07
Nodes (27): dependencies, depth, source, url, version, dependencies, depth, source (+19 more)

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
Cohesion: 0.06
Nodes (28): Boss1BoosterFlameVisual, BoundExhaustDirection, CurrentBodySprite, CurrentNozzleUses, CurrentState, ExhaustDirection, FlameRenderer, FlameState (+20 more)

### Community 36 - "HealthBarUI"
Cohesion: 0.13
Nodes (5): HealthBarUI, DamagePopup, IsSettled, CityParallax, 2. 기존 코드와의 연결점

### Community 38 - "manifest.json"
Cohesion: 0.12
Nodes (15): com.unity.2d.sprite@1.0.0, com.unity.ai.inference@2.6.1, com.unity.modules.terrain@1.0.0, com.unity.modules.unitywebrequesttexture@1.0.0, com.unity.modules.vectorgraphics@1.0.0, com.unity.modules.wind@1.0.0, com.unity.pipeline@1.0.0, com.unity.2d.sprite (+7 more)

### Community 39 - "dependencies"
Cohesion: 0.12
Nodes (16): dependencies, com.unity.2d.tilemap@1.0.0, com.unity.2d.tooling@1.0.4, com.unity.modules.accessibility@1.0.0, com.unity.modules.androidjni@1.0.0, com.unity.modules.imageconversion@1.0.0, com.unity.modules.screencapture@1.0.0, com.unity.modules.unitywebrequestwww@1.0.0 (+8 more)

### Community 40 - "com.unity.modules.jsonserialize@1.0.0"
Cohesion: 0.11
Nodes (20): dependencies, dependencies, depth, source, version, dependencies, depth, dependencies (+12 more)

### Community 41 - "com.unity.modules.screencapture@1.0.0"
Cohesion: 0.09
Nodes (24): dependencies, depth, source, url, version, dependencies, depth, source (+16 more)

### Community 42 - "Boss1Health"
Cohesion: 0.16
Nodes (6): Boss1Health, CurrentHealth, IsDead, IsPhaseTransitionPending, IsPhaseTwo, MaxHealth

### Community 43 - "TrainingDummy"
Cohesion: 0.18
Nodes (5): TrainingDummy, CurrentHealth, IsDead, IsPlayingHit, MaxHealth

### Community 44 - "BossHitboxEditor"
Cohesion: 0.21
Nodes (3): BossHitboxEditor, Boxes, RadiusField

### Community 45 - "com.unity.modules.physics2d@1.0.0"
Cohesion: 0.15
Nodes (13): dependencies, depth, source, url, version, dependencies, depth, source (+5 more)

### Community 46 - "Boss1BoostStep"
Cohesion: 0.16
Nodes (10): Boss1BoostStep, CurrentPhase, DesiredSpeed, Facing, IsBusy, Phase, Idle, Moving (+2 more)

### Community 47 - "RushStage"
Cohesion: 0.33
Nodes (6): RushStage, Braking, Idle, Interval, Moving, Windup

### Community 49 - "PlayerHealth"
Cohesion: 0.17
Nodes (5): PlayerHealth, CurrentHealth, IsDead, IsInvulnerable, MaxHealth

### Community 50 - "타격감 튜닝"
Cohesion: 0.33
Nodes (5): 공중·방향 공격 및 키 설정, 자동 검증, 타격감 튜닝, 플레이 확인 순서, 현재 값

### Community 52 - "보스 1 구현 스펙 (초안)"
Cohesion: 0.18
Nodes (11): 10. 몸통 공격 판정 제거 (2026-11-24), 1. 범위, 3. 구성 요소 (새 파일 6개, `Assets/Scripts/Boss/`), 4. 거리 단위 H (실측), 5. 조정 가능한 수치 (전부 인스펙터), 6. 동작 규칙 요약 (구현 기준), 7. 검증, 8. 결정 사항 (2026-11-12, 기획서보다 우선) (+3 more)

### Community 55 - "BoxCollider2D"
Cohesion: 0.16
Nodes (5): Boss1AttackHitboxes, ActiveAttack, BodyFront, ArenaShutter, StandingColliderHeight

### Community 56 - "Q: 전체적으로 보스패턴에 몸통 판정이 어떻게 들어가있는지 설명해줘"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: 전체적으로 보스패턴에 몸통 판정이 어떻게 들어가있는지 설명해줘, Source Nodes

### Community 57 - "com.unity.ide.visualstudio"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.ide.visualstudio

### Community 58 - "Q: 보스가 떨어질 때 영화 같은 검은 띠, 큰 이름, 카메라 줌과 쉐이크로 등장 연출을 만들고 싶은데 어떤 것을 추가하면 좋을까"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: 보스가 떨어질 때 영화 같은 검은 띠, 큰 이름, 카메라 줌과 쉐이크로 등장 연출을 만들고 싶은데 어떤 것을 추가하면 좋을까, Source Nodes

### Community 59 - "Pattern"
Cohesion: 0.29
Nodes (7): Pattern, Backstep, Leap, Melee, None, RearMissiles, Rush

### Community 60 - "com.unity.collections@2.4.3"
Cohesion: 0.17
Nodes (12): dependencies, depth, source, url, version, depth, source, url (+4 more)

### Community 61 - "Q: 전체적으로 모든 패턴의 후딜을 20% 증가시켜주고, 미사일 난사 패턴에서 미사일이 도착지점에서만 피격 판정이 있게 해줘."
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: 전체적으로 모든 패턴의 후딜을 20% 증가시켜주고, 미사일 난사 패턴에서 미사일이 도착지점에서만 피격 판정이 있게 해줘., Source Nodes

### Community 62 - "com.unity.modules.terrain@1.0.0"
Cohesion: 0.18
Nodes (11): dependencies, depth, source, version, dependencies, depth, source, version (+3 more)

### Community 66 - "ExclusiveAction"
Cohesion: 0.50
Nodes (4): ExclusiveAction, Barrage, None, PhaseTransition

### Community 67 - "Phase"
Cohesion: 0.33
Nodes (6): Phase, Exploding, Flying, Idle, Locked, Tracking

### Community 68 - "Canvas 체력바"
Cohesion: 0.50
Nodes (3): Canvas 체력바, 동작, 조정

### Community 112 - "MonoBehaviour"
Cohesion: 0.07
Nodes (17): Boss1Encounter, CurrentPhase, Phase, Approach, Cleared, Combat, Entrance, CityCameraFollow (+9 more)

### Community 113 - "Q: 지금 C 대쉬가 평타를 캔슬할 수 있는지 확인해줘"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: 지금 C 대쉬가 평타를 캔슬할 수 있는지 확인해줘, Source Nodes

### Community 114 - "List"
Cohesion: 0.13
Nodes (9): NozzleUse, Backstep, Leap, None, Pursuit, Rush, Step, BossCollisionValidation (+1 more)

### Community 115 - "com.unity.modules.unitywebrequest@1.0.0"
Cohesion: 0.06
Nodes (39): dependencies, depth, source, version, dependencies, depth, source, version (+31 more)

### Community 117 - "Stage"
Cohesion: 0.33
Nodes (6): Stage, Draining, Firing, Preparing, Recovery, Waiting

### Community 119 - "Q: 내 깃 확인하고 문제해결해서 메인에 푸시해줘"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: 내 깃 확인하고 문제해결해서 메인에 푸시해줘, Source Nodes

### Community 120 - "Q: 이펙터 시스템 우리 설계된게 뭐가 잇었지"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: 이펙터 시스템 우리 설계된게 뭐가 잇었지, Source Nodes

### Community 121 - "com.unity.modules.subsystems@1.0.0"
Cohesion: 0.18
Nodes (11): dependencies, depth, source, version, dependencies, depth, source, version (+3 more)

### Community 125 - "Q: 몸통 관련 판정을 그냥 다 빼줘."
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: 몸통 관련 판정을 그냥 다 빼줘., Source Nodes

### Community 128 - "Q: 보스가 왼쪽을 볼 때 히트박스의 몸 쪽 경계를 조정할 수 없는 이유와 수정"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: 보스가 왼쪽을 볼 때 히트박스의 몸 쪽 경계를 조정할 수 없는 이유와 수정, Source Nodes

### Community 129 - "Q: 보스의 패턴 사이사이 간격이 늘어났으면 좋겠어. 넘 짧다. 지금"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: 보스의 패턴 사이사이 간격이 늘어났으면 좋겠어. 넘 짧다. 지금, Source Nodes

## Knowledge Gaps
- **229 isolated node(s):** `ActiveBursts`, `HasArt`, `HasExplosionArt`, `HasMissileArt`, `BlocksActions` (+224 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 695 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **59 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Work-memory lessons

**Preferred sources** — corroborated by past sessions; start here.
- `BossHitboxEditor` (2× useful, score=1.874740669)

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Boss1ProximityAttack` connect `Boss1ProximityAttack` to `Boss1Backstep`, `Boss1MissileArt`, `ExclusiveAction`, `Boss1BoosterFlameVisual`, `AttackHit`, `Boss1MissileBarrage`, `Boss1SlamSpriteVisual`, `Boss1Health`, `Boss1BoostRush`, `BossHitboxEditor`, `MonoBehaviour`, `Boss1ProximityAttack.Tick`, `Vector2`, `BoxCollider2D`, `Pattern`, `Rigidbody2D`, `BossMeleeTimeline`, `SpriteRenderer`?**
  _High betweenness centrality (0.059) - this node is a cross-community bridge._
- **What connects `ActiveBursts`, `HasArt`, `HasExplosionArt` to the rest of the system?**
  _229 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `com.unity.modules.physics@1.0.0` be split into smaller, more focused modules?**
  _Cohesion score 0.09523809523809523 - nodes in this community are weakly interconnected._
- **Why does `dependencies` connect `dependencies` to `com.unity.modules.physics@1.0.0`, `com.unity.modules.imageconversion@1.0.0`, `com.unity.modules.jsonserialize@1.0.0`, `com.unity.render-pipelines.core`, `com.unity.nuget.mono-cecil@1.11.5`, `packages-lock.json`, `com.unity.modules.audio@1.0.0`, `com.unity.ide.rider`, `com.unity.modules.uielements@1.0.0`, `com.unity.2d.sprite@1.0.0`, `com.unity.2d.tilemap@1.0.0`, `com.unity.modules.jsonserialize@1.0.0`, `com.unity.modules.screencapture@1.0.0`, `com.unity.modules.physics2d@1.0.0`, `com.unity.ide.visualstudio`, `com.unity.collections@2.4.3`, `com.unity.modules.terrain@1.0.0`, `com.unity.modules.unitywebrequest@1.0.0`, `com.unity.modules.subsystems@1.0.0`?**
  _High betweenness centrality (0.056) - this node is a cross-community bridge._
- **Should `AttackHit` be split into smaller, more focused modules?**
  _Cohesion score 0.07317073170731707 - nodes in this community are weakly interconnected._
- **Why does `보스 1 구현 스펙 (초안)` connect `보스 1 구현 스펙 (초안)` to `보스 1 기획서`, `HealthBarUI`?**
  _High betweenness centrality (0.037) - this node is a cross-community bridge._
- **Should `보스 1 기획서` be split into smaller, more focused modules?**
  _Cohesion score 0.04878048780487805 - nodes in this community are weakly interconnected._