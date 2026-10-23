# Canvas 체력바

`Assets/Prefabs/TrainingDummy.prefab`의 `HealthBar`를 World Space Canvas로 구성했습니다. 더미 위에서 함께 이동하며, 피격 반동을 주는 `HitReactionPivot` 밖에 있어 몸통 반동에 흔들리지 않습니다.

```text
TrainingDummy
├─ HealthBar (Canvas, HealthBarUI, 테두리 Image)
│  ├─ Background    어두운 배경
│  ├─ DamageTrail   주황색 피해 잔상
│  └─ CurrentHealth 초록색 실제 체력
└─ HitReactionPivot
   └─ Visual
```

초록색 바가 주황색 바보다 앞에 표시됩니다. 따라서 체력이 줄어들면 두 바의 차이만큼 주황색 피해 구간이 드러납니다. SpriteRenderer 대신 uGUI Image의 가로 `fillAmount`를 사용합니다.

## 동작

- 피해를 받으면 초록색 바는 즉시 감소합니다.
- 주황색 바는 0.25초 대기 후 0.4초 동안 부드럽게 따라 감소합니다.
- 연속 피격은 주황색 바의 현재 길이를 유지하며 대기 시간을 다시 시작합니다. 이미 감소한 주황색 바가 위로 튀지 않습니다.
- 히트스탑과 메뉴 일시정지 중에는 주황색 감소를 멈춥니다.
- 회복·초기화·재생성 시 두 바를 같은 값으로 맞추고 이전 피해 연출을 지웁니다.
- 사망 시 초록색 바는 즉시 0이 됩니다. 사망 애니메이션과 주황색 감소가 모두 끝난 뒤 Canvas를 숨깁니다.

## 조정

더미 프리팹의 `HealthBar`를 선택합니다.

| 조정 위치 | 항목 | 기본값 |
| --- | --- | --- |
| HealthBarUI | Damage Delay | 0.25초 |
| HealthBarUI | Damage Duration | 0.4초 |
| HealthBar RectTransform | 너비 / 높이 | 144 / 16 |
| HealthBar Transform | 배율 | 0.01 |
| HealthBar Transform | 더미 기준 높이 | 1.12 |
| CurrentHealth / DamageTrail Image | 색상 | 초록 / 주황 |

월드에 표시되는 최종 크기는 가로 1.44, 세로 0.16유닛입니다. Canvas 정렬 순서는 12이며 모든 Image의 Raycast Target은 꺼두었습니다. 입력용 EventSystem이나 GraphicRaycaster는 필요하지 않습니다.

`HealthBarUI.SetHealth(current, maximum)`으로 다른 체력 시스템에도 연결할 수 있습니다. 즉시 초기화할 때는 세 번째 인자로 `true`를 전달합니다.

`Tools/ValidateCombatFeel.ps1`의 Play 모드 검증에 Canvas 연결, 실제 체력 반영, 피해 잔상 지연·감소, 연속 피격, 정지, 회복, 사망·재생성 확인을 포함했습니다.
