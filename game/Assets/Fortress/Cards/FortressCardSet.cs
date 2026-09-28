using System.Collections.Generic;
using UnityEngine;

namespace MiniFortress
{
    // One class's cards: its starting deck, the cards stage rewards can offer, and where its card art lives.
    // Every class owns its cards outright, so a card named like another class's card (e.g. 방어) is a separate
    // card with its own numbers and art. Assets live in Assets/FortressContent/Cards/<Class>; a character uses
    // the set in its Card Set field.
    [CreateAssetMenu(menuName = "Mini Fortress/Card Set", fileName = "Cards")]
    public sealed class FortressCardSet : ScriptableObject
    {
        public string className;
        [Tooltip("카드 그림 폴더: Assets/Resources/FortressCardArt/<이 이름>. 카드 이름(또는 효과 이름)과 같은 파일을 찾습니다.")]
        public string artFolder;
        [Tooltip("시작 덱. Copies만큼 덱에 들어갑니다.")]
        public List<FortressCardEntry> starterDeck = new List<FortressCardEntry>();
        [Tooltip("스테이지 보상으로 나올 수 있는 카드")]
        public List<FortressCardEntry> rewardPool = new List<FortressCardEntry>();

        public static FortressCardSet Create(string className, string artFolder, FortressCardEntry[] starterDeck, FortressCardEntry[] rewardPool)
        {
            var set = CreateInstance<FortressCardSet>();
            set.name = className + " Cards"; set.className = className; set.artFolder = artFolder;
            set.starterDeck.AddRange(starterDeck); set.rewardPool.AddRange(rewardPool);
            return set;
        }
    }

    // Card sets built from the class seed files, used when a character has no card set asset.
    public static class FortressBuiltInCards
    {
        static FortressCardSet archer, spearman;

        public static FortressCardSet For(FortressWeapon weapon)
        {
            if (weapon == FortressWeapon.Spear)
            {
                if (!spearman) spearman = FortressCardSet.Create("Spearman", FortressSpearmanCardSeeds.ArtFolder,
                    FortressSpearmanCardSeeds.StarterDeck, FortressSpearmanCardSeeds.RewardPool);
                return spearman;
            }
            if (!archer) archer = FortressCardSet.Create("Archer", FortressArcherCardSeeds.ArtFolder,
                FortressArcherCardSeeds.StarterDeck, FortressArcherCardSeeds.RewardPool);
            return archer;
        }
    }
}
