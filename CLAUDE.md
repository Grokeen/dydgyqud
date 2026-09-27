@AGENTS.md

# Claude Code 역할

이 저장소에서 Claude Code는 **게임플레이 담당**입니다. 위 `AGENTS.md`의 "게임플레이 담당" 파일만 수정합니다.

- UI 담당 파일(`FortressHud.cs`, `FortressHudLayout.cs` 등)은 읽기만 합니다. UI 변경이 필요하면 무엇을 바꿔야 하는지 사용자에게 알려 주세요. 사용자가 상대(Codex)에게 전달합니다.
- UI가 새 게임 값을 필요로 하면 `FortressGame.Hud.cs`에 읽기 전용 속성을 추가하는 것까지가 이쪽 일입니다.
- 씬 파일(`FortressBattle.unity`)을 수정해야 하면, 수정하기 전에 사용자에게 먼저 확인합니다.
