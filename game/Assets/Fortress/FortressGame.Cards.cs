using System.Collections.Generic;
using UnityEngine;

namespace MiniFortress
{
    public sealed partial class FortressGame
    {
        readonly List<FortressCardEntry> drawPile = new List<FortressCardEntry>();
        readonly List<FortressCardEntry> discardPile = new List<FortressCardEntry>();
        readonly List<FortressCardEntry> hand = new List<FortressCardEntry>();
        int energy, block, battleDamage;
        // Banked by cards until the player's next attack.
        int bonusShots, bonusDamage, bonusCritical;
        // Locked in when the player attacks and used by every projectile of that volley.
        int volleyRemaining, volleyIndex, shotDamageBonus, shotCriticalChance;
        bool shotCritical;
        bool discardForDraw;
        int nextBleed, nextBleedVolley;
        bool nextRupture, nextArmorBreak, nextSplitShot, nextEvasion, nextRain;

        // Every battle starts from the run deck, so reward cards picked in earlier stages are included.
        void BuildDeck()
        {
            if (runDeck.Count == 0) StartRun();
            drawPile.Clear(); discardPile.Clear(); hand.Clear();
            drawPile.AddRange(runDeck);
            Shuffle(drawPile);
            block = battleDamage = bonusShots = bonusDamage = bonusCritical = 0;
            volleyRemaining = volleyIndex = shotDamageBonus = shotCriticalChance = 0; shotCritical = false;
            arrows = 0; arrowCapacity = 10; shotsRequested = 1; playerTurnsStarted = 0;
            discardForDraw = false; nextBleed = nextBleedVolley = 0;
            nextRupture = nextArmorBreak = nextSplitShot = nextEvasion = nextRain = false;
            ClearArcherObjects();
        }
        static void Shuffle(List<FortressCardEntry> cards)
        {
            for (int i = cards.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (cards[i], cards[j]) = (cards[j], cards[i]); }
        }
        void StartPlayerCardTurn()
        {
            if (playerTurnsStarted++ == 0) arrows = 3;
            else LoadArrows(1);
            shotsRequested = Mathf.Clamp(shotsRequested, 1, Mathf.Max(1, arrows));
            discardPile.AddRange(hand); hand.Clear();
            energy = arena.rules.cardEnergy; block = 0;
            DrawCards(arena.rules.handSize);
        }
        // Draw and reshuffle run cards once for both the reward system and archer effects.
        void DrawCards(int count)
        {
            for (int i = 0; i < count && hand.Count < arena.rules.maxHandSize; i++)
            {
                if (drawPile.Count == 0) { drawPile.AddRange(discardPile); discardPile.Clear(); Shuffle(drawPile); }
                if (drawPile.Count == 0) break;
                hand.Add(drawPile[drawPile.Count - 1]); drawPile.RemoveAt(drawPile.Count - 1);
            }
        }
        bool CanPlay(int index)
        {
            if (!ready || phase != Phase.Aim || current != 0 || index < 0 || index >= hand.Count) return false;
            if (discardForDraw) return true;
            var card = hand[index];
            return energy >= card.cost && !(card.IsAttackBuff && playerHasAttacked) && CanUseCard(card);
        }
        void PlayCard(int index)
        {
            if (!CanPlay(index)) return;
            if (discardForDraw)
            {
                discardPile.Add(hand[index]); hand.RemoveAt(index); discardForDraw = false;
                message = "선택한 카드를 버렸습니다."; return;
            }
            var card = hand[index];
            energy -= card.cost; hand.RemoveAt(index); discardPile.Add(card);
            switch (card.effect)
            {
                case FortressCardEffect.ExtraShot: bonusShots += card.value; break;
                case FortressCardEffect.Defense: block += card.value; break;
                case FortressCardEffect.ShotDamage: bonusDamage += card.value; break;
                case FortressCardEffect.CriticalChance: bonusCritical += card.value; break;
                case FortressCardEffect.Draw: DrawCards(card.value); break;
                case FortressCardEffect.BattleDamage: battleDamage += card.value; break;
                case FortressCardEffect.Arrow: LoadArrows(card.value); break;
                case FortressCardEffect.QuickLoad: LoadArrows(card.value); DrawCards(1); break;
                case FortressCardEffect.DrawDiscard: DrawCards(2); discardForDraw = hand.Count > 0; break;
                case FortressCardEffect.BleedShot: nextBleed = card.value; break;
                case FortressCardEffect.Stake: SpendArrowForStake(); break;
                case FortressCardEffect.PickArrow: RecoverNearbyArrows(card.value); break;
                case FortressCardEffect.Rupture: nextRupture = true; break;
                case FortressCardEffect.ArmorBreak: nextArmorBreak = true; break;
                case FortressCardEffect.SplitShot: nextSplitShot = true; break;
                case FortressCardEffect.ArrowTrap: PlaceArrowTrap(); break;
                case FortressCardEffect.EvasiveManeuver: nextEvasion = true; break;
                case FortressCardEffect.BleedVolley: nextBleedVolley = card.value; break;
                case FortressCardEffect.ExtractPain: ExtractPinnedArrows(card.value); break;
                case FortressCardEffect.Rain: nextRain = true; break;
                case FortressCardEffect.RecoverArrows: RecoverPreviousTurnArrows(card.value); break;
                case FortressCardEffect.Quiver: arrowCapacity = Mathf.Max(arrowCapacity, card.value); break;
                case FortressCardEffect.Outpost: SetOutpostOrigin(); break;
                case FortressCardEffect.TacticalRearrangement: TacticalRearrange(); break;
            }
            message = $"카드 사용 · {card.title}: {card.Description}";
        }
        void ConsumeAttackBuffs(int shotCount = 1)
        {
            volleyRemaining = Mathf.Max(0, shotCount - 1); volleyIndex = 0;
            shotDamageBonus = bonusDamage; shotCriticalChance = bonusCritical;
            bonusShots = bonusDamage = bonusCritical = 0;
        }
        // Alternates +1, -1, +2, -2 … spread steps so a volley fans around the aimed arc.
        float VolleyAngleOffset(int shot) => shot == 0 ? 0 : (shot % 2 == 1 ? 1 : -1) * ((shot + 1) / 2) * Mathf.Min(4, arena.rules.volleySpread);
        bool TryContinueVolley()
        {
            if (current != 0 || volleyRemaining <= 0) return false;
            volleyRemaining--; volleyIndex++;
            phase = Phase.Attack; timer = fighters[0].definition.releaseTime;
            fighters[0].animator.SetTrigger(fighters[0].definition.weapon == FortressWeapon.Spear ? SpearAttackParameter : BowAttackParameter);
            return true;
        }
        void RollCritical(int index) => shotCritical = index == 0 && Random.Range(0, 100) < shotCriticalChance;
        int ShotDamage(int index)
        {
            int damage = fighters[index].definition.damage +
                (index == 0 ? battleDamage : 0) + (index == 0 && volleyIndex == 0 ? shotDamageBonus : 0);
            return shotCritical && index == 0 ? Mathf.RoundToInt(damage * arena.rules.criticalMultiplier) : damage;
        }
        int AbsorbWithBlock(int damage)
        {
            int absorbed = Mathf.Min(block, damage); block -= absorbed; return damage - absorbed;
        }
        string PendingBuffText()
        {
            var parts = new List<string>();
            if (bonusShots > 0) parts.Add($"+{bonusShots}발");
            if (bonusDamage > 0) parts.Add($"피해 +{bonusDamage}");
            if (bonusCritical > 0) parts.Add($"치명타 {Mathf.Min(bonusCritical, 100)}%");
            return parts.Count == 0 ? "없음" : string.Join(", ", parts);
        }
    }
}
