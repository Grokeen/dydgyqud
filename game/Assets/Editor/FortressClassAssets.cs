using MiniFortress;
using UnityEditor;
using UnityEngine;

// Prepares each player class's folder, Assets/FortressContent/Classes/<Class>:
//  - "<Class> Class.asset": the class module's tuning (FortressArcherClass, FortressSpearmanClass ...);
//  - "<Class> Cards.asset": the class's card set, seeded from Assets/Fortress/Classes/<Class>/*CardSeeds.cs;
// plus its card art folder, Assets/Resources/FortressCardArt/<Class>, and links both assets to the class's
// character data. Runs automatically for anything missing; existing assets and links are never overwritten.
[InitializeOnLoad]
public static class FortressClassAssets
{
    public const string Folder = FortressSceneBuilder.Content + "/Classes";
    const string ArtRoot = "Assets/Resources/FortressCardArt";

    struct PlayerClass
    {
        public string name, artFolder, character;
        public System.Func<FortressClassBehaviour> createClass;
        public FortressCardEntry[] starter, rewards;
    }

    static PlayerClass[] Classes => new[]
    {
        new PlayerClass { name = "Archer", artFolder = FortressArcherCardSeeds.ArtFolder, character = "Archer",
            createClass = ScriptableObject.CreateInstance<FortressArcherClass>,
            starter = FortressArcherCardSeeds.StarterDeck, rewards = FortressArcherCardSeeds.RewardPool },
        new PlayerClass { name = "Spearman", artFolder = FortressSpearmanCardSeeds.ArtFolder, character = "Spearman",
            createClass = ScriptableObject.CreateInstance<FortressSpearmanClass>,
            starter = FortressSpearmanCardSeeds.StarterDeck, rewards = FortressSpearmanCardSeeds.RewardPool },
    };

    static FortressClassAssets() => EditorApplication.delayCall += EnsureAssets;

    public static void EnsureAssets()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling) { EditorApplication.delayCall += EnsureAssets; return; }
        Write(false);
    }

    [MenuItem("Mini Fortress/Restore Built-in Classes and Card Sets")]
    public static void Restore()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Stop Play Mode before restoring classes."); return; }
        if (!EditorUtility.DisplayDialog("기본 직업 복원", "궁수·창병의 직업 수치와 카드 세트를 기본값으로 덮어씁니다. 카드에 넣은 그림 연결도 초기화됩니다.", "복원", "취소")) return;
        Write(true);
    }

    static void Write(bool overwrite)
    {
        bool changed = false;
        foreach (var playerClass in Classes)
        {
            string folder = Folder + "/" + playerClass.name;
            changed |= EnsureFolder(folder);
            changed |= EnsureFolder(ArtRoot + "/" + playerClass.artFolder);

            string classPath = $"{folder}/{playerClass.name} Class.asset";
            var behaviour = AssetDatabase.LoadAssetAtPath<FortressClassBehaviour>(classPath);
            if (!behaviour || overwrite)
            {
                if (behaviour) AssetDatabase.DeleteAsset(classPath);
                behaviour = playerClass.createClass();
                AssetDatabase.CreateAsset(behaviour, classPath);
                changed = true;
            }

            string cardsPath = $"{folder}/{playerClass.name} Cards.asset";
            var cards = AssetDatabase.LoadAssetAtPath<FortressCardSet>(cardsPath);
            if (!cards || overwrite)
            {
                if (cards) AssetDatabase.DeleteAsset(cardsPath);
                cards = FortressCardSet.Create(playerClass.name, playerClass.artFolder, playerClass.starter, playerClass.rewards);
                AssetDatabase.CreateAsset(cards, cardsPath);
                changed = true;
            }

            var character = AssetDatabase.LoadAssetAtPath<FortressCharacterDefinition>($"{FortressSceneBuilder.Content}/Data/{playerClass.character}.asset");
            if (!character) continue;
            var serialized = new SerializedObject(character);
            changed |= Link(serialized, "classBehaviour", behaviour, overwrite);
            changed |= Link(serialized, "cardSet", cards, overwrite);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        if (!changed) return;
        AssetDatabase.SaveAssets();
        Debug.Log("Mini Fortress: 직업 모듈과 카드 세트를 " + Folder + "에 준비했습니다.");
    }

    static bool Link(SerializedObject owner, string property, Object value, bool overwrite)
    {
        var field = owner.FindProperty(property);
        if (field.objectReferenceValue && !overwrite) return false;
        field.objectReferenceValue = value;
        return true;
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
