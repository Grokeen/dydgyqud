using System.Collections.Generic;
using UnityEngine;

namespace MiniFortress
{
    // A run is a chain of stages with one deck. Winning a stage offers reward cards; the chosen card joins the
    // deck for every later stage. There is one map for now, so each stage replays it with full health.
    public sealed partial class FortressGame
    {
        const int RewardChoices = 3;
        readonly List<FortressCardEntry> runDeck = new List<FortressCardEntry>();
        readonly List<FortressCardEntry> rewards = new List<FortressCardEntry>();
        int stage = 1;
        bool choosingReward;
        bool[] present = new bool[0];

        // Early stages field only the weakest few enemies; the rest sit out with no health, so turns,
        // hits and the win check skip them and their bodies stay hidden.
        void ApplyStageRoster()
        {
            present = new bool[fighters.Count];
            if (present.Length == 0) return;
            present[0] = true;
            var enemies = new List<int>();
            for (int i = 1; i < fighters.Count; i++) enemies.Add(i);
            int count = stage <= arena.rules.earlyStages ? Mathf.Min(arena.rules.earlyStageEnemies, enemies.Count) : enemies.Count;
            // Stable: equal health keeps hierarchy order.
            enemies.Sort((a, b) => fighters[a].maxHp != fighters[b].maxHp ? fighters[a].maxHp.CompareTo(fighters[b].maxHp) : a.CompareTo(b));
            for (int k = 0; k < enemies.Count; k++)
            {
                int index = enemies[k];
                present[index] = k < count;
                if (!present[index]) { fighters[index].hp = 0; fighters[index].deathRemaining = 0; }
            }
        }
        int EnemiesPresent() { int count = 0; for (int i = 1; i < present.Length; i++) if (present[i]) count++; return count; }

        // Fresh run from the chosen class: base deck, stage 1.
        void StartRun()
        {
            runDeck.Clear(); rewards.Clear(); choosingReward = false; stage = 1;
            foreach (var card in fighters[0].definition.Deck)
                if (card != null) for (int i = 0; i < card.copies; i++) runDeck.Add(card);
        }

        void OfferRewards()
        {
            rewards.Clear();
            var pool = new List<FortressCardEntry>(FortressCardEntry.RewardPool(fighters[0].definition.weapon));
            pool.RemoveAll(card => card.rarity == FortressCardRarity.Legend);
            while (rewards.Count < RewardChoices && pool.Count > 0)
            {
                var rarity = RollRarity(pool, arena.rules);
                var candidates = pool.FindAll(card => card.rarity == rarity);
                var pick = candidates[Random.Range(0, candidates.Count)];
                rewards.Add(pick); pool.Remove(pick);
            }
            choosingReward = rewards.Count > 0;
            var text = new System.Text.StringBuilder($"스테이지 {stage} 클리어! 카드 선택");
            for (int i = 0; i < rewards.Count; i++) text.Append($" [{i + 1}] {rewards[i].title}");
            text.Append($" · [{rewards.Count + 1}] 건너뛰기");
            message = text.ToString();
        }

        // Each choice rolls its rarity from the rules (default Common 65 / Rare 25 / Hero 10). If the pool has
        // no card of that rarity left, it falls back to the next lower rarity that it does have.
        static FortressCardRarity RollRarity(List<FortressCardEntry> pool, FortressBattleRules rules)
        {
            float total = rules.commonChance + rules.rareChance + rules.heroChance;
            float roll = Random.value * (total > 0 ? total : 1);
            var wanted = roll < rules.commonChance ? FortressCardRarity.Common
                : roll < rules.commonChance + rules.rareChance ? FortressCardRarity.Rare : FortressCardRarity.Hero;
            for (var rarity = wanted; rarity >= FortressCardRarity.Common; rarity--)
                if (pool.Exists(card => card.rarity == rarity)) return rarity;
            return pool[0].rarity;
        }

        void ChooseReward(int index)
        {
            if (!choosingReward || index < 0 || index >= rewards.Count) return;
            var card = rewards[index];
            runDeck.Add(card);
            NextStage($"{card.title} 카드가 덱에 추가되었습니다.");
        }

        void SkipReward() { if (choosingReward) NextStage("보상을 건너뛰었습니다."); }

        void NextStage(string note)
        {
            choosingReward = false; rewards.Clear(); stage++;
            Restart();
            message = $"스테이지 {stage} · {note} (덱 {runDeck.Count}장)";
        }

        void UpdateRewardInput()
        {
            int key = input.CardHotkey;
            if (key < 0) return;
            if (key < rewards.Count) ChooseReward(key);
            else if (key == rewards.Count) SkipReward();
        }
    }
}
