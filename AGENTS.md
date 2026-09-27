# 작업 분담 규칙 (AI 에이전트 공통)

이 저장소는 두 사람이 각자 AI로 동시에 작업합니다. 충돌을 막기 위해 **파일 단위로 담당을 나눕니다.**

| 담당 | 사람 / AI | 영역 |
|---|---|---|
| **UI** | 상대 · Codex | HUD, 카드 UI, 캐릭터 선택 화면, UI 에디터 툴 |
| **게임플레이** | 나 · Claude Code | 전투 규칙, 조준·발사, 이동, 적 AI, 카드 효과, 전장·지형, 캐릭터 데이터 |

## 기본 규칙

1. **자기 담당 파일만 수정합니다.** 담당이 아닌 파일은 읽기만 합니다.
2. 담당이 아닌 파일을 바꿔야 할 것 같으면, 수정하지 말고 사람에게 알립니다. 무엇을 왜 바꿔야 하는지 적어 주세요.
3. 새 파일은 자기 담당 폴더나 이름 규칙 안에 만듭니다(아래 표 참고).
4. 목록에 없는 파일은 수정하기 전에 사람에게 어느 쪽 담당인지 확인합니다.
5. 커밋은 자기 담당 파일만 담습니다. `git add -A`처럼 전체를 담는 명령은 쓰지 않습니다.

## 파일별 담당

경로는 `game/` 기준입니다. `.meta` 파일은 원본 파일과 같은 담당입니다.

### UI 담당 (Codex)

| 경로 | 내용 |
|---|---|
| `Assets/Fortress/FortressHud.cs` | HUD 표시 전체 |
| `Assets/Fortress/FortressCardSlot.cs` | 손패 카드 한 장 표시 |
| `Assets/Fortress/FortressActorHud.cs` | 턴 순서 초상화, 머리 위 체력바 |
| `Assets/Fortress/FortressClassCard.cs` | 캐릭터 선택 카드 |
| `Assets/Fortress/FortressHoldButton.cs` | 누르고 있는 버튼(이동, 충전 발사) |
| `Assets/Editor/FortressHudLayout.cs` | HUD 배치 메뉴 |
| `Assets/Editor/FortressPreview.cs` | Battlefield 창의 화면 미리보기 |
| `Assets/Editor/FortressSceneBuilder.cs` | 새 씬 생성(대부분 HUD 생성 코드) |
| `Assets/FortressContent/UI/` | 폰트 등 UI 에셋 |
| `Assets/Tests/PlayMode/FortressHudTests.cs` | UI 테스트(새로 만들 파일) |

### 게임플레이 담당 (Claude Code)

| 경로 | 내용 |
|---|---|
| `Assets/Fortress/FortressGame.cs` 외 `FortressGame.*.cs` | 전투 진행 (`FortressGame.Hud.cs`는 아래 "경계 파일" 참고) |
| `Assets/Fortress/FortressInput.cs` | 키 입력 |
| `Assets/Fortress/FortressArena.cs`, `FortressTerrain.cs`, `FortressSpawnPoint.cs` | 전장, 지형, 출전 위치 |
| `Assets/Fortress/FortressBattleRules.cs`, `FortressCard.cs`, `FortressCharacterDefinition.cs`, `FortressFighterView.cs` | 규칙, 카드 효과, 캐릭터 데이터 |
| `Assets/Editor/FortressTerrainPieces.cs`, `FortressPlacement.cs`, `FortressBattlefieldWindow.cs` | 지형 조각, 배치 진단, 전장 편집 창 |
| `Assets/Editor/FortressAnimationBuilder.cs`, `FortressArtImporter.cs`, `FortressAuthoringEditors.cs` | 캐릭터 애니메이션, 아트 가져오기, 데이터 인스펙터 |
| `Assets/FortressContent/Art/`, `Data/`, `Input/`, `Prefabs/` | 아트, 규칙·캐릭터 데이터, 입력 설정, 캐릭터 프리팹 |
| `Assets/Resources/` | 런타임 아트, 궤적 셰이더 |
| `Assets/Tests/PlayMode/FortressPlayModeTests.cs` | 게임플레이 테스트 |
| `Tools/` | 이미지 자르기 스크립트 |

## 경계 파일 (특히 주의)

### `Assets/Fortress/FortressGame.Hud.cs` — UI와 게임의 창구

UI는 이 파일의 속성과 `Request...` 메서드로만 게임 상태를 읽고 조작합니다. UI 코드는 `FortressGame`의 다른 내부 필드를 쓰지 않습니다.

- **담당은 게임플레이(Claude Code)입니다.**
- UI 쪽에서 새 값이 필요하면(예: 돈, 유물 목록), Codex는 필요한 속성 이름과 의미를 사람에게 알립니다. 게임플레이 쪽이 추가해 줍니다.
- 게임플레이 쪽은 이미 있는 속성의 이름이나 의미를 바꾸기 전에 사람에게 알립니다. UI가 깨질 수 있습니다.

### `Assets/Scenes/FortressBattle.unity` — 씬 파일

HUD 캔버스와 전장이 한 파일에 들어 있어서 **두 쪽 모두 건드릴 수 있고, 충돌하면 병합이 사실상 불가능합니다.**

- 씬을 수정하기 전에 사람에게 먼저 알리고, 상대가 씬을 수정 중이 아닌지 확인합니다.
- 씬 변경은 코드와 섞지 말고 **씬만 단독으로, 작게** 커밋합니다.
- UI 담당은 `Battle UI - Canvas` 아래만, 게임플레이 담당은 `Battlefield`와 `Battle Controller` 아래만 수정합니다.
- 나중에 HUD 캔버스를 프리팹으로 분리하면 이 규칙은 필요 없어집니다.

### 공용 설정 — 사람이 직접 판단

`ProjectSettings/`, `Packages/`, `Assets/Settings/`, 문서(`*.md`)는 수정하기 전에 사람에게 확인합니다. Unity가 자동으로 바꾼 설정 파일은 커밋에 넣지 않습니다.

## 작업 흐름

- 작업 전에 `git pull`로 상대 변경을 받습니다.
- 작게 자주 커밋하고 푸시합니다. 오래 쌓아 두면 충돌이 커집니다.
- 커밋 전에 Unity에서 컴파일 에러가 없는지, 플레이가 되는지 확인합니다.
