# Graph Report - GuitarHero  (2026-11-27)

## Corpus Check
- 90 files · ~69,564 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 2338 file(s) not represented in the graph (top: .meta 1271, .asset 1002, .anim 28)
- WARNING: 2338 of 2553 files seen (92%) have no supported extractor and are missing from this graph entirely. It may look complete while representing only part of the codebase.

## Summary
- 1681 nodes · 3315 edges · 134 communities (71 shown, 63 thin omitted)
- Extraction: 96% EXTRACTED · 4% INFERRED · 0% AMBIGUOUS · INFERRED: 147 edges (avg confidence: 0.87)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `af46c6a5`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- dependencies
- Boss1MissileArt
- AttackVisualsValidation.CheckInPlayMode
- 보스 1 기획서
- unityengine
- Boss1MissileBarrage
- Boss1SlamSpriteVisual
- Boss1EntrancePresentation
- com.unity.modules.jsonserialize@1.0.0
- PlayerMovement
- com.unity.render-pipelines.core
- Boss1BoostRush
- Boss1AimedMissile
- CalculateRhythmGauge.mjs
- Boss1ProximityAttack
- com.unity.nuget.mono-cecil@1.11.5
- Boss1LeapSlam
- RhythmGaugeValidation
- packages-lock.json
- PlayerRhythmGauge
- PlayerAttack
- com.unity.modules.audio@1.0.0
- com.unity.ext.nunit
- com.unity.modules.uielements@1.0.0
- Boss1RearMissiles
- 리듬 게이지 기획문서
- com.unity.collections@2.4.3
- com.unity.collab-proxy
- Collider2D
- BossMeleeTimeline
- SpriteRenderer
- Boss1Backstep
- BoxCollider2D
- com.unity.2d.tilemap@1.0.0
- Boss1BoosterFlameVisual
- PlayerChordStrike
- PlayerHitAudio
- manifest.json
- dependencies
- com.unity.modules.jsonserialize@1.0.0
- com.unity.modules.imageconversion@1.0.0
- Boss1Health
- TrainingDummy
- BossHitboxEditor
- AttackHit
- Boss1BoostStep
- RushStage
- Vector2
- PlayerHealth
- PlayerHitCameraShake
- com.unity.2d.common@12.0.4
- 보스 1 구현 스펙 (초안)
- MonoBehaviour
- PlayerDamageEffects
- PlayerDamageEffectsValidation.CheckInPlayMode
- Q: 전체적으로 보스패턴에 몸통 판정이 어떻게 들어가있는지 설명해줘
- PlayerHitStop
- Q: 보스가 떨어질 때 영화 같은 검은 띠, 큰 이름, 카메라 줌과 쉐이크로 등장 연출을 만들고 싶은데 어떤 것을 추가하면 좋을까
- Pattern
- com.unity.dt.app-ui@1.3.6
- Q: 전체적으로 모든 패턴의 후딜을 20% 증가시켜주고, 미사일 난사 패턴에서 미사일이 도착지점에서만 피격 판정이 있게 해줘.
- com.unity.modules.physics@1.0.0
- Boss1EncounterValidation.Run
- CombatFeelValidation.CheckInPlayMode
- ExclusiveAction
- com.unity.modules.subsystems@1.0.0
- 기타 히어로 (GuitarHero)
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
- Boss1Encounter
- Q: 지금 C 대쉬가 평타를 캔슬할 수 있는지 확인해줘
- List
- com.unity.modules.unitywebrequest@1.0.0
- Stage
- FlameState
- Q: 내 깃 확인하고 문제해결해서 메인에 푸시해줘
- Q: 이펙터 시스템 우리 설계된게 뭐가 잇었지
- 타격감 튜닝
- Q: 몸통 관련 판정을 그냥 다 빼줘.
- Q: 보스가 왼쪽을 볼 때 히트박스의 몸 쪽 경계를 조정할 수 없는 이유와 수정
- Q: 보스의 패턴 사이사이 간격이 늘어났으면 좋겠어. 넘 짧다. 지금
- NozzleUse
- Boss1BoostRushVisual
- Phase
- Q: ㅇㅋ 좋아. 그렇게 구현해줘. 보스 이름은 메가 메카트론 JT8-3이야
- Phase
- Phase
- LobbyInteraction

## God Nodes (most connected - your core abstractions)
1. `Boss1ProximityAttack` - 67 edges
2. `PlayerAttack` - 60 edges
3. `PlayerMovement` - 59 edges
4. `Boss1BoosterFlameVisual` - 50 edges
5. `Boss1BoostRush` - 44 edges
6. `Boss1LeapSlam` - 44 edges
7. `Boss1AimedMissile` - 43 edges
8. `Boss1RearMissiles` - 39 edges
9. `Boss1Health` - 37 edges
10. `Boss1EntrancePresentation` - 37 edges

## Surprising Connections (you probably didn't know these)
- `배경·셔터 에셋 출처와 기술 설정` --references--> `ArenaShutter`  [INFERRED]
  Docs/위키/보스 1 씬 진입.md → Assets/Scripts/Environment/ArenaShutter.cs
- `공중·방향 공격 및 키 설정` --references--> `PlayerAttack`  [INFERRED]
  Docs/위키/타격감 튜닝.md → Assets/Scripts/Player/PlayerAttack.cs
- `현재 값` --references--> `AttackHit`  [INFERRED]
  Docs/위키/타격감 튜닝.md → Assets/Scripts/Combat/AttackHit.cs
- `보스 1 씬 진입` --references--> `Boss1Health`  [INFERRED]
  Docs/위키/보스 1 씬 진입.md → Assets/Scripts/Characters/Boss1Health.cs
- `보스 1 씬 진입` --references--> `Boss1SlamSpriteVisual`  [INFERRED]
  Docs/위키/보스 1 씬 진입.md → Assets/Scripts/Characters/Boss1SlamSpriteVisual.cs

## Import Cycles
- None detected.

## Communities (134 total, 63 thin omitted)

### Community 0 - "dependencies"
Cohesion: 0.07
Nodes (31): dependencies, depth, source, version, dependencies, depth, source, version (+23 more)

### Community 1 - "Boss1MissileArt"
Cohesion: 0.11
Nodes (7): Boss1MissileArt, ActiveBursts, Burst, HasArt, HasExplosionArt, HasMissileArt, BluntHitBurst

### Community 2 - "AttackVisualsValidation.CheckInPlayMode"
Cohesion: 0.10
Nodes (3): EnemyHitFlash, PlayerAttackTrail, AttackVisualsValidation

### Community 3 - "보스 1 기획서"
Cohesion: 0.05
Nodes (37): 2페이즈 가속, 개요, 공통 전투 규칙, 도약 내려찍기 (신규), 미사일 난사, 반격 창 설계 원칙, 보스 1 기획서, 보스 3타 (+29 more)

### Community 4 - "unityengine"
Cohesion: 0.10
Nodes (3): 게임의 정체성 v20261110, **기타 히어로**, **총 기획 문서**

### Community 5 - "Boss1MissileBarrage"
Cohesion: 0.22
Nodes (6): Boss1MissileBarrage, CurrentStage, H, IsActive, Rocket, ShotsFired

### Community 6 - "Boss1SlamSpriteVisual"
Cohesion: 0.26
Nodes (3): Boss1SlamSpriteVisual, BodyRenderer, EntranceCompression

### Community 8 - "com.unity.modules.jsonserialize@1.0.0"
Cohesion: 0.07
Nodes (30): dependencies, depth, source, url, version, dependencies, depth, source (+22 more)

### Community 9 - "PlayerMovement"
Cohesion: 0.11
Nodes (8): PlayerMovement, IsAttacking, IsCrouching, IsOnGround, IsSliding, IsUsingSkill, SlideColliderHeight, SlideCooldownFraction

### Community 10 - "com.unity.render-pipelines.core"
Cohesion: 0.07
Nodes (29): depth, source, version, dependencies, depth, source, version, dependencies (+21 more)

### Community 11 - "Boss1BoostRush"
Cohesion: 0.14
Nodes (11): Boss1BoostRush, BlocksActions, CooldownRemaining, DesiredSpeed, Facing, Hitbox, HitboxActive, PoseHeight (+3 more)

### Community 12 - "Boss1AimedMissile"
Cohesion: 0.08
Nodes (23): Boss1AimedMissile, AimEnd, AimOrigin, AimVisible, Clock, CurrentPhase, Direction, ExplosionPoint (+15 more)

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

### Community 19 - "packages-lock.json"
Cohesion: 0.11
Nodes (20): dependencies, depth, source, version, dependencies, depth, source, version (+12 more)

### Community 20 - "PlayerRhythmGauge"
Cohesion: 0.08
Nodes (24): PlayerRhythmGauge, AttackDamageMultiplier, AttackSpeedMultiplier, CurrentGauge, DecayPerSecond, LifeVersion, NormalizedGauge, Stage (+16 more)

### Community 21 - "PlayerAttack"
Cohesion: 0.11
Nodes (10): PlayerAttack, CurrentAttackDamageMultiplier, CurrentAttackHitbox, CurrentAttackSpeedMultiplier, IsAttacking, IsBasicComboAttack, IsSliding, IsUsingSkill (+2 more)

### Community 22 - "com.unity.modules.audio@1.0.0"
Cohesion: 0.09
Nodes (24): dependencies, depth, source, version, dependencies, depth, source, version (+16 more)

### Community 23 - "com.unity.ext.nunit"
Cohesion: 0.15
Nodes (13): dependencies, depth, source, version, dependencies, depth, source, url (+5 more)

### Community 24 - "com.unity.modules.uielements@1.0.0"
Cohesion: 0.09
Nodes (22): dependencies, depth, source, url, version, dependencies, depth, source (+14 more)

### Community 25 - "Boss1RearMissiles"
Cohesion: 0.10
Nodes (15): Boss1AttackHitboxes, ActiveAttack, Boss1RearMissiles, CooldownRemaining, Exploding, Facing, FlightProgress, HasLiveMissiles (+7 more)

### Community 26 - "리듬 게이지 기획문서"
Cohesion: 0.10
Nodes (21): 0에서 시작하는 첫 진입 시간, 10 조정 검토 사항, 11 기능 검증 항목, 1 기획 의도와 구현 범위, 2 단계와 능력치, 3 게이지 획득과 피격, 4 공격 능력치 적용, 5 현재 프로젝트에서 확인한 공격 값 (+13 more)

### Community 27 - "com.unity.collections@2.4.3"
Cohesion: 0.17
Nodes (12): dependencies, depth, source, url, version, depth, source, url (+4 more)

### Community 28 - "com.unity.collab-proxy"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.collab-proxy

### Community 30 - "BossMeleeTimeline"
Cohesion: 0.11
Nodes (14): BossMeleeTimeline, Active, Attack, CurrentStage, Elapsed, Recovery, Stage, Active (+6 more)

### Community 32 - "Boss1Backstep"
Cohesion: 0.17
Nodes (9): Boss1Backstep, CooldownRemaining, CurrentPhase, DesiredSpeed, EndPoint, Facing, IsBusy, Progress (+1 more)

### Community 33 - "BoxCollider2D"
Cohesion: 0.14
Nodes (3): BodyFront, ArenaShutter, StandingColliderHeight

### Community 34 - "com.unity.2d.tilemap@1.0.0"
Cohesion: 0.09
Nodes (24): dependencies, depth, dependencies, depth, source, url, version, source (+16 more)

### Community 35 - "Boss1BoosterFlameVisual"
Cohesion: 0.09
Nodes (16): Boss1BoosterFlameVisual, BoundExhaustDirection, CurrentBodySprite, CurrentNozzleUses, CurrentState, ExhaustDirection, FlameRenderer, HasCurrentNozzleBinding (+8 more)

### Community 38 - "manifest.json"
Cohesion: 0.12
Nodes (15): com.unity.2d.sprite@1.0.0, com.unity.ai.inference@2.6.1, com.unity.modules.terrain@1.0.0, com.unity.modules.unitywebrequesttexture@1.0.0, com.unity.modules.vectorgraphics@1.0.0, com.unity.modules.wind@1.0.0, com.unity.pipeline@1.0.0, com.unity.2d.sprite (+7 more)

### Community 39 - "dependencies"
Cohesion: 0.12
Nodes (16): dependencies, com.unity.2d.tilemap@1.0.0, com.unity.2d.tooling@1.0.4, com.unity.modules.accessibility@1.0.0, com.unity.modules.androidjni@1.0.0, com.unity.modules.imageconversion@1.0.0, com.unity.modules.screencapture@1.0.0, com.unity.modules.unitywebrequestwww@1.0.0 (+8 more)

### Community 40 - "com.unity.modules.jsonserialize@1.0.0"
Cohesion: 0.11
Nodes (19): dependencies, dependencies, depth, source, url, version, depth, dependencies (+11 more)

### Community 41 - "com.unity.modules.imageconversion@1.0.0"
Cohesion: 0.12
Nodes (16): dependencies, depth, source, version, dependencies, depth, source, version (+8 more)

### Community 42 - "Boss1Health"
Cohesion: 0.05
Nodes (16): Boss1Health, CurrentHealth, DisplayName, IsDead, IsPhaseTransitionPending, IsPhaseTwo, MaxHealth, NameFont (+8 more)

### Community 43 - "TrainingDummy"
Cohesion: 0.18
Nodes (5): TrainingDummy, CurrentHealth, IsDead, IsPlayingHit, MaxHealth

### Community 44 - "BossHitboxEditor"
Cohesion: 0.17
Nodes (4): BossHitboxEditor, Boxes, RadiusField, BossHitboxValidation

### Community 45 - "AttackHit"
Cohesion: 0.20
Nodes (6): AttackHit, ComboStep, Damage, Direction, DisplayPoint, Point

### Community 46 - "Boss1BoostStep"
Cohesion: 0.22
Nodes (5): Boss1BoostStep, CurrentPhase, DesiredSpeed, Facing, IsBusy

### Community 47 - "RushStage"
Cohesion: 0.33
Nodes (6): RushStage, Braking, Idle, Interval, Moving, Windup

### Community 49 - "PlayerHealth"
Cohesion: 0.18
Nodes (5): PlayerHealth, CurrentHealth, IsDead, IsInvulnerable, MaxHealth

### Community 51 - "com.unity.2d.common@12.0.4"
Cohesion: 0.06
Nodes (38): dependencies, depth, source, url, version, dependencies, depth, source (+30 more)

### Community 52 - "보스 1 구현 스펙 (초안)"
Cohesion: 0.13
Nodes (14): Damageable, 코드 컨벤션 (기존 코드 기준), 10. 몸통 공격 판정 제거 (2026-11-24), 1. 범위, 2. 기존 코드와의 연결점, 3. 구성 요소 (새 파일 6개, `Assets/Scripts/Boss/`), 4. 거리 단위 H (실측), 5. 조정 가능한 수치 (전부 인스펙터) (+6 more)

### Community 53 - "MonoBehaviour"
Cohesion: 0.15
Nodes (5): CityCameraFollow, HorizontalBounds, ViewCamera, CityParallax, CityStageFlow

### Community 56 - "Q: 전체적으로 보스패턴에 몸통 판정이 어떻게 들어가있는지 설명해줘"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: 전체적으로 보스패턴에 몸통 판정이 어떻게 들어가있는지 설명해줘, Source Nodes

### Community 58 - "Q: 보스가 떨어질 때 영화 같은 검은 띠, 큰 이름, 카메라 줌과 쉐이크로 등장 연출을 만들고 싶은데 어떤 것을 추가하면 좋을까"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: 보스가 떨어질 때 영화 같은 검은 띠, 큰 이름, 카메라 줌과 쉐이크로 등장 연출을 만들고 싶은데 어떤 것을 추가하면 좋을까, Source Nodes

### Community 59 - "Pattern"
Cohesion: 0.29
Nodes (7): Pattern, Backstep, Leap, Melee, None, RearMissiles, Rush

### Community 60 - "com.unity.dt.app-ui@1.3.6"
Cohesion: 0.08
Nodes (25): dependencies, depth, source, url, version, dependencies, depth, source (+17 more)

### Community 61 - "Q: 전체적으로 모든 패턴의 후딜을 20% 증가시켜주고, 미사일 난사 패턴에서 미사일이 도착지점에서만 피격 판정이 있게 해줘."
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: 전체적으로 모든 패턴의 후딜을 20% 증가시켜주고, 미사일 난사 패턴에서 미사일이 도착지점에서만 피격 판정이 있게 해줘., Source Nodes

### Community 62 - "com.unity.modules.physics@1.0.0"
Cohesion: 0.07
Nodes (27): dependencies, depth, source, version, dependencies, depth, source, version (+19 more)

### Community 66 - "ExclusiveAction"
Cohesion: 0.50
Nodes (4): ExclusiveAction, Barrage, None, PhaseTransition

### Community 67 - "com.unity.modules.subsystems@1.0.0"
Cohesion: 0.18
Nodes (11): dependencies, depth, source, version, dependencies, depth, source, version (+3 more)

### Community 68 - "기타 히어로 (GuitarHero)"
Cohesion: 0.22
Nodes (8): 검증, 게임 디자인 핵심 (구현 판단의 기준), 기술 스택, 기타 히어로 (GuitarHero), 기획 문서 위치, 작업 시 주의, 팀 개발 방식, 폴더 구조

### Community 112 - "Boss1Encounter"
Cohesion: 0.19
Nodes (4): Boss1Encounter, CurrentPhase, 배경·셔터 에셋 출처와 기술 설정, 보스 1 씬 진입

### Community 113 - "Q: 지금 C 대쉬가 평타를 캔슬할 수 있는지 확인해줘"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: 지금 C 대쉬가 평타를 캔슬할 수 있는지 확인해줘, Source Nodes

### Community 115 - "com.unity.modules.unitywebrequest@1.0.0"
Cohesion: 0.06
Nodes (40): dependencies, depth, source, version, dependencies, depth, source, version (+32 more)

### Community 117 - "Stage"
Cohesion: 0.33
Nodes (6): Stage, Draining, Firing, Preparing, Recovery, Waiting

### Community 118 - "FlameState"
Cohesion: 0.18
Nodes (11): FlameState, BackstepMoving, BackstepWindup, Hidden, LeapAirborne, LeapWindup, Pursuit, RushMoving (+3 more)

### Community 119 - "Q: 내 깃 확인하고 문제해결해서 메인에 푸시해줘"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: 내 깃 확인하고 문제해결해서 메인에 푸시해줘, Source Nodes

### Community 120 - "Q: 이펙터 시스템 우리 설계된게 뭐가 잇었지"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: 이펙터 시스템 우리 설계된게 뭐가 잇었지, Source Nodes

### Community 121 - "타격감 튜닝"
Cohesion: 0.33
Nodes (5): 공중·방향 공격 및 키 설정, 자동 검증, 타격감 튜닝, 플레이 확인 순서, 현재 값

### Community 125 - "Q: 몸통 관련 판정을 그냥 다 빼줘."
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: 몸통 관련 판정을 그냥 다 빼줘., Source Nodes

### Community 128 - "Q: 보스가 왼쪽을 볼 때 히트박스의 몸 쪽 경계를 조정할 수 없는 이유와 수정"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: 보스가 왼쪽을 볼 때 히트박스의 몸 쪽 경계를 조정할 수 없는 이유와 수정, Source Nodes

### Community 129 - "Q: 보스의 패턴 사이사이 간격이 늘어났으면 좋겠어. 넘 짧다. 지금"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: 보스의 패턴 사이사이 간격이 늘어났으면 좋겠어. 넘 짧다. 지금, Source Nodes

### Community 130 - "NozzleUse"
Cohesion: 0.29
Nodes (7): NozzleUse, Backstep, Leap, None, Pursuit, Rush, Step

### Community 134 - "Phase"
Cohesion: 0.40
Nodes (5): Phase, Idle, Moving, Settling, Windup

### Community 136 - "Q: ㅇㅋ 좋아. 그렇게 구현해줘. 보스 이름은 메가 메카트론 JT8-3이야"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: ㅇㅋ 좋아. 그렇게 구현해줘. 보스 이름은 메가 메카트론 JT8-3이야, Source Nodes

### Community 139 - "Phase"
Cohesion: 0.40
Nodes (5): Phase, Idle, Moving, Preparing, Resting

### Community 140 - "Phase"
Cohesion: 0.40
Nodes (5): Phase, Approach, Cleared, Combat, Entrance

### Community 142 - "LobbyInteraction"
Cohesion: 0.22
Nodes (5): LobbyInteraction, 로비 구현, 미정 사항, 배경 에셋 출처와 생성 프롬프트, LobbyValidation

## Knowledge Gaps
- **237 isolated node(s):** `ActiveBursts`, `HasArt`, `HasExplosionArt`, `HasMissileArt`, `BlocksActions` (+232 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 710 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **63 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Work-memory lessons

**Preferred sources** — corroborated by past sessions; start here.
- `BossHitboxEditor` (2× useful, score=1.874740669)

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Boss1BoosterFlameVisual` connect `Boss1BoosterFlameVisual` to `Boss1Backstep`, `BoxCollider2D`, `NozzleUse`, `Boss1MissileArt`, `Boss1BoostRushVisual`, `Boss1SlamSpriteVisual`, `Boss1Health`, `Boss1BoostRush`, `Boss1BoostStep`, `Boss1ProximityAttack`, `Boss1LeapSlam`, `Vector2`, `MonoBehaviour`, `FlameState`, `SpriteRenderer`?**
  _High betweenness centrality (0.048) - this node is a cross-community bridge._
- **What connects `ActiveBursts`, `HasArt`, `HasExplosionArt` to the rest of the system?**
  _237 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `dependencies` be split into smaller, more focused modules?**
  _Cohesion score 0.06666666666666667 - nodes in this community are weakly interconnected._
- **Why does `PlayerAttack` connect `PlayerAttack` to `Transform`, `BoxCollider2D`, `AttackVisualsValidation.CheckInPlayMode`, `타격감 튜닝`, `unityengine`, `PlayerChordStrike`, `PlayerHitAudio`, `PlayerMovement`, `AttackHit`, `Vector2`, `RhythmGaugeValidation`, `CallbackContext`, `MonoBehaviour`, `Boss1RearMissiles`, `Collider2D`, `SpriteRenderer`?**
  _High betweenness centrality (0.043) - this node is a cross-community bridge._
- **Should `Boss1MissileArt` be split into smaller, more focused modules?**
  _Cohesion score 0.10574712643678161 - nodes in this community are weakly interconnected._
- **Why does `dependencies` connect `dependencies` to `com.unity.2d.tilemap@1.0.0`, `com.unity.modules.subsystems@1.0.0`, `com.unity.modules.jsonserialize@1.0.0`, `com.unity.modules.jsonserialize@1.0.0`, `com.unity.modules.imageconversion@1.0.0`, `com.unity.render-pipelines.core`, `com.unity.collab-proxy`, `com.unity.nuget.mono-cecil@1.11.5`, `com.unity.2d.common@12.0.4`, `com.unity.modules.unitywebrequest@1.0.0`, `packages-lock.json`, `com.unity.modules.audio@1.0.0`, `com.unity.ext.nunit`, `com.unity.modules.uielements@1.0.0`, `com.unity.collections@2.4.3`, `com.unity.dt.app-ui@1.3.6`, `com.unity.modules.physics@1.0.0`?**
  _High betweenness centrality (0.041) - this node is a cross-community bridge._
- **Should `AttackVisualsValidation.CheckInPlayMode` be split into smaller, more focused modules?**
  _Cohesion score 0.09851551956815115 - nodes in this community are weakly interconnected._