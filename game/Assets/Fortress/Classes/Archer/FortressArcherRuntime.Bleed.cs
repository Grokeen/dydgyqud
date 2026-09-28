using System.Collections.Generic;
using UnityEngine;

namespace MiniFortress
{
    // Archer bleed: enemies collect bleed from the player's arrows. Reaching the threshold (10) bursts it for 20
    // damage and removes that much bleed. Cards bank "next arrow" effects, which lock in when the player attacks
    // and travel with every arrow of that attack; "next N arrows" effects are handed out one per arrow at launch.
    // Weaken (견제 사격) also lives here: it lowers the hit enemy's next shot.
    public sealed partial class FortressArcherRuntime
    {
        // "Next arrow" effects banked by cards; they apply to every arrow of the next attack.
        sealed class AttackBuffs
        {
            public int bleed, damagePerBleed, bonusVsBleeding, execute, drawIfBleeding, weaken, detonate, markPrey;
            public bool brutal, doubleBleed;
            // Arrow effects resolved when the attack starts (see FortressArcherRuntime arrows).
            public bool recoverMisses, recallLastTurn, recallAll;
            public int storm, lastArrow;
        }
        // What a single arrow carries when it lands.
        sealed class ArcherShotMods : FortressShotMods
        {
            public AttackBuffs buffs;
            public int extraBleed;
        }

        AttackBuffs pendingBuffs = new AttackBuffs(), lockedBuffs = new AttackBuffs();
        // Per fighter index (0 is the player and never bleeds).
        int[] bleed = new int[0], weakened = new int[0];
        // "Next N arrows" counters.
        int polishArrows, polishDamage, bleedArrows, bleedArrowAmount;
        // Battle-long and turn-long bleed modifiers.
        int prey = -1, preyBleed, bleedQuiver, chainBleed, acceleratedUntilRound = -1, acceleratedThreshold;
        int hitStreak, bloodCallStacks;
        bool bloodCall;

        public int Bleed(int index) => index >= 0 && index < bleed.Length ? bleed[index] : 0;
        public void AddBleed(int index, int amount) { if (index > 0 && index < bleed.Length) bleed[index] += amount; }
        public int BleedThreshold => Mathf.Max(1, (battle.Round <= acceleratedUntilRound ? acceleratedThreshold : Settings.bleedThreshold) - bloodCallStacks);

        void ResetBleedState()
        {
            pendingBuffs = new AttackBuffs(); lockedBuffs = new AttackBuffs();
            polishArrows = polishDamage = bleedArrows = bleedArrowAmount = 0;
            prey = -1; preyBleed = bleedQuiver = chainBleed = 0; acceleratedUntilRound = -1;
            hitStreak = bloodCallStacks = 0; bloodCall = false;
            bleed = new int[battle.FighterCount]; weakened = new int[battle.FighterCount];
        }

        // Turn-long effects end when a new player turn starts.
        void ResetTurnBleedEffects() { hitStreak = 0; bloodCall = false; }

        public void LockAttackBuffs() { lockedBuffs = pendingBuffs; pendingBuffs = new AttackBuffs(); }

        public override FortressShotMods NextShotMods()
        {
            var mods = new ArcherShotMods { buffs = lockedBuffs };
            if (polishArrows > 0) { polishArrows--; mods.extraDamage += polishDamage; }
            if (bleedArrows > 0) { bleedArrows--; mods.extraBleed += bleedArrowAmount; }
            if (stormArrows > 0) { stormArrows--; mods.extraDamage += stormDamage; }
            return mods;
        }

        // Damage bonuses that depend on the enemy being hit.
        public override int TargetBonus(int index, FortressShotMods shot)
        {
            if (!(shot is ArcherShotMods mods) || index == 0) return 0;
            var b = mods.buffs;
            int bonus = b.damagePerBleed * Bleed(index) + (Bleed(index) > 0 ? b.bonusVsBleeding : 0);
            if (battle.FighterHp(index) <= battle.FighterMaxHp(index) * .3f) bonus += b.execute;
            return bonus;
        }

        public override int AdjustEnemyShotDamage(int enemy, int damage)
        {
            if (enemy <= 0 || enemy >= weakened.Length) return damage;
            damage = Mathf.Max(0, damage - weakened[enemy]); weakened[enemy] = 0;
            return damage;
        }

        // On-hit effects for the enemy that took the most damage from a player arrow.
        public override void OnHit(int target, FortressShotMods shot)
        {
            if (target <= 0 || target >= bleed.Length) return;
            var mods = shot as ArcherShotMods;
            var b = mods?.buffs ?? new AttackBuffs();
            bool wasBleeding = bleed[target] >= 5;
            if (b.detonate > 0 && bleed[target] > 0)
            {
                int extra = bleed[target] * b.detonate; bleed[target] = 0;
                battle.DamageFighter(target, extra); battle.AddHitNote($" · 피의 폭발 {extra}");
            }
            if (b.markPrey > 0 && prey < 0) { prey = target; preyBleed = b.markPrey; battle.AddHitNote($" · {battle.FighterName(target)} 사냥감 지정"); }
            else if (target == prey) bleed[target] += preyBleed;
            bleed[target] += b.bleed + (mods?.extraBleed ?? 0) + bleedQuiver;
            if (b.doubleBleed) bleed[target] *= 2;
            if (b.drawIfBleeding > 0 && wasBleeding) battle.DrawCards(b.drawIfBleeding);
            if (b.weaken > 0) weakened[target] += b.weaken;
            if (hitStreak > 0) battle.AddNextAttackDamage(hitStreak);
            if (bloodCall) bloodCallStacks++;
            if (b.brutal && bleed[target] >= 5 && bleed[target] < BleedThreshold) BurstBleed(target, bleed[target]);
            CheckBleed(target);
        }

        void CheckBleed(int index)
        {
            while (battle.FighterHp(index) > 0 && bleed[index] >= BleedThreshold) BurstBleed(index, BleedThreshold);
        }

        void BurstBleed(int index, int consumed)
        {
            bleed[index] = Mathf.Max(0, bleed[index] - consumed); bloodCallStacks = 0;
            battle.DamageFighter(index, Settings.bleedBurstDamage);
            battle.AddHitNote($" · 출혈 폭발 {Settings.bleedBurstDamage}");
            if (chainBleed <= 0) return;
            int amount = chainBleed; chainBleed = 0;
            Vector2 origin = battle.FighterFeet(index);
            for (int i = 1; i < bleed.Length; i++)
            {
                if (i == index || battle.FighterHp(i) <= 0 || Vector2.Distance(battle.FighterFeet(i), origin) > Settings.chainBleedRange) continue;
                bleed[i] += amount; CheckBleed(i);
            }
        }

        // Card effects that feed the bleed system; returns false for effects handled elsewhere.
        public bool PlayBleedCard(FortressCardEntry card)
        {
            switch (card.effect)
            {
                case FortressCardEffect.Bleed: pendingBuffs.bleed += card.value; return true;
                case FortressCardEffect.BleedArrows:
                    bleedArrows += Mathf.Max(1, card.count); bleedArrowAmount = Mathf.Max(bleedArrowAmount, card.value); return true;
                case FortressCardEffect.ArrowPolish:
                    polishArrows += Mathf.Max(1, card.count); polishDamage = Mathf.Max(polishDamage, card.value); return true;
                case FortressCardEffect.DrawIfBleeding: pendingBuffs.drawIfBleeding += card.value; return true;
                case FortressCardEffect.Execute: pendingBuffs.execute += card.value; return true;
                case FortressCardEffect.Weaken: pendingBuffs.weaken += card.value; return true;
                case FortressCardEffect.DamagePerBleed: pendingBuffs.damagePerBleed += card.value; return true;
                case FortressCardEffect.BonusVsBleeding: pendingBuffs.bonusVsBleeding += card.value; return true;
                case FortressCardEffect.HitStreak: hitStreak += card.value; return true;
                case FortressCardEffect.BleedAccelerate: acceleratedUntilRound = battle.Round + 1; acceleratedThreshold = card.value; return true;
                case FortressCardEffect.BrutalShot: battle.AddNextAttackDamage(card.value); pendingBuffs.brutal = true; return true;
                case FortressCardEffect.BleedDetonate: pendingBuffs.detonate = Mathf.Max(pendingBuffs.detonate, card.value); return true;
                case FortressCardEffect.MarkPrey: pendingBuffs.markPrey = Mathf.Max(pendingBuffs.markPrey, card.value); return true;
                case FortressCardEffect.DoubleBleed: pendingBuffs.doubleBleed = true; return true;
                case FortressCardEffect.BleedQuiver: bleedQuiver += card.value; return true;
                case FortressCardEffect.ChainBleed: chainBleed = Mathf.Max(chainBleed, card.value); return true;
                case FortressCardEffect.BloodCall: bloodCall = true; return true;
            }
            return false;
        }

        public override string PendingAttackText
        {
            get
            {
                var b = pendingBuffs; var parts = new List<string>();
                if (b.bleed > 0) parts.Add($"출혈 {b.bleed}");
                if (bleedArrows > 0) parts.Add($"출혈 화살 {bleedArrows}");
                if (polishArrows > 0) parts.Add($"손질 {polishArrows}");
                if (b.detonate > 0) parts.Add("피의 폭발");
                if (b.doubleBleed) parts.Add("출혈 2배");
                if (b.markPrey > 0) parts.Add("사냥감 지정");
                if (b.damagePerBleed > 0 || b.bonusVsBleeding > 0 || b.execute > 0) parts.Add("조건부 피해");
                return string.Join(", ", parts);
            }
        }

        public override string ActorStatusText(int index) => Bleed(index) > 0 ? $"출혈 {Bleed(index)}/{BleedThreshold}" : "";
    }
}
