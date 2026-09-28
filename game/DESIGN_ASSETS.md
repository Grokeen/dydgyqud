# 디자인 에셋 교체 가이드

맵·캐릭터·카드의 이미지와 3D 모델, 직업 수치와 카드 구성은 코드를 고치지 않고 **파일을 넣고 Inspector 칸을 채우는 것만으로** 바꿀 수 있습니다.
칸을 비워 두면 지금의 기본 모양(색으로 구분한 캡슐 캐릭터, 벽돌·판자 발판, 기본 화살·창)이 그대로 쓰입니다.
전투 판정(충돌, 탄도, 체력)은 외형과 분리되어 있어서 외형을 바꿔도 게임 규칙은 달라지지 않습니다.

권장 형식: 이미지 **PNG**(캐릭터·카드는 투명 배경, 맵 배경은 16:9), 3D 모델 **FBX**(Humanoid 뼈대가 있으면 애니메이션 연결이 쉬움).

## 좌표 약속

- 게임은 옆에서 보는 평면(z = 0)에서 진행됩니다. **+X가 오른쪽, +Y가 위**입니다. 캐릭터가 왼쪽을 볼 때는 게임이 자동으로 뒤집습니다.
- 1 유닛 = 1m. 기본 캐릭터 키는 4.2m(`Height`)입니다.
- 발판은 캐릭터 뒤쪽(z = 1 ~ 5.4)에 그려지므로 캐릭터를 가리지 않습니다.

## 맵

맵 하나 = `FortressMapDefinition` 에셋 하나. `Assets/Resources/FortressMaps`에 있는 맵이 모두 메인 메뉴에 나옵니다.

- **새 맵 추가**: 기존 맵 에셋을 복제(Ctrl+D)하고 고칩니다. 또는 Project 창에서 `Create → Mini Fortress → Map`을 선택합니다.
- **순서**: `Order` 값이 작은 맵이 먼저 나옵니다.
- **특정 맵만 쓰기**: 씬의 `Battlefield`(FortressArena) → `Maps`에 원하는 맵을 넣으면 그 목록만 사용합니다.
- **기본 맵 4개**: Unity를 처음 열 때 폴더가 비어 있으면 자동으로 만들어집니다. 망가뜨렸을 때는 메뉴 `Mini Fortress → Restore Built-in Map Assets`로 되돌립니다.

| 칸 | 내용 |
|---|---|
| Display Name / Description | 메인 메뉴 카드의 이름과 설명 |
| Menu Tagline / Menu Description | 이 맵을 고르면 메인 메뉴 위쪽 부제와 설명이 이 문구로 바뀝니다. 비우면 공통 문구 |
| Background | 전투 배경 이미지(Sprite). 카메라를 따라가며 화면을 덮습니다 |
| Preview | 메인 메뉴 카드 이미지. 비우면 배경을 씁니다 |
| Tint | 기본 발판 색에 곱하는 색조 |
| Ground Look / Drop Through Look | 일반 발판 / `S`로 내려갈 수 있는 발판의 외형(아래 표) |
| Platforms | 발판 목록. `Left·Right·Top·Bottom`은 충돌 영역이고, 캐릭터는 `Top`에 섭니다. 개수는 자유입니다 |
| Platforms → Model | 이 발판 하나만 다른 3D 모델로 그립니다(폭·두께에 맞춰 늘어남) |
| Player Spawn / Enemies | 플레이어·적의 발 위치. 적 목록 순서가 적의 턴 순서입니다. 적 수도 자유입니다 |
| Enemies → Character | 적 캐릭터 데이터. 비우면 씬의 같은 순서 스폰 캐릭터를 씁니다 |

**발판 외형(Look)**

| 칸 | 내용 |
|---|---|
| Model | 발판 전체를 이 3D 모델로 그립니다 |
| Fit | `Stretch`: 발판 폭·두께·깊이에 맞춰 늘립니다. `PivotAtTopCentre`: 모델 원점을 발판 윗면 가운데에 두고 크기는 그대로 둡니다 |
| Body Material / Cap Material | 기본 상자 모양에 입힐 재질(본체 / 윗면 테두리) |
| Texture / Texture Tile | 재질 대신 질감 이미지만 바꿀 때. Tile은 한 번 반복되는 크기(m)입니다 |
| Override Colors | 켜면 Body Color / Cap Color를 씁니다 |

맵을 적용할 때 스폰이 발판 위에 있지 않거나 머리 위 발판과 겹치면 Console에 경고가 뜹니다.

## 캐릭터

캐릭터 데이터: `Assets/FortressContent/Data`(Archer, Spearman, Goblin, Captain)

| 칸 | 내용 |
|---|---|
| Portrait | UI 초상화(선택 화면, 턴 순서, 상태창) |
| Model Prefab | 캐릭터 3D 모델. **원점 = 발, +X = 앞**. 비우면 `Model Style` 색의 캡슐을 씁니다 |
| Fit Model To Height | 모델 높이를 `Height`에 맞춰 자동으로 크기를 조절하고 발을 땅에 붙입니다 |
| Model Offset / Rotation / Scale | 위치·회전·크기 미세 조정. 기본 회전 Y 28°는 카메라 쪽으로 살짝 돌려 입체감을 주는 값입니다 |
| Use Built In Body Motion | 켜면 기본 애니메이션(호흡·기울기·피격 흔들림·쓰러짐)이 모델 전체를 움직입니다. 자체 애니메이션이 있는 모델은 끄세요 |
| Weapon Model Prefab | 손에 든 무기. **원점 = 어깨(조준 회전축), +X = 조준 방향**. 비우면 기본 활(창병은 장전된 창만) |
| Projectile Model Prefab | 장전된 화살·날아가는 화살·바닥에 떨어진 화살 모델. **원점 = 촉 끝, 자루는 -X 방향** |

- **자체 애니메이션**: 모델에 Animator와 컨트롤러가 있으면, 게임이 아래 파라미터 중 **그 컨트롤러에 있는 것만** 자동으로 보냅니다. 이름만 맞추면 됩니다.

  | 파라미터 | 형식 | 의미 |
  |---|---|---|
  | `Speed` | Float | 수평 이동 속도 |
  | `Grounded` | Bool | 착지 여부 |
  | `VerticalSpeed` | Float | 수직 속도 |
  | `Aiming` | Bool | 조준 중 |
  | `BowAttack` / `SpearAttack` | Trigger | 공격(발사 시점은 데이터의 `Release Time`) |
  | `Hit` | Trigger | 피격 |
  | `Dead` | Bool | 사망 |

- 피격 시 붉게 깜빡이는 효과는 모델 재질 색(`_BaseColor` / `_Color`)에 자동으로 곱해집니다.
- 판정 크기(`Height`, `Half Width`, `Shoulder Height`, `Muzzle Distance`)는 모델과 별개입니다. 모델을 크게 바꾸면 이 값들도 함께 맞추세요.

## 직업

직업마다 고유 능력, 수치, 카드가 **완전히 분리**되어 있습니다. 공용 전투 코드는 직업 이름을 모르고, 정해진 시점에 플레이어의 직업 모듈을 부르기만 합니다.

```
Assets/FortressContent/Classes/Archer/Archer Class.asset     궁수 수치 (시작·최대 화살, 출혈 폭발 기준·피해, 연쇄 거리, 공격 이름)
Assets/FortressContent/Classes/Archer/Archer Cards.asset     궁수 카드 세트 (시작 덱 + 보상 카드)
Assets/FortressContent/Classes/Spearman/Spearman Class.asset 창병 수치
Assets/FortressContent/Classes/Spearman/Spearman Cards.asset 창병 카드 세트
Assets/Resources/FortressCardArt/Archer/                     궁수 카드 그림
Assets/Resources/FortressCardArt/Spearman/                   창병 카드 그림
Assets/Fortress/Classes/FortressClassModule.cs               모든 직업이 따르는 공통 약속 (아래 시점 목록)
Assets/Fortress/Classes/Archer/                              궁수 모듈: 화살·회수·출혈·약화 로직, 궁수 카드 효과, 기본 카드 원본
Assets/Fortress/Classes/Spearman/                            창병 모듈 (아직 고유 능력 없음)
Assets/Fortress/Cards/                                       카드 공통 틀과 공용 효과 (방어, 드로우, 피해, 치명타, 코스트)
```

- **캐릭터와 연결**: 캐릭터 데이터의 `Class`(직업 모듈)와 `Card Set`(카드 세트) 칸입니다. 비우면 무기 종류로 기본 직업을 고릅니다(활 → 궁수, 창 → 창병).
- **수치 조정**: 직업 에셋(예: `Archer Class`)을 열어 Inspector에서 고칩니다. 공용 규칙(`BattleRules`)에는 직업 수치가 없습니다.
- **직업 모듈이 불리는 시점**: 전투 시작, 턴 시작, 공격 확정, 발사 수 결정, 화살마다의 효과, 특정 적 추가 피해, 적 공격 약화, 적중, 빗맞음, 카드 효과, HUD 문구(공격 버튼 위, 상태 줄, 적 상태), 전장 정리. 새 직업은 필요한 시점만 구현하면 됩니다.
- **카드 수정·추가**: 직업의 카드 세트 에셋에서 `Starter Deck`(시작 덱, `Copies`만큼 들어감) 또는 `Reward Pool`(스테이지 보상 후보)을 편집합니다. 이름이 같은 카드(예: 궁수 `방어`, 창병 `방어`)도 직업마다 별개 카드입니다.
- **카드 그림 연결**(둘 중 하나)
  1. 카드의 `Art` 칸에 이미지를 직접 넣습니다(우선).
  2. 카드 세트의 `Art Folder`(예: `Archer`) 폴더에 **카드 이름과 같은 이름**의 PNG를 넣습니다(예: `FortressCardArt/Archer/방어.png`). 없으면 **효과 이름** 파일을 찾습니다(예: `Archer/ShotDamage.png`).
- 기존 이미지 `ArcherVolley`, `ArcherPrecision`, `ArcherFieldcraft`는 `FortressCardArt/Archer`에 있지만 카드 이름과 맞지 않아 지금은 쓰이지 않습니다.
- 직업 수치나 카드 세트를 망가뜨렸을 때는 메뉴 `Mini Fortress → Restore Built-in Classes and Card Sets`로 되돌립니다.

**새 직업 추가**
1. 코드: `Assets/Fortress/Classes/<직업>/` 폴더를 만들고 창병 폴더의 두 파일을 참고해 `Fortress<직업>Class`(수치, ScriptableObject)와 `Fortress<직업>Runtime`(전투 중 상태와 능력)을 만듭니다. 필요한 시점만 override합니다.
2. 전용 카드 효과가 필요하면 `FortressCard.cs`의 효과 목록 **맨 뒤**에 추가하고(중간에 넣으면 저장된 카드의 효과가 바뀝니다), 새 직업 Runtime의 `PlayCard`에서 처리합니다.
3. 데이터: `Assets/FortressContent/Classes/<직업>/`에 `Create → Mini Fortress → Classes → <직업>`으로 직업 에셋을, `Create → Mini Fortress → Card Set`으로 카드 세트를 만들고 `Art Folder`에 직업 이름을 적습니다.
4. 그림: `Assets/Resources/FortressCardArt/<직업>` 폴더에 카드 그림을 넣습니다.
5. 캐릭터 데이터를 만들어 `Class`, `Card Set`을 연결하고, 씬 `Battlefield`의 `Player Classes`에 추가합니다.

## 코드 위치(참고)

- 맵 데이터: `Assets/Fortress/FortressMapDefinition.cs`, 적용: `FortressGame.MapSelection.cs`, 기본 맵 원본: `FortressBuiltInMaps.cs`
- 발판 외형 생성: `FortressGame.World3D.cs`
- 캐릭터 외형: `FortressFighterModel.cs`, 기본 발판 질감·활·화살: `FortressModel3D.cs`
- 직업 모듈: `Assets/Fortress/Classes/` (공통 약속 `FortressClassModule.cs`, 직업별 하위 폴더), 게임과의 연결 `FortressGame.PlayerClass.cs`
- 카드: `Assets/Fortress/Cards/` (세트 `FortressCardSet.cs`, 그림 찾기 `FortressCardArt.cs`, 공용 효과 `Common/`)
- 기본 맵·직업·카드 세트 에셋 생성·복원: `Assets/Editor/FortressMapAssets.cs`, `Assets/Editor/FortressClassAssets.cs`
