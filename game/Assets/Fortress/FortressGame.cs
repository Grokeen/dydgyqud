using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MiniFortress
{
    public sealed partial class FortressGame : MonoBehaviour
    {
        const float Gravity = 12, ShotStep = 0.0125f, MoveLimit = 10;
        enum Phase { Aim, Flight, Impact, EnemyAim, Finished }
        sealed class Fighter
        {
            public Vector2 feet;
            public int hp, maxHp;
            public Transform root, weapon, loadedArrow;
            public SpriteRenderer body;
            public string name;
            public float angle = 48, power = 26;
        }
        struct Platform
        {
            public float left, right, top, bottom;
            public Platform(float l, float r, float t, float b) { left = l; right = r; top = t; bottom = b; }
        }
        readonly Platform[] terrain = {
            new Platform(-3, 22, 13, -14), new Platform(22, 34, 3, -14),
            new Platform(37, 50, 6, -14), new Platform(22, 64, 13, 12.3f),
            new Platform(64, 81, 22, -14), new Platform(81, 103, 5, -14),
            new Platform(88, 103, 13, 12),
            new Platform(57, 60, 16, 13), new Platform(60, 64, 19, 13)
        };
        readonly Vector2[] starts = { new Vector2(9, 13), new Vector2(46, 13), new Vector2(72, 22), new Vector2(95, 13), new Vector2(85, 5) };
        readonly List<Fighter> fighters = new List<Fighter>();
        readonly List<Object> ownedAssets = new List<Object>();
        readonly List<Transform> guide = new List<Transform>();
        Camera worldCamera;
        RenderTexture pixelFrame;
        Sprite square;
        Transform arrow, burst;
        Font uiFont;
        Phase phase;
        Vector2 shotPosition, shotVelocity, safePosition;
        float accumulator, shotAge, timer, moveRemaining, fallSpeed, mouseMove;
        bool grounded = true;
        int current, round, playerFacing = 1;
        string message;
        readonly Color gold = new Color(0.77f, 0.64f, 0.39f);
        readonly Color pale = new Color(0.9f, 0.93f, 0.91f);
        readonly Color blue = new Color(0.3f, 0.76f, 0.94f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (SceneManager.GetActiveScene().name == "SampleScene" && FindAnyObjectByType<FortressGame>() == null)
                new GameObject("Archer Fortress").AddComponent<FortressGame>();
        }
        void Start()
        {
            square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1);
            ownedAssets.Add(square);
            foreach (Camera camera in FindObjectsByType<Camera>()) camera.enabled = false;
            var displayCamera = new GameObject("HUD display camera").AddComponent<Camera>();
            displayCamera.transform.SetParent(transform);
            displayCamera.clearFlags = CameraClearFlags.SolidColor;
            displayCamera.backgroundColor = Color.black;
            displayCamera.cullingMask = 0;
            worldCamera = new GameObject("Pixel battlefield camera").AddComponent<Camera>();
            worldCamera.transform.SetParent(transform);
            worldCamera.transform.position = new Vector3(50, 18, -30);
            worldCamera.orthographic = true; worldCamera.orthographicSize = 30; worldCamera.aspect = 16f / 9;
            worldCamera.clearFlags = CameraClearFlags.SolidColor;
            worldCamera.backgroundColor = new Color(0.065f, 0.105f, 0.18f);
            pixelFrame = new RenderTexture(800, 450, 16) { filterMode = FilterMode.Point, antiAliasing = 1 };
            pixelFrame.Create(); worldCamera.targetTexture = pixelFrame;
            uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 20);
            ownedAssets.Add(uiFont);
            BuildWorld();
            AddFighter("궁수", starts[0], 120, blue, false);
            AddFighter("다리 파수꾼", starts[1], 40, new Color(0.56f, 0.65f, 0.29f), false);
            AddFighter("성채 대장", starts[2], 64, new Color(0.73f, 0.35f, 0.22f), true);
            AddFighter("절벽 사수", starts[3], 40, new Color(0.62f, 0.56f, 0.31f), false);
            AddFighter("하단 경비병", starts[4], 40, new Color(0.45f, 0.62f, 0.29f), false);
            arrow = BuildArrow(transform, "Flying arrow", 30);
            burst = Shape("Impact flash", Vector2.zero, Vector2.one, new Color(1, 0.6f, 0.2f, 0.7f), 31);
            for (int i = 0; i < 28; i++) guide.Add(Shape("Aim dot", Vector2.zero, Vector2.one * 0.13f, new Color(1, 0.78f, 0.43f), 22));
            Restart();
        }
        int Facing(int index) => index == 0 ? playerFacing : fighters[0].feet.x < fighters[index].feet.x ? -1 : 1;
        Vector2 Direction(int index, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            return new Vector2(Facing(index) * Mathf.Cos(r), Mathf.Sin(r));
        }
        Vector2 Origin(int index, float degrees) => fighters[index].feet + Vector2.up * (1.9f * fighters[index].root.localScale.x) + Direction(index, degrees) * (1.6f * fighters[index].root.localScale.x);
        void Update()
        {
            if (fighters.Count == 0) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            Keyboard keys = Keyboard.current;
            if (keys != null && keys.rKey.wasPressedThisFrame) { Restart(); return; }
            if (phase == Phase.Aim)
            {
                Fighter player = fighters[0]; float move = mouseMove; mouseMove = 0;
                if (keys != null)
                {
                    move += (keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0);
                    player.angle = Mathf.Clamp(player.angle + ((keys.upArrowKey.isPressed ? 1 : 0) - (keys.downArrowKey.isPressed ? 1 : 0)) * 35 * dt, 10, 80);
                    player.power = Mathf.Clamp(player.power + ((keys.rightArrowKey.isPressed ? 1 : 0) - (keys.leftArrowKey.isPressed ? 1 : 0)) * 12 * dt, 10, 38);
                    if (keys.wKey.wasPressedThisFrame) Jump();
                    if (keys.sKey.wasPressedThisFrame) DropFromBridge();
                }
                MovePlayer(Mathf.Clamp(move, -1, 1) * 5 * dt); FallPlayer(dt);
                if (keys != null && keys.spaceKey.wasPressedThisFrame && grounded && phase == Phase.Aim) Fire(0);
            }
            else if (phase == Phase.Flight)
            {
                accumulator += dt;
                while (accumulator >= ShotStep && phase == Phase.Flight)
                {
                    accumulator -= ShotStep; Integrate(ref shotPosition, ref shotVelocity); shotAge += ShotStep;
                    arrow.position = shotPosition;
                    arrow.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(shotVelocity.y, shotVelocity.x) * Mathf.Rad2Deg);
                    int hit = HitFighter(shotPosition, current);
                    if (hit >= 0 || HitsTerrain(shotPosition)) Impact(hit);
                    else if (shotPosition.x < -6 || shotPosition.x > 106 || shotPosition.y < -13 || shotAge > 10) Impact(-1, true);
                }
            }
            else if (phase == Phase.Impact)
            {
                timer -= dt; burst.localScale = Vector3.one * Mathf.Lerp(6, 0.4f, Mathf.Clamp01(timer / 0.55f));
                if (timer <= 0) NextTurn();
            }
            else if (phase == Phase.EnemyAim) { timer -= dt; if (timer <= 0) Fire(current); }
            UpdatePoses();
        }
        void MovePlayer(float distance)
        {
            if (phase != Phase.Aim || moveRemaining <= 0) return;
            if (Mathf.Abs(distance) > 0.0001f) playerFacing = distance > 0 ? 1 : -1;
            Fighter player = fighters[0];
            float wanted = Mathf.Clamp(player.feet.x + Mathf.Clamp(distance, -moveRemaining, moveRemaining), 0, 101);
            foreach (Platform p in terrain)
            {
                if (player.feet.y >= p.top - 0.05f || player.feet.y + 3 <= p.bottom) continue;
                if (wanted + 0.55f > p.left && wanted - 0.55f < p.right)
                {
                    if (player.feet.x <= p.left) wanted = Mathf.Min(wanted, p.left - 0.55f);
                    else if (player.feet.x >= p.right) wanted = Mathf.Max(wanted, p.right + 0.55f);
                }
            }
            moveRemaining = Mathf.Max(0, moveRemaining - Mathf.Abs(wanted - player.feet.x)); player.feet.x = wanted;
        }
        void LateUpdate()
        {
            if (worldCamera == null) return;
            // Keep high-arcing shots visible while retaining the complete battlefield.
            float top = phase == Phase.Flight ? Mathf.Max(48, shotPosition.y + 5) : 48;
            float size = (top + 12) * 0.5f;
            float blend = 1 - Mathf.Exp(-6 * Time.deltaTime);
            worldCamera.orthographicSize = Mathf.Lerp(worldCamera.orthographicSize, size, blend);
            worldCamera.transform.position = Vector3.Lerp(worldCamera.transform.position, new Vector3(50, top - size, -30), blend);
        }
        void Jump()
        {
            if (phase != Phase.Aim || !grounded || moveRemaining < 1) return;
            moveRemaining -= 1; fallSpeed = 11; grounded = false;
        }
        void DropFromBridge()
        {
            if (phase != Phase.Aim || !grounded) return;
            foreach (Platform p in terrain)
                if (p.top - p.bottom <= 1.1f && fighters[0].feet.x >= p.left && fighters[0].feet.x <= p.right && Mathf.Abs(fighters[0].feet.y - p.top) < 0.05f)
                {
                    fighters[0].feet.y -= 0.12f; fallSpeed = 0; grounded = false; return;
                }
        }
        void FallPlayer(float dt)
        {
            Fighter player = fighters[0]; float oldY = player.feet.y;
            fallSpeed -= Gravity * dt; float nextY = oldY + fallSpeed * dt;
            grounded = false; float landing = float.NegativeInfinity;
            foreach (Platform p in terrain)
            {
                if (player.feet.x < p.left || player.feet.x > p.right) continue;
                if (fallSpeed <= 0 && oldY >= p.top - 0.06f && nextY <= p.top) landing = Mathf.Max(landing, p.top);
                if (fallSpeed > 0 && oldY + 3 <= p.bottom && nextY + 3 >= p.bottom) { nextY = p.bottom - 3; fallSpeed = 0; }
            }
            if (!float.IsNegativeInfinity(landing)) { nextY = landing; fallSpeed = 0; grounded = true; }
            player.feet.y = nextY; if (grounded) safePosition = player.feet;
            if (player.feet.y < -10)
            {
                player.feet = safePosition; fallSpeed = 0; grounded = true; player.hp = Mathf.Max(0, player.hp - 15);
                message = "추락! 체력 15 감소 · 마지막 발판으로 복귀"; if (player.hp == 0) Finish(false);
            }
        }
        void UpdatePoses()
        {
            for (int i = 0; i < fighters.Count; i++)
            {
                Fighter f = fighters[i]; f.root.gameObject.SetActive(f.hp > 0); f.root.position = f.feet;
                int facing = Facing(i); f.body.flipX = facing < 0; f.weapon.localPosition = Vector3.up * 1.9f;
                Vector2 d = Direction(i, f.angle);
                f.weapon.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                f.weapon.localScale = new Vector3(1, facing, 1);
                f.loadedArrow.gameObject.SetActive(!((phase == Phase.Flight || phase == Phase.Impact) && current == i));
            }
            Vector2 point = Origin(0, fighters[0].angle), speed = Direction(0, fighters[0].angle) * fighters[0].power;
            bool visible = phase == Phase.Aim;
            foreach (Transform dot in guide)
            {
                for (int j = 0; j < 6; j++) { Integrate(ref point, ref speed); if (HitsTerrain(point) || HitFighter(point, 0) >= 0) visible = false; }
                dot.gameObject.SetActive(visible); dot.position = point;
            }
        }
        static void Integrate(ref Vector2 p, ref Vector2 v)
        {
            p += v * ShotStep + Vector2.down * (0.5f * Gravity * ShotStep * ShotStep); v += Vector2.down * (Gravity * ShotStep);
        }
        bool HitsTerrain(Vector2 p)
        {
            foreach (Platform t in terrain) if (p.x >= t.left && p.x <= t.right && p.y <= t.top && p.y >= t.bottom) return true;
            return false;
        }
        int HitFighter(Vector2 p, int owner)
        {
            for (int i = 0; i < fighters.Count; i++)
            {
                if (i == owner || fighters[i].hp <= 0 || (owner > 0 && i > 0)) continue;
                Fighter f = fighters[i]; float scale = f.root.localScale.x;
                if (Mathf.Abs(p.x - f.feet.x) <= 0.7f * scale && p.y >= f.feet.y && p.y <= f.feet.y + 3.25f * scale) return i;
            }
            return -1;
        }
        void Fire(int index)
        {
            if (phase != Phase.Aim && phase != Phase.EnemyAim) return;
            current = index; shotPosition = Origin(index, fighters[index].angle);
            shotVelocity = Direction(index, fighters[index].angle) * fighters[index].power;
            shotAge = accumulator = 0; arrow.position = shotPosition;
            arrow.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(shotVelocity.y, shotVelocity.x) * Mathf.Rad2Deg);
            arrow.gameObject.SetActive(true); phase = Phase.Flight; message = fighters[index].name + "의 화살!";
        }
        void Impact(int directHit, bool miss = false)
        {
            arrow.gameObject.SetActive(false); burst.position = shotPosition;
            burst.localScale = Vector3.one * 0.4f; burst.gameObject.SetActive(!miss); int total = 0;
            if (!miss)
                for (int i = 0; i < fighters.Count; i++)
                {
                    if (fighters[i].hp <= 0 || (current > 0 && i > 0)) continue;
                    Fighter f = fighters[i];
                    Vector2 nearest = new Vector2(f.feet.x, Mathf.Clamp(shotPosition.y, f.feet.y + 0.2f, f.feet.y + 2.9f * f.root.localScale.x));
                    float distance = Vector2.Distance(shotPosition, nearest); int maximum = current == 0 ? 32 : 10;
                    int damage = directHit == i ? maximum : Mathf.RoundToInt(maximum * Mathf.Clamp01(1 - distance / 3.5f));
                    f.hp = Mathf.Max(0, f.hp - damage); total += damage;
                }
            message = total > 0 ? "명중! 피해 " + total : "빗나갔습니다. 각도와 위력을 조절하세요.";
            phase = Phase.Impact; timer = 0.55f;
        }
        int EnemiesAlive()
        {
            int count = 0; for (int i = 1; i < fighters.Count; i++) if (fighters[i].hp > 0) count++; return count;
        }
        void NextTurn()
        {
            burst.gameObject.SetActive(false);
            if (fighters[0].hp <= 0) { Finish(false); return; }
            if (EnemiesAlive() == 0) { Finish(true); return; }
            do { current++; } while (current < fighters.Count && fighters[current].hp <= 0);
            if (current >= fighters.Count)
            {
                current = 0; round++; phase = Phase.Aim; moveRemaining = MoveLimit;
                message = "내 턴 · 이동하고 조준한 뒤 화살을 발사하세요.";
            }
            else { phase = Phase.EnemyAim; timer = 0.85f; PlanEnemy(current); message = fighters[current].name + " 조준 중…"; }
        }
        void PlanEnemy(int index)
        {
            Fighter f = fighters[index]; float best = float.MaxValue;
            for (float angle = 15; angle <= 78; angle += 3)
                for (float power = 10; power <= 38; power += 0.75f)
                {
                    Vector2 p = Origin(index, angle), v = Direction(index, angle) * power;
                    for (int step = 0; step < 800; step++)
                    {
                        Integrate(ref p, ref v);
                        if (HitsTerrain(p) || HitFighter(p, index) >= 0 || p.x < -6 || p.x > 106 || p.y < -13)
                        {
                            float error = Vector2.Distance(p, fighters[0].feet + Vector2.up * 1.5f);
                            if (error < best) { best = error; f.angle = angle; f.power = power; }
                            break;
                        }
                    }
                }
            f.angle = Mathf.Clamp(f.angle + Random.Range(-2.8f, 2.8f), 10, 80);
            f.power = Mathf.Clamp(f.power + Random.Range(-0.8f, 0.8f), 10, 38);
        }
        void Finish(bool won) { phase = Phase.Finished; message = won ? "승리 · 모든 적을 처치했습니다!" : "패배 · 궁수가 쓰러졌습니다."; }
        void Restart()
        {
            for (int i = 0; i < fighters.Count; i++) { fighters[i].hp = fighters[i].maxHp; fighters[i].feet = starts[i]; fighters[i].angle = 48; fighters[i].power = 26; }
            current = 0; round = 1; playerFacing = 1; moveRemaining = MoveLimit; grounded = true; fallSpeed = mouseMove = 0;
            safePosition = starts[0]; phase = Phase.Aim; arrow.gameObject.SetActive(false); burst.gameObject.SetActive(false);
            message = "궁수 한 명으로 성채를 돌파하세요 · A/D 이동 · W 점프"; UpdatePoses();
        }
        void OnDestroy()
        {
            if (worldCamera != null) worldCamera.targetTexture = null;
            if (pixelFrame != null) { pixelFrame.Release(); Destroy(pixelFrame); }
            foreach (Object asset in ownedAssets) if (asset != null) Destroy(asset);
        }
    }
}
