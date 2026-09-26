using System.Collections.Generic;
using UnityEngine;

namespace MiniFortress
{
    public sealed partial class FortressGame
    {
        readonly List<FortressCardEntry> drawPile = new List<FortressCardEntry>();
        readonly List<FortressCardEntry> discardPile = new List<FortressCardEntry>();
        readonly List<FortressCardEntry> hand = new List<FortressCardEntry>();
        int energy, block;
        // Banked by cards until the player's next attack.
        int bonusShots, bonusDamage, bonusCritical;
        // Locked in when the player attacks and used by every projectile of that volley.
        int volleyRemaining, volleyIndex, shotDamageBonus, shotCriticalChance;
        bool shotCritical;

        void BuildDeck()
        {
            drawPile.Clear(); discardPile.Clear(); hand.Clear();
            foreach (var card in fighters[0].definition.Deck)
                if (card != null) for (int i = 0; i < card.copies; i++) drawPile.Add(card);
            Shuffle(drawPile);
            block = bonusShots = bonusDamage = bonusCritical = 0;
            volleyRemaining = volleyIndex = shotDamageBonus = shotCriticalChance = 0; shotCritical = false;
        }
        static void Shuffle(List<FortressCardEntry> cards)
        {
            for (int i = cards.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (cards[i], cards[j]) = (cards[j], cards[i]); }
        }
        void StartPlayerCardTurn()
        {
            discardPile.AddRange(hand); hand.Clear();
            energy = arena.rules.cardEnergy; block = 0;
            for (int i = 0; i < arena.rules.handSize; i++)
            {
                if (drawPile.Count == 0) { drawPile.AddRange(discardPile); discardPile.Clear(); Shuffle(drawPile); }
                if (drawPile.Count == 0) break;
                hand.Add(drawPile[drawPile.Count - 1]); drawPile.RemoveAt(drawPile.Count - 1);
            }
        }
        bool CanPlay(int index)
        {
            if (!ready || phase != Phase.Aim || current != 0 || index < 0 || index >= hand.Count) return false;
            var card = hand[index];
            return energy >= card.cost && !(card.IsAttackBuff && playerHasAttacked);
        }
        void PlayCard(int index)
        {
            if (!CanPlay(index)) return;
            var card = hand[index];
            energy -= card.cost; hand.RemoveAt(index); discardPile.Add(card);
            switch (card.effect)
            {
                case FortressCardEffect.ExtraShot: bonusShots += card.value; break;
                case FortressCardEffect.Defense: block += card.value; break;
                case FortressCardEffect.ShotDamage: bonusDamage += card.value; break;
                case FortressCardEffect.CriticalChance: bonusCritical += card.value; break;
            }
            message = $"카드 사용 · {card.title}: {card.Description}";
        }
        void ConsumeAttackBuffs()
        {
            volleyRemaining = bonusShots; volleyIndex = 0;
            shotDamageBonus = bonusDamage; shotCriticalChance = bonusCritical;
            bonusShots = bonusDamage = bonusCritical = 0;
        }
        // Alternates +1, -1, +2, -2 … spread steps so a volley fans around the aimed arc.
        float VolleyAngleOffset(int shot) => shot == 0 ? 0 : (shot % 2 == 1 ? 1 : -1) * ((shot + 1) / 2) * arena.rules.volleySpread;
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
            int damage = fighters[index].definition.damage + (index == 0 ? shotDamageBonus : 0);
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
