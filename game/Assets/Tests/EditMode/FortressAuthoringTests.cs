using System.Collections.Generic;
using System.Linq;
using System.Collections;
using MiniFortress;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class FortressAuthoringTests
{
    Scene scene;
    FortressArena arena;
    FortressHud hud;
    [SetUp]
    public void Setup()
    {
        scene = EditorSceneManager.OpenScene(FortressSceneBuilder.ScenePath, OpenSceneMode.Single);
        arena = Object.FindAnyObjectByType<FortressArena>();
        hud = Object.FindAnyObjectByType<FortressHud>();
        Assert.That(arena, Is.Not.Null);
    }
    [TearDown]
    public void Cleanup()
    {
        Undo.ClearAll();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }
    [UnityTest]
    public IEnumerator RenderingPreviewPreservesSourceCanvasAndTexture()
    {
        Texture originalTexture = hud.battlefield.texture;
        var originalMode = hud.GetComponent<Canvas>().renderMode;
        int roots = scene.rootCount;
        using (var preview = new FortressPreview(arena, hud, 0, FortressPreview.Screen.Battle))
        {
            yield return null;
            yield return null;
            var rendered = preview.Render();
            Assert.That(rendered, Is.Not.Null);
            Assert.That(rendered.width, Is.EqualTo(1600));
            Assert.That(rendered.height, Is.EqualTo(900));
        }
        Assert.That(hud.battlefield.texture, Is.SameAs(originalTexture));
        Assert.That(hud.GetComponent<Canvas>().renderMode, Is.EqualTo(originalMode));
        Assert.That(scene.rootCount, Is.EqualTo(roots));
        Assert.That(scene.isDirty, Is.False);
    }

    [Test]
    public void DefaultPlacementIsValidAndInactiveEnemiesAreExcluded()
    {
        Assert.That(FortressPlacement.Diagnose(arena), Is.Empty);
        var enemy = arena.enemySpawns.GetChild(0);
        enemy.gameObject.SetActive(false);
        Assert.That(FortressPlacement.Spawns(arena).Count, Is.EqualTo(4));
    }
    [Test]
    public void GroupSnapSupportsUndoAndNeverMovesACharacterAcrossAGap()
    {
        var spawns = FortressPlacement.Spawns(arena);
        spawns[0].position += Vector3.up * 3;
        spawns[1].position += Vector3.up * 2;
        Vector3 first = spawns[0].position, second = spawns[1].position;
        Assert.That(FortressPlacement.Snap(new[] { spawns[0], spawns[1] }), Is.EqualTo(2));
        Assert.That(FortressPlacement.Diagnose(arena), Is.Empty);
        Undo.PerformUndo();
        Assert.That(spawns[0].position, Is.EqualTo(first));
        Assert.That(spawns[1].position, Is.EqualTo(second));
        spawns[0].position = new Vector3(200, 50, 0);
        Assert.That(FortressPlacement.Snap(new[] { spawns[0] }), Is.Zero);
        Assert.That(spawns[0].position, Is.EqualTo(new Vector3(200, 50, 0)));
    }
    [Test]
    public void DiagnosticsFindFloatingEmbeddedAndOverlappingSpawns()
    {
        var spawns = FortressPlacement.Spawns(arena);
        spawns[0].position += Vector3.up * 2;
        spawns[1].position += Vector3.down * 1;
        spawns[2].position = spawns[3].position;
        var issues = FortressPlacement.Diagnose(arena);
        Assert.That(issues.Any(i => i.target == spawns[0] && i.message.Contains("바닥")), Is.True);
        Assert.That(issues.Any(i => i.target == spawns[1] && i.message.Contains("지형")), Is.True);
        Assert.That(issues.Any(i => i.message.Contains("시작 위치")), Is.True);
    }
    [Test]
    public void PreviewModesNeverModifyOrPolluteAuthoredScene()
    {
        int rootCount = scene.rootCount, sceneCount = SceneManager.sceneCount;
        string hudBefore = EditorJsonUtility.ToJson(hud);
        string statusBefore = hud.playerStatus.text;
        bool selecting = hud.selectionPanel.activeSelf, battle = hud.battlePanel.activeSelf;
        var originalSelection = Selection.activeObject;
        Assert.That(scene.isDirty, Is.False);
        foreach (FortressPreview.Screen mode in System.Enum.GetValues(typeof(FortressPreview.Screen)))
        {
            using (var preview = new FortressPreview(arena, hud, 1, mode))
            {
                Assert.That(preview.Actors.Count, Is.EqualTo(5));
                Assert.That(preview.Actors[0].data, Is.SameAs(arena.playerClasses[1]));
                Assert.That(preview.Actors[0].feet, Is.EqualTo(arena.playerSpawn.position));
                Assert.That(preview.Camera.gameObject.scene, Is.Not.EqualTo(scene));
                Assert.That(preview.Camera.scene, Is.EqualTo(preview.Camera.gameObject.scene));
            }
            Assert.That(scene.rootCount, Is.EqualTo(rootCount));
            Assert.That(SceneManager.sceneCount, Is.EqualTo(sceneCount));
            Assert.That(scene.isDirty, Is.False);
            Assert.That(hud.playerStatus.text, Is.EqualTo(statusBefore));
            Assert.That(EditorJsonUtility.ToJson(hud), Is.EqualTo(hudBefore));
            Assert.That(hud.selectionPanel.activeSelf, Is.EqualTo(selecting));
            Assert.That(hud.battlePanel.activeSelf, Is.EqualTo(battle));
            Assert.That(Selection.activeObject, Is.SameAs(originalSelection));
        }
    }
}
