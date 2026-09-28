using System.Collections.Generic;
using UnityEngine;

namespace MiniFortress
{
    // Archer arrows. Each player turn starts with 1 arrow (plus arrows recovered last turn); cards add more, but
    // never past the cap (10, raised only by max-arrow cards). One attack looses every arrow held in a quick
    // stream at the same angle, and the count drops back to 1 next turn. Arrows that miss stay on the map;
    // recovering takes the ones nearest the archer off the map and adds them to next turn's volley.
    public sealed partial class FortressGame
    {
        sealed class FallenArrow { public Transform view; public int round; }
        readonly List<FallenArrow> fallenArrows = new List<FallenArrow>();
        int arrows, maxArrowBonus, recoveredNext, recoveredTotal;
        int stormArrows, stormDamage, attackMultiplier = 1;

        int MaxArrows => arena.rules.baseMaxArrows + maxArrowBonus;

        void ResetArrows()
        {
            ClearFallenArrows();
            maxArrowBonus = recoveredNext = recoveredTotal = stormArrows = stormDamage = 0; attackMultiplier = 1;
            arrows = arena.rules.baseArrows;
        }

        void RefillArrows() { arrows = Mathf.Min(MaxArrows, arena.rules.baseArrows + recoveredNext); recoveredNext = 0; }

        void AddArrows(int count) => arrows = Mathf.Min(MaxArrows, arrows + count);

        // Number of arrows this attack looses. Called once when the player attacks; extra comes from multi-shot
        // cards. Non-archers throw one plus extras.
        int TakeArrowsForAttack(int extra)
        {
            attackMultiplier = 1;
            if (!UsesArrows) return 1 + extra;
            var b = lockedBuffs;
            int shots = arrows + extra; arrows = 0;
            if (b.recallAll) shots += Recall(a => true);
            else if (b.recallLastTurn) shots += Recall(a => a.round == round - 1);
            if (b.storm > 0) { int more = shots / 2; shots += more; stormArrows += more; stormDamage = b.storm; }
            shots = Mathf.Clamp(shots, 1, MaxArrows);
            if (b.lastArrow > 0 && shots == 1) attackMultiplier = b.lastArrow;
            return shots;
        }

        // A player arrow that hurt nobody: it stays where it landed, unless "빗나간 한 발" recovers it at once.
        void PlayerArrowMissed(Vector2 at, Vector2 velocity, bool landed)
        {
            if (!UsesArrows) return;
            if (lockedBuffs.recoverMisses) { recoveredNext++; recoveredTotal++; return; }
            if (!landed) return; // flew off the map
            var view = ProjectileObject("Fallen arrow", fighters[0].definition.weapon, at);
            view.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
            view.localScale = Vector3.one * fighters[0].root.localScale.x;
            fallenArrows.Add(new FallenArrow { view = view, round = round });
        }

        // Removes the fallen arrows nearest the archer; returns how many were picked up.
        int RecoverNearest(int count)
        {
            Vector2 archer = fighters[0].feet;
            fallenArrows.Sort((a, b) => Vector2.Distance(a.view.position, archer).CompareTo(Vector2.Distance(b.view.position, archer)));
            int taken = Mathf.Min(count, fallenArrows.Count);
            for (int i = 0; i < taken; i++) Destroy(fallenArrows[i].view.gameObject);
            fallenArrows.RemoveRange(0, taken);
            recoveredTotal += taken;
            return taken;
        }

        int Recall(System.Predicate<FallenArrow> which)
        {
            int taken = 0;
            for (int i = fallenArrows.Count - 1; i >= 0; i--)
                if (which(fallenArrows[i])) { Destroy(fallenArrows[i].view.gameObject); fallenArrows.RemoveAt(i); taken++; }
            recoveredTotal += taken;
            return taken;
        }

        void ClearFallenArrows()
        {
            foreach (var arrow in fallenArrows) if (arrow.view) Destroy(arrow.view.gameObject);
            fallenArrows.Clear();
        }

        // Arrows beyond the base cap of 10, possible once max-arrow cards have raised the cap.
        int ExcessArrows => Mathf.Max(0, arrows - arena.rules.baseMaxArrows);

        // Card effects about arrows; returns false for effects handled elsewhere.
        bool PlayArrowCard(FortressCardEntry card)
        {
            switch (card.effect)
            {
                case FortressCardEffect.AddArrows: AddArrows(card.value); if (card.count > 0) DrawCards(card.count); return true;
                case FortressCardEffect.SloppyArrow: AddArrows(card.value); bonusDamage -= card.count; return true;
                case FortressCardEffect.MaxArrows: maxArrowBonus += card.value; return true;
                case FortressCardEffect.Bundle:
                    maxArrowBonus = Mathf.Max(maxArrowBonus, card.count - arena.rules.baseMaxArrows); AddArrows(card.value); return true;
                case FortressCardEffect.Recover: recoveredNext += RecoverNearest(card.value); return true;
                case FortressCardEffect.RecoverNow: AddArrows(RecoverNearest(card.value)); return true;
                case FortressCardEffect.RecoverMaxUp: recoveredNext += RecoverNearest(card.value); maxArrowBonus += 1; return true;
                case FortressCardEffect.RecoverMisses: pendingBuffs.recoverMisses = true; return true;
                case FortressCardEffect.RecallLastTurn: pendingBuffs.recallLastTurn = true; return true;
                case FortressCardEffect.RecallAll: pendingBuffs.recallAll = true; return true;
                case FortressCardEffect.ArrowStorm: pendingBuffs.storm = Mathf.Max(pendingBuffs.storm, card.value); return true;
                case FortressCardEffect.LastArrow: pendingBuffs.lastArrow = Mathf.Max(pendingBuffs.lastArrow, card.value); return true;
                case FortressCardEffect.Overflow: case FortressCardEffect.Stockpile: bonusDamage += card.value * ExcessArrows; return true;
                case FortressCardEffect.StoreArrows: bonusShots += ExcessArrows; return true;
                case FortressCardEffect.RecoveryExpert: bonusDamage += card.value * recoveredTotal; return true;
            }
            return false;
        }
    }
}
