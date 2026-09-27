using UnityEngine;

namespace MiniFortress
{
    public enum FortressCardEffect { ExtraShot, Defense, ShotDamage, CriticalChance, Draw, BattleDamage }
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
        public FortressCardRarity rarity;
        [Tooltip("비워 두면 효과에서 설명을 만듭니다.")]
        [TextArea] public string text;

        public FortressCardEntry() { }
        public FortressCardEntry(string title, FortressCardEffect effect, int value, int copies)
        { this.title = title; this.effect = effect; this.value = value; this.copies = copies; }
        public FortressCardEntry(string title, FortressCardEffect effect, int value, int cost, FortressCardRarity rarity, string text)
        { this.title = title; this.effect = effect; this.value = value; this.cost = cost; this.rarity = rarity; this.text = text; }

        public string Description => !string.IsNullOrEmpty(text) ? text : effect switch
        {
            FortressCardEffect.ExtraShot => $"다음 공격 +{value}발 연사",
            FortressCardEffect.Defense => $"방어도 +{value}",
            FortressCardEffect.ShotDamage => $"다음 공격 피해 +{value}",
            FortressCardEffect.Draw => $"카드를 {value}장 뽑습니다.",
            FortressCardEffect.BattleDamage => $"이번 전투 동안 기본 피해 +{value}",
            _ => $"다음 공격 치명타 +{value}%"
        };
        // Attack buffs are banked until the next shot; the others can still be played after attacking.
        public bool IsAttackBuff => effect == FortressCardEffect.ExtraShot || effect == FortressCardEffect.ShotDamage
            || effect == FortressCardEffect.CriticalChance;

        public static FortressCardEntry[] DefaultDeck(FortressWeapon weapon) => weapon == FortressWeapon.Spear
            ? new[] {
                new FortressCardEntry("창", FortressCardEffect.ShotDamage, 15, 4),
                new FortressCardEntry("방어", FortressCardEffect.Defense, 10, 4),
                new FortressCardEntry("창 크리티컬", FortressCardEffect.CriticalChance, 10, 2) }
            : new[] {
                new FortressCardEntry("활", FortressCardEffect.ExtraShot, 1, 4),
                new FortressCardEntry("방어", FortressCardEffect.Defense, 10, 4),
                new FortressCardEntry("강화사격", FortressCardEffect.ShotDamage, 8, 1) };

        // Cards a stage reward can offer. Only archer cards whose effects exist in combat are listed; the rest
        // of the archer design (bleed, arrow ammo, recovery ...) joins as its effects are implemented.
        public static FortressCardEntry[] RewardPool(FortressWeapon weapon) => weapon == FortressWeapon.Spear
            ? new[] {
                new FortressCardEntry("창", FortressCardEffect.ShotDamage, 15, 1, FortressCardRarity.Common, null),
                new FortressCardEntry("방어", FortressCardEffect.Defense, 10, 1, FortressCardRarity.Common, null),
                new FortressCardEntry("창 크리티컬", FortressCardEffect.CriticalChance, 10, 1, FortressCardRarity.Rare, null) }
            : new[] {
                new FortressCardEntry("방어", FortressCardEffect.Defense, 10, 1, FortressCardRarity.Common, "방어도 10을 얻습니다."),
                new FortressCardEntry("강화 사격", FortressCardEffect.BattleDamage, 10, 1, FortressCardRarity.Common, "이번 전투 동안 활의 기본 데미지가 10 증가합니다."),
                new FortressCardEntry("짐정리", FortressCardEffect.Draw, 1, 1, FortressCardRarity.Common, "카드를 1장 뽑습니다."),
                new FortressCardEntry("조준", FortressCardEffect.ShotDamage, 10, 1, FortressCardRarity.Common, "다음 화살의 데미지가 10 증가합니다."),
                new FortressCardEntry("집중", FortressCardEffect.ShotDamage, 20, 1, FortressCardRarity.Common, "다음 화살의 데미지가 20 증가합니다."),
                new FortressCardEntry("연속 사격", FortressCardEffect.ExtraShot, 1, 1, FortressCardRarity.Rare, "다음 화살이 2발 발사됩니다.") };
    }
}
