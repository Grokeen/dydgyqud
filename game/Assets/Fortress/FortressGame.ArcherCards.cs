using System.Collections.Generic;
using UnityEngine;

namespace MiniFortress
{
    public sealed partial class FortressGame
    {
        // 코덱스code(CodexCode) 작업 메모: 설계 v2.0 우선 20장에 필요한 탄약, 잔류 화살, 출혈, 덫, 보조 사격 상태를 관리합니다.
        sealed class FieldArrow
        {
            public Transform visual;
            public Vector2 position;
            public int owner, firedRound, enemyIndex = -1;
            public bool installed;
        }

        sealed class FieldTrap
        {
            public Transform visual;
            public Vector2 position;
        }

        readonly List<FieldArrow> fieldArrows = new List<FieldArrow>();
        readonly List<FieldTrap> fieldTraps = new List<FieldTrap>();
        bool activeRupture, activeArmorBreak, activeSplitShot, activeEvasion, activeRain, hasOutpostOrigin;
        int activeBleed, activeBleedVolley, shotsInCurrentVolley, splitAtShot;
        Vector2 outpostOrigin;

        public int Arrows => arrows;
        public int ArrowCapacity => arrowCapacity;
        public int ShotCount => shotsRequested;
        public int FieldArrowCount => fieldArrows.Count;
        public void SetShotCount(int count)
        {
            if (phase == Phase.Aim && !playerHasAttacked && arena.playerClasses[playerClass].weapon == FortressWeapon.Bow)
                shotsRequested = Mathf.Clamp(count, 1, Mathf.Max(1, arrows));
        }

        bool CanUseCard(FortressCardEntry card)
        {
            switch (card.effect)
            {
                case FortressCardEffect.Stake: return arrows > 0 && NearestPlatformPoint(fighters[0].feet, 1f, out _);
                case FortressCardEffect.PickArrow: return HasRecoverableArrow(fighters[0].feet, 1f, false);
                case FortressCardEffect.RecoverArrows: return HasRecentFiredArrow();
                case FortressCardEffect.ArrowTrap: return HasRecoverableArrow(fighters[0].feet, 1f, false);
                case FortressCardEffect.ExtractPain: return HasPinnedArrowOnEnemy();
                case FortressCardEffect.Outpost: return HasRecoverableArrow(fighters[0].feet, 3f, false);
                case FortressCardEffect.Rupture: return HasEnemyWithBleed();
                case FortressCardEffect.DrawDiscard: return hand.Count < 8;
                case FortressCardEffect.TacticalRearrangement: return HasDiscardedAttackCard();
                default: return true;
            }
        }

        // CodexCode: card draws now use the shared run-deck implementation in FortressGame.Cards.cs.
        void LoadArrows(int amount)
        {
            int before = arrows;
            arrows = Mathf.Min(arrowCapacity, arrows + Mathf.Max(0, amount));
            shotsRequested = Mathf.Clamp(shotsRequested, 1, Mathf.Max(1, arrows));
            if (before < arrowCapacity && arrows == arrowCapacity && amount > arrows - before)
                message = $"화살 {arrows} / {arrowCapacity} · 초과 장전분은 소멸";
        }


        void ClearArcherObjects()
        {
            foreach (FieldArrow item in fieldArrows) if (item.visual) Destroy(item.visual.gameObject);
            foreach (FieldTrap item in fieldTraps) if (item.visual) Destroy(item.visual.gameObject);
            fieldArrows.Clear(); fieldTraps.Clear();
            activeRupture = activeArmorBreak = activeSplitShot = activeEvasion = activeRain = hasOutpostOrigin = false;
            activeBleed = activeBleedVolley = shotsInCurrentVolley = splitAtShot = 0;
        }

        bool NearestPlatformPoint(Vector2 origin, float maxDistance, out Vector2 point)
        {
            float best = maxDistance; point = origin;
            bool found = false;
            foreach (Platform platform in terrain)
            {
                float x = Mathf.Clamp(origin.x, platform.left, platform.right);
                Vector2 candidate = new Vector2(x, platform.top);
                float distance = Vector2.Distance(origin, candidate);
                if (distance > best) continue;
                best = distance; point = candidate; found = true;
            }
            return found;
        }

        bool HasRecoverableArrow(Vector2 origin, float distance, bool previousTurnOnly)
        {
            foreach (FieldArrow item in fieldArrows)
                if (item.owner == 0 && item.enemyIndex < 0 &&
                    (!previousTurnOnly || item.firedRound == round - 1) &&
                    Vector2.Distance(origin, item.position) <= distance) return true;
            return false;
        }

        void PinArrow(Vector2 position, int enemyIndex, Vector2 velocity)
        {
            Transform visual = ArtObject(transform, "Lodged arrow", arena.playerClasses[playerClass].projectile, position, 24);
            visual.localScale = Vector3.one * .65f;
            visual.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
            fieldArrows.Add(new FieldArrow { visual = visual, position = position, owner = 0, firedRound = round, enemyIndex = enemyIndex });
        }

        void UpdateFieldObjects()
        {
            for (int i = fieldArrows.Count - 1; i >= 0; i--)
            {
                FieldArrow item = fieldArrows[i];
                if (item.enemyIndex >= 0)
                {
                    Fighter enemy = fighters[item.enemyIndex];
                    if (enemy.hp > 0) item.position = enemy.feet + Vector2.up * (Height(enemy) * .56f);
                    else
                    {
                        item.enemyIndex = -1;
                        if (!NearestPlatformPoint(item.position, float.PositiveInfinity, out Vector2 floor) || floor.y > item.position.y + .1f)
                        { Destroy(item.visual.gameObject); fieldArrows.RemoveAt(i); continue; }
                        item.position = floor;
                    }
                }
                if (item.visual) item.visual.position = item.position;
            }
        }

        void SpendArrowForStake()
        {
            if (arrows <= 0 || !NearestPlatformPoint(fighters[0].feet, 1f, out Vector2 point)) return;
            arrows--; PinArrow(point, -1, Vector2.right);
            fieldArrows[fieldArrows.Count - 1].installed = true;
            message = "화살을 지형에 말뚝으로 박았습니다.";
        }

        void PlaceArrowTrap()
        {
            FieldArrow material = FindNearbyGroundArrow(fighters[0].feet, 1f);
            if (material == null) return;
            Vector2 position = material.position; RemoveArrow(material);
            Transform marker = Shape("Arrow Trap", position, new Vector2(.65f, .25f), new Color(1f, .62f, .24f), 25);
            fieldTraps.Add(new FieldTrap { visual = marker, position = position });
        }

        void TriggerArrowTraps(int enemyIndex)
        {
            if (enemyIndex <= 0 || fighters[enemyIndex].hp <= 0) return;
            Fighter enemy = fighters[enemyIndex];
            for (int i = fieldTraps.Count - 1; i >= 0; i--)
            {
                FieldTrap trap = fieldTraps[i];
                if (Vector2.Distance(trap.position, enemy.feet) > .5f) continue;
                enemy.hp = Mathf.Max(0, enemy.hp - 5);
                if (enemy.hp > 0) enemy.bleed = Mathf.Min(10, enemy.bleed + 2);
                PlayDamageReaction(enemy);
                Destroy(trap.visual.gameObject); fieldTraps.RemoveAt(i);
                message = enemy.name + "이 화살 덫에 걸렸습니다. 피해 5 · 출혈 2";
                break;
            }
        }

        FieldArrow FindNearbyGroundArrow(Vector2 origin, float distance)
        {
            FieldArrow best = null; float nearest = distance;
            foreach (FieldArrow item in fieldArrows)
            {
                if (item.owner != 0 || item.enemyIndex >= 0) continue;
                float candidate = Vector2.Distance(origin, item.position);
                if (candidate > nearest) continue;
                nearest = candidate; best = item;
            }
            return best;
        }

        void RemoveArrow(FieldArrow item)
        {
            if (item.visual) Destroy(item.visual.gameObject);
            fieldArrows.Remove(item);
        }

        void RecoverNearbyArrows(int maximum)
        {
            int recovered = 0;
            while (recovered < maximum && arrows < arrowCapacity)
            {
                FieldArrow item = FindNearbyGroundArrow(fighters[0].feet, 1f);
                if (item == null) break;
                RemoveArrow(item); arrows++; recovered++;
            }
            shotsRequested = Mathf.Clamp(shotsRequested, 1, Mathf.Max(1, arrows));
            message = recovered > 0 ? $"지형 화살 {recovered}발 회수" : "회수할 화살이 없습니다.";
        }

        void RecoverPreviousTurnArrows(int maximum)
        {
            int recovered = 0;
            for (int i = fieldArrows.Count - 1; i >= 0 && recovered < maximum && arrows < arrowCapacity; i--)
            {
                FieldArrow item = fieldArrows[i];
                if (item.owner != 0 || item.installed || item.firedRound != round - 1) continue;
                RemoveArrow(item); arrows++; recovered++;
            }
            shotsRequested = Mathf.Clamp(shotsRequested, 1, Mathf.Max(1, arrows));
            message = $"지난 내 턴의 화살 {recovered}발을 회수했습니다.";
        }

        bool HasPinnedArrowOnEnemy()
        {
            foreach (FieldArrow item in fieldArrows) if (item.owner == 0 && item.enemyIndex > 0) return true;
            return false;
        }

        void ExtractPinnedArrows(int maximum)
        {
            int target = -1; float nearest = float.PositiveInfinity;
            foreach (FieldArrow item in fieldArrows)
                if (item.owner == 0 && item.enemyIndex > 0)
                {
                    float distance = Vector2.Distance(fighters[0].feet, fighters[item.enemyIndex].feet);
                    if (distance < nearest) { nearest = distance; target = item.enemyIndex; }
                }
            if (target < 0) return;
            int pulled = 0;
            for (int i = fieldArrows.Count - 1; i >= 0 && pulled < maximum; i--)
                if (fieldArrows[i].owner == 0 && fieldArrows[i].enemyIndex == target) { RemoveArrow(fieldArrows[i]); pulled++; }
            fighters[target].bleed = Mathf.Min(10, fighters[target].bleed + pulled);
        }

        bool HasEnemyWithBleed()
        {
            for (int i = 1; i < fighters.Count; i++) if (fighters[i].hp > 0 && fighters[i].bleed > 0) return true;
            return false;
        }

        bool HasRecentFiredArrow()
        {
            foreach (FieldArrow item in fieldArrows)
                if (item.owner == 0 && !item.installed && item.firedRound == round - 1) return true;
            return false;
        }

        bool HasDiscardedAttackCard()
        {
            foreach (FortressCardEntry card in discardPile)
                if (card.effect == FortressCardEffect.ShotDamage || card.effect == FortressCardEffect.BleedShot ||
                    card.effect == FortressCardEffect.Rupture || card.effect == FortressCardEffect.ArmorBreak) return true;
            return false;
        }

        void TacticalRearrange()
        {
            int index = discardPile.FindIndex(card => card.effect == FortressCardEffect.ShotDamage ||
                card.effect == FortressCardEffect.BleedShot || card.effect == FortressCardEffect.Rupture ||
                card.effect == FortressCardEffect.ArmorBreak);
            if (index < 0) return;
            hand.Add(discardPile[index]); discardPile.RemoveAt(index);
        }

        void SetOutpostOrigin()
        {
            FieldArrow item = FindNearbyGroundArrow(fighters[0].feet, 3f);
            if (item == null) return;
            outpostOrigin = item.position; hasOutpostOrigin = true; RemoveArrow(item);
        }

        Vector2 ArcherShotOrigin(float angle)
        {
            if (!hasOutpostOrigin) return Origin(0, angle);
            return outpostOrigin + Direction(0, angle) * (fighters[0].definition.muzzleDistance * fighters[0].root.localScale.x);
        }

        float ArcherShotAngle(int index)
        {
            if (activeSplitShot && index >= splitAtShot) return fighters[0].angle + (index % 2 == 0 ? 14 : -14);
            return fighters[0].angle + VolleyAngleOffset(index);
        }

        int ResolveArcherImpact(int directHit)
        {
            if (directHit <= 0) return 0;
            Fighter target = fighters[directHit];
            if (volleyIndex == 0 && activeArmorBreak) target.armor = Mathf.Max(0, target.armor - 15);
            int ruptureDamage = 0;
            if (volleyIndex == 0 && activeRupture)
            {
                int removed = target.bleed; target.bleed = 0; ruptureDamage = removed * 8;
            }
            return ruptureDamage;
        }

        void FinishArcherImpact(int directHit)
        {
            if (directHit > 0 && fighters[directHit].hp > 0)
            {
                if (volleyIndex == 0 && activeBleed > 0) fighters[directHit].bleed = Mathf.Min(10, fighters[directHit].bleed + activeBleed);
                if (activeBleedVolley > 0) fighters[directHit].bleed = Mathf.Min(10, fighters[directHit].bleed + 1);
            }
            if (activeBleedVolley > 0) activeBleedVolley--;
            if (volleyIndex == 0)
            {
                activeBleed = 0; activeRupture = false; activeArmorBreak = false;
            }
        }

        void TickEnemyBleed(int index)
        {
            if (index <= 0 || index >= fighters.Count) return;
            Fighter enemy = fighters[index];
            if (enemy.hp <= 0 || enemy.bleed <= 0) return;
            int damage = enemy.bleed * 5; enemy.hp = Mathf.Max(0, enemy.hp - damage); enemy.bleed--;
            PlayDamageReaction(enemy);
            message = $"{enemy.name} 출혈 피해 {damage} · 출혈 {enemy.bleed}";
        }

        void TryRainArrows(int directHit)
        {
            if (!activeRain || directHit <= 0 || volleyIndex >= 3) return;
            Fighter target = fighters[directHit];
            for (int i = 1; i < fighters.Count; i++)
            {
                if (i == directHit || fighters[i].hp <= 0) continue;
                if (Mathf.Abs(fighters[i].feet.x - target.feet.x) <= 2f)
                { fighters[i].hp = Mathf.Max(0, fighters[i].hp - 5); PlayDamageReaction(fighters[i]); }
            }
        }
    }
}
