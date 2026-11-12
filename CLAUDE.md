# 기타 히어로 (GuitarHero)

기타를 휘두르며 끊임없이 공격하고 회피하는 **2D 사이드뷰 액션 보스러쉬** 게임.
소재: 미소녀 · 기타리스트 · 일렉기타. **핵심 경험은 타격감**이다.

> 이 문서는 Claude가 세션마다 읽는 프로젝트 안내서다. 팀원 누구나 수정해도 된다.
> 대화와 문서는 **한국어**로 작성한다.

## 게임 디자인 핵심 (구현 판단의 기준)

기준 문서: **게임의 정체성 v20261110** (`Docs/기획문서/게임의 정체성 v20261110.md`)

- **모순 해결이 설계 축이다**: 기타 휘두르기의 *느리고 묵직함* ↔ 이동·속주의 *빠른 속도감*.
  새 기능은 "이 모순을 어떻게 풀어주는가"로 판단한다.
- **전투 핵심 루프**: 묵직한 평타 → 빠른 이동·**슬라이드로 후딜 캔슬** → 패턴 회피 직후 다시 평타.
  보스의 필수 패턴은 **기본 이동과 슬라이드만으로** 피할 수 있게 설계한다.
- **평타 = 기타 휘두르기**가 메인 공격이다.
- **리듬 게이지**: 데미지를 줄 때 쌓이고, 자연 감소·보스 피격으로 줄어든다. 0~1000, 250씩
  **Low → Normal → High → FEVER 4단계**로 공격속도(주)·공격력(보조)이 오른다. 수치는 `리듬게이지.md` 참고.
- **연주 스킬**: 회피기·딜링기·유틸기. 보스전/재도전 전에 해금된 스킬 중 **서로 다른 2개를 장착**한다.
  시작 시 대시형 회피기 1 + 딜링기 1, 보스를 처음 격파할 때마다 1~2개씩 해금된다.
  스킬을 써도 리듬은 유지되고, 피해형 스킬은 리듬이 높을수록 강해진다.
- **이펙터**: 보스전 전에 1개 장착해서 평타의 성격을 바꾼다. 리버브(지속 피해, 기본 선택), 디스토션(공속 −30%, 공격력 +50%),
  딜레이(후보: 3타 적중 시 25% 추가 피해). 추가 피해가 나도 리듬 획득과 히트스톱은 **본타 적중 기준**으로 처리한다.
- **퍼펙트 닷지·바인드는 삭제된 기획이다** (v20261110). 구현하거나 참조하지 않는다.

**마일스톤**: 무조건 보스 먼저. **2026-12-20 명량하제까지 보스 2종 이상, 연주 스킬 4종 이상** 구현.

상세 기획은 아래 문서를 본다. 기획과 코드가 충돌하면 임의로 판단하지 말고 사용자에게 묻는다.

## 기획 문서 위치

- **원본(source of truth)**: 팀 Google Drive의 Google Docs
  https://drive.google.com/drive/u/0/folders/1owj2kwbBLUpSqbRMMgk0FRZy-rC_tlxf
- **레포 사본**: `Docs/기획문서/`(기획서), `Docs/위키/`(구현 결과·튜닝 값 기록)
  - 각 .md 상단에 Google Docs 원본 링크가 있다. 사본이 오래됐을 수 있으니 중요한 판단 전에는 원본을 확인한다.
  - Google Drive 커넥터로 원본을 직접 읽을 수 있다. 원본이 사본보다 새로우면 사본을 갱신하자고 제안한다.
  - 정체성 문서는 버전별로 파일이 따로 있다(`게임의 정체성 vYYYYMMDD`). **가장 최신 버전이 기준**이다.

## 팀 개발 방식

- 정해진 프로세스는 따로 없다. **기획서를 Drive에 올리면 → 개발자가 각자 스펙을 설계하고 → 구현한다.**
- 기능 단위로 브랜치를 따서 작업하고 PR로 `main`에 머지한다 (예: `feature/city-zombie`, `fix/player-01`, `damage-number`).
- 커밋 메시지는 한국어로 쓰고, 필요하면 `feat :`, `fix :`, `chore :`, `test :` 접두어를 붙인다.
- Claude가 기능을 구현할 때는 아래 순서를 따른다.
  1. 관련 기획서를 읽는다.
  2. 짧은 스펙(구성 요소, 조정 가능한 수치, 기존 코드와의 연결점)을 먼저 제안한다.
  3. 합의한 뒤 코딩한다.
- 커밋·푸시·PR은 사용자가 요청할 때만 한다.

## 기술 스택

- **Unity 6000.3.25f1**, URP 2D, **새 Input System** (`Assets/InputSystem_Actions.inputactions`)
- 2D Animation / Aseprite / PSD Importer / Tilemap / SpriteShape
- `com.unity.ai.assistant` (Unity MCP 서버 포함 — 아래 참고)

## 폴더 구조

```
Assets/
  Scripts/
    Player/       PlayerMovement, PlayerAttack(콤보·히트박스), PlayerChordStrike, PlayerRhythmGauge(+UI),
                  타격 연출(HitStop/CameraShake/Effects/Audio)
    Combat/       Damageable(추상), AttackHit, ChordNoteProjectile, BluntHitBurst
    Characters/   TrainingDummy, ZombieMovement, HealthBarUI
    Environment/  CityCameraFollow, CityParallax, CityStageFlow
  Scenes/         Main.unity(타격감 테스트), City01_SilentBlock.unity(스테이지), SampleScene.unity
  Prefabs/        Player, TrainingDummy, Zombie
  Audio/ Characters/ Effects/ Environment/ PhysicsMaterials/ Settings/
Docs/             기획문서, 위키
Tools/            에디터 밖에서 돌리는 검증·생성 스크립트 (PowerShell)
```

## 코드 컨벤션 (기존 코드 기준)

- namespace는 쓰지 않는다. 클래스 하나당 파일 하나, 파일명 = 클래스명.
- 튜닝할 수치는 `[SerializeField] private` 필드에 `[Header]`, `[Tooltip("한국어 설명")]`, `[Min]`/`[Range]`를 붙여 **인스펙터에서 조정**할 수 있게 한다.
  기획자가 코드 수정 없이 수치를 바꿀 수 있어야 한다.
- 필드는 camelCase, 시간 단위는 **초(float)** 로 쓰고 Header에 `(Seconds)`처럼 단위를 적는다.
- 주석은 한국어로 짧게, *왜* 그렇게 했는지 적는다.
- 컴포넌트 간 연결은 C# `event`(예: `PlayerAttack.HitConfirmed`)를 쓴다. 타격 연출은 피해가 실제로 들어간 순간에만 발생시킨다.
- 피격 대상은 `Damageable`을 상속하고 `TryTakeDamage(AttackHit)`를 구현한다.
- 실행 순서가 중요하면 `[DefaultExecutionOrder]`를 쓴다 (예: PlayerAttack은 -10).

## 작업 시 주의

- **`.meta` 파일은 에셋과 함께** 추가·이동·삭제한다. 에셋을 옮길 때 GUID가 깨지지 않게 한다.
- `Library/`, `Temp/`, `Logs/`, `UserSettings/`, 루트의 `*.csproj`·`*.slnx`는 Unity가 자동으로 생성한다. 수정하거나 커밋하지 않는다.
- `.unity`, `.prefab`, `.asset` YAML을 직접 편집하는 건 위험하다. 가능하면 Unity MCP로 에디터에서 조작하고,
  YAML을 손대야 하면 먼저 사용자에게 알린다. 씬은 여러 사람이 동시에 수정하면 충돌이 잦다.
- 수치를 튜닝하면 `Docs/위키/`의 해당 문서(예: `타격감 튜닝.md`) 표도 함께 갱신한다.
- 외부 에셋이나 음원은 출처·라이선스를 확인한다. `Tools/GenerateCombatAudio.ps1`은 직접 합성한 사운드만 만든다.

## 검증

- **Unity MCP**(연결돼 있을 때): 코드 수정 후 콘솔 컴파일 에러와 경고를 확인한다 (`Unity_ReadConsole` 등).
  Unity 에디터가 켜져 있고 `Project Settings > AI > Unity MCP Server`에서 Bridge가 Running이어야 한다.
- **타격감 자동 검증**: `Tools/ValidateCombatFeel.ps1` — `Temp/CombatFeelValidation`에 격리 프로젝트를 복사해서 검사한다
  (열려 있는 에디터에는 영향 없음).
- **리듬 게이지 검증**: `Tools/ValidateRhythmGauge.ps1`. 수급 계산은 `Tools/CalculateRhythmGauge.mjs`로 한다
  (결과는 `Docs/기획문서/assets/리듬게이지/`, 구현 안내는 `Docs/위키/리듬 게이지 구현.md`).
- 코드에 `RhythmHitKind.Enhanced`(강화 평타)가 남아 있지만, 퍼펙트 닷지가 삭제됐으므로 새 코드에서 쓰지 않는다.
- 플레이 감각(타격감·조작감)은 자동으로 판정할 수 없으니, 사용자에게 플레이 테스트를 요청한다.
