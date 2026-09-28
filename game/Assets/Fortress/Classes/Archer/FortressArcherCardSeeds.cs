namespace MiniFortress
{
    // Archer cards. Stage rewards list only cards whose effects exist in combat; the rest of the archer design
    // (sky arrows, piercing, tracking, discarding) joins as it is implemented.
    // Seeds for the editable card set asset (Assets/FortressContent/Cards/Archer); edit that asset, not this file.
    // It is also the runtime fallback when the character has no card set. Card art: Resources/FortressCardArt/Archer.
    public static class FortressArcherCardSeeds
    {
        public const string ArtFolder = "Archer";

        public static FortressCardEntry[] StarterDeck => new[]
        {
            new FortressCardEntry("활", FortressCardEffect.ExtraShot, 1, 4),
            new FortressCardEntry("방어", FortressCardEffect.Defense, 10, 4),
            new FortressCardEntry("강화사격", FortressCardEffect.ShotDamage, 8, 1),
        };

        public static FortressCardEntry[] RewardPool => new[]
        {
            new FortressCardEntry("방어", FortressCardEffect.Defense, 10, 1, FortressCardRarity.Common, "방어도 10을 얻습니다."),
            new FortressCardEntry("강화 사격", FortressCardEffect.BattleDamage, 10, 1, FortressCardRarity.Common, "이번 전투 동안 활의 기본 데미지가 10 증가합니다."),
            new FortressCardEntry("짐정리", FortressCardEffect.Draw, 1, 1, FortressCardRarity.Common, "카드를 1장 뽑습니다."),
            new FortressCardEntry("조준", FortressCardEffect.ShotDamage, 10, 1, FortressCardRarity.Common, "다음 화살의 데미지가 10 증가합니다."),
            new FortressCardEntry("집중", FortressCardEffect.ShotDamage, 20, 1, FortressCardRarity.Common, "다음 화살의 데미지가 20 증가합니다."),
            new FortressCardEntry("출혈", FortressCardEffect.Bleed, 2, 1, FortressCardRarity.Common, "다음 화살에 출혈 2를 부여합니다."),
            new FortressCardEntry("피 묻은 화살", FortressCardEffect.Bleed, 3, 1, FortressCardRarity.Common, "다음 화살에 출혈 3을 부여합니다."),
            new FortressCardEntry("견제 사격", FortressCardEffect.Weaken, 5, 1, FortressCardRarity.Common, "다음 화살이 적중하면 적의 다음 공격 데미지가 5 감소합니다."),
            new FortressCardEntry("뒤로 물러서기", FortressCardEffect.Retreat, 6, 1, FortressCardRarity.Common, "방어도 6을 얻습니다. 이번 턴 화살을 발사하지 않았다면 방어도 6을 추가로 얻습니다."),
            new FortressCardEntry("깊은 호흡", FortressCardEffect.CostDiscount, 1, 0, FortressCardRarity.Common, "다음 카드의 코스트가 1 감소합니다."),
            new FortressCardEntry("화살 손질", FortressCardEffect.ArrowPolish, 3, 1, FortressCardRarity.Common, "다음 3개의 화살의 데미지가 3 증가합니다.", 3),
            new FortressCardEntry("상처 확인", FortressCardEffect.DrawIfBleeding, 1, 1, FortressCardRarity.Common, "출혈이 5 이상인 적에게 다음 화살이 적중하면 카드를 1장 뽑습니다."),
            new FortressCardEntry("마무리 사격", FortressCardEffect.Execute, 15, 1, FortressCardRarity.Common, "적의 체력이 30% 이하라면 다음 화살의 데미지가 15 증가합니다."),
            new FortressCardEntry("화살", FortressCardEffect.AddArrows, 4, 1, FortressCardRarity.Common, "화살 4발을 추가합니다."),
            new FortressCardEntry("빠른 사격", FortressCardEffect.AddArrows, 1, 1, FortressCardRarity.Common, "화살 1발을 추가합니다. 카드를 1장 뽑습니다.", 1),
            new FortressCardEntry("발 빠진 화살", FortressCardEffect.SloppyArrow, 1, 0, FortressCardRarity.Common, "화살 1발을 추가합니다. 이번 턴 다음 화살의 데미지가 3 감소합니다.", 3),
            new FortressCardEntry("화살 줍기", FortressCardEffect.Recover, 1, 0, FortressCardRarity.Common, "전장에 떨어진 화살 1발을 회수합니다."),

            new FortressCardEntry("연속 사격", FortressCardEffect.ExtraShot, 1, 1, FortressCardRarity.Rare, "다음 화살이 2발 발사됩니다."),
            new FortressCardEntry("출혈 화살", FortressCardEffect.BleedArrows, 3, 1, FortressCardRarity.Rare, "다음 2개의 화살에 출혈 3을 부여합니다.", 2),
            new FortressCardEntry("꿰뚫는 화살", FortressCardEffect.DamagePerBleed, 4, 1, FortressCardRarity.Rare, "다음 화살의 데미지가 출혈 1개당 4 증가합니다."),
            new FortressCardEntry("상처 벌리기", FortressCardEffect.Bleed, 5, 1, FortressCardRarity.Rare, "다음 화살이 적중하면 대상의 출혈이 5 증가합니다."),
            new FortressCardEntry("피 냄새", FortressCardEffect.BonusVsBleeding, 30, 1, FortressCardRarity.Rare, "다음 화살이 출혈이 있는 적에게 데미지가 30 증가합니다."),
            new FortressCardEntry("연속 조준", FortressCardEffect.HitStreak, 3, 1, FortressCardRarity.Rare, "이번 턴 화살이 적중할 때마다 다음 화살의 데미지가 3 증가합니다."),
            new FortressCardEntry("피의 흔적", FortressCardEffect.Bleed, 5, 1, FortressCardRarity.Rare, "다음 화살이 적중하면 대상의 출혈이 5 증가합니다."),
            new FortressCardEntry("출혈 촉진", FortressCardEffect.BleedAccelerate, 8, 1, FortressCardRarity.Rare, "다음턴에 출혈 폭발에 필요한 출혈이 8로 감소합니다."),
            new FortressCardEntry("피의 조준", FortressCardEffect.DamagePerBleed, 1, 1, FortressCardRarity.Rare, "다음 화살의 데미지가 대상의 출혈 1당 1 증가합니다."),
            new FortressCardEntry("잔혹한 화살", FortressCardEffect.BrutalShot, 30, 2, FortressCardRarity.Rare, "다음 화살의 데미지가 30 증가합니다. 대상의 출혈이 5 이상이면 출혈 폭발이 발생합니다."),
            new FortressCardEntry("화살 회수술", FortressCardEffect.Recover, 3, 2, FortressCardRarity.Rare, "전장에 떨어진 화살 3발을 회수합니다."),
            new FortressCardEntry("빗나간 한 발", FortressCardEffect.RecoverMisses, 1, 1, FortressCardRarity.Rare, "다음 화살이 빗나가면 해당 화살을 회수합니다."),
            new FortressCardEntry("화살 줍는 손", FortressCardEffect.Recover, 2, 0, FortressCardRarity.Rare, "전장에 떨어진 화살 2발을 회수합니다."),
            new FortressCardEntry("화살 재사용", FortressCardEffect.RecoverMaxUp, 1, 1, FortressCardRarity.Rare, "전장에 있는 화살 1발을 회수합니다. 최대 화살의 갯수가 1 증가합니다."),
            new FortressCardEntry("다발 제작", FortressCardEffect.Bundle, 3, 1, FortressCardRarity.Rare, "화살 3발을 추가합니다. 화살 최대치가 13발이 됩니다.", 13),
            new FortressCardEntry("되쏘기", FortressCardEffect.RecoverNow, 3, 1, FortressCardRarity.Rare, "화살 3발을 회수하여 쏩니다."),

            new FortressCardEntry("피의 폭발", FortressCardEffect.BleedDetonate, 2, 2, FortressCardRarity.Hero, "다음 화살이 대상의 출혈을 모두 제거하고 제거한 출혈 ×2의 피해를 줍니다."),
            new FortressCardEntry("사냥의 시작", FortressCardEffect.MarkPrey, 2, 1, FortressCardRarity.Hero, "다음 화살에 맞은 적을 사냥감으로 지정합니다. 사냥감에게 화살이 적중할 때마다 출혈 2를 부여합니다."),
            new FortressCardEntry("상처 확대", FortressCardEffect.DoubleBleed, 2, 2, FortressCardRarity.Hero, "[소모] 다음 화살이 적중하면 대상의 출혈이 2배가 됩니다.", 0, true),
            new FortressCardEntry("피의 화살통", FortressCardEffect.BleedQuiver, 2, 2, FortressCardRarity.Hero, "이번 전투 동안 화살이 적중할 때마다 출혈 2를 부여합니다."),
            new FortressCardEntry("연쇄 출혈", FortressCardEffect.ChainBleed, 5, 2, FortressCardRarity.Hero, "다음 출혈 폭발이 발생하면 주변 적에게 출혈 5를 부여합니다."),
            new FortressCardEntry("피를 부르는 화살", FortressCardEffect.BloodCall, 1, 3, FortressCardRarity.Hero, "이번 턴 발사되는 화살이 적중할 때마다 다음 출혈 폭발에 필요한 출혈이 1 감소합니다."),
            new FortressCardEntry("화살 회수", FortressCardEffect.RecallLastTurn, 1, 3, FortressCardRarity.Hero, "다음 화살이 발사될 때 전 턴에 발사한 화살을 회수하여 추가로 발사합니다."),
            new FortressCardEntry("화살통", FortressCardEffect.MaxArrows, 3, 1, FortressCardRarity.Hero, "최대 화살 수가 3발 증가합니다."),
            new FortressCardEntry("전장 회수", FortressCardEffect.RecallAll, 1, 3, FortressCardRarity.Hero, "다음 화살이 발사될 때 지금까지 빗나간 화살을 회수하여 추가로 발사합니다."),
            new FortressCardEntry("화살 폭풍", FortressCardEffect.ArrowStorm, 3, 3, FortressCardRarity.Hero, "다음 화살이 발사될 때 현재 화살의 절반을 추가로 발사합니다. 추가 화살의 데미지가 3 증가합니다."),
            new FortressCardEntry("회수 전문가", FortressCardEffect.RecoveryExpert, 2, 2, FortressCardRarity.Hero, "이번 전투에서 회수한 화살 1발마다 다음 화살의 데미지가 2 증가합니다."),
            new FortressCardEntry("마지막 화살", FortressCardEffect.LastArrow, 5, 1, FortressCardRarity.Hero, "[소모] 화살이 1발 남아 있다면 다음 화살의 데미지가 5배 증가합니다.", 0, true),
            // "최대치 초과" = arrows beyond the base cap of 10, once max-arrow cards have raised the cap.
            new FortressCardEntry("넘치는 화살", FortressCardEffect.Overflow, 5, 3, FortressCardRarity.Hero, "[소모] 현재 화살이 최대치를 초과했다면 다음 화살의 데미지가 초과한 화살 1발당 5 증가합니다.", 0, true),
            new FortressCardEntry("화살 저장", FortressCardEffect.StoreArrows, 1, 1, FortressCardRarity.Hero, "최대치 이상 화살 보유한 수만큼 다음 화살에 추가"),
            new FortressCardEntry("화살 비축", FortressCardEffect.Stockpile, 5, 2, FortressCardRarity.Hero, "최대치 이상 추가된 화살만큼 데미지 +5 추가합니다."),
        };
    }
}
