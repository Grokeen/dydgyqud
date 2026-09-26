using System.Collections.Generic;
using UnityEngine;

namespace MiniFortress
{
    public sealed partial class FortressGame : MonoBehaviour
    {
        [SerializeField] FortressArena arena;
        [SerializeField] FortressInput input;
        [SerializeField] FortressHud hud;
        float Gravity => arena.rules.gravity;
        float ShotStep => arena.rules.shotStep;
        float MoveLimit => fighters[0].definition.movementPerTurn;
        enum Phase { Selecting, Aim, Attack, Flight, Impact, EnemyMove, EnemyAim, Finished }
        float EnemyMoveLimit => fighters[current].definition.movementPerTurn;
        float EnemyMoveSpeed => fighters[current].definition.movementSpeed;
        float enemyMoveTarget;
        float enemyMoveSpent;
        bool enemyMovingAfterAttack, hasPlayerImpact;
        Vector2 lastPlayerImpact;
        int playerClass, highlightedClass;
        string AttackName => fighters[0].definition.AttackName;
        sealed class Fighter
        {
            public FortressCharacterDefinition definition;
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
            public bool allowDrop;
            public Platform(Rect r, bool drop) { left = r.xMin; right = r.xMax; top = r.yMax; bottom = r.yMin; allowDrop = drop; }
        }
        Platform[] terrain;
        Vector2[] starts;
        readonly List<Fighter> fighters = new List<Fighter>();
        readonly List<Object> ownedAssets = new List<Object>();
        Camera worldCamera;
        RenderTexture pixelFrame;
        Sprite square;
        Transform arrow, burst;
        Phase phase;
        Vector2 shotPosition, shotVelocity, safePosition;
        float accumulator, shotAge, timer, moveRemaining, fallSpeed, mouseMove;
        bool grounded = true;
        bool playerHasAttacked;
        int current, round, playerFacing = 1;
        string message;
        Vector3 cameraHome;
        float cameraSize;
        bool ready;

        void Start()
        {
            if (!arena || !input || !input.actions || !hud)
            { Debug.LogError("FortressGame: Arena / Input / HUD 연결이 필요합니다.", this); enabled = false; return; }
            var errors = arena.ValidateSetup();
            if (errors.Count > 0) { Debug.LogError(string.Join("\n", errors), arena); enabled = false; return; }
            worldCamera = arena.worldCamera;
            cameraHome = worldCamera.transform.position; cameraSize = worldCamera.orthographicSize;
            worldCamera.aspect = 16f / 9;
            pixelFrame = new RenderTexture(1920, 1080, 24) { filterMode = FilterMode.Bilinear, antiAliasing = 1 };
            pixelFrame.Create(); worldCamera.targetTexture = pixelFrame;
            square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
            ownedAssets.Add(square);
            LoadArenaLayout();
            AddFighter(arena.playerClasses[0], starts[0], null);
            var spawns = arena.enemySpawns.GetComponentsInChildren<FortressSpawnPoint>();
            foreach (var spawn in spawns) AddFighter(spawn.character, spawn.transform.position, spawn.displayName);
            arrow = ArtObject(transform, "Projectile", arena.playerClasses[0].projectile, Vector2.zero, 30);
            burst = Shape("Impact flash", Vector2.zero, Vector2.one, new Color(1, .6f, .2f, .7f), 31);
            burst.GetComponent<SpriteRenderer>().sprite = arena.effectSprite;
            BuildTrajectoryEffects();
            Restart(); OpenSelection(); ready = true;
            hud.Bind();
        }

        void LoadArenaLayout()
        {
            var boxes = arena.terrainRoot.GetComponentsInChildren<FortressTerrain>();
            terrain = new Platform[boxes.Length];
            for (int i = 0; i < boxes.Length; i++) terrain[i] = new Platform(boxes[i].WorldRect, boxes[i].allowDropThrough);
            var spawns = arena.enemySpawns.GetComponentsInChildren<FortressSpawnPoint>();
            starts = new Vector2[spawns.Length + 1]; starts[0] = arena.playerSpawn.position;
            for (int i = 0; i < spawns.Length; i++) starts[i + 1] = spawns[i].transform.position;
        }
        float Height(Fighter f) => f.definition.height * f.root.localScale.x;
        float HalfWidth(Fighter f) => f.definition.halfWidth * f.root.localScale.x;
        bool ShotOutside(Vector2 point) => !arena.rules.projectileLimits.Contains(point);
        int Facing(int index) => index == 0 ? playerFacing : fighters[0].feet.x < fighters[index].feet.x ? -1 : 1;
        Vector2 Direction(int index, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            return new Vector2(Facing(index) * Mathf.Cos(r), Mathf.Sin(r));
        }
        Vector2 Origin(int index, float degrees) => fighters[index].feet + Vector2.up * (fighters[index].definition.shoulderHeight * fighters[index].root.localScale.x) + Direction(index, degrees) * (fighters[index].definition.muzzleDistance * fighters[index].root.localScale.x);
        void Update()
        {
            if (fighters.Count == 0) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);

            if (phase == Phase.Selecting)
            {
                if (input.DirectSelection >= 0) SelectClass(input.DirectSelection);
                if (input.SelectionDelta != 0) SelectClass((highlightedClass + input.SelectionDelta + arena.playerClasses.Length) % arena.playerClasses.Length);
                if (input.Confirm) BeginBattle();
                return;
            }
            if (input.Back) { OpenSelection(); return; }
            if (input.Restart) { Restart(); return; }
            if (phase == Phase.Aim)
            {
                Fighter player = fighters[0];
                if (!playerHasAttacked)
                {
                    SetAngle(player.angle + input.Angle * arena.rules.angleSpeed * dt);
                    SetPower(player.power + input.Power * arena.rules.powerSpeed * dt);
                }
                if (input.Jump) Jump();
                if (input.Drop) DropFromBridge();
                MovePlayer(Mathf.Clamp(mouseMove + input.Move, -1, 1) * player.definition.movementSpeed * dt);
                FallPlayer(dt);
                if (input.Fire && grounded && phase == Phase.Aim) Fire(0);
                if (input.EndTurn) EndPlayerTurn();
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
                float diameter = fighters[current].definition.blastRadius * 2;
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
            float wanted = Mathf.Clamp(player.feet.x + Mathf.Clamp(distance, -moveRemaining, moveRemaining), arena.rules.horizontalLimits.x, arena.rules.horizontalLimits.y - 1);
            foreach (Platform p in terrain)
            {
                if (player.feet.y >= p.top - 0.05f || player.feet.y + Height(player) <= p.bottom) continue;
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
            if (!ready) return;
            if (arena.followHighShots) FrameTrajectoryCamera(1 - Mathf.Exp(-6 * Time.deltaTime));
            AnimateTrajectoryEffects(Time.time);
        }
        void Jump()
        {
            if (phase != Phase.Aim || !grounded || moveRemaining < arena.rules.jumpCost) return;
            moveRemaining -= arena.rules.jumpCost; fallSpeed = arena.rules.jumpSpeed; grounded = false;
        }
        void DropFromBridge()
        {
            if (phase != Phase.Aim || !grounded) return;
            foreach (Platform p in terrain)
                if (p.allowDrop && fighters[0].feet.x >= p.left && fighters[0].feet.x <= p.right && Mathf.Abs(fighters[0].feet.y - p.top) < 0.05f)
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
                if (fallSpeed > 0 && oldY + Height(player) <= p.bottom && nextY + Height(player) >= p.bottom) { nextY = p.bottom - Height(player); fallSpeed = 0; }
            }
            if (!float.IsNegativeInfinity(landing)) { nextY = landing; fallSpeed = 0; grounded = true; }
            player.feet.y = nextY; if (grounded) safePosition = player.feet;
            if (player.feet.y < arena.rules.fallRespawnY)
            {
                player.feet = safePosition; fallSpeed = 0; grounded = true; player.hp = Mathf.Max(0, player.hp - arena.rules.fallDamage);
                player.previousFeet = player.feet;
                PlayDamageReaction(player);
                message = "추락! 체력 " + arena.rules.fallDamage + " 감소 · 마지막 발판으로 복귀"; if (player.hp == 0) Finish(false);
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
                f.aimPivot.localPosition = Vector3.up * f.definition.shoulderHeight;
                f.aimPivot.localRotation = Quaternion.Euler(0, 0, f.angle);
                f.loadedArrow.gameObject.SetActive(!(i == 0 && playerHasAttacked && phase != Phase.Attack) && !((phase == Phase.Flight || phase == Phase.Impact) && current == i));
            }
            UpdateTrajectoryPreview();
        }
        void Integrate(ref Vector2 p, ref Vector2 v)
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
                if (Mathf.Abs(p.x - f.feet.x) <= f.definition.halfWidth * scale && p.y >= f.feet.y && p.y <= f.feet.y + f.definition.height * scale) return i;
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
            timer = fighters[index].definition.releaseTime;
            fighters[index].animator.SetTrigger(fighters[index].definition.weapon == FortressWeapon.Spear ? SpearAttackParameter : BowAttackParameter);
            message = fighters[index].name + (fighters[index].definition.weapon == FortressWeapon.Spear ? " 창 투척 준비…" : " 활시위를 당기는 중…");
        }
        void LaunchShot()
        {
            int index = current;
            arrow.GetComponent<SpriteRenderer>().sprite = fighters[index].definition.projectile;
            arrow.localScale = Vector3.one * fighters[index].root.localScale.x;
            current = index; shotPosition = Origin(index, fighters[index].angle);
            shotVelocity = Direction(index, fighters[index].angle) * fighters[index].power;
            shotAge = accumulator = 0; arrow.position = shotPosition;
            arrow.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(shotVelocity.y, shotVelocity.x) * Mathf.Rad2Deg);
            arrow.gameObject.SetActive(true); phase = Phase.Flight;
            BeginShotTrail();
            message = fighters[index].name + (fighters[index].definition.weapon == FortressWeapon.Spear ? "의 창 투척!" : "의 화살!");
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
                    int maximum = fighters[current].definition.damage;
                    float radius = fighters[current].definition.blastRadius;
                    int damage = directHit == i ? maximum : Mathf.RoundToInt(maximum * Mathf.Clamp01(1 - distance / radius));
                    f.hp = Mathf.Max(0, f.hp - damage); total += damage;
                    if (damage > 0) PlayDamageReaction(f);
                }
            message = total > 0 ? "명중! 피해 " + total : "빗나갔습니다. 각도와 위력을 조절하세요.";
            phase = Phase.Impact; timer = 0.55f;
        }
        void Finish(bool won) { phase = Phase.Finished; message = won ? "승리 · 모든 적을 처치했습니다!" : "패배 · " + fighters[0].name + "가 쓰러졌습니다."; }
        void Restart()
        {
            ClearTrajectoryEffects();
            for (int i = 0; i < fighters.Count; i++) { fighters[i].hp = fighters[i].maxHp; fighters[i].feet = starts[i]; fighters[i].angle = arena.rules.defaultAngle; fighters[i].power = arena.rules.defaultPower; }
            current = 0; round = 1; playerFacing = 1; moveRemaining = MoveLimit; grounded = true; fallSpeed = mouseMove = 0;
            playerHasAttacked = false;
            safePosition = starts[0]; phase = Phase.Aim;
            arrow.gameObject.SetActive(false); burst.gameObject.SetActive(false);
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
