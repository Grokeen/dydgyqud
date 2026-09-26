using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MiniFortress
{
    public sealed partial class FortressGame : MonoBehaviour
    {
        const float Gravity = 12, ShotStep = 0.0125f, MoveLimit = 10;
        const float ActorHeight = 4.2f, ShoulderHeight = 2.5f;
        enum Phase { Selecting, Aim, Attack, Flight, Impact, EnemyMove, EnemyAim, Finished }
        const float EnemyMoveLimit = 4, EnemyMoveSpeed = 3;
        float enemyMoveTarget;
        float enemyMoveSpent;
        bool enemyMovingAfterAttack, hasPlayerImpact;
        Vector2 lastPlayerImpact;
        enum PlayerClass { Archer, Spearman }
        PlayerClass playerClass, highlightedClass;
        readonly Sprite[] classSprites = new Sprite[2];
        readonly Texture2D[] classPortraits = new Texture2D[2];
        Transform bowWeapon, spearWeapon, bowLoaded, spearLoaded, arrowProjectile, spearProjectile;
        bool IsSpearman => playerClass == PlayerClass.Spearman;
        string AttackName => IsSpearman ? "창 투척" : "화살 발사";
        sealed class Fighter
        {
            public Vector2 feet;
            public int hp, maxHp;
            public Transform root, weapon, loadedArrow;
            public Transform motion, aimPivot, weaponMotion;
            public Animator animator;
            public Vector2 previousFeet;
            public float deathRemaining;
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
            new Platform(-3, 18.3f, 24.7f, -14), new Platform(18.3f, 32.7f, 6.9f, -14),
            new Platform(38.3f, 52, 8.6f, -14), new Platform(18.3f, 63, 18.4f, 17.7f),
            new Platform(63, 81.5f, 28.5f, -14), new Platform(81.5f, 103, 6.9f, -14),
            new Platform(92, 103, 17, 16),
            new Platform(54, 58, 22, 18.4f), new Platform(58, 63, 24.5f, 18.4f)
        };
        readonly Vector2[] starts = { new Vector2(9, 24.7f), new Vector2(46, 18.4f), new Vector2(72, 28.5f), new Vector2(97, 17), new Vector2(85, 6.9f) };
        readonly List<Fighter> fighters = new List<Fighter>();
        readonly List<Object> ownedAssets = new List<Object>();
        Camera worldCamera;
        RenderTexture pixelFrame;
        Sprite square;
        Transform arrow, burst;
        Font uiFont;
        Phase phase;
        Vector2 shotPosition, shotVelocity, safePosition;
        float accumulator, shotAge, timer, moveRemaining, fallSpeed, mouseMove;
        bool grounded = true;
        bool playerHasAttacked;
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
            worldCamera = new GameObject("Illustrated battlefield camera").AddComponent<Camera>();
            worldCamera.transform.SetParent(transform);
            worldCamera.transform.position = new Vector3(50, 18, -30);
            worldCamera.orthographic = true; worldCamera.orthographicSize = 30; worldCamera.aspect = 16f / 9;
            worldCamera.clearFlags = CameraClearFlags.SolidColor;
            worldCamera.backgroundColor = new Color(0.065f, 0.105f, 0.18f);
            pixelFrame = new RenderTexture(1920, 1080, 24) { filterMode = FilterMode.Bilinear, antiAliasing = 1 };
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
            arrowProjectile = arrow;
            InitializeClasses();
            burst = Shape("Impact flash", Vector2.zero, Vector2.one, new Color(1, 0.6f, 0.2f, 0.7f), 31);
            burst.GetComponent<SpriteRenderer>().sprite = softCircle;
            BuildTrajectoryEffects();
            Restart(); OpenSelection();
        }
        int Facing(int index) => index == 0 ? playerFacing : fighters[0].feet.x < fighters[index].feet.x ? -1 : 1;
        Vector2 Direction(int index, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            return new Vector2(Facing(index) * Mathf.Cos(r), Mathf.Sin(r));
        }
        Vector2 Origin(int index, float degrees) => fighters[index].feet + Vector2.up * (ShoulderHeight * fighters[index].root.localScale.x) + Direction(index, degrees) * (1.6f * fighters[index].root.localScale.x);
        void Update()
        {
            if (fighters.Count == 0) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            Keyboard keys = Keyboard.current;
            if (phase == Phase.Selecting)
            {
                if (keys != null)
                {
                    if (keys.digit1Key.wasPressedThisFrame || keys.leftArrowKey.wasPressedThisFrame) highlightedClass = PlayerClass.Archer;
                    if (keys.digit2Key.wasPressedThisFrame || keys.rightArrowKey.wasPressedThisFrame) highlightedClass = PlayerClass.Spearman;
                    if (keys.enterKey.wasPressedThisFrame) BeginBattle();
                }
                return;
            }
            if (keys != null && keys.escapeKey.wasPressedThisFrame) { OpenSelection(); return; }
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
            else if (phase == Phase.Attack)
            {
                timer -= Time.deltaTime;
                if (timer <= 0) LaunchShot();
            }
            else if (phase == Phase.Flight)
            {
                accumulator += dt;
                while (accumulator >= ShotStep && phase == Phase.Flight)
                {
                    accumulator -= ShotStep; Integrate(ref shotPosition, ref shotVelocity); shotAge += ShotStep;
                    arrow.position = shotPosition;
                    RecordShotTrail(shotPosition);
                    arrow.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(shotVelocity.y, shotVelocity.x) * Mathf.Rad2Deg);
                    int hit = HitFighter(shotPosition, current);
                    if (hit >= 0 || HitsTerrain(shotPosition)) Impact(hit);
                    else if (ShotExpired(shotPosition, shotAge)) Impact(-1, true);
                }
            }
            else if (phase == Phase.Impact)
            {
                timer -= dt;
                float diameter = current == 0 && IsSpearman ? 3 : 6;
                burst.localScale = Vector3.one * Mathf.Lerp(diameter, 0.4f, Mathf.Clamp01(timer / 0.55f));
                if (timer <= 0) ResolveShot();
            }
            else if (phase == Phase.EnemyMove) UpdateEnemyMovement(dt);
            else if (phase == Phase.EnemyAim) { timer -= dt; if (timer <= 0) Fire(current); }
            UpdatePoses(dt);
            UpdateShotTrail(Time.deltaTime);
        }
        void MovePlayer(float distance)
        {
            if (phase != Phase.Aim || moveRemaining <= 0) return;
            if (Mathf.Abs(distance) > 0.0001f) playerFacing = distance > 0 ? 1 : -1;
            Fighter player = fighters[0];
            float wanted = Mathf.Clamp(player.feet.x + Mathf.Clamp(distance, -moveRemaining, moveRemaining), 0, 101);
            foreach (Platform p in terrain)
            {
                if (player.feet.y >= p.top - 0.05f || player.feet.y + ActorHeight <= p.bottom) continue;
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
            FrameTrajectoryCamera(1 - Mathf.Exp(-6 * Time.deltaTime));
            AnimateTrajectoryEffects(Time.time);
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
                if (fallSpeed > 0 && oldY + ActorHeight <= p.bottom && nextY + ActorHeight >= p.bottom) { nextY = p.bottom - ActorHeight; fallSpeed = 0; }
            }
            if (!float.IsNegativeInfinity(landing)) { nextY = landing; fallSpeed = 0; grounded = true; }
            player.feet.y = nextY; if (grounded) safePosition = player.feet;
            if (player.feet.y < -10)
            {
                player.feet = safePosition; fallSpeed = 0; grounded = true; player.hp = Mathf.Max(0, player.hp - 15);
                player.previousFeet = player.feet;
                PlayDamageReaction(player);
                message = "추락! 체력 15 감소 · 마지막 발판으로 복귀"; if (player.hp == 0) Finish(false);
            }
        }
        void UpdatePoses(float dt = 0)
        {
            for (int i = 0; i < fighters.Count; i++)
            {
                Fighter f = fighters[i]; f.root.position = f.feet;
                UpdateFighterAnimation(i, dt);
                if (f.hp <= 0) continue;
                f.motion.localScale = new Vector3(Facing(i), 1, 1);
                f.aimPivot.localPosition = Vector3.up * ShoulderHeight;
                f.aimPivot.localRotation = Quaternion.Euler(0, 0, f.angle);
                f.loadedArrow.gameObject.SetActive(!(i == 0 && playerHasAttacked && phase != Phase.Attack) && !((phase == Phase.Flight || phase == Phase.Impact) && current == i));
            }
            UpdateTrajectoryPreview();
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
                if (Mathf.Abs(p.x - f.feet.x) <= 0.7f * scale && p.y >= f.feet.y && p.y <= f.feet.y + ActorHeight * scale) return i;
            }
            return -1;
        }
        void Fire(int index)
        {
            if (index == 0)
            {
                if (phase != Phase.Aim || current != 0 || !grounded || playerHasAttacked) return;
                playerHasAttacked = true;
            }
            else if (phase != Phase.EnemyAim || index != current) return;
            current = index;
            phase = Phase.Attack;
            timer = index == 0 && IsSpearman ? SpearReleaseTime : BowReleaseTime;
            fighters[index].animator.SetTrigger(index == 0 && IsSpearman ? SpearAttackParameter : BowAttackParameter);
            message = fighters[index].name + (index == 0 && IsSpearman ? " 창 투척 준비…" : " 활시위를 당기는 중…");
        }
        void LaunchShot()
        {
            int index = current;
            arrowProjectile.gameObject.SetActive(false); spearProjectile.gameObject.SetActive(false);
            arrow = index == 0 && IsSpearman ? spearProjectile : arrowProjectile;
            current = index; shotPosition = Origin(index, fighters[index].angle);
            shotVelocity = Direction(index, fighters[index].angle) * fighters[index].power;
            shotAge = accumulator = 0; arrow.position = shotPosition;
            arrow.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(shotVelocity.y, shotVelocity.x) * Mathf.Rad2Deg);
            arrow.gameObject.SetActive(true); phase = Phase.Flight;
            BeginShotTrail();
            message = fighters[index].name + (index == 0 && IsSpearman ? "의 창 투척!" : "의 화살!");
        }
        void Impact(int directHit, bool miss = false)
        {
            StopShotTrail();
            if (current == 0 && !miss) { lastPlayerImpact = shotPosition; hasPlayerImpact = true; }
            arrow.gameObject.SetActive(false); burst.position = shotPosition;
            burst.localScale = Vector3.one * 0.4f; burst.gameObject.SetActive(!miss); int total = 0;
            if (!miss)
                for (int i = 0; i < fighters.Count; i++)
                {
                    if (fighters[i].hp <= 0 || (current > 0 && i > 0)) continue;
                    Fighter f = fighters[i];
                    Vector2 nearest = new Vector2(f.feet.x, Mathf.Clamp(shotPosition.y, f.feet.y + 0.2f, f.feet.y + 2.9f * f.root.localScale.x));
                    float distance = Vector2.Distance(shotPosition, nearest);
                    int maximum = current == 0 ? (IsSpearman ? 40 : 32) : 10;
                    float radius = current == 0 && IsSpearman ? 1.5f : 3.5f;
                    int damage = directHit == i ? maximum : Mathf.RoundToInt(maximum * Mathf.Clamp01(1 - distance / radius));
                    f.hp = Mathf.Max(0, f.hp - damage); total += damage;
                    if (damage > 0) PlayDamageReaction(f);
                }
            message = total > 0 ? "명중! 피해 " + total : "빗나갔습니다. 각도와 위력을 조절하세요.";
            phase = Phase.Impact; timer = 0.55f;
        }
        int EnemiesAlive()
        {
            int count = 0; for (int i = 1; i < fighters.Count; i++) if (fighters[i].hp > 0) count++; return count;
        }
        void ResolveShot()
        {
            burst.gameObject.SetActive(false);
            if (fighters[0].hp <= 0) { Finish(false); return; }
            if (EnemiesAlive() == 0) { Finish(true); return; }
            if (current == 0)
            {
                phase = Phase.Aim;
                message += " · 남은 이동 후 '턴 넘기기'를 누르세요.";
            }
            else if (!TryEnemyRepositionAfterAttack()) NextTurn();
        }
        void EndPlayerTurn()
        {
            // A shot, landing, or exhausted movement never ends the player's turn automatically.
            if (phase != Phase.Aim || current != 0 || !grounded) return;
            mouseMove = 0;
            NextTurn();
        }
        void NextTurn()
        {
            burst.gameObject.SetActive(false);
            if (fighters[0].hp <= 0) { Finish(false); return; }
            if (EnemiesAlive() == 0) { Finish(true); return; }
            do { current++; } while (current < fighters.Count && fighters[current].hp <= 0);
            if (current >= fighters.Count)
            {
                current = 0; round++; phase = Phase.Aim; moveRemaining = MoveLimit; playerHasAttacked = false;
                message = "내 턴 · 이동하고 조준한 뒤 " + AttackName + "!";
            }
            else PlanEnemyTurn();
        }
        bool EnemyCanStand(int index, float x)
        {
            Fighter enemy = fighters[index];
            float halfWidth = 0.7f * enemy.root.localScale.x;
            float height = ActorHeight * enemy.root.localScale.x;
            if (x - halfWidth < 0 || x + halfWidth > 102) return false;
            // Both feet and the centre need support; never cross a gap or walk off a ledge.
            for (int sample = -1; sample <= 1; sample++)
            {
                float foot = x + sample * halfWidth;
                bool supported = false;
                foreach (Platform p in terrain)
                    if (Mathf.Abs(p.top - enemy.feet.y) < 0.05f && foot >= p.left && foot <= p.right) { supported = true; break; }
                if (!supported) return false;
            }
            foreach (Platform p in terrain)
                if (x + halfWidth > p.left && x - halfWidth < p.right && enemy.feet.y + 0.05f < p.top && enemy.feet.y + height > p.bottom)
                    return false;
            for (int i = 0; i < fighters.Count; i++)
            {
                if (i == index || fighters[i].hp <= 0) continue;
                Fighter other = fighters[i];
                if (Mathf.Abs(x - other.feet.x) < halfWidth + 0.7f * other.root.localScale.x + 0.2f &&
                    enemy.feet.y < other.feet.y + ActorHeight * other.root.localScale.x && enemy.feet.y + height > other.feet.y)
                    return false;
            }
            return true;
        }
        float ReachableEnemyX(int index, float distance)
        {
            float start = fighters[index].feet.x, last = start;
            int steps = Mathf.CeilToInt(Mathf.Abs(distance) / 0.1f);
            for (int i = 1; i <= steps; i++)
            {
                float candidate = start + distance * i / steps;
                if (!EnemyCanStand(index, candidate)) break;
                last = candidate;
            }
            return last;
        }
        float EnemyPositionRisk(int index, float x)
        {
            Fighter enemy = fighters[index];
            Vector2 centre = new Vector2(x, enemy.feet.y + 1.6f * enemy.root.localScale.x);
            float risk = hasPlayerImpact ? 4 * Mathf.Clamp01(1 - Vector2.Distance(centre, lastPlayerImpact) / 7) : 0;
            // Terrain that blocks a straight shot offers some cover, but is not treated as immunity to arcs.
            Vector2 playerCentre = fighters[0].feet + Vector2.up * ShoulderHeight;
            bool cover = false;
            for (int s = 1; s < 40; s++)
                if (HitsTerrain(Vector2.Lerp(playerCentre, centre, s / 40f))) { cover = true; break; }
            risk += cover ? 0.3f : 1.5f;
            for (int i = 1; i < fighters.Count; i++)
                if (i != index && fighters[i].hp > 0)
                    risk += 0.8f * Mathf.Clamp01(1 - Vector2.Distance(centre, fighters[i].feet + Vector2.up * 1.6f) / 4);
            return risk;
        }
        void PlanEnemyTurn()
        {
            enemyMoveSpent = 0; enemyMovingAfterAttack = false;
            Fighter enemy = fighters[current];
            float start = enemy.feet.x;
            float baseline = FindEnemyShot(current, out _, out _);
            float bestScore = baseline, destination = start;
            for (int option = -2; baseline > 1 && option <= 2; option++)
            {
                if (option == 0) continue;
                float candidate = ReachableEnemyX(current, option * EnemyMoveLimit / 2);
                if (Mathf.Abs(candidate - start) < 0.2f) continue;
                enemy.feet.x = candidate;
                float error = FindEnemyShot(current, out _, out _);
                enemy.feet.x = start;
                float score = error + Mathf.Abs(candidate - start) * 0.12f;
                if (score < bestScore) { bestScore = score; destination = candidate; }
            }
            // Move first only when it materially improves a shot; a good firing position is worth keeping.
            if (baseline > 1 && baseline - bestScore > 0.8f && Mathf.Abs(destination - start) > 0.2f)
                BeginEnemyMovement(destination, false);
            else BeginEnemyAim();
        }
        bool TryEnemyRepositionAfterAttack()
        {
            float budget = EnemyMoveLimit - enemyMoveSpent;
            if (budget < 0.2f) return false;
            float start = fighters[current].feet.x;
            float baseline = EnemyPositionRisk(current, start);
            float bestScore = baseline, destination = start;
            for (int option = -2; option <= 2; option++)
            {
                if (option == 0) continue;
                float candidate = ReachableEnemyX(current, option * budget / 2);
                float score = EnemyPositionRisk(current, candidate) + Mathf.Abs(candidate - start) * 0.06f;
                if (score < bestScore) { bestScore = score; destination = candidate; }
            }
            if (baseline - bestScore < 0.25f || Mathf.Abs(destination - start) < 0.2f) return false;
            BeginEnemyMovement(destination, true); return true;
        }
        void BeginEnemyMovement(float destination, bool afterAttack)
        {
            enemyMoveTarget = destination; enemyMovingAfterAttack = afterAttack;
            phase = Phase.EnemyMove;
            message = fighters[current].name + (afterAttack ? " 공격 후 안전한 위치로 이동…" : " 사격 위치로 이동…");
        }
        void BeginEnemyAim()
        {
            phase = Phase.EnemyAim; timer = 0.85f;
            PlanEnemy(current); message = fighters[current].name + " 조준 중…";
        }
        void UpdateEnemyMovement(float dt)
        {
            if (phase != Phase.EnemyMove) return;
            Fighter enemy = fighters[current];
            float next = Mathf.MoveTowards(enemy.feet.x, enemyMoveTarget, EnemyMoveSpeed * Mathf.Clamp(dt, 0, 0.05f));
            bool blocked = !EnemyCanStand(current, next);
            if (!blocked) { enemyMoveSpent += Mathf.Abs(next - enemy.feet.x); enemy.feet.x = next; }
            if (blocked || Mathf.Abs(enemy.feet.x - enemyMoveTarget) < 0.01f)
            {
                if (enemyMovingAfterAttack) NextTurn();
                else BeginEnemyAim();
            }
        }
        void PlanEnemy(int index)
        {
            Fighter f = fighters[index];
            FindEnemyShot(index, out f.angle, out f.power);
            f.angle = Mathf.Clamp(f.angle + Random.Range(-2.8f, 2.8f), 10, 80);
            f.power = Mathf.Clamp(f.power + Random.Range(-0.8f, 0.8f), 10, 38);
        }
        float FindEnemyShot(int index, out float bestAngle, out float bestPower)
        {
            float best = float.MaxValue; bestAngle = 48; bestPower = 26;
            Vector2 target = fighters[0].feet;
            float targetHeight = ActorHeight * fighters[0].root.localScale.x;
            for (float angle = 15; angle <= 78; angle += 3)
                for (float power = 10; power <= 38; power += 0.75f)
                {
                    Vector2 p = Origin(index, angle), v = Direction(index, angle) * power;
                    for (int step = 0; step < 800; step++)
                    {
                        Integrate(ref p, ref v);
                        bool hit = Mathf.Abs(p.x - target.x) <= 0.7f && p.y >= target.y && p.y <= target.y + targetHeight;
                        if (HitsTerrain(p) || hit || p.x < -6 || p.x > 106 || p.y < -13)
                        {
                            float error = hit ? 0 : Vector2.Distance(p, new Vector2(target.x, Mathf.Clamp(p.y, target.y, target.y + targetHeight)));
                            if (error < best) { best = error; bestAngle = angle; bestPower = power; }
                            if (hit) return 0;
                            break;
                        }
                    }
                }
            return best;
        }
        void Finish(bool won) { phase = Phase.Finished; message = won ? "승리 · 모든 적을 처치했습니다!" : "패배 · " + fighters[0].name + "가 쓰러졌습니다."; }
        void Restart()
        {
            ClearTrajectoryEffects();
            for (int i = 0; i < fighters.Count; i++) { fighters[i].hp = fighters[i].maxHp; fighters[i].feet = starts[i]; fighters[i].angle = 48; fighters[i].power = 26; }
            current = 0; round = 1; playerFacing = 1; moveRemaining = MoveLimit; grounded = true; fallSpeed = mouseMove = 0;
            playerHasAttacked = false;
            safePosition = starts[0]; phase = Phase.Aim;
            arrowProjectile.gameObject.SetActive(false); spearProjectile.gameObject.SetActive(false); burst.gameObject.SetActive(false);
            accumulator = shotAge = timer = enemyMoveTarget = 0;
            enemyMoveSpent = 0; enemyMovingAfterAttack = hasPlayerImpact = false;
            foreach (Fighter fighter in fighters) ResetFighterAnimation(fighter);
            message = fighters[0].name + " 한 명으로 성채를 돌파하세요 · A/D 이동 · W 점프"; UpdatePoses();
        }
        void OnDestroy()
        {
            if (worldCamera != null) worldCamera.targetTexture = null;
            if (pixelFrame != null) { pixelFrame.Release(); Destroy(pixelFrame); }
            foreach (Object asset in ownedAssets) if (asset != null) Destroy(asset);
        }
    }
}
