using System.Collections.Generic;
using System.IO;
using MiniFortress;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Turns the pieces cut by Tools/slice_citadel.py into reusable prefabs: each prefab carries its painted
// art and the FortressTerrain colliders standing on it, so a new map is built by placing prefabs.
public static class FortressTerrainPieces
{
    public const string ArtFolder = FortressSceneBuilder.Content + "/Art/Terrain";
    public const string PrefabFolder = FortressSceneBuilder.Content + "/Prefabs/Terrain";
    const string SourcePath = FortressSceneBuilder.Content + "/Art/Citadel.png";
    const string BackdropPath = FortressSceneBuilder.Content + "/Art/CitadelBackdrop.png";
    const int ArtOrderOffset = 10;

    [System.Serializable] sealed class Piece { public string name; public int x, y, width, height; }
    [System.Serializable] sealed class Manifest { public int width, height; public Piece[] pieces; }

    // Which piece of the painting each platform of the original battlefield stands on.
    static readonly Dictionary<string, string> Owners = new Dictionary<string, string>
    {
        { "Left Cliff", "LeftCliff" }, { "Lower Left", "LowerLeft" }, { "Lower Center", "CenterPillar" },
        { "Bridge", "Bridge" }, { "Right Cliff", "RightCliff" }, { "Lower Right", "LowerRight" },
        { "Right Ledge", "RightLedge" }, { "Step 1", "Stairs" }, { "Step 2", "Stairs" },
    };

    [MenuItem("Mini Fortress/지형 조각으로 전장 전환")]
    static void ConvertMenu()
    {
        var arena = Object.FindAnyObjectByType<FortressArena>();
        if (!arena) { Debug.LogError("FortressArena가 있는 씬을 여세요."); return; }
        int count = Convert(arena);
        Debug.Log($"지형 조각 {count}개를 배치했습니다. 프리팹: {PrefabFolder} · 씬을 저장하세요.", arena);
    }

    public static int Convert(FortressArena arena)
    {
        var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(ArtFolder + "/pieces.json"));
        var source = AssetDatabase.LoadAssetAtPath<Sprite>(SourcePath);
        var sourceImporter = (TextureImporter)AssetImporter.GetAtPath(SourcePath);
        if (!source || manifest?.pieces == null) throw new System.InvalidOperationException("Citadel.png 또는 pieces.json이 없습니다. Tools/slice_citadel.py를 실행하세요.");
        var background = arena.background;
        // Piece pixels are file pixels; the imported painting may be scaled by its max texture size.
        float filePerTexel = manifest.width / (float)source.texture.width;
        float pixelsPerUnit = source.pixelsPerUnit * filePerTexel;
        Vector3 ToWorld(float fileX, float fileYFromTop)
        {
            var texel = new Vector2(fileX, manifest.height - fileYFromTop) / filePerTexel;
            return background.transform.TransformPoint((texel - source.pivot - source.rect.position) / source.pixelsPerUnit);
        }

        Directory.CreateDirectory(PrefabFolder);
        var oldTerrain = arena.terrainRoot.GetComponentsInChildren<FortressTerrain>();
        int placed = 0;
        foreach (var piece in manifest.pieces)
        {
            Vector3 centre = ToWorld(piece.x + piece.width * .5f, piece.y + piece.height * .5f);
            centre.z = arena.terrainRoot.position.z;
            string prefabPath = $"{PrefabFolder}/{piece.name}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (!prefab)
            {
                var sprite = ImportPiece($"{ArtFolder}/{piece.name}.png", pixelsPerUnit);
                prefab = BuildPrefab(piece.name, sprite, centre, background, oldTerrain, prefabPath);
            }
            if (HasInstance(arena.terrainRoot, prefab)) continue;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, arena.terrainRoot);
            Undo.RegisterCreatedObjectUndo(instance, "Place terrain piece");
            instance.transform.position = centre;
            placed++;
        }
        // The prefabs now own these platforms; drop the loose originals.
        foreach (var terrain in oldTerrain)
            if (terrain && Owners.ContainsKey(terrain.name) && !PrefabUtility.IsPartOfPrefabInstance(terrain))
                Undo.DestroyObjectImmediate(terrain.gameObject);

        // Keep the background object and sprite but switch its renderer off, so only the pieces show in the
        // editor and in play. Tick the Background SpriteRenderer in the Inspector to bring it back.
        var backdrop = ImportBackdrop(sourceImporter);
        Undo.RecordObject(background, "Use terrain backdrop");
        background.sprite = backdrop;
        background.enabled = false;
        EditorSceneManager.MarkSceneDirty(arena.gameObject.scene);
        return placed;
    }

    static bool HasInstance(Transform root, GameObject prefab)
    {
        foreach (Transform child in root)
            if (PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject) == prefab) return true;
        return false;
    }

    static GameObject BuildPrefab(string name, Sprite sprite, Vector3 centre, SpriteRenderer background, FortressTerrain[] terrain, string path)
    {
        var root = new GameObject(name);
        try
        {
            var art = new GameObject("Art").AddComponent<SpriteRenderer>();
            art.transform.SetParent(root.transform, false);
            art.sprite = sprite;
            art.sortingLayerID = background.sortingLayerID; art.sortingOrder = background.sortingOrder + ArtOrderOffset;
            // Match the painting's (slightly non-uniform) scale so the piece lines up pixel for pixel.
            art.transform.localScale = background.transform.lossyScale;
            foreach (var original in terrain)
            {
                if (!Owners.TryGetValue(original.name, out string owner) || owner != name) continue;
                var platform = new GameObject(original.name).AddComponent<FortressTerrain>();
                platform.transform.SetParent(root.transform, false);
                platform.transform.localPosition = original.transform.position - centre;
                platform.transform.localScale = original.transform.lossyScale;
                platform.allowDropThrough = original.allowDropThrough;
                var from = original.GetComponent<BoxCollider2D>(); var to = platform.GetComponent<BoxCollider2D>();
                to.size = from.size; to.offset = from.offset; to.isTrigger = from.isTrigger;
            }
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { Object.DestroyImmediate(root); }
    }

    static Sprite ImportPiece(string path, float pixelsPerUnit)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (!importer) throw new System.InvalidOperationException("조각 이미지가 없습니다: " + path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Center; importer.SetTextureSettings(settings);
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // Same import settings as the painting, so the background keeps its exact size and position.
    static Sprite ImportBackdrop(TextureImporter source)
    {
        AssetDatabase.ImportAsset(BackdropPath, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(BackdropPath);
        var settings = new TextureImporterSettings(); source.ReadTextureSettings(settings);
        importer.SetTextureSettings(settings);
        importer.maxTextureSize = source.maxTextureSize; importer.textureCompression = source.textureCompression;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(BackdropPath);
    }
}
