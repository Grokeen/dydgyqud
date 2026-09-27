using UnityEngine;

namespace MiniFortress
{
    public enum FortressCardEffect
    {
        ExtraShot, Defense, ShotDamage, CriticalChance, Draw, BattleDamage,
        Arrow, QuickLoad, DrawDiscard, BleedShot, Stake, PickArrow, Rupture, ArmorBreak,
        SplitShot, ArrowTrap, EvasiveManeuver, BleedVolley, ExtractPain, Rain, RecoverArrows,
        Quiver, Outpost, TacticalRearrangement
    }
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
        [Tooltip("70장 설계 문서의 카드 식별자")]
        public string id;
        public FortressCardRarity rarity;
        [TextArea] public string text;

        public FortressCardEntry() { }
        public FortressCardEntry(string title, FortressCardEffect effect, int value, int copies)
        { this.title = title; this.effect = effect; this.value = value; this.copies = copies; }
        public FortressCardEntry(string id, string title, FortressCardEffect effect, int value, int cost)
        { this.id = id; this.title = title; this.effect = effect; this.value = value; this.cost = cost; copies = 1; }
        public FortressCardEntry(string title, FortressCardEffect effect, int value, int cost, FortressCardRarity rarity, string text)
        { this.title = title; this.effect = effect; this.value = value; this.cost = cost; this.rarity = rarity; this.text = text; }

        public string Description => !string.IsNullOrEmpty(text) ? text : effect switch
        {
            FortressCardEffect.ExtraShot => $"다음 공격 +{value}발 연사",
            FortressCardEffect.Defense => $"방어도 +{value}",
            FortressCardEffect.ShotDamage => $"다음 공격 피해 +{value}",
            FortressCardEffect.CriticalChance => $"다음 공격 치명타 +{value}%",
            FortressCardEffect.Arrow => $"화살 {value}발을 장전합니다.",
            FortressCardEffect.QuickLoad => $"화살 {value}발을 장전하고 카드 1장을 뽑습니다.",
            FortressCardEffect.DrawDiscard => "카드 2장을 뽑은 뒤 손패 1장을 버립니다.",
            FortressCardEffect.BleedShot => $"다음 본사격 첫 명중에 출혈 {value}을 부여합니다.",
            FortressCardEffect.Stake => "화살 1발을 가까운 지형에 설치합니다.",
            FortressCardEffect.PickArrow => "가까운 지형 화살을 최대 2발 회수합니다.",
            FortressCardEffect.Rupture => "다음 첫 명중 시 출혈을 제거하고 출혈당 피해 8을 줍니다.",
            FortressCardEffect.ArmorBreak => $"다음 첫 명중 전에 방어도 최대 {value}를 제거합니다.",
            FortressCardEffect.SplitShot => "다음 사격을 두 각도의 묶음으로 나눕니다.",
            FortressCardEffect.ArrowTrap => "가까운 지형 화살을 소모해 적을 기다리는 덫을 놓습니다.",
            FortressCardEffect.EvasiveManeuver => "사격 직후 최대 2U 회피 이동을 준비합니다.",
            FortressCardEffect.BleedVolley => "다음 화살 3발의 첫 명중마다 출혈 1을 부여합니다.",
            FortressCardEffect.ExtractPain => "대상에게 박힌 화살을 최대 3발 뽑아 출혈로 바꿉니다.",
            FortressCardEffect.Rain => "다음 화살 3발마다 좌우 파생 화살을 만듭니다.",
            FortressCardEffect.RecoverArrows => "지난 내 턴에 발사해 남은 화살을 최대 5발 회수합니다.",
            FortressCardEffect.Quiver => "이번 전투의 최대 화살 수를 13발로 늘립니다.",
            FortressCardEffect.Outpost => "화살을 소모해 다음 사격의 위치를 지정합니다.",
            _ => "버린 더미의 사격 카드 1장을 손으로 가져옵니다."
        };
        // Only effects that must be declared before the next shot are locked after the main action.
        public bool IsAttackBuff => effect == FortressCardEffect.ExtraShot || effect == FortressCardEffect.ShotDamage ||
            effect == FortressCardEffect.CriticalChance || effect == FortressCardEffect.BleedShot ||
            effect == FortressCardEffect.Rupture || effect == FortressCardEffect.ArmorBreak ||
            effect == FortressCardEffect.SplitShot || effect == FortressCardEffect.EvasiveManeuver ||
            effect == FortressCardEffect.BleedVolley || effect == FortressCardEffect.Rain ||
            effect == FortressCardEffect.Outpost;

        public static FortressCardEntry[] DefaultDeck(FortressWeapon weapon) => weapon == FortressWeapon.Spear
            ? new[] {
                new FortressCardEntry("창", FortressCardEffect.ShotDamage, 15, 4),
                new FortressCardEntry("방어", FortressCardEffect.Defense, 10, 4),
                new FortressCardEntry("창 크리티컬", FortressCardEffect.CriticalChance, 10, 2) }
            : new[] {
                new FortressCardEntry("활", FortressCardEffect.ExtraShot, 1, 4),
                new FortressCardEntry("방어", FortressCardEffect.Defense, 10, 4),
                new FortressCardEntry("강화사격", FortressCardEffect.ShotDamage, 8, 1) };

        // 코덱스code(CodexCode): 설계 문서가 첫 실험 묶음으로 지정한 20장을 편집·테스트 가능한 카드 풀로 제공합니다.
        // CodexCode: keep the pulled stage reward system while retaining the archer effects defined locally.
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

        public static FortressCardEntry[] ArcherPrototypePool() => new[]
        {
            new FortressCardEntry("Double Shot", FortressCardEffect.ExtraShot, 1, 4),
                new FortressCardEntry("Guard", FortressCardEffect.Defense, 10, 4),
                new FortressCardEntry("Strengthened Shot", FortressCardEffect.ShotDamage, 8, 1) };

        public Sprite Artwork
        {
            get
            {
                if (effect == FortressCardEffect.BleedShot || effect == FortressCardEffect.Rupture ||
                    effect == FortressCardEffect.BleedVolley || effect == FortressCardEffect.ExtractPain)
                    return Resources.Load<Sprite>("FortressCardArt/ArcherPrecision");
                if (effect == FortressCardEffect.Stake || effect == FortressCardEffect.PickArrow ||
                    effect == FortressCardEffect.ArrowTrap || effect == FortressCardEffect.Outpost)
                    return Resources.Load<Sprite>("FortressCardArt/ArcherFieldcraft");
                if (effect == FortressCardEffect.Arrow || effect == FortressCardEffect.QuickLoad ||
                    effect == FortressCardEffect.RecoverArrows || effect == FortressCardEffect.Quiver ||
                    effect == FortressCardEffect.TacticalRearrangement || effect == FortressCardEffect.SplitShot ||
                    effect == FortressCardEffect.Rain || effect == FortressCardEffect.ArmorBreak ||
                    effect == FortressCardEffect.EvasiveManeuver || effect == FortressCardEffect.ShotDamage)
                    return Resources.Load<Sprite>("FortressCardArt/ArcherVolley");
                return Resources.Load<Sprite>("FortressCardArt/ArcherFieldcraft");
            }
        }
    }
}
