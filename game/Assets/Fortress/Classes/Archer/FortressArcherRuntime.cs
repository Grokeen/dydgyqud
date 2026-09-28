using System.Collections.Generic;
using UnityEngine;

namespace MiniFortress
{
    // Archer arrows. Each player turn starts with 1 arrow (plus arrows recovered last turn); cards add more, but
    // never past the cap (10, raised only by max-arrow cards). One attack looses every arrow held in a quick
    // stream at the same angle, and the count drops back to 1 next turn. Arrows that miss stay on the map;
    // recovering takes the ones nearest the archer off the map and adds them to next turn's volley.
    // Bleed and weaken are in FortressArcherRuntime.Bleed.cs.
    public sealed partial class FortressArcherRuntime : FortressClassRuntime
    {
        sealed class FallenArrow { public Transform view; public int round; }
        readonly List<FallenArrow> fallenArrows = new List<FallenArrow>();
        int arrows, maxArrowBonus, recoveredNext, recoveredTotal;
        int stormArrows, stormDamage, attackMultiplier = 1;

        public FortressArcherClass Settings { get; }
        public int ArrowCount => arrows;
        public int MaxArrowCount => Settings.baseMaxArrows + maxArrowBonus;
        public int RecoveredNextTurn => recoveredNext;
        public int FallenArrowCount => fallenArrows.Count;

        public FortressArcherRuntime(FortressArcherClass settings, IFortressBattle battle, FortressCharacterDefinition definition)
            : base(battle, definition) => Settings = settings;

        public override void OnBattleStart()
        {
            ClearVisuals();
            maxArrowBonus = recoveredNext = recoveredTotal = stormArrows = stormDamage = 0; attackMultiplier = 1;
            arrows = Settings.baseArrows;
            ResetBleedState();
        }

        public override void OnPlayerTurnStart()
        {
            ResetTurnBleedEffects();
            RefillArrows();
        }

        public void RefillArrows() { arrows = Mathf.Min(MaxArrowCount, Settings.baseArrows + recoveredNext); recoveredNext = 0; }

        void AddArrows(int count) => arrows = Mathf.Min(MaxArrowCount, arrows + count);

        public override void OnAttackCommitted() => LockAttackBuffs();

        // One attack looses every arrow held plus card extras, recalled arrows and storm arrows, up to the cap.
        public override int ShotsForAttack(int extraShots)
        {
            attackMultiplier = 1;
            var b = lockedBuffs;
            int shots = arrows + extraShots; arrows = 0;
            if (b.recallAll) shots += Recall(a => true);
            else if (b.recallLastTurn) shots += Recall(a => a.round == battle.Round - 1);
            if (b.storm > 0) { int more = shots / 2; shots += more; stormArrows += more; stormDamage = b.storm; }
            shots = Mathf.Clamp(shots, 1, MaxArrowCount);
            if (b.lastArrow > 0 && shots == 1) attackMultiplier = b.lastArrow;
            return shots;
        }

        public override int DamageMultiplier => attackMultiplier;

        // A player arrow that hurt nobody: it stays where it landed, unless "빗나간 한 발" recovers it at once.
        public override void OnMiss(Vector2 at, Vector2 velocity, bool landed)
        {
            if (lockedBuffs.recoverMisses) { recoveredNext++; recoveredTotal++; return; }
            if (!landed) return; // flew off the map
            var view = battle.SpawnProjectileView("Fallen arrow", at, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
            fallenArrows.Add(new FallenArrow { view = view, round = battle.Round });
        }

        // Removes the fallen arrows nearest the archer; returns how many were picked up.
        int RecoverNearest(int count)
        {
            Vector2 archer = battle.FighterFeet(0);
            fallenArrows.Sort((a, b) => Vector2.Distance(a.view.position, archer).CompareTo(Vector2.Distance(b.view.position, archer)));
            int taken = Mathf.Min(count, fallenArrows.Count);
            for (int i = 0; i < taken; i++) Object.Destroy(fallenArrows[i].view.gameObject);
            fallenArrows.RemoveRange(0, taken);
            recoveredTotal += taken;
            return taken;
        }

        int Recall(System.Predicate<FallenArrow> which)
        {
            int taken = 0;
            for (int i = fallenArrows.Count - 1; i >= 0; i--)
                if (which(fallenArrows[i])) { Object.Destroy(fallenArrows[i].view.gameObject); fallenArrows.RemoveAt(i); taken++; }
            recoveredTotal += taken;
            return taken;
        }

        public override void ClearVisuals()
        {
            foreach (var arrow in fallenArrows) if (arrow.view) Object.Destroy(arrow.view.gameObject);
            fallenArrows.Clear();
        }

        // Arrows beyond the base cap of 10, possible once max-arrow cards have raised the cap.
        int ExcessArrows => Mathf.Max(0, arrows - Settings.baseMaxArrows);

        public override bool PlayCard(FortressCardEntry card) => PlayBleedCard(card) || PlayArrowCard(card);

        // Card effects about arrows; returns false for effects handled elsewhere.
        public bool PlayArrowCard(FortressCardEntry card)
        {
            switch (card.effect)
            {
                case FortressCardEffect.AddArrows: AddArrows(card.value); if (card.count > 0) battle.DrawCards(card.count); return true;
                case FortressCardEffect.SloppyArrow: AddArrows(card.value); battle.AddNextAttackDamage(-card.count); return true;
                case FortressCardEffect.MaxArrows: maxArrowBonus += card.value; return true;
                case FortressCardEffect.Bundle:
                    maxArrowBonus = Mathf.Max(maxArrowBonus, card.count - Settings.baseMaxArrows); AddArrows(card.value); return true;
                case FortressCardEffect.Recover: recoveredNext += RecoverNearest(card.value); return true;
                case FortressCardEffect.RecoverNow: AddArrows(RecoverNearest(card.value)); return true;
                case FortressCardEffect.RecoverMaxUp: recoveredNext += RecoverNearest(card.value); maxArrowBonus += 1; return true;
                case FortressCardEffect.RecoverMisses: pendingBuffs.recoverMisses = true; return true;
                case FortressCardEffect.RecallLastTurn: pendingBuffs.recallLastTurn = true; return true;
                case FortressCardEffect.RecallAll: pendingBuffs.recallAll = true; return true;
                case FortressCardEffect.ArrowStorm: pendingBuffs.storm = Mathf.Max(pendingBuffs.storm, card.value); return true;
                case FortressCardEffect.LastArrow: pendingBuffs.lastArrow = Mathf.Max(pendingBuffs.lastArrow, card.value); return true;
                case FortressCardEffect.Overflow: case FortressCardEffect.Stockpile: battle.AddNextAttackDamage(card.value * ExcessArrows); return true;
                case FortressCardEffect.StoreArrows: battle.AddNextAttackShots(ExcessArrows); return true;
                case FortressCardEffect.RecoveryExpert: battle.AddNextAttackDamage(card.value * recoveredTotal); return true;
            }
            return false;
        }

        public override string AttackResourceText => $"화살 {arrows}/{MaxArrowCount} · 일제 발사";
        public override string PlayerStatusText => $"화살 {arrows}/{MaxArrowCount}" + (recoveredNext > 0 ? $" (+{recoveredNext})" : "");
    }
}
