using UnityEngine;

namespace MiniFortress
{
    public sealed partial class FortressGame
    {
        bool EnemyCanStand(int index, float x)
        {
            Fighter enemy = fighters[index];
            float halfWidth = HalfWidth(enemy);
            float height = Height(enemy);
            if (x - halfWidth < arena.rules.horizontalLimits.x || x + halfWidth > arena.rules.horizontalLimits.y) return false;
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
                if (Mathf.Abs(x - other.feet.x) < halfWidth + HalfWidth(other) + 0.2f &&
                    enemy.feet.y < other.feet.y + Height(other) && enemy.feet.y + height > other.feet.y)
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
            Vector2 playerCentre = fighters[0].feet + Vector2.up * fighters[0].definition.shoulderHeight;
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
            f.angle = Mathf.Clamp(f.angle + Random.Range(-arena.rules.enemyAngleError, arena.rules.enemyAngleError), arena.rules.angleLimits.x, arena.rules.angleLimits.y);
            f.power = Mathf.Clamp(f.power + Random.Range(-arena.rules.enemyPowerError, arena.rules.enemyPowerError), arena.rules.powerLimits.x, arena.rules.powerLimits.y);
        }
        float FindEnemyShot(int index, out float bestAngle, out float bestPower)
        {
            float best = float.MaxValue; bestAngle = arena.rules.defaultAngle; bestPower = arena.rules.defaultPower;
            Vector2 target = fighters[0].feet;
            float targetHeight = Height(fighters[0]);
            for (float angle = arena.rules.angleLimits.x; angle <= arena.rules.angleLimits.y; angle += 3)
                for (float power = arena.rules.powerLimits.x; power <= arena.rules.powerLimits.y; power += 0.75f)
                {
                    Vector2 p = Origin(index, angle), v = Direction(index, angle) * power;
                    for (int step = 0; step < Mathf.CeilToInt(arena.rules.shotLifetime / ShotStep); step++)
                    {
                        Integrate(ref p, ref v);
                        bool hit = Mathf.Abs(p.x - target.x) <= HalfWidth(fighters[0]) && p.y >= target.y && p.y <= target.y + targetHeight;
                        if (HitsTerrain(p) || hit || ShotOutside(p))
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
    }
}
