using MiniFortress;
using UnityEditor;
using UnityEngine;

// Creates each class's editable card set (Assets/FortressContent/Cards/<Class>/<Class> Cards.asset) from the seed
// files, its art folder (Assets/Resources/FortressCardArt/<Class>), and links the set to the class's character data.
// Runs automatically for anything missing; existing card sets and links are never overwritten.
[InitializeOnLoad]
public static class FortressCardAssets
{
    public const string Folder = FortressSceneBuilder.Content + "/Cards";
    const string ArtRoot = "Assets/Resources/FortressCardArt";

    struct ClassCards
    {
        public string name, artFolder, character;
        public FortressCardEntry[] starter, rewards;
    }

    static ClassCards[] Classes => new[]
    {
        new ClassCards { name = "Archer", artFolder = FortressArcherCardSeeds.ArtFolder, character = "Archer",
            starter = FortressArcherCardSeeds.StarterDeck, rewards = FortressArcherCardSeeds.RewardPool },
        new ClassCards { name = "Spearman", artFolder = FortressSpearmanCardSeeds.ArtFolder, character = "Spearman",
            starter = FortressSpearmanCardSeeds.StarterDeck, rewards = FortressSpearmanCardSeeds.RewardPool },
    };

    static FortressCardAssets() => EditorApplication.delayCall += EnsureAssets;

    public static void EnsureAssets()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling) { EditorApplication.delayCall += EnsureAssets; return; }
        Write(false);
    }

    [MenuItem("Mini Fortress/Restore Built-in Card Sets")]
    public static void Restore()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Stop Play Mode before restoring card sets."); return; }
        if (!EditorUtility.DisplayDialog("기본 카드 복원", "궁수·창병 카드 세트를 기본값으로 덮어씁니다. 카드에 넣은 그림 연결도 초기화됩니다.", "복원", "취소")) return;
        Write(true);
    }

    static void Write(bool overwrite)
    {
        bool changed = false;
        foreach (var cards in Classes)
        {
            changed |= EnsureFolder(Folder + "/" + cards.name);
            changed |= EnsureFolder(ArtRoot + "/" + cards.artFolder);
            string path = $"{Folder}/{cards.name}/{cards.name} Cards.asset";
            var set = AssetDatabase.LoadAssetAtPath<FortressCardSet>(path);
            if (!set || overwrite)
            {
                if (set) AssetDatabase.DeleteAsset(path);
                set = FortressCardSet.Create(cards.name, cards.artFolder, cards.starter, cards.rewards);
                AssetDatabase.CreateAsset(set, path);
                changed = true;
            }
            var character = AssetDatabase.LoadAssetAtPath<FortressCharacterDefinition>($"{FortressSceneBuilder.Content}/Data/{cards.character}.asset");
            if (character && (!character.cardSet || overwrite))
            {
                var serialized = new SerializedObject(character);
                serialized.FindProperty("cardSet").objectReferenceValue = set;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }
        }
        if (!changed) return;
        AssetDatabase.SaveAssets();
        Debug.Log("Mini Fortress: 직업별 카드 세트를 " + Folder + "에 준비했습니다.");
    }

    static bool EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return false;
        int slash = path.LastIndexOf('/');
        EnsureFolder(path.Substring(0, slash));
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        return true;
    }
}
