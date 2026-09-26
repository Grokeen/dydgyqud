using UnityEngine;

namespace MiniFortress
{
    public enum FortressCardEffect { ExtraShot, Defense, ShotDamage, CriticalChance }

    [System.Serializable]
    public sealed class FortressCardEntry
    {
        public string title;
        public FortressCardEffect effect;
        [Tooltip("추가 발사 수 / 방어도 / 추가 피해 / 치명타 확률(%)")]
        [Min(0)] public int value = 1;
        [Min(1)] public int copies = 1;
        [Min(0)] public int cost = 1;

        public FortressCardEntry() { }
        public FortressCardEntry(string title, FortressCardEffect effect, int value, int copies)
        { this.title = title; this.effect = effect; this.value = value; this.copies = copies; }

        public string Description => effect switch
        {
            FortressCardEffect.ExtraShot => $"다음 공격 +{value}발 연사",
            FortressCardEffect.Defense => $"방어도 +{value}",
            FortressCardEffect.ShotDamage => $"다음 공격 피해 +{value}",
            _ => $"다음 공격 치명타 +{value}%"
        };
        // Attack buffs are banked until the next shot; defense can be played after attacking.
        public bool IsAttackBuff => effect != FortressCardEffect.Defense;

        public static FortressCardEntry[] DefaultDeck(FortressWeapon weapon) => weapon == FortressWeapon.Spear
            ? new[] {
                new FortressCardEntry("창", FortressCardEffect.ShotDamage, 15, 4),
                new FortressCardEntry("방어", FortressCardEffect.Defense, 10, 4),
                new FortressCardEntry("창 크리티컬", FortressCardEffect.CriticalChance, 10, 2) }
            : new[] {
                new FortressCardEntry("활", FortressCardEffect.ExtraShot, 1, 4),
                new FortressCardEntry("방어", FortressCardEffect.Defense, 10, 4),
                new FortressCardEntry("강화사격", FortressCardEffect.ShotDamage, 8, 1) };
    }
}
