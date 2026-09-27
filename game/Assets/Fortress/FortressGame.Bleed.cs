using UnityEngine;

namespace MiniFortress
{
    // Bleed: enemies collect bleed from the player's arrows. Reaching the threshold (10) bursts it for 20 damage
    // and removes that much bleed. Cards bank "next arrow" effects, which lock in when the player attacks and
    // travel with every arrow of that attack; "next N arrows" effects are handed out one per arrow at launch.
    public sealed partial class FortressGame
    {
        // "Next arrow" effects banked by cards; they apply to every arrow of the next attack.
        sealed class AttackBuffs
        {
            public int bleed, damagePerBleed, bonusVsBleeding, execute, drawIfBleeding, weaken, detonate, markPrey;
            public bool brutal, doubleBleed;
            // Arrow effects resolved when the attack starts (see FortressGame.Arrows).
            public bool recoverMisses, recallLastTurn, recallAll;
            public int storm, lastArrow;
        }
        // What a single arrow carries when it lands.
        sealed class ShotMods
        {
            public AttackBuffs buffs;
            public int extraDamage, extraBleed;
        }

        AttackBuffs pendingBuffs = new AttackBuffs(), lockedBuffs = new AttackBuffs();
        ShotMods mainShotMods;
        // "Next N arrows" counters.
        int polishArrows, polishDamage, bleedArrows, bleedArrowAmount;
        // Battle-long and turn-long bleed modifiers.
        int prey = -1, preyBleed, bleedQuiver, chainBleed, acceleratedUntilRound = -1, acceleratedThreshold;
        int hitStreak, bloodCallStacks;
        bool bloodCall;
        string hitNote = "";

        void ResetBleedState()
        {
            pendingBuffs = new AttackBuffs(); lockedBuffs = new AttackBuffs(); mainShotMods = null;
            polishArrows = polishDamage = bleedArrows = bleedArrowAmount = 0;
            prey = -1; preyBleed = bleedQuiver = chainBleed = 0; acceleratedUntilRound = -1;
            hitStreak = bloodCallStacks = 0; bloodCall = false;
            foreach (var f in fighters) { f.bleed = 0; f.weakened = 0; }
        }

        // Turn-long effects end when a new player turn starts.
        void ResetTurnBleedEffects() { hitStreak = 0; bloodCall = false; }

        void LockAttackBuffs() { lockedBuffs = pendingBuffs; pendingBuffs = new AttackBuffs(); }

        ShotMods NextArrowMods()
        {
            var mods = new ShotMods { buffs = lockedBuffs };
            if (polishArrows > 0) { polishArrows--; mods.extraDamage += polishDamage; }
            if (bleedArrows > 0) { bleedArrows--; mods.extraBleed += bleedArrowAmount; }
            if (stormArrows > 0) { stormArrows--; mods.extraDamage += stormDamage; }
            return mods;
        }

        int BleedThreshold => Mathf.Max(1, (round <= acceleratedUntilRound ? acceleratedThreshold : arena.rules.bleedThreshold) - bloodCallStacks);

        // Damage bonuses that depend on the enemy being hit.
        int TargetBonus(int index, ShotMods mods)
        {
            if (mods == null || index == 0) return 0;
            var b = mods.buffs; var f = fighters[index];
            int bonus = b.damagePerBleed * f.bleed + (f.bleed > 0 ? b.bonusVsBleeding : 0);
            if (f.hp <= f.maxHp * .3f) bonus += b.execute;
            return bonus;
        }

        // On-hit effects for the enemy that took the most damage from a player arrow.
        void OnPlayerHit(int target, ShotMods mods)
        {
            var f = fighters[target];
            var b = mods?.buffs ?? new AttackBuffs();
            bool wasBleeding = f.bleed >= 5;
            if (b.detonate > 0 && f.bleed > 0)
            {
                int extra = f.bleed * b.detonate; f.bleed = 0;
                Hurt(f, extra); hitNote += $" · 피의 폭발 {extra}";
            }
            if (b.markPrey > 0 && prey < 0) { prey = target; preyBleed = b.markPrey; hitNote += $" · {f.name} 사냥감 지정"; }
            else if (target == prey) f.bleed += preyBleed;
            f.bleed += b.bleed + (mods?.extraBleed ?? 0) + bleedQuiver;
            if (b.doubleBleed) f.bleed *= 2;
            if (b.drawIfBleeding > 0 && wasBleeding) DrawCards(b.drawIfBleeding);
            if (b.weaken > 0) f.weakened += b.weaken;
            if (hitStreak > 0) bonusDamage += hitStreak;
            if (bloodCall) bloodCallStacks++;
            if (b.brutal && f.bleed >= 5 && f.bleed < BleedThreshold) BurstBleed(target, f.bleed);
            CheckBleed(target);
        }

        void CheckBleed(int index)
        {
            while (fighters[index].hp > 0 && fighters[index].bleed >= BleedThreshold) BurstBleed(index, BleedThreshold);
        }

        void BurstBleed(int index, int consumed)
        {
            var f = fighters[index];
            f.bleed = Mathf.Max(0, f.bleed - consumed); bloodCallStacks = 0;
            Hurt(f, arena.rules.bleedBurstDamage);
            hitNote += $" · 출혈 폭발 {arena.rules.bleedBurstDamage}";
            if (chainBleed <= 0) return;
            int amount = chainBleed; chainBleed = 0;
            for (int i = 1; i < fighters.Count; i++)
            {
                if (i == index || fighters[i].hp <= 0 || Vector2.Distance(fighters[i].feet, f.feet) > arena.rules.chainBleedRange) continue;
                fighters[i].bleed += amount; CheckBleed(i);
            }
        }

        void Hurt(Fighter f, int damage)
        {
            if (damage <= 0 || f.hp <= 0) return;
            f.hp = Mathf.Max(0, f.hp - damage); PlayDamageReaction(f);
        }

        // Card effects that feed the bleed system; returns false for effects handled elsewhere.
        bool PlayBleedCard(FortressCardEntry card)
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
                case FortressCardEffect.BleedAccelerate: acceleratedUntilRound = round + 1; acceleratedThreshold = card.value; return true;
                case FortressCardEffect.BrutalShot: bonusDamage += card.value; pendingBuffs.brutal = true; return true;
                case FortressCardEffect.BleedDetonate: pendingBuffs.detonate = Mathf.Max(pendingBuffs.detonate, card.value); return true;
                case FortressCardEffect.MarkPrey: pendingBuffs.markPrey = Mathf.Max(pendingBuffs.markPrey, card.value); return true;
                case FortressCardEffect.DoubleBleed: pendingBuffs.doubleBleed = true; return true;
                case FortressCardEffect.BleedQuiver: bleedQuiver += card.value; return true;
                case FortressCardEffect.ChainBleed: chainBleed = Mathf.Max(chainBleed, card.value); return true;
                case FortressCardEffect.BloodCall: bloodCall = true; return true;
            }
            return false;
        }

        string PendingBleedText()
        {
            var b = pendingBuffs; var parts = new System.Collections.Generic.List<string>();
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
}
