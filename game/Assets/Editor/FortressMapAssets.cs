using System.IO;
using MiniFortress;
using UnityEditor;
using UnityEngine;

// Writes the built-in maps as editable FortressMapDefinition assets in Assets/Resources/FortressMaps.
// Runs once automatically when that folder has no map asset; existing map assets are never touched.
[InitializeOnLoad]
public static class FortressMapAssets
{
    public const string Folder = "Assets/Resources/FortressMaps";

    static FortressMapAssets() => EditorApplication.delayCall += EnsureAssets;

    public static void EnsureAssets()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling) { EditorApplication.delayCall += EnsureAssets; return; }
        if (AssetDatabase.IsValidFolder(Folder) && AssetDatabase.FindAssets("t:FortressMapDefinition", new[] { Folder }).Length > 0) return;
        Write(false);
    }

    [MenuItem("Mini Fortress/Restore Built-in Map Assets")]
    public static void Restore()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Stop Play Mode before restoring map assets."); return; }
        if (!EditorUtility.DisplayDialog("기본 맵 복원", "기본 맵 4개를 다시 만듭니다. 같은 이름의 맵 에셋은 기본값으로 덮어씁니다.", "복원", "취소")) return;
        Write(true);
    }

    static void Write(bool overwrite)
    {
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        var goblin = AssetDatabase.LoadAssetAtPath<FortressCharacterDefinition>(FortressSceneBuilder.Content + "/Data/Goblin.asset");
        var captain = AssetDatabase.LoadAssetAtPath<FortressCharacterDefinition>(FortressSceneBuilder.Content + "/Data/Captain.asset");
        foreach (var seed in FortressBuiltInMaps.Seeds)
        {
            string path = $"{Folder}/{seed.order + 1:00} {seed.name}.asset";
            if (!overwrite && File.Exists(path)) continue;
            var map = FortressBuiltInMaps.Create(seed, LoadBackground);
            // Same line-up as the battle scene's spawns: the second enemy is the captain.
            for (int i = 0; i < map.enemies.Count; i++) map.enemies[i].character = i == 1 && captain ? captain : goblin;
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(map, path);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Mini Fortress: 기본 맵 에셋을 " + Folder + "에 만들었습니다.");
    }

    // Built-in backgrounds are Resources paths; the original citadel painting is imported as a plain texture,
    // so its baked sprite copy is used instead.
    static Sprite LoadBackground(string resourcePath)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/" + resourcePath + ".png");
        if (sprite) return sprite;
        if (resourcePath == "FortressArt/CitadelArena")
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FortressSceneBuilder.Content + "/Art/Citadel.png");
        if (!sprite) Debug.LogWarning("Mini Fortress: 맵 배경 스프라이트를 찾지 못했습니다: " + resourcePath);
        return sprite;
    }
}
