# Graph Report - GuitarHero  (2026-11-24)

## Corpus Check
- 75 files · ~59,466 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 2318 file(s) not represented in the graph (top: .meta 1252, .asset 1002, .anim 28)
- WARNING: 2318 of 2507 files seen (92%) have no supported extractor and are missing from this graph entirely. It may look complete while representing only part of the codebase.

## Summary
- 1527 nodes · 2852 edges · 125 communities (68 shown, 57 thin omitted)
- Extraction: 96% EXTRACTED · 4% INFERRED · 0% AMBIGUOUS · INFERRED: 108 edges (avg confidence: 0.87)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `1f4e8bbf`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- com.unity.modules.terrain@1.0.0
- SpriteRenderer
- AttackHit
- 보스 1 기획서
- unityengine
- Boss1MissileBarrage
- RhythmGaugeValidation
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
- com.unity.modules.imageconversion@1.0.0
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
- TrainingDummy
- BossMeleeTimeline
- PlayerHitAudio
- Boss1Backstep
- PlayerChordStrike
- com.unity.2d.tilemap@1.0.0
- Boss1BoosterFlameVisual
- HealthBarUI
- CombatFeelValidation.CheckInPlayMode
- manifest.json
- dependencies
- com.unity.modules.jsonserialize@1.0.0
- com.unity.modules.screencapture@1.0.0
- Boss1Health
- com.unity.collections@2.4.3
- Vector2
- com.unity.modules.physics2d@1.0.0
- Phase
- FlameState
- Phase
- 기타 히어로 (GuitarHero)
- 보스 1 GPT 이미지 프롬프트
- 보스 1 구현 스펙 (초안)
- RushStage
- 보스 1 아트 디렉션
- BoxCollider2D
- Q: 전체적으로 보스패턴에 몸통 판정이 어떻게 들어가있는지 설명해줘
- NozzleUse
- Boss1BoostRushVisual
- Pattern
- 패턴 상세
- Q: 전체적으로 모든 패턴의 후딜을 20% 증가시켜주고, 미사일 난사 패턴에서 미사일이 도착지점에서만 피격 판정이 있게 해줘.
- 타격감 튜닝
- Sprite
- PlayerRhythmGaugeUI
- com.unity.collab-proxy
- ExclusiveAction
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
- Collider2D
- Stage
- PlayerHealth
- com.unity.ide.visualstudio
- Q: 이펙터 시스템 우리 설계된게 뭐가 잇었지
- com.unity.modules.subsystems@1.0.0
- Q: 몸통 관련 판정을 그냥 다 빼줘.
- Q: 보스가 왼쪽을 볼 때 히트박스의 몸 쪽 경계를 조정할 수 없는 이유와 수정
- com.unity.modules.wind

## God Nodes (most connected - your core abstractions)
1. `Boss1ProximityAttack` - 73 edges
2. `PlayerAttack` - 58 edges
3. `PlayerMovement` - 54 edges
4. `Boss1BoosterFlameVisual` - 50 edges
5. `Boss1LeapSlam` - 48 edges
6. `Boss1BoostRush` - 47 edges
7. `Boss1AimedMissile` - 47 edges
8. `Boss1RearMissiles` - 44 edges
9. `TrainingDummy` - 37 edges
10. `PlayerRhythmGauge` - 33 edges

## Surprising Connections (you probably didn't know these)
- `현재 값` --references--> `AttackHit`  [INFERRED]
  Docs/위키/타격감 튜닝.md → Assets/Scripts/Combat/AttackHit.cs
- `공중·방향 공격 및 키 설정` --references--> `PlayerAttack`  [INFERRED]
  Docs/위키/타격감 튜닝.md → Assets/Scripts/Player/PlayerAttack.cs
- `2. 기존 코드와의 연결점` --references--> `ZombieMovement`  [INFERRED]
  Docs/위키/보스 1 구현 스펙.md → Assets/Scripts/Characters/ZombieMovement.cs
- `2. 기존 코드와의 연결점` --references--> `PlayerHealth`  [INFERRED]
  Docs/위키/보스 1 구현 스펙.md → Assets/Scripts/Player/PlayerHealth.cs
- `3. 구성 요소 (새 파일 6개, `Assets/Scripts/Boss/`)` --references--> `PlayerHealth`  [INFERRED]
  Docs/위키/보스 1 구현 스펙.md → Assets/Scripts/Player/PlayerHealth.cs

## Import Cycles
- None detected.

## Communities (125 total, 57 thin omitted)

### Community 0 - "com.unity.modules.terrain@1.0.0"
Cohesion: 0.18
Nodes (11): dependencies, depth, source, version, dependencies, depth, source, version (+3 more)

### Community 1 - "SpriteRenderer"
Cohesion: 0.08
Nodes (9): Boss1MissileArt, ActiveBursts, Burst, HasArt, HasExplosionArt, HasMissileArt, Boss1PrototypeModel, Boss1SlamSpriteVisual (+1 more)

### Community 2 - "AttackHit"
Cohesion: 0.23
Nodes (6): AttackHit, ComboStep, Damage, Direction, DisplayPoint, Point

### Community 3 - "보스 1 기획서"
Cohesion: 0.18
Nodes (11): 2페이즈 가속, 개요, 공통 전투 규칙, 반격 창 설계 원칙, 보스 1 기획서, 외형, 이동: 부스트 스텝 (신규), 패턴 수치 초안 (+3 more)

### Community 4 - "unityengine"
Cohesion: 0.10
Nodes (3): 게임의 정체성 v20261110, **기타 히어로**, **총 기획 문서**

### Community 5 - "Boss1MissileBarrage"
Cohesion: 0.17
Nodes (6): Boss1MissileBarrage, CurrentStage, H, IsActive, Rocket, ShotsFired

### Community 7 - "com.unity.modules.unitywebrequest@1.0.0"
Cohesion: 0.06
Nodes (40): dependencies, depth, source, version, dependencies, depth, source, version (+32 more)

### Community 8 - "com.unity.modules.jsonserialize@1.0.0"
Cohesion: 0.07
Nodes (30): dependencies, depth, source, url, version, dependencies, depth, source (+22 more)

### Community 9 - "PlayerMovement"
Cohesion: 0.09
Nodes (8): PlayerMovement, IsAttacking, IsCrouching, IsOnGround, IsSliding, IsUsingSkill, SlideColliderHeight, StandingColliderHeight

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
Nodes (27): Boss1ProximityAttack, AttackSerial, BarrageActive, BarragePending, BodyBusy, BodyFront, EffectiveWindupMultiplier, ExclusiveActionPending (+19 more)

### Community 15 - "com.unity.nuget.mono-cecil@1.11.5"
Cohesion: 0.08
Nodes (26): dependencies, depth, source, url, version, dependencies, depth, source (+18 more)

### Community 16 - "Boss1LeapSlam"
Cohesion: 0.11
Nodes (15): Boss1LeapSlam, CurrentPhase, H, IsBusy, LandingPoint, MarkerVisible, Progress, Radius (+7 more)

### Community 18 - "com.unity.modules.imageconversion@1.0.0"
Cohesion: 0.12
Nodes (16): dependencies, depth, source, version, dependencies, depth, source, version (+8 more)

### Community 19 - "packages-lock.json"
Cohesion: 0.11
Nodes (20): dependencies, depth, source, version, dependencies, depth, source, version (+12 more)

### Community 20 - "PlayerRhythmGauge"
Cohesion: 0.08
Nodes (25): PlayerRhythmGauge, AttackDamageMultiplier, AttackSpeedMultiplier, CurrentGauge, DecayPerSecond, LifeVersion, NormalizedGauge, Stage (+17 more)

### Community 21 - "PlayerAttack"
Cohesion: 0.12
Nodes (7): PlayerAttack, CurrentAttackDamageMultiplier, CurrentAttackSpeedMultiplier, IsAttacking, IsBasicComboAttack, IsSliding, IsUsingSkill

### Community 22 - "com.unity.modules.audio@1.0.0"
Cohesion: 0.11
Nodes (18): dependencies, depth, source, version, dependencies, depth, source, version (+10 more)

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
Cohesion: 0.06
Nodes (37): dependencies, depth, source, version, dependencies, depth, source, version (+29 more)

### Community 29 - "TrainingDummy"
Cohesion: 0.13
Nodes (6): TrainingDummy, CurrentHealth, IsDead, IsPlayingHit, MaxHealth, ZombieMovement

### Community 30 - "BossMeleeTimeline"
Cohesion: 0.10
Nodes (14): BossMeleeTimeline, Active, Attack, CurrentStage, Elapsed, Recovery, Stage, Active (+6 more)

### Community 32 - "Boss1Backstep"
Cohesion: 0.07
Nodes (24): Boss1Backstep, CooldownRemaining, CurrentPhase, DesiredSpeed, EndPoint, Facing, IsBusy, Phase (+16 more)

### Community 34 - "com.unity.2d.tilemap@1.0.0"
Cohesion: 0.12
Nodes (17): dependencies, depth, dependencies, depth, source, url, version, source (+9 more)

### Community 35 - "Boss1BoosterFlameVisual"
Cohesion: 0.12
Nodes (15): Boss1BoosterFlameVisual, BoundExhaustDirection, CurrentBodySprite, CurrentNozzleUses, CurrentState, ExhaustDirection, FlameRenderer, HasCurrentNozzleBinding (+7 more)

### Community 36 - "HealthBarUI"
Cohesion: 0.20
Nodes (3): HealthBarUI, DamagePopup, IsSettled

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

### Community 43 - "com.unity.collections@2.4.3"
Cohesion: 0.17
Nodes (12): dependencies, depth, source, url, version, depth, source, url (+4 more)

### Community 45 - "com.unity.modules.physics2d@1.0.0"
Cohesion: 0.10
Nodes (20): dependencies, depth, source, url, version, dependencies, depth, source (+12 more)

### Community 46 - "Phase"
Cohesion: 0.33
Nodes (6): Phase, Airborne, Idle, Impact, Recovery, Windup

### Community 47 - "FlameState"
Cohesion: 0.18
Nodes (11): FlameState, BackstepMoving, BackstepWindup, Hidden, LeapAirborne, LeapWindup, Pursuit, RushMoving (+3 more)

### Community 48 - "Phase"
Cohesion: 0.33
Nodes (6): Phase, Exploding, Flying, Idle, Locked, Tracking

### Community 50 - "기타 히어로 (GuitarHero)"
Cohesion: 0.14
Nodes (11): Damageable, 검증, 게임 디자인 핵심 (구현 판단의 기준), 기술 스택, 기타 히어로 (GuitarHero), 기획 문서 위치, 작업 시 주의, 코드 컨벤션 (기존 코드 기준) (+3 more)

### Community 51 - "보스 1 GPT 이미지 프롬프트"
Cohesion: 0.22
Nodes (9): 0. 준비, 1. 시드 콘셉트 (그림체 결정), 2. 시드 다듬기 (필요할 때만), 3. 리그용 부위 분리, 4. 애니메이션 참고 포즈 (선택), 5. 이펙트, 6. 저장과 기록, 보스 1 GPT 이미지 프롬프트 (+1 more)

### Community 52 - "보스 1 구현 스펙 (초안)"
Cohesion: 0.20
Nodes (10): 10. 몸통 공격 판정 제거 (2026-11-24), 1. 범위, 4. 거리 단위 H (실측), 5. 조정 가능한 수치 (전부 인스펙터), 6. 동작 규칙 요약 (구현 기준), 7. 검증, 8. 결정 사항 (2026-11-12, 기획서보다 우선), 9. 후딜 및 난사 판정 조정 (2026-11-24) (+2 more)

### Community 53 - "RushStage"
Cohesion: 0.33
Nodes (6): RushStage, Braking, Idle, Interval, Moving, Windup

### Community 54 - "보스 1 아트 디렉션"
Cohesion: 0.22
Nodes (9): 1. 게임 프레임, 2. 비주얼 시스템, 3. 제작 방식 (제안), 4. 기술 계약, 5. 애니메이션 목록 (패턴 타이밍 기준), 6. 이펙트 목록, 7. 진행 순서, 8. 출처 기록 (+1 more)

### Community 55 - "BoxCollider2D"
Cohesion: 0.12
Nodes (6): Boss1AttackHitboxes, ActiveAttack, BossHitboxEditor, Boxes, RadiusField, BossHitboxValidation

### Community 56 - "Q: 전체적으로 보스패턴에 몸통 판정이 어떻게 들어가있는지 설명해줘"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: 전체적으로 보스패턴에 몸통 판정이 어떻게 들어가있는지 설명해줘, Source Nodes

### Community 57 - "NozzleUse"
Cohesion: 0.29
Nodes (7): NozzleUse, Backstep, Leap, None, Pursuit, Rush, Step

### Community 59 - "Pattern"
Cohesion: 0.29
Nodes (7): Pattern, Backstep, Leap, Melee, None, RearMissiles, Rush

### Community 60 - "패턴 상세"
Cohesion: 0.25
Nodes (8): 도약 내려찍기 (신규), 미사일 난사, 보스 3타, 부스터 돌진, 부스터 백스텝 (신규), 조준 고정 미사일, 패턴 상세, 후방 미사일

### Community 61 - "Q: 전체적으로 모든 패턴의 후딜을 20% 증가시켜주고, 미사일 난사 패턴에서 미사일이 도착지점에서만 피격 판정이 있게 해줘."
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: 전체적으로 모든 패턴의 후딜을 20% 증가시켜주고, 미사일 난사 패턴에서 미사일이 도착지점에서만 피격 판정이 있게 해줘., Source Nodes

### Community 62 - "타격감 튜닝"
Cohesion: 0.33
Nodes (5): 공중·방향 공격 및 키 설정, 자동 검증, 타격감 튜닝, 플레이 확인 순서, 현재 값

### Community 65 - "com.unity.collab-proxy"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.collab-proxy

### Community 66 - "ExclusiveAction"
Cohesion: 0.50
Nodes (4): ExclusiveAction, Barrage, None, PhaseTransition

### Community 68 - "Canvas 체력바"
Cohesion: 0.50
Nodes (3): Canvas 체력바, 동작, 조정

### Community 112 - "MonoBehaviour"
Cohesion: 0.06
Nodes (10): BluntHitBurst, CityCameraFollow, CityParallax, CityStageFlow, LobbyInteraction, PlayerHitEffects, 로비 구현, 미정 사항 (+2 more)

### Community 113 - "Q: 지금 C 대쉬가 평타를 캔슬할 수 있는지 확인해줘"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: 지금 C 대쉬가 평타를 캔슬할 수 있는지 확인해줘, Source Nodes

### Community 117 - "Stage"
Cohesion: 0.33
Nodes (6): Stage, Draining, Firing, Preparing, Recovery, Waiting

### Community 118 - "PlayerHealth"
Cohesion: 0.06
Nodes (10): PlayerDamageEffectsValidation, ReportPath, PlayerDamageEffects, PlayerHealth, CurrentHealth, IsDead, IsInvulnerable, MaxHealth (+2 more)

### Community 119 - "com.unity.ide.visualstudio"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.ide.visualstudio

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

### Community 130 - "com.unity.modules.wind"
Cohesion: 0.40
Nodes (5): dependencies, depth, source, version, com.unity.modules.wind

## Knowledge Gaps
- **224 isolated node(s):** `Idle`, `Windup`, `Moving`, `Braking`, `Interval` (+219 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 674 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **57 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Work-memory lessons

**Preferred sources** — corroborated by past sessions; start here.
- `Boss1BoostRush` (4× useful, score=3.996469087) _(code changed — re-verify)_
- `BossHitboxEditor` (2× useful, score=1.998556875)

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Boss1ProximityAttack` connect `Boss1ProximityAttack` to `SpriteRenderer`, `ExclusiveAction`, `Boss1BoosterFlameVisual`, `Boss1Health`, `Boss1BoostRush`, `Vector2`, `MonoBehaviour`, `Boss1ProximityAttack.Awake`, `Boss1ProximityAttack.Tick`, `Collider2D`, `BoxCollider2D`, `Boss1BoostRushVisual`, `Pattern`, `TrainingDummy`, `BossMeleeTimeline`?**
  _High betweenness centrality (0.072) - this node is a cross-community bridge._
- **What connects `Idle`, `Windup`, `Moving` to the rest of the system?**
  _224 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `SpriteRenderer` be split into smaller, more focused modules?**
  _Cohesion score 0.07878787878787878 - nodes in this community are weakly interconnected._
- **Why does `dependencies` connect `dependencies` to `com.unity.modules.terrain@1.0.0`, `com.unity.modules.wind`, `com.unity.modules.unitywebrequest@1.0.0`, `com.unity.modules.jsonserialize@1.0.0`, `com.unity.render-pipelines.core`, `com.unity.nuget.mono-cecil@1.11.5`, `com.unity.modules.imageconversion@1.0.0`, `packages-lock.json`, `com.unity.modules.audio@1.0.0`, `com.unity.ide.rider`, `com.unity.modules.uielements@1.0.0`, `com.unity.2d.sprite@1.0.0`, `com.unity.2d.tilemap@1.0.0`, `com.unity.modules.jsonserialize@1.0.0`, `com.unity.modules.screencapture@1.0.0`, `com.unity.collections@2.4.3`, `com.unity.modules.physics2d@1.0.0`, `com.unity.collab-proxy`, `com.unity.ide.visualstudio`, `com.unity.modules.subsystems@1.0.0`?**
  _High betweenness centrality (0.051) - this node is a cross-community bridge._
- **Should `unityengine` be split into smaller, more focused modules?**
  _Cohesion score 0.10241545893719807 - nodes in this community are weakly interconnected._
- **Why does `보스 1 구현 스펙 (초안)` connect `보스 1 구현 스펙 (초안)` to `기타 히어로 (GuitarHero)`, `보스 1 아트 디렉션.md`, `PlayerRhythmGauge`?**
  _High betweenness centrality (0.039) - this node is a cross-community bridge._
- **Should `com.unity.modules.unitywebrequest@1.0.0` be split into smaller, more focused modules?**
  _Cohesion score 0.05641025641025641 - nodes in this community are weakly interconnected._