# Unity 에디터에서 작업하기

이제 게임의 배치와 설정은 씬·Prefab·데이터 에셋에 저장됩니다. 전투의 탄도와 턴 규칙은 기존 C# 계산을 유지합니다.

## 시작

1. Unity Hub에서 `game` 폴더를 열어 주세요. 프로젝트 버전은 `6000.6.3f1`입니다.
2. `Assets/Scenes/FortressBattle.unity`를 엽니다. 또는 **Mini Fortress → Open Editable Battlefield** 메뉴를 사용합니다.
3. Scene 창에서 **2D**와 **Gizmos**를 켜 주세요. Hierarchy의 `Battlefield/Background...`를 선택하고 `F`를 누르면 배경을 중심으로 볼 수 있습니다.
4. Play를 누르고 캐릭터를 선택합니다.

`SampleScene.unity`는 원래 템플릿 씬으로 남겨 두었습니다. 더 이상 씬 이름을 검사해 게임을 자동 생성하지 않습니다. 빌드 시작 씬은 `FortressBattle`입니다.

## Play 없이 전장과 UI 확인

**Mini Fortress → Battlefield Editor**를 열어 주세요. `Battlefield` 또는 Spawn의 Inspector에 있는 미리보기 버튼으로도 열 수 있습니다.

- **Battlefield**: 실제 배경과 캐릭터 Prefab 배치를 표시합니다.
- **Selection**: 선택 카드가 직업 목록에 맞게 채워진 화면을 표시합니다.
- **Battle**: 턴 순서, 체력바, 능력치, 조준 UI를 표시합니다.
- **Result**: 전투 결과 패널을 포함한 화면을 표시합니다.
- **미리 볼 직업**: 궁수·창병 등 등록된 직업으로 플레이어 외형과 UI를 확인합니다.
- **자동 갱신**: Inspector에서 바꾼 배치·Prefab·능력치·UI를 약 0.75초 간격으로 다시 읽습니다. 부담이 있으면 끄고 새로 고침을 누르세요.
- **바닥 영역**: 배경 위에 충돌 사각형을 표시합니다.

미리보기 그림에서 캐릭터를 클릭하거나 아래 배치 목록의 이름을 누르면 **실제 씬의 Spawn**이 선택됩니다. Scene 창에서 이동하면 됩니다. 목록에는 능력치 에셋과 Prefab을 여는 버튼도 있습니다. 미리보기 속 게임 버튼은 실행되지 않습니다.

미리보기는 별도의 임시 씬에 복제한 오브젝트로 구성됩니다. 원본 Canvas의 활성 상태나 내용, 씬 배치를 바꾸지 않습니다. 애니메이션·전투·낙하는 실행하지 않는 정적인 배치 확인 기능이므로 실제 동작은 Play에서 확인하세요.

**모든 출전 위치 바닥 맞추기**는 실제 씬의 발 위치를 변경합니다. 한 번의 Ctrl+Z로 전체 작업을 취소할 수 있습니다. Spawn 여러 개를 함께 선택한 뒤 Inspector의 바닥 맞추기도 사용할 수 있습니다. 같은 X 위치에 바닥이 없는 Spawn은 이동하지 않습니다.

배치 진단은 바닥에 닿지 않은 발, 몸과 지형의 겹침, 캐릭터끼리 겹친 시작 위치, 월드 경계 밖 배치를 표시합니다. 경고 아래 버튼으로 해당 위치를 선택할 수 있습니다. Scene의 청록색 상자는 데이터와 Prefab 크기를 반영한 캐릭터 충돌 범위입니다.

## Hierarchy 안내

```text
Battlefield                     FortressArena: 전체 참조와 직업 목록
├─ Background...                배경 SpriteRenderer
├─ Battle Camera                전장 카메라: 위치, Orthographic Size
├─ HUD Display Camera           화면 초기화용 카메라
├─ Terrain...
│  ├─ Left Cliff                왼쪽 절벽
│  ├─ Bridge                    중앙 다리
│  ├─ Right Ledge               얇은 오른쪽 발판
│  └─ ...                       총 9개 기본 지형
└─ Spawns
   ├─ Player Spawn              플레이어 발 위치
   └─ Enemies...
      ├─ 다리 파수꾼             적 데이터와 발 위치
      ├─ 성채 대장
      └─ ...                    Hierarchy 순서가 적 행동 순서
Battle Controller               FortressGame + FortressInput
Battle UI - Canvas              실제 편집 가능한 UI
└─ 16 by 9 Frame
   ├─ Battlefield Output        전장 카메라 출력을 보여주는 RawImage
   ├─ Battle HUD                체력·턴·조준·버튼·결과 화면
   └─ Character Selection       선택 화면과 카드 템플릿
EventSystem                     마우스와 UI 입력
```

## 바닥과 다리 수정

1. Play를 종료합니다.
2. Hierarchy에서 `Battlefield/Terrain.../Bridge`를 선택합니다.
3. Move 도구(`W`)로 위치를 옮깁니다.
4. Inspector의 **Box Collider 2D → Edit Collider**를 눌러 테두리를 드래그합니다. `Size`와 `Offset`에 숫자를 입력해도 됩니다.
5. `Fortress Terrain → Allow Drop Through`를 켜면 S 키로 내려갈 수 있는 발판이 됩니다.
6. 씬을 저장하고 Play하여 확인합니다.

초록 영역은 일반 지형, 주황 영역은 내려갈 수 있는 발판입니다. 이 색은 Scene의 Gizmo이며 게임 화면에는 나오지 않습니다.

- Collider의 사각형을 기존 착지·벽·탄도 계산이 읽습니다. **Rigidbody2D를 추가할 필요가 없습니다.**
- **회전과 경사는 지원하지 않습니다.** Rotation은 0으로 두고 Position, Size, Offset을 편집하세요. 사각형을 여러 개 조합해 계단을 만들 수 있습니다.
- 판정은 Play 시작 시 읽습니다. Play 중 편집한 좌표는 즉시 재수집하지 않으며, Play를 종료하면 씬 변경도 원래대로 돌아갑니다.
- 바닥 영역을 복제하면 새 지형을 추가할 수 있습니다. 비활성화한 오브젝트는 게임 판정에서 제외됩니다.
- 배경 그림은 별개입니다. 그림 속 다리 높이와 Collider 윗면을 맞춰 주세요.
- 월드 이동 범위를 늘릴 때는 `BattleRules.asset`의 Horizontal Limits, Projectile Limits, Fall Respawn Y 및 카메라 구도도 확인하세요.

## 플레이어와 적 배치

`Player Spawn` 또는 적의 Spawn 오브젝트를 이동하세요. Transform Position이 **발 위치**입니다.

Spawn의 Inspector에서 **가장 가까운 바닥 위로 맞추기**를 누르면 같은 X 위치의 가장 가까운 바닥 윗면에 Y를 맞춥니다. Undo가 가능합니다.

적을 추가하려면 `Enemies...` 아래의 Spawn을 복제하고 위치·Character·Display Name을 바꾸세요. 삭제하거나 비활성화하면 출전하지 않습니다. 적 수와 턴 UI는 실행 시 목록에 맞춰 구성됩니다. 적이 많이 늘어나면 Turn Order의 UI 너비도 조정하세요.

플레이어 직업은 `Battlefield → Player Classes`에서 설정합니다. Player Spawn의 Character는 편집 참고용이며, 실제 플레이어 직업은 Player Classes에서 고른 데이터가 결정합니다.

## 체력·공격력·이동량 수정

`Assets/FortressContent/Data`에 다음 에셋이 있습니다.

| 에셋 | 내용 |
|---|---|
| Archer.asset | 궁수 |
| Spearman.asset | 창병 |
| Goblin.asset | 일반 적 공통 설정 |
| Captain.asset | 대장 |
| BattleRules.asset | 중력·점프·추락 피해·조준 범위·월드 경계 |

캐릭터 데이터를 선택하고 Inspector에서 Health, Damage, Blast Radius, Movement Per Turn, Movement Speed를 바꾸면 됩니다. 선택 화면의 수치도 같은 데이터를 읽습니다.

`Height`, `Half Width`, `Shoulder Height`, `Muzzle Distance`는 충돌 크기와 발사 위치입니다. 그림의 크기를 크게 바꾸면 이 값도 확인하세요. Prefab의 루트 Scale은 캐릭터 충돌 크기에도 반영됩니다. 캐릭터 Prefab은 양수의 균일한 루트 Scale을 사용하세요.

새 직업은 기존 데이터와 Prefab을 복제한 뒤 Player Classes에 추가할 수 있습니다. 현재 공격 종류는 Bow와 Spear를 지원합니다. 새 공격 규칙을 만들려면 C# 확장이 필요합니다.

## 캐릭터·무기 외형 수정

`Assets/FortressContent/Prefabs`의 Archer, Spearman, Goblin, Captain을 더블클릭하세요.

```text
캐릭터 루트
├─ Ground shadow
└─ Motion                       Animator / 바라보는 방향
   ├─ Body                      SpriteRenderer
   └─ AimPivot                  조준 각도
      └─ WeaponMotion           무기 모션
         └─ Longbow 또는 Throwing spear
            └─ Loaded projectile
```

Body의 Sprite, 무기의 Sprite·위치·크기, 그림자 등을 수정할 수 있습니다. `FortressFighterView`의 참조를 유지하세요. Animator 클립이 `Body`와 `AimPivot/WeaponMotion` 경로를 사용하므로 해당 이름과 계층을 바꿀 때는 애니메이션 바인딩도 수정해야 합니다.

애니메이션이 Body와 WeaponMotion의 Transform을 매 프레임 제어합니다. 해당 Transform의 기본 크기만 바꾸면 모션이 덮어쓸 수 있으므로 모션 클립도 함께 편집하거나 캐릭터 루트 Scale을 조정하세요. AimPivot 위치는 데이터의 Shoulder Height를 사용합니다.

실제 이미지 에셋은 `Assets/FortressContent/Art`에 있습니다. 기존 원본 아틀라스는 `Assets/Resources/FortressArt`에 보존했습니다. 실행할 때 이미지를 자르지 않고 미리 분리한 Sprite를 참조합니다. 따라서 원본 아틀라스만 교체해도 분리된 Sprite가 자동 갱신되지는 않습니다.

## UI 수정

`Battle UI - Canvas` 아래에서 원하는 오브젝트를 선택합니다.

- 위치·크기: **RectTransform** 또는 Rect 도구(`T`)
- 문구·색·폰트: **Text**
- 패널·이미지: **Image**
- 버튼 동작: **Button → On Click()**
- 슬라이더 연결: **Slider → On Value Changed()**

기본 해상도는 1600×900이며 CanvasScaler가 화면에 맞춰 확대·축소합니다. 다른 화면 비율에서는 중앙에 16:9 영역을 유지합니다. 전장 화면은 RawImage이며 카메라의 1920×1080 출력을 표시합니다.

선택 카드·턴 슬롯·머리 위 체력바는 씬의 **Template** 오브젝트를 실행 시 복제합니다. Template에서 모양을 수정하면 모든 항목에 적용됩니다. 에디터에는 편집용 샘플 하나가 보이고 Play 중에는 원본 템플릿을 숨깁니다.

체력, 선택 직업명, 공격 버튼 문구, 남은 적 수 등은 게임 데이터에서 갱신됩니다. 이런 동적 문구의 형식은 `FortressHud.cs` 또는 `FortressClassCard.cs`에서 수정하세요. 일반 버튼·제목·안내 문구는 Text에서 편집할 수 있습니다.

한국어 표시는 포함된 Noto Sans CJK KR 폰트를 사용합니다. 폰트 출처와 라이선스는 `FortressContent/UI/NotoSans-LICENSE.txt` 및 아래 링크를 참고하세요.
- https://github.com/notofonts/noto-cjk/tree/main/Sans

## 키 설정

`Assets/FortressContent/Input/FortressControls.inputactions`를 더블클릭하고 Battle 맵의 Binding을 편집한 뒤 저장하세요. 런타임은 이 에셋을 복제해 읽으므로 플레이 중 변경이 원본에 저장되지는 않습니다.

기본 조작은 A/D 이동, W 점프, S 내려가기, 방향키 각도·위력, Space 발사, Tab 턴 종료, R 재시작, Esc 캐릭터 선택입니다. 선택 화면에서는 1/2 또는 좌우 방향키, Enter를 사용합니다.

키 설정을 바꾸면 화면의 고정 조작 안내 문구도 함께 수정하세요. 버튼의 키보드 Navigation은 게임 단축키와 중복 발동하지 않도록 꺼져 있습니다.

## 애니메이션

기존 `Assets/Resources/FortressAnimation/Fighter.controller`와 9개 `.anim`을 그대로 사용합니다. Unity Animator/Animation 창에서 편집할 수 있습니다.

발사 시점은 각 캐릭터 데이터의 **Release Time**입니다. 기본값은 활 0.16초, 창 0.22초입니다. 공격 클립의 동작 시점과 함께 맞춰 주세요.

**Mini Fortress → Rebuild Character Animations**는 기존 기본 클립을 다시 만들기 때문에 수동 편집을 덮어씁니다. 일반 작업에는 사용하지 마세요. **Open Editable Battlefield**는 이미 존재하는 씬·Prefab·데이터를 재생성하지 않습니다.

## 코드 구조

| 파일 | 역할 |
|---|---|
| FortressArena / FortressTerrain / FortressSpawnPoint | 에디터에서 작성한 배치·지형·직업 참조 |
| FortressCharacterDefinition / FortressBattleRules | 데이터 에셋 |
| FortressFighterView | Prefab 내부 연결 |
| FortressInput | Input Actions 읽기 |
| FortressHud / FortressClassCard / FortressActorHud / FortressHoldButton | Canvas 표시와 조작 |
| FortressGame.cs | 초기화, 이동, 탄도, 충돌, 피해 |
| FortressGame.Turns.cs | 턴 진행 |
| FortressGame.Enemies.cs | 적 이동·사격 판단 |
| FortressGame.Visuals.cs | Prefab 생성·직업 선택 |
| FortressGame.Animation.cs | Animator 상태 연결 |
| FortressGame.Hud.cs | UI가 사용하는 상태 조회·명령 인터페이스 |
| Editor/FortressSceneBuilder.cs | 최초 기본 에셋·씬 생성, 기존 작업은 보존 |
| Editor/FortressAuthoringEditors.cs | Inspector 안내·배치 보조 기능 |
| Editor/FortressBattlefieldWindow.cs | 전장 편집 창과 대상 선택 |
| Editor/FortressPreview.cs | 원본 씬을 변경하지 않는 전장·UI 미리보기 |
| Editor/FortressPlacement.cs | 배치 진단·일괄 바닥 맞춤·Undo |

전투 상태의 최종 책임은 FortressGame에 남겨 두었습니다. `.Turns`, `.Enemies` 등은 같은 partial 클래스이며, 별도의 독립 컴포넌트라고 보면 안 됩니다. 배치·데이터·UI·입력은 별도 컴포넌트/에셋으로 분리했습니다.

## 검증

`Window → General → Test Runner`의 PlayMode에서 `FortressPlayModeTests`를 실행할 수 있습니다. 테스트는 `FortressBattle`이 빌드 씬 목록에 등록된 상태에서 실행합니다.

검사 범위: 직업별 Prefab/능력치, 편집된 지형·스폰과 착지, 데이터 변경과 피해·UI, 공격 중 입력 잠금과 취소, 수동 턴 종료와 적 한 라운드, Canvas 버튼·슬라이더, 승패·재시작, Fire 키 재설정.

EditMode의 `FortressAuthoringTests`는 기본 배치 진단, 여러 Spawn의 바닥 맞춤과 Undo, 잘못된 배치 탐지, 네 가지 미리보기의 원본 씬 보존, 렌더링 후 Canvas와 텍스처 보존을 검사합니다.
