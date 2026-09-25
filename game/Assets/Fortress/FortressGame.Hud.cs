using UnityEngine;

namespace MiniFortress
{
    public sealed partial class FortressGame
    {
        GUIStyle titleStyle, textStyle, smallStyle, buttonStyle;
        void InitStyles()
        {
            titleStyle = new GUIStyle(GUI.skin.label) { font = uiFont, fontSize = 25, fontStyle = FontStyle.Bold };
            textStyle = new GUIStyle(GUI.skin.label) { font = uiFont, fontSize = 20 };
            smallStyle = new GUIStyle(GUI.skin.label) { font = uiFont, fontSize = 16 };
            buttonStyle = new GUIStyle(GUI.skin.button) { font = uiFont, fontSize = 21, fontStyle = FontStyle.Bold };
            titleStyle.normal.textColor = textStyle.normal.textColor = smallStyle.normal.textColor = pale;
        }
        void Fill(Rect r, Color color) { GUI.color = color; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white; }
        void Panel(Rect r)
        {
            Fill(r, new Color(gold.r * 0.8f, gold.g * 0.8f, gold.b * 0.8f, 1));
            Fill(new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4), new Color(0.035f, 0.06f, 0.075f, 0.95f));
        }
        void Text(float x, float y, float w, string text, GUIStyle style = null) => GUI.Label(new Rect(x, y, w, 36), text, style ?? textStyle);
        void HealthBar(Rect r, int hp, int max, Color color)
        {
            Fill(r, new Color(0.13f, 0.16f, 0.18f)); Fill(new Rect(r.x, r.y, r.width * hp / max, r.height), color);
        }
        void Portrait(Rect r, Fighter f) => GUI.DrawTexture(r, f.body.sprite.texture, ScaleMode.ScaleToFit);
        void OnGUI()
        {
            if (pixelFrame == null || fighters.Count == 0) return;
            if (titleStyle == null) InitStyles();
            float scale = Mathf.Min(Screen.width / 1600f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.identity; Fill(new Rect(0, 0, Screen.width, Screen.height), Color.black);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1600 * scale) / 2, (Screen.height - 900 * scale) / 2, 0), Quaternion.identity, Vector3.one * scale);
            GUI.DrawTexture(new Rect(0, 0, 1600, 900), pixelFrame, ScaleMode.StretchToFill);
            Panel(new Rect(16, 14, 368, 65)); Text(32, 19, 350, "검은 달의 성채", titleStyle); Text(32, 51, 345, "궁수의 공성전  /  무너진 교각", smallStyle);
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
            Text(101, 105, 145, "궁수 · 1P", textStyle);
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
                Vector3 view = worldCamera.WorldToViewportPoint(f.feet + Vector2.up * (3.7f * f.root.localScale.x));
                float x = view.x * 1600 - 48, y = (1 - view.y) * 900;
                Fill(new Rect(x - 2, y - 2, 100, 14), new Color(0.025f, 0.03f, 0.04f));
                HealthBar(new Rect(x, y, 96, 10), f.hp, f.maxHp, i == 0 ? new Color(0.2f, 0.85f, 0.45f) : new Color(0.86f, 0.25f, 0.2f));
                Text(x, y - 28, 175, (i == 0 ? "1P " : "") + f.hp + "/" + f.maxHp, smallStyle);
            }
            // Only actual gameplay controls appear here; no card/currency placeholders.
            Panel(new Rect(16, 749, 339, 135)); Portrait(new Rect(28, 758, 76, 116), player);
            Text(116, 762, 225, "궁수", titleStyle); Text(116, 804, 225, "HP  " + player.hp + " / " + player.maxHp, textStyle);
            Text(116, 842, 225, "이동  " + moveRemaining.ToString("0.0") + " m", smallStyle);
            Panel(new Rect(370, 749, 827, 135)); GUI.enabled = phase == Phase.Aim;
            Text(390, 760, 240, "각도  " + player.angle.ToString("0") + "°");
            player.angle = GUI.HorizontalSlider(new Rect(397, 810, 225, 25), player.angle, 10, 80);
            Text(656, 760, 238, "위력  " + player.power.ToString("0.0"));
            player.power = GUI.HorizontalSlider(new Rect(662, 810, 225, 25), player.power, 10, 38);
            bool left = GUI.RepeatButton(new Rect(929, 760, 72, 37), "A", buttonStyle);
            bool right = GUI.RepeatButton(new Rect(1009, 760, 72, 37), "D", buttonStyle);
            if (Event.current.type == EventType.Repaint) mouseMove = (right ? 1 : 0) - (left ? 1 : 0);
            if (GUI.Button(new Rect(1089, 760, 91, 37), "점프", buttonStyle)) Jump();
            GUI.enabled = true;
            Text(390, 843, 802, "A/D 이동  W 점프  S 다리 아래로  ↑↓ 각도  ←→ 위력  Space 발사", smallStyle);
            GUI.enabled = phase == Phase.Aim && grounded;
            if (GUI.Button(new Rect(1220, 779, 364, 66), phase == Phase.Aim ? "화살 발사  [Space]" : phase == Phase.Finished ? "전투 종료" : "적 행동 / 화살 비행 중", buttonStyle)) Fire(0);
            GUI.enabled = true;
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
