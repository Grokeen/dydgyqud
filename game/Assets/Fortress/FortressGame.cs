using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MiniFortress
{
    // All gameplay lives in the XY plane. No scene wiring or external art is needed.
    public sealed class FortressGame : MonoBehaviour
    {
        const float Gravity = 9.8f;
        const float Step = 0.01f;
        const float BlastRadius = 3.3f;
        readonly Vector2[] tanks = { new Vector2(-10, 0.65f), new Vector2(10, 0.65f) };
        readonly int[] health = { 100, 100 };
        readonly Transform[] barrels = new Transform[2];
        readonly Bounds hill = new Bounds(new Vector3(0, 1.25f, 0), new Vector3(3.2f, 2.5f, 2));
        Sprite square;
        Sprite circle;
        Texture2D circleTexture;
        Camera gameCamera;
        Transform shell;
        Transform explosion;
        Transform[] dots;
        Vector2 position;
        Vector2 velocity;
        float angle = 45;
        float power = 16;
        float aiAngle = 45;
        float aiPower = 16;
        float timer;
        float accumulator;
        float flightTime;
        int shooter;
        int round = 1;
        string message = "Aim over the wall and hit the red tank.";
        enum Phase { Aim, Flight, Explosion, Thinking, Finished }
        Phase phase;
        GUIStyle heading;
        GUIStyle label;
        GUIStyle button;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (SceneManager.GetActiveScene().name == "SampleScene" && FindAnyObjectByType<FortressGame>() == null)
                new GameObject("Mini Fortress").AddComponent<FortressGame>();
        }

        void Start()
        {
            square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1);
            circleTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var pixels = new Color[32 * 32];
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                    pixels[y * 32 + x] = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) <= 15.5f ? Color.white : Color.clear;
            circleTexture.SetPixels(pixels);
            circleTexture.Apply();
            circle = Sprite.Create(circleTexture, new Rect(0, 0, 32, 32), Vector2.one * 0.5f, 32);

            foreach (Camera existing in FindObjectsByType<Camera>()) existing.enabled = false;
            gameCamera = new GameObject("Fortress Camera").AddComponent<Camera>();
            gameCamera.transform.SetParent(transform);
            gameCamera.transform.position = new Vector3(0, 6, -20);
            gameCamera.orthographic = true;
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = new Color(0.055f, 0.09f, 0.15f);
            gameCamera.nearClipPlane = 0.1f;
            gameCamera.farClipPlane = 50;
            Shape("Ground", new Vector2(0, -2), new Vector2(60, 4), new Color(0.18f, 0.25f, 0.31f));
            Shape("Grass", new Vector2(0, -0.08f), new Vector2(60, 0.16f), new Color(0.36f, 0.64f, 0.46f), 1);
            Shape("Central wall", new Vector2(0, 1.25f), new Vector2(3.2f, 2.5f), new Color(0.4f, 0.45f, 0.52f));
            for (int i = 0; i < 2; i++)
            {
                Color color = i == 0 ? new Color(0.2f, 0.75f, 1) : new Color(1, 0.35f, 0.3f);
                Shape("Tank " + i, tanks[i], new Vector2(1.8f, 0.8f), color, 3);
                Shape("Turret " + i, tanks[i] + Vector2.up * 0.4f, new Vector2(0.9f, 0.65f), color, 4, true);
                for (int wheel = -1; wheel <= 1; wheel++)
                    Shape("Wheel", tanks[i] + new Vector2(wheel * 0.6f, -0.35f), Vector2.one * 0.48f, new Color(0.08f, 0.12f, 0.19f), 5, true);
                barrels[i] = Shape("Barrel " + i, tanks[i], new Vector2(1.35f, 0.22f), color, 3);
            }
            shell = Shape("Shell", Vector2.zero, Vector2.one * 0.3f, new Color(1, 0.88f, 0.35f), 8, true);
            explosion = Shape("Explosion", Vector2.zero, Vector2.one, new Color(1, 0.65f, 0.15f, 0.6f), 9, true);
            dots = new Transform[15];
            for (int i = 0; i < dots.Length; i++)
                dots[i] = Shape("Aim guide", Vector2.zero, Vector2.one * 0.09f, new Color(0.5f, 0.8f, 1, 0.55f), 2, true);
            Restart();
        }

        Transform Shape(string title, Vector2 center, Vector2 size, Color color, int order = 0, bool roundShape = false)
        {
            var obj = new GameObject(title);
            obj.transform.SetParent(transform);
            obj.transform.position = center;
            obj.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = roundShape ? circle : square;
            renderer.color = color;
            renderer.sortingOrder = order;
            return obj.transform;
        }

        Vector2 Direction(int side, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians) * (side == 0 ? 1 : -1), Mathf.Sin(radians));
        }

        Vector2 Muzzle(int side, float degrees) => tanks[side] + Vector2.up * 0.4f + Direction(side, degrees) * 1.45f;

        void AimBarrel(int side, float degrees)
        {
            Vector2 direction = Direction(side, degrees);
            barrels[side].position = tanks[side] + Vector2.up * 0.4f + direction * 0.7f;
            barrels[side].rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        void Update()
        {
            if (gameCamera == null) return;
            gameCamera.orthographicSize = Mathf.Max(10.5f, 16f / Mathf.Max(gameCamera.aspect, 0.1f));
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame) Restart();
            if (phase == Phase.Aim && keyboard != null)
            {
                float dt = Time.deltaTime;
                angle = Mathf.Clamp(angle + ((keyboard.upArrowKey.isPressed ? 1 : 0) - (keyboard.downArrowKey.isPressed ? 1 : 0)) * 35 * dt, 10, 80);
                power = Mathf.Clamp(power + ((keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.leftArrowKey.isPressed ? 1 : 0)) * 8 * dt, 8, 22);
                if (keyboard.spaceKey.wasPressedThisFrame) Fire(0, angle, power);
            }
            AimBarrel(0, angle);
            AimBarrel(1, aiAngle);
            UpdateGuide();
            if (phase == Phase.Flight)
            {
                accumulator += Mathf.Min(Time.deltaTime, 0.1f);
                while (accumulator >= Step && phase == Phase.Flight)
                {
                    accumulator -= Step;
                    Integrate(ref position, ref velocity);
                    flightTime += Step;
                    shell.position = position;
                    if (Hit(position, shooter)) Explode(position);
                    else if (Mathf.Abs(position.x) > 32 || position.y < -5 || flightTime > 10)
                    {
                        message = "Miss! The shell left the battlefield.";
                        shell.gameObject.SetActive(false);
                        phase = Phase.Explosion;
                        timer = 0.5f;
                    }
                }
            }
            else if (phase == Phase.Explosion)
            {
                timer -= Time.deltaTime;
                explosion.localScale = Vector3.one * Mathf.Lerp(BlastRadius * 2, 0.4f, Mathf.Clamp01(timer / 0.6f));
                if (timer <= 0) EndShot();
            }
            else if (phase == Phase.Thinking)
            {
                timer -= Time.deltaTime;
                if (timer <= 0) Fire(1, aiAngle, aiPower);
            }
        }

        static void Integrate(ref Vector2 point, ref Vector2 speed)
        {
            point += speed * Step + Vector2.down * (0.5f * Gravity * Step * Step);
            speed += Vector2.down * (Gravity * Step);
        }

        bool Hit(Vector2 point, int owner)
        {
            if (point.y <= 0.15f || hill.Contains(new Vector3(point.x, point.y, 0))) return true;
            int target = 1 - owner;
            return Mathf.Abs(point.x - tanks[target].x) <= 1.05f && Mathf.Abs(point.y - tanks[target].y) <= 0.75f;
        }

        void UpdateGuide()
        {
            Vector2 point = Muzzle(0, angle);
            Vector2 speed = Direction(0, angle) * power;
            bool visible = phase == Phase.Aim;
            for (int i = 0; i < dots.Length; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    Integrate(ref point, ref speed);
                    if (Hit(point, 0)) visible = false;
                }
                dots[i].gameObject.SetActive(visible);
                dots[i].position = point;
            }
        }

        void Fire(int side, float degrees, float strength)
        {
            if (phase != Phase.Aim && phase != Phase.Thinking) return;
            shooter = side;
            position = Muzzle(side, degrees);
            velocity = Direction(side, degrees) * strength;
            flightTime = accumulator = 0;
            shell.position = position;
            shell.gameObject.SetActive(true);
            phase = Phase.Flight;
            message = side == 0 ? "Your shell is flying..." : "Enemy shell incoming!";
        }

        void Explode(Vector2 center)
        {
            shell.gameObject.SetActive(false);
            explosion.position = center;
            explosion.localScale = Vector3.one * 0.4f;
            explosion.gameObject.SetActive(true);
            int total = 0;
            for (int i = 0; i < 2; i++)
            {
                float distance = Vector2.Distance(center, tanks[i]);
                int damage = distance < BlastRadius ? Mathf.RoundToInt(40 * (1 - Mathf.Clamp01((distance - 0.9f) / (BlastRadius - 0.9f)))) : 0;
                health[i] = Mathf.Max(0, health[i] - damage);
                total += damage;
            }
            message = total > 0 ? "Hit! " + total + " damage." : "Miss! Adjust your angle or power.";
            phase = Phase.Explosion;
            timer = 0.6f;
        }

        void EndShot()
        {
            explosion.gameObject.SetActive(false);
            if (health[0] <= 0 || health[1] <= 0)
            {
                phase = Phase.Finished;
                message = health[0] == health[1] ? "DRAW" : health[1] <= 0 ? "YOU WIN!" : "ENEMY WINS";
            }
            else if (shooter == 0)
            {
                phase = Phase.Thinking;
                timer = 1.2f;
                PlanAI();
            }
            else
            {
                round++;
                phase = Phase.Aim;
            }
        }

        void PlanAI()
        {
            float best = float.MaxValue;
            for (float degrees = 25; degrees <= 75; degrees += 2)
                for (float strength = 11; strength <= 22; strength += 0.3f)
                {
                    Vector2 point = Muzzle(1, degrees);
                    Vector2 speed = Direction(1, degrees) * strength;
                    for (int step = 0; step < 700; step++)
                    {
                        Integrate(ref point, ref speed);
                        if (Hit(point, 1) || Mathf.Abs(point.x) > 32)
                        {
                            float error = Vector2.Distance(point, tanks[0]);
                            if (error < best) { best = error; aiAngle = degrees; aiPower = strength; }
                            break;
                        }
                    }
                }
            aiAngle = Mathf.Clamp(aiAngle + Random.Range(-4f, 4f), 10, 80);
            aiPower = Mathf.Clamp(aiPower + Random.Range(-1.3f, 1.3f), 8, 22);
        }

        void Restart()
        {
            health[0] = health[1] = 100;
            angle = aiAngle = 45;
            power = aiPower = 16;
            round = 1;
            phase = Phase.Aim;
            shell.gameObject.SetActive(false);
            explosion.gameObject.SetActive(false);
            message = "Aim over the wall and hit the red tank.";
        }

        void OnGUI()
        {
            if (shell == null) return;
            if (heading == null)
            {
                heading = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                label = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
                button = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold };
            }
            float scale = Mathf.Min(Screen.width / 1000f, Screen.height / 650f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1000 * scale) / 2, 0, 0), Quaternion.identity, Vector3.one * scale);
            float height = Screen.height / scale;
            GUI.Box(new Rect(15, 12, 970, 110), GUIContent.none);
            GUI.Label(new Rect(300, 18, 400, 36), "MINI FORTRESS", heading);
            GUI.Label(new Rect(320, 60, 360, 28), "ROUND " + round + "  /  " + (phase == Phase.Finished ? "GAME OVER" : phase == Phase.Aim ? "YOUR TURN" : shooter == 0 && phase != Phase.Thinking ? "YOUR SHOT" : "ENEMY TURN"), label);
            DrawHealth(35, "YOU", health[0], new Color(0.2f, 0.75f, 1));
            DrawHealth(725, "ENEMY", health[1], new Color(1, 0.35f, 0.3f));
            GUI.Label(new Rect(50, height - 176, 900, 32), message, label);
            GUI.Box(new Rect(15, height - 140, 970, 125), GUIContent.none);
            GUI.enabled = phase == Phase.Aim;
            GUI.Label(new Rect(40, height - 130, 240, 28), "ANGLE  " + angle.ToString("0") + " degrees", label);
            angle = GUI.HorizontalSlider(new Rect(50, height - 89, 220, 24), angle, 10, 80);
            GUI.Label(new Rect(315, height - 130, 240, 28), "POWER  " + power.ToString("0.0"), label);
            power = GUI.HorizontalSlider(new Rect(325, height - 89, 220, 24), power, 8, 22);
            if (GUI.Button(new Rect(595, height - 119, 180, 52), "FIRE [Space]", button)) Fire(0, angle, power);
            GUI.enabled = true;
            if (GUI.Button(new Rect(795, height - 119, 165, 52), "RESTART [R]", button)) Restart();
            GUI.Label(new Rect(35, height - 53, 920, 28), "Up / Down: angle     Left / Right: power     Space: fire     R: restart", label);
            if (phase == Phase.Finished)
            {
                GUI.Box(new Rect(280, height / 2 - 80, 440, 145), GUIContent.none);
                GUI.Label(new Rect(290, height / 2 - 65, 420, 45), message, heading);
                if (GUI.Button(new Rect(380, height / 2 - 5, 240, 48), "PLAY AGAIN", button)) Restart();
            }
        }

        void DrawHealth(float x, string title, int value, Color color)
        {
            GUI.Label(new Rect(x, 28, 240, 30), title + "  " + value + " / 100", label);
            GUI.color = new Color(0.15f, 0.2f, 0.26f);
            GUI.DrawTexture(new Rect(x, 70, 240, 16), Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, 70, 240 * value / 100f, 16), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        void OnDestroy()
        {
            if (square != null) Destroy(square);
            if (circle != null) Destroy(circle);
            if (circleTexture != null) Destroy(circleTexture);
        }
    }
}
