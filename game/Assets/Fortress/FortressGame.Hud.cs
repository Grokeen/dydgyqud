using UnityEngine;

namespace MiniFortress
{
    public sealed partial class FortressGame
    {
        GUIStyle titleStyle, textStyle, smallStyle, buttonStyle, panelStyle, accentButtonStyle, sliderStyle, thumbStyle;
        Texture2D Chrome(Color fill, Color trim, int width = 128, int height = 64)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int dx = Mathf.Min(x, width - x - 1), dy = Mathf.Min(y, height - y - 1);
                    int edge = Mathf.Min(dx, dy);
                    Color c = fill * Mathf.Lerp(0.68f, 1.18f, y / (float)height); c.a = fill.a;
                    if (dx + dy < 7) c = Color.clear;
                    else if (edge < 2 || dx + dy < 9) c = trim * (y > height / 2 ? 1.1f : 0.55f);
                    else if (edge == 3) c = new Color(0.015f, 0.02f, 0.027f, 1);
                    else if (edge == 5) c = new Color(trim.r, trim.g, trim.b, 0.3f);
                    else if (dx < 12 && dy < 12 && Mathf.Abs(dx - dy) < 2) c = trim;
                    pixels[y * width + x] = c;
                }
            texture.SetPixels(pixels); texture.Apply(); ownedAssets.Add(texture); return texture;
        }
        void InitStyles()
        {
            titleStyle = new GUIStyle(GUI.skin.label) { font = uiFont, fontSize = 25, fontStyle = FontStyle.Bold };
            textStyle = new GUIStyle(GUI.skin.label) { font = uiFont, fontSize = 20 };
            smallStyle = new GUIStyle(GUI.skin.label) { font = uiFont, fontSize = 16 };
            panelStyle = new GUIStyle { border = new RectOffset(14, 14, 14, 14) };
            panelStyle.normal.background = Chrome(new Color(0.035f, 0.055f, 0.069f, 0.94f), gold);
            buttonStyle = new GUIStyle(GUI.skin.button) { font = uiFont, fontSize = 19, fontStyle = FontStyle.Bold, border = new RectOffset(14, 14, 14, 14), padding = new RectOffset(10, 10, 5, 5) };
            buttonStyle.normal.background = Chrome(new Color(0.065f, 0.13f, 0.17f), new Color(0.39f, 0.5f, 0.53f));
            buttonStyle.hover.background = Chrome(new Color(0.09f, 0.23f, 0.28f), gold);
            buttonStyle.active.background = Chrome(new Color(0.04f, 0.1f, 0.12f), gold);
            buttonStyle.focused.background = buttonStyle.hover.background;
            buttonStyle.normal.textColor = buttonStyle.hover.textColor = buttonStyle.active.textColor = buttonStyle.focused.textColor = pale;
            accentButtonStyle = new GUIStyle(buttonStyle);
            accentButtonStyle.normal.background = Chrome(new Color(0.045f, 0.23f, 0.29f), gold);
            sliderStyle = new GUIStyle(GUI.skin.horizontalSlider) { fixedHeight = 9, border = new RectOffset(5, 5, 4, 4) };
            sliderStyle.normal.background = Chrome(new Color(0.12f, 0.18f, 0.19f), new Color(0.45f, 0.4f, 0.27f), 64, 18);
            thumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb) { fixedWidth = 16, fixedHeight = 22, border = new RectOffset(5, 5, 5, 5) };
            thumbStyle.normal.background = Chrome(gold, pale, 24, 32);
            thumbStyle.hover.background = thumbStyle.active.background = thumbStyle.normal.background;
            titleStyle.normal.textColor = textStyle.normal.textColor = smallStyle.normal.textColor = pale;
        }
        void Fill(Rect r, Color color) { GUI.color = color; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white; }
        void Panel(Rect r)
        {
            if (Event.current.type == EventType.Repaint) panelStyle.Draw(r, GUIContent.none, false, false, false, false);
        }
        void Text(float x, float y, float w, string text, GUIStyle style = null) => GUI.Label(new Rect(x, y, w, 36), text, style ?? textStyle);
        void HealthBar(Rect r, int hp, int max, Color color)
        {
            Fill(r, new Color(0.13f, 0.16f, 0.18f)); Fill(new Rect(r.x, r.y, r.width * hp / max, r.height), color);
            Fill(new Rect(r.x, r.y, r.width * hp / max, 2), new Color(1, 1, 0.8f, 0.35f));
        }
        void Portrait(Rect r, Fighter f)
        {
            Texture2D texture = f == fighters[0] ? classPortraits[(int)playerClass] : f.body.sprite.texture;
            float cropHeight = 0.42f, cropWidth = Mathf.Min(0.68f, cropHeight * r.width / r.height * texture.height / texture.width);
            GUI.DrawTextureWithTexCoords(r, texture, new Rect(Mathf.Clamp(0.64f - cropWidth / 2, 0, 1 - cropWidth), 0.56f, cropWidth, cropHeight));
        }
        void DrawSelection()
        {
            Fill(new Rect(0, 0, 1600, 900), new Color(0.015f, 0.025f, 0.045f, 0.48f));
            Panel(new Rect(280, 87, 1040, 97));
            Text(325, 100, 950, "검은 달의 성채 · 출전 준비", titleStyle);
            Text(325, 142, 900, "성채에 도전할 캐릭터 한 명을 선택하세요.", textStyle);
            for (int i = 0; i < 2; i++)
            {
                float x = 280 + i * 540;
                bool selected = (int)highlightedClass == i;
                Panel(new Rect(x, 204, 500, 487));
                if (selected) Fill(new Rect(x + 18, 210, 464, 3), gold);
                Text(x + 28, 228, 440, (i == 0 ? "01  궁수" : "02  창병") + (selected ? "  · 선택됨" : ""), titleStyle);
                GUI.DrawTexture(new Rect(x + 12, 280, 278, 302), classPortraits[i], ScaleMode.ScaleToFit);
                Text(x + 302, 322, 180, i == 0 ? "활과 화살" : "투척용 창", textStyle);
                Text(x + 302, 372, 180, i == 0 ? "체력 120" : "체력 140", textStyle);
                Text(x + 302, 412, 180, i == 0 ? "직격 피해 32" : "직격 피해 40", textStyle);
                Text(x + 302, 452, 180, "이동 10 m / 턴", smallStyle);
                Text(x + 28, 582, 450, i == 0 ? "정교한 곡사 · 넓은 범위 피해" : "강력한 직격 · 높은 생존력", smallStyle);
                if (GUI.Button(new Rect(x + 28, 630, 444, 43), selected ? "선택됨" : i == 0 ? "궁수 선택" : "창병 선택", selected ? accentButtonStyle : buttonStyle))
                    highlightedClass = (PlayerClass)i;
            }
            if (GUI.Button(new Rect(570, 725, 460, 65), (highlightedClass == PlayerClass.Archer ? "궁수" : "창병") + "로 전투 시작  [Enter]", accentButtonStyle)) BeginBattle();
            Text(465, 820, 800, "1 / 2 또는 ← / → 선택 · Enter 시작 · 출전 인원 1명", smallStyle);
        }
        void OnGUI()
        {
            if (pixelFrame == null || fighters.Count == 0) return;
            if (titleStyle == null) InitStyles();
            float scale = Mathf.Min(Screen.width / 1600f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.identity; Fill(new Rect(0, 0, Screen.width, Screen.height), Color.black);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1600 * scale) / 2, (Screen.height - 900 * scale) / 2, 0), Quaternion.identity, Vector3.one * scale);
            GUI.DrawTexture(new Rect(0, 0, 1600, 900), pixelFrame, ScaleMode.StretchToFill);
            if (phase == Phase.Selecting) { DrawSelection(); return; }
            Fill(new Rect(0, 0, 1600, 87), new Color(0.005f, 0.01f, 0.02f, 0.56f));
            Panel(new Rect(16, 14, 368, 65)); Text(32, 19, 350, "검은 달의 성채", titleStyle); Text(32, 51, 345, fighters[0].name + "의 공성전  /  무너진 교각", smallStyle);
            Text(416, 23, 150, "턴 " + round, titleStyle);
            for (int i = 0; i < fighters.Count; i++)
            {
                Rect slot = new Rect(575 + i * 73, 12, 64, 68); Panel(slot);
                if (current == i && phase != Phase.Finished) Fill(new Rect(slot.x, slot.y, slot.width, 4), blue);
                GUI.color = fighters[i].hp > 0 ? Color.white : new Color(0.3f, 0.3f, 0.3f);
                Portrait(new Rect(slot.x + 8, slot.y + 4, 48, 55), fighters[i]); GUI.color = Color.white;
                Text(slot.x + 8, slot.y + 45, 55, i == 0 ? "나" : fighters[i].hp > 0 ? "적 " + i : "처치", smallStyle);
            }
            Fighter player = fighters[0];
            Panel(new Rect(16, 96, 235, 160)); Portrait(new Rect(26, 105, 67, 102), player);
            Text(101, 105, 145, player.name + " · 1P", textStyle);
            HealthBar(new Rect(103, 146, 130, 12), player.hp, player.maxHp, new Color(0.2f, 0.8f, 0.46f));
            Text(103, 165, 140, player.hp + " / " + player.maxHp, smallStyle);
            Text(31, 216, 213, "이동 " + moveRemaining.ToString("0.0") + " / 10 m", smallStyle);
            Panel(new Rect(1350, 96, 234, 208)); Text(1368, 108, 204, "전장 정보", titleStyle);
            Text(1368, 153, 204, "높낮이 · 다리 · 절벽", smallStyle); Text(1368, 188, 204, "승리 조건", textStyle);
            Text(1368, 224, 204, "모든 적 처치  " + (4 - EnemiesAlive()) + " / 4", smallStyle);
            Text(1368, 262, 204, "남은 적  " + EnemiesAlive() + "명", smallStyle);
            for (int i = 0; i < fighters.Count; i++)
            {
                Fighter f = fighters[i]; if (f.hp <= 0) continue;
                Vector3 view = worldCamera.WorldToViewportPoint(f.feet + Vector2.up * ((ActorHeight + 0.45f) * f.root.localScale.x));
                float x = view.x * 1600 - 48, y = (1 - view.y) * 900;
                Fill(new Rect(x - 2, y - 2, 100, 14), new Color(0.025f, 0.03f, 0.04f));
                HealthBar(new Rect(x, y, 96, 10), f.hp, f.maxHp, i == 0 ? new Color(0.2f, 0.85f, 0.45f) : new Color(0.86f, 0.25f, 0.2f));
                Text(x, y - 28, 175, (i == 0 ? "1P " : "") + f.hp + "/" + f.maxHp, smallStyle);
            }
            // Only actual gameplay controls appear here; no card/currency placeholders.
            Panel(new Rect(16, 749, 339, 135)); Portrait(new Rect(28, 758, 76, 116), player);
            Text(116, 762, 225, player.name, titleStyle); Text(116, 804, 225, "HP  " + player.hp + " / " + player.maxHp, textStyle);
            Text(116, 842, 225, "이동  " + moveRemaining.ToString("0.0") + " m", smallStyle);
            Panel(new Rect(370, 749, 827, 135)); GUI.enabled = phase == Phase.Aim && !playerHasAttacked;
            Text(390, 760, 240, "각도  " + player.angle.ToString("0") + "°");
            player.angle = GUI.HorizontalSlider(new Rect(397, 810, 225, 25), player.angle, 10, 80, sliderStyle, thumbStyle);
            Text(656, 760, 238, "위력  " + player.power.ToString("0.0"));
            player.power = GUI.HorizontalSlider(new Rect(662, 810, 225, 25), player.power, 10, 38, sliderStyle, thumbStyle);
            GUI.enabled = phase == Phase.Aim;
            bool left = GUI.RepeatButton(new Rect(929, 760, 72, 37), "A", buttonStyle);
            bool right = GUI.RepeatButton(new Rect(1009, 760, 72, 37), "D", buttonStyle);
            if (Event.current.type == EventType.Repaint) mouseMove = (right ? 1 : 0) - (left ? 1 : 0);
            if (GUI.Button(new Rect(1089, 760, 91, 37), "점프", buttonStyle)) Jump();
            GUI.enabled = true;
            Text(390, 843, 802, "A/D 이동  W 점프  S 다리 아래로  ↑↓ 각도  ←→ 위력  Space 발사", smallStyle);
            Text(1220, 710, 364, "공격 " + (playerHasAttacked ? "0" : "1") + " / 1  ·  이동 " + moveRemaining.ToString("0.0") + " m", smallStyle);
            GUI.enabled = phase == Phase.Aim && grounded && !playerHasAttacked;
            if (GUI.Button(new Rect(1220, 750, 364, 52), phase == Phase.Finished ? "전투 종료" : playerHasAttacked ? "공격 완료" : AttackName + "  [Space]", buttonStyle)) Fire(0);
            GUI.enabled = phase == Phase.Aim && grounded;
            if (GUI.Button(new Rect(1220, 814, 364, 70), phase == Phase.Finished ? "전투 종료" : current == 0 ? "턴 넘기기" : "적 행동 중", accentButtonStyle)) EndPlayerTurn();
            GUI.enabled = true;
            if (GUI.Button(new Rect(1220, 22, 190, 45), "캐릭터 선택 [Esc]", buttonStyle)) { OpenSelection(); return; }
            if (GUI.Button(new Rect(1425, 22, 159, 45), "다시 시작", buttonStyle)) Restart();
            Panel(new Rect(370, 698, 827, 40)); Text(386, 701, 799, message, smallStyle);
            if (phase == Phase.Finished)
            {
                Panel(new Rect(490, 320, 620, 190)); Text(530, 346, 550, message, titleStyle);
                if (GUI.Button(new Rect(650, 422, 300, 58), "다시 도전", buttonStyle)) Restart();
            }
        }
    }
}
