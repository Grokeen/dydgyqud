using UnityEngine;

namespace MiniFortress
{
    // New effects are appended so cards already saved in assets keep their effect (assets store the number).
    // Effects are shared by every class; which class handles one is decided in Cards/<Class>/FortressGame.<Class>Cards.cs.
    public enum FortressCardEffect
    {
        ExtraShot, Defense, ShotDamage, CriticalChance, Draw, BattleDamage,
        Bleed, BleedArrows, ArrowPolish, DrawIfBleeding, Execute, Weaken, Retreat, CostDiscount,
        DamagePerBleed, BonusVsBleeding, HitStreak, BleedAccelerate, BrutalShot, BleedDetonate,
        MarkPrey, DoubleBleed, BleedQuiver, ChainBleed, BloodCall,
        AddArrows, SloppyArrow, MaxArrows, Bundle, Recover, RecoverNow, RecoverMaxUp, RecoverMisses,
        RecallLastTurn, RecallAll, ArrowStorm, LastArrow, Overflow, Stockpile, StoreArrows, RecoveryExpert
    }
    // Legend cards are bought with boss currency; stage rewards and shops only offer Common to Hero.
    public enum FortressCardRarity { Common, Rare, Hero, Legend }

    [System.Serializable]
    public sealed class FortressCardEntry
    {
        public string title;
        public FortressCardEffect effect;
        [Tooltip("추가 발사 수 / 방어도 / 추가 피해 / 치명타 확률(%) / 뽑을 장수 / 이번 전투 기본 피해 증가")]
        [Min(0)] public int value = 1;
        [Min(1)] public int copies = 1;
        [Min(0)] public int cost = 1;
        [Tooltip("\"다음 N개의 화살\" 효과의 화살 수")]
        [Min(0)] public int count;
        [Tooltip("소모: 이번 전투에서 한 번만 사용")]
        public bool exhaust;
        public FortressCardRarity rarity;
        [Tooltip("비워 두면 효과에서 설명을 만듭니다.")]
        [TextArea] public string text;
        [Tooltip("카드 그림. 비워 두면 Resources/FortressCardArt/<직업 폴더>에서 카드 이름, 그다음 효과 이름(예: ShotDamage)과 같은 파일을 찾습니다.")]
        public Sprite art;

        public FortressCardEntry() { }
        public FortressCardEntry(string title, FortressCardEffect effect, int value, int copies)
        { this.title = title; this.effect = effect; this.value = value; this.copies = copies; }
        public FortressCardEntry(string title, FortressCardEffect effect, int value, int cost, FortressCardRarity rarity, string text,
            int count = 0, bool exhaust = false)
        {
            this.title = title; this.effect = effect; this.value = value; this.cost = cost; this.rarity = rarity; this.text = text;
            this.count = count; this.exhaust = exhaust;
        }

        public string Description => !string.IsNullOrEmpty(text) ? text : effect switch
        {
            FortressCardEffect.ExtraShot => $"다음 공격 +{value}발 연사",
            FortressCardEffect.Defense => $"방어도 +{value}",
            FortressCardEffect.ShotDamage => $"다음 공격 피해 +{value}",
            FortressCardEffect.Draw => $"카드를 {value}장 뽑습니다.",
            FortressCardEffect.BattleDamage => $"이번 전투 동안 기본 피해 +{value}",
            FortressCardEffect.CriticalChance => $"다음 공격 치명타 +{value}%",
            FortressCardEffect.Bleed => $"다음 화살에 출혈 {value}",
            FortressCardEffect.BleedArrows => $"다음 {count}개의 화살에 출혈 {value}",
            FortressCardEffect.ArrowPolish => $"다음 {count}개의 화살 피해 +{value}",
            FortressCardEffect.Retreat => $"방어도 {value}, 이번 턴 공격 전이면 +{value}",
            FortressCardEffect.CostDiscount => $"다음 카드 코스트 -{value}",
            FortressCardEffect.AddArrows => $"이번 턴 화살 +{value}",
            FortressCardEffect.MaxArrows => $"최대 화살 +{value}",
            FortressCardEffect.Recover => $"떨어진 화살 {value}발 회수",
            _ => effect.ToString()
        };
        // Attack buffs are banked until the next shot; the others can still be played after attacking.
        public bool IsAttackBuff => !(effect == FortressCardEffect.Defense || effect == FortressCardEffect.Draw
            || effect == FortressCardEffect.BattleDamage || effect == FortressCardEffect.Retreat || effect == FortressCardEffect.CostDiscount
            || effect == FortressCardEffect.BleedAccelerate || effect == FortressCardEffect.BleedQuiver || effect == FortressCardEffect.ChainBleed
            || effect == FortressCardEffect.MaxArrows || effect == FortressCardEffect.Bundle || effect == FortressCardEffect.Recover
            || effect == FortressCardEffect.RecoverMaxUp);
    }
}
