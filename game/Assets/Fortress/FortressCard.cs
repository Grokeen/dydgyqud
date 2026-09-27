using UnityEngine;

namespace MiniFortress
{
    public enum FortressCardEffect
    {
        ExtraShot, Defense, ShotDamage, CriticalChance,
        Arrow, QuickLoad, DrawDiscard, BleedShot, Stake, PickArrow, Rupture, ArmorBreak,
        SplitShot, ArrowTrap, EvasiveManeuver, BleedVolley, ExtractPain, Rain, RecoverArrows,
        Quiver, Outpost, TacticalRearrangement
    }

    [System.Serializable]
    public sealed class FortressCardEntry
    {
        public string title;
        public FortressCardEffect effect;
        [Tooltip("추가 발사 수 / 방어도 / 추가 피해 / 치명타 확률(%)")]
        [Min(0)] public int value = 1;
        [Min(1)] public int copies = 1;
        [Min(0)] public int cost = 1;
        [Tooltip("70장 설계 문서의 카드 식별자")]
        public string id;

        public FortressCardEntry() { }
        public FortressCardEntry(string title, FortressCardEffect effect, int value, int copies)
        { this.title = title; this.effect = effect; this.value = value; this.copies = copies; }
        public FortressCardEntry(string id, string title, FortressCardEffect effect, int value, int cost)
        { this.id = id; this.title = title; this.effect = effect; this.value = value; this.cost = cost; copies = 1; }

        public string Description => effect switch
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
                new FortressCardEntry("ARC-001", "화살", FortressCardEffect.Arrow, 2, 1) { copies = 4 },
                new FortressCardEntry("ARC-002", "방어", FortressCardEffect.Defense, 10, 1) { copies = 4 },
                new FortressCardEntry("ARC-003", "강화 사격", FortressCardEffect.ShotDamage, 10, 1) };

        // 코덱스code(CodexCode): 설계 문서가 첫 실험 묶음으로 지정한 20장을 편집·테스트 가능한 카드 풀로 제공합니다.
        public static FortressCardEntry[] ArcherPrototypePool() => new[]
        {
            new FortressCardEntry("ARC-001", "화살", FortressCardEffect.Arrow, 2, 1),
            new FortressCardEntry("ARC-002", "방어", FortressCardEffect.Defense, 10, 1),
            new FortressCardEntry("ARC-003", "강화 사격", FortressCardEffect.ShotDamage, 10, 1),
            new FortressCardEntry("ARC-004", "빠른 장전", FortressCardEffect.QuickLoad, 1, 1),
            new FortressCardEntry("ARC-005", "짐정리", FortressCardEffect.DrawDiscard, 0, 1),
            new FortressCardEntry("ARC-006", "출혈 화살", FortressCardEffect.BleedShot, 2, 1),
            new FortressCardEntry("ARC-014", "말뚝 박기", FortressCardEffect.Stake, 0, 1),
            new FortressCardEntry("ARC-015", "화살 줍기", FortressCardEffect.PickArrow, 2, 0),
            new FortressCardEntry("ARC-026", "파열", FortressCardEffect.Rupture, 8, 2),
            new FortressCardEntry("ARC-028", "철갑 파괴", FortressCardEffect.ArmorBreak, 15, 1),
            new FortressCardEntry("ARC-029", "나눠 쏘기", FortressCardEffect.SplitShot, 0, 1),
            new FortressCardEntry("ARC-030", "화살 덫", FortressCardEffect.ArrowTrap, 0, 1),
            new FortressCardEntry("ARC-031", "회피 기동", FortressCardEffect.EvasiveManeuver, 2, 1),
            new FortressCardEntry("ARC-039", "상처 덧긋기", FortressCardEffect.BleedVolley, 3, 1),
            new FortressCardEntry("ARC-046", "뽑아낸 고통", FortressCardEffect.ExtractPain, 3, 1),
            new FortressCardEntry("ARC-047", "폭우", FortressCardEffect.Rain, 3, 2),
            new FortressCardEntry("ARC-048", "화살 회수", FortressCardEffect.RecoverArrows, 5, 2),
            new FortressCardEntry("ARC-049", "화살통", FortressCardEffect.Quiver, 13, 1),
            new FortressCardEntry("ARC-053", "사격 거점", FortressCardEffect.Outpost, 0, 2),
            new FortressCardEntry("ARC-056", "전술 재편", FortressCardEffect.TacticalRearrangement, 0, 1)
        };

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
