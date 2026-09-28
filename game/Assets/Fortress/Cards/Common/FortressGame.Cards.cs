using System.Collections.Generic;
using UnityEngine;

namespace MiniFortress
{
    // Deck, hand, energy and the effects every class may use (defense, draw, damage, critical, cost discount).
    public sealed partial class FortressGame
    {
        readonly List<FortressCardEntry> drawPile = new List<FortressCardEntry>();
        readonly List<FortressCardEntry> discardPile = new List<FortressCardEntry>();
        readonly List<FortressCardEntry> hand = new List<FortressCardEntry>();
        int energy, block, battleDamage, costDiscount;
        // Banked by cards until the player's next attack.
        int bonusShots, bonusDamage, bonusCritical;
        // Locked in when the player attacks and used by every projectile of that volley.
        int volleyRemaining, shotDamageBonus, shotCriticalChance;
        bool shotCritical;

        // Every battle starts from the run deck, so reward cards picked in earlier stages are included.
        void BuildDeck()
        {
            if (runDeck.Count == 0) StartRun();
            drawPile.Clear(); discardPile.Clear(); hand.Clear();
            drawPile.AddRange(runDeck);
            Shuffle(drawPile);
            block = battleDamage = bonusShots = bonusDamage = bonusCritical = 0;
            volleyRemaining = shotDamageBonus = shotCriticalChance = costDiscount = 0; shotCritical = false;
            PlayerClass.OnBattleStart();
        }
        static void Shuffle(List<FortressCardEntry> cards)
        {
            for (int i = cards.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (cards[i], cards[j]) = (cards[j], cards[i]); }
        }
        void StartPlayerCardTurn()
        {
            discardPile.AddRange(hand); hand.Clear();
            energy = arena.rules.cardEnergy; block = 0;
            PlayerClass.OnPlayerTurnStart();
            DrawCards(arena.rules.handSize);
        }
        // An empty draw pile reshuffles the discard pile back in; the hand never exceeds maxHandSize.
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
            var card = hand[index];
            return energy >= EffectiveCost(card) && !(card.IsAttackBuff && playerHasAttacked);
        }
        int EffectiveCost(FortressCardEntry card) => Mathf.Max(0, card.cost - costDiscount);
        void PlayCard(int index)
        {
            if (!CanPlay(index)) return;
            var card = hand[index];
            // A discount is used up by the next card played, then that card's own effect applies.
            energy -= EffectiveCost(card); costDiscount = 0;
            hand.RemoveAt(index);
            if (!card.exhaust) discardPile.Add(card); // exhausted cards sit out the rest of the battle
            switch (card.effect)
            {
                case FortressCardEffect.Retreat: block += card.value + (playerHasAttacked ? 0 : card.value); break;
                case FortressCardEffect.CostDiscount: costDiscount += card.value; break;
                case FortressCardEffect.ExtraShot: bonusShots += card.value; break;
                case FortressCardEffect.Defense: block += card.value; break;
                case FortressCardEffect.ShotDamage: bonusDamage += card.value; break;
                case FortressCardEffect.CriticalChance: bonusCritical += card.value; break;
                case FortressCardEffect.Draw: DrawCards(card.value); break;
                case FortressCardEffect.BattleDamage: battleDamage += card.value; break;
                // Class-specific effects are handled by the player's class module (Assets/Fortress/Classes/<Class>).
                default:
                    if (!PlayerClass.PlayCard(card))
                        Debug.LogWarning("FortressGame: 처리하는 직업이 없는 카드 효과입니다: " + card.effect, this);
                    break;
            }
            message = $"카드 사용 · {card.title}: {card.Description}";
        }
        // The class decides how many projectiles the attack looses (the archer: every arrow held, plus card extras).
        void ConsumeAttackBuffs()
        {
            PlayerClass.OnAttackCommitted();
            volleyRemaining = PlayerClass.ShotsForAttack(bonusShots) - 1;
            shotDamageBonus = bonusDamage; shotCriticalChance = bonusCritical;
            bonusShots = bonusDamage = bonusCritical = 0;
        }
        void RollCritical(int index) => shotCritical = index == 0 && Random.Range(0, 100) < shotCriticalChance;
        int ShotDamage(int index, bool critical)
        {
            int damage = fighters[index].definition.damage + (index == 0 ? shotDamageBonus + battleDamage : 0);
            if (index == 0) damage = Mathf.Max(0, damage * PlayerClass.DamageMultiplier);
            return critical && index == 0 ? Mathf.RoundToInt(damage * arena.rules.criticalMultiplier) : damage;
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
            string classText = PlayerClass.PendingAttackText; if (classText.Length > 0) parts.Add(classText);
            return parts.Count == 0 ? "없음" : string.Join(", ", parts);
        }
    }
}
