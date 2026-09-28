namespace MiniFortress
{
    // Spearman cards. They share names with some archer cards (방어) but are separate cards with their own art.
    // Seeds for the editable card set asset (Assets/FortressContent/Cards/Spearman); edit that asset, not this file.
    // It is also the runtime fallback when the character has no card set. Card art: Resources/FortressCardArt/Spearman.
    public static class FortressSpearmanCardSeeds
    {
        public const string ArtFolder = "Spearman";

        public static FortressCardEntry[] StarterDeck => new[]
        {
            new FortressCardEntry("창", FortressCardEffect.ShotDamage, 15, 4),
            new FortressCardEntry("방어", FortressCardEffect.Defense, 10, 4),
            new FortressCardEntry("창 크리티컬", FortressCardEffect.CriticalChance, 10, 2),
        };

        public static FortressCardEntry[] RewardPool => new[]
        {
            new FortressCardEntry("창", FortressCardEffect.ShotDamage, 15, 1, FortressCardRarity.Common, null),
            new FortressCardEntry("방어", FortressCardEffect.Defense, 10, 1, FortressCardRarity.Common, null),
            new FortressCardEntry("창 크리티컬", FortressCardEffect.CriticalChance, 10, 1, FortressCardRarity.Rare, null),
        };
    }
}
