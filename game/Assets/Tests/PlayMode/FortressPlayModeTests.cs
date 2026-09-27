using System;
using System.Collections;
using System.Reflection;
using MiniFortress;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

public sealed class FortressPlayModeTests
{
    FortressGame game;
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    object Call(string name, params object[] args) => typeof(FortressGame).GetMethod(name, Hidden).Invoke(game, args);
    object Field(string name) => typeof(FortressGame).GetField(name, Hidden).GetValue(game);
    object Fighter(int index) => ((IList)Field("fighters"))[index];
    void SetActor(int index, string field, object value) => Fighter(index).GetType().GetField(field).SetValue(Fighter(index), value);
    Vector2 Feet(int index) => (Vector2)Fighter(index).GetType().GetField("feet").GetValue(Fighter(index));

    [UnitySetUp]
    public IEnumerator Setup()
    {
        Time.timeScale = 1;
        yield return SceneManager.LoadSceneAsync("FortressBattle");
        yield return null;
        game = Object.FindAnyObjectByType<FortressGame>();
        Assert.That(game, Is.Not.Null); Assert.That(game.Ready, Is.True);
    }
    [UnityTearDown]
    public IEnumerator Cleanup() { Time.timeScale = 1; yield return null; LogAssert.NoUnexpectedReceived(); }

    [UnityTest]
    public IEnumerator SceneReferencesAndBothClassesWork()
    {
        Assert.That(Object.FindAnyObjectByType<FortressArena>().ValidateSetup(), Is.Empty);
        Assert.That(game.IsSelecting, Is.True);
        Assert.That(game.FighterCount, Is.EqualTo(5));
        var hud = Object.FindAnyObjectByType<FortressHud>();
        for (int role = 0; role < game.Classes.Length; role++)
        {
            game.SelectClass(role); game.BeginBattle(); yield return null;
            Assert.That(game.GetActor(0).maxHp, Is.EqualTo(game.Classes[role].health));
            Assert.That(game.PlayerMovementLimit, Is.EqualTo(game.Classes[role].movementPerTurn));
            Assert.That(game.CanFire, Is.True);
            Assert.That((hud.healthText ? hud.healthText : hud.playerStatus).text, Does.Contain(game.Classes[role].health.ToString()));
            game.OpenSelection(); yield return null;
        }
    }

    [UnityTest]
    public IEnumerator EditedTerrainAndSpawnDriveLandingAndProjectiles()
    {
        var arena = Object.FindAnyObjectByType<FortressArena>();
        FortressTerrain platform = null;
        foreach (var candidate in arena.terrainRoot.GetComponentsInChildren<FortressTerrain>())
            if (candidate.WorldRect.xMin <= arena.playerSpawn.position.x && candidate.WorldRect.xMax >= arena.playerSpawn.position.x
                && Mathf.Abs(candidate.WorldRect.yMax - arena.playerSpawn.position.y) < .05f) platform = candidate;
        Assert.That(platform, Is.Not.Null);
        platform.transform.position += Vector3.up * 2;
        arena.playerSpawn.position += Vector3.up * 2;
        Call("LoadArenaLayout"); game.BeginBattle();
        Assert.That(Feet(0).y, Is.EqualTo(platform.WorldRect.yMax).Within(.001));
        Assert.That((bool)Call("HitsTerrain", platform.WorldRect.center), Is.True);
        game.RequestJump(); Assert.That(game.CanEndTurn, Is.False);
        for (int i = 0; i < 180; i++) Call("FallPlayer", 1f / 60);
        Assert.That(Feet(0).y, Is.EqualTo(platform.WorldRect.yMax).Within(.001));
        Assert.That(game.CanEndTurn, Is.True);
        yield return null;
    }

    [UnityTest]
    public IEnumerator DataChangesReachCombatAndHud()
    {
        var arena = Object.FindAnyObjectByType<FortressArena>();
        var edited = Object.Instantiate(arena.playerClasses[0]);
        edited.health = 173; edited.damage = 19; edited.movementPerTurn = 7;
        arena.playerClasses = new[] { edited, arena.playerClasses[1] };
        game.BeginBattle(); yield return null;
        Assert.That(game.GetActor(0).hp, Is.EqualTo(173));
        Assert.That(game.MovementRemaining, Is.EqualTo(7));
        int before = game.GetActor(1).hp;
        typeof(FortressGame).GetField("shotPosition", Hidden).SetValue(game, Feet(1) + Vector2.up * 2);
        Call("Impact", 1, false);
        Assert.That(before - game.GetActor(1).hp, Is.EqualTo(19));
        var hud = Object.FindAnyObjectByType<FortressHud>();
        Assert.That((hud.healthText ? hud.healthText : hud.playerStatus).text, Does.Contain("173"));
        Object.Destroy(edited);
    }

    [UnityTest]
    public IEnumerator AttackLocksAndManualTurnSurviveMigration()
    {
        game.BeginBattle();
        game.RequestFire(); Assert.That(game.HasAttacked, Is.True); Assert.That(game.CanEndTurn, Is.False);
        game.RequestEndTurn(); Assert.That(game.CurrentActor, Is.EqualTo(0));
        game.OpenSelection(); yield return null;
        Assert.That(game.IsSelecting, Is.True);
        game.SelectClass(1); game.BeginBattle();
        game.RequestFire();
        Time.timeScale = 6;
        float deadline = Time.realtimeSinceStartup + 10;
        while (!game.CanEndTurn && !game.IsFinished && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(game.CanEndTurn, Is.True); Assert.That(game.HasAttacked, Is.True);
        Assert.That(game.CanFire, Is.False); Assert.That(game.CurrentActor, Is.Zero);
        game.RequestRestart(); yield return null;
        Assert.That(game.CanFire, Is.True); Assert.That(game.GetActor(0).hp, Is.EqualTo(140));
    }

    [UnityTest]
    public IEnumerator EnemiesCompleteRoundAndRestorePlayerBudget()
    {
        game.BeginBattle(); game.RequestEndTurn(); Time.timeScale = 8;
        float deadline = Time.realtimeSinceStartup + 25;
        while (game.Round == 1 && !game.IsFinished && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(game.Round, Is.EqualTo(2)); Assert.That(game.CurrentActor, Is.Zero);
        Assert.That(game.MovementRemaining, Is.EqualTo(game.PlayerMovementLimit)); Assert.That(game.CanFire, Is.True);
    }

    [UnityTest]
    public IEnumerator CanvasButtonsSlidersAndEndConditionsAreConnected()
    {
        var hud = Object.FindAnyObjectByType<FortressHud>();
        var buttons = hud.selectionPanel.GetComponentsInChildren<Button>();
        foreach (var button in buttons) if (button.name == "Begin Battle") button.onClick.Invoke();
        yield return null; Assert.That(game.CanAim, Is.True);
        game.SetAngle(35); Assert.That(game.Angle, Is.EqualTo(35));
        // Space-style charge: starts at minimum power, fires on release.
        game.RequestBeginCharge(); Assert.That(game.IsCharging, Is.True); Assert.That(game.ChargeFraction, Is.Zero);
        yield return new WaitForSeconds(.2f); Assert.That(game.ChargeFraction, Is.GreaterThan(0));
        game.RequestReleaseCharge(); Assert.That(game.HasAttacked, Is.True); Assert.That(game.IsCharging, Is.False);
        game.RequestRestart(); yield return null;
        hud.jumpButton.onClick.Invoke(); Assert.That(game.CanFire, Is.False);
        game.RequestRestart();
        for (int i = 1; i < game.FighterCount; i++) SetActor(i, "hp", 0);
        Call("ResolveShot"); yield return null;
        Assert.That(game.IsFinished, Is.True); Assert.That(hud.rewardPanel.activeSelf, Is.True);
        game.RequestRestart(); SetActor(0, "hp", 0); Call("ResolveShot"); yield return null;
        Assert.That(game.IsFinished, Is.True); Assert.That(game.Message, Does.Contain("패배"));
    }

    void SetField(string name, object value) => typeof(FortressGame).GetField(name, Hidden).SetValue(game, value);

    [UnityTest]
    public IEnumerator EarlyStagesFieldOnlyTheWeakestEnemies()
    {
        game.BeginBattle(); yield return null;
        Assert.That(game.EnemyCount, Is.EqualTo(game.Rules.earlyStageEnemies));
        Assert.That(game.LivingEnemies, Is.EqualTo(game.EnemyCount));
        int weakest = int.MaxValue, strongestPresent = 0;
        for (int i = 1; i < game.FighterCount; i++)
        {
            int health = game.GetActor(i).maxHp;
            if (game.IsPresent(i)) strongestPresent = Mathf.Max(strongestPresent, health);
            else weakest = Mathf.Min(weakest, health);
        }
        Assert.That(strongestPresent, Is.LessThanOrEqualTo(weakest));
        SetField("stage", game.Rules.earlyStages + 1); game.RequestRestart(); yield return null;
        Assert.That(game.EnemyCount, Is.EqualTo(game.FighterCount - 1));
    }

    [UnityTest]
    public IEnumerator WinningOffersRewardThatJoinsNextStageDeck()
    {
        game.BeginBattle(); yield return null;
        int deck = game.DeckSize;
        Assert.That(game.Stage, Is.EqualTo(1));
        for (int i = 1; i < game.FighterCount; i++) SetActor(i, "hp", 0);
        Call("ResolveShot"); yield return null;
        Assert.That(game.IsFinished, Is.True); Assert.That(game.IsChoosingReward, Is.True);
        Assert.That(game.RewardCount, Is.InRange(1, 3));
        for (int i = 0; i < game.RewardCount; i++) Assert.That(game.RewardRarity(i), Is.Not.EqualTo(FortressCardRarity.Legend));
        var hud = Object.FindAnyObjectByType<FortressHud>();
        Assert.That(hud.rewardPanel.activeSelf, Is.True); Assert.That(hud.resultPanel.activeSelf, Is.False);
        hud.rewardSlots[0].button.onClick.Invoke(); yield return null;
        Assert.That(hud.rewardPanel.activeSelf, Is.False);
        Assert.That(game.Stage, Is.EqualTo(2)); Assert.That(game.IsFinished, Is.False);
        Assert.That(game.DeckSize, Is.EqualTo(deck + 1));
        Assert.That(game.HandCount + game.DrawPileCount, Is.EqualTo(deck + 1));
        Assert.That(game.LivingEnemies, Is.EqualTo(game.EnemyCount));
        // Retrying a stage keeps the grown deck; picking a class again starts a fresh run.
        game.RequestRestart(); yield return null;
        Assert.That(game.Stage, Is.EqualTo(2)); Assert.That(game.DeckSize, Is.EqualTo(deck + 1));
        game.OpenSelection(); game.BeginBattle(); yield return null;
        Assert.That(game.Stage, Is.EqualTo(1)); Assert.That(game.DeckSize, Is.EqualTo(deck));
    }

    [UnityTest]
    public IEnumerator CardsSpendEnergyBlockDamageAndBuffNextAttack()
    {
        game.SelectClass(1); game.BeginBattle(); yield return null;
        Assert.That(game.HandCount, Is.EqualTo(game.Rules.handSize));
        Assert.That(game.CardEnergy, Is.EqualTo(game.Rules.cardEnergy));
        Assert.That(game.HandCount + game.DrawPileCount, Is.EqualTo(10));
        game.RequestPlayCard(0);
        Assert.That(game.CardEnergy, Is.EqualTo(game.Rules.cardEnergy - 1));
        Assert.That(game.DiscardPileCount, Is.EqualTo(1));

        var enemy = (FortressCharacterDefinition)Fighter(1).GetType().GetField("definition").GetValue(Fighter(1));
        SetField("block", enemy.damage + 3); SetField("current", 1);
        int before = game.GetActor(0).hp;
        SetField("shotPosition", Feet(0) + Vector2.up * 2);
        Call("Impact", 0, false);
        Assert.That(game.GetActor(0).hp, Is.EqualTo(before));
        Assert.That(game.Block, Is.EqualTo(3));

        game.RequestRestart(); yield return null;
        SetField("shotDamageBonus", 15);
        before = game.GetActor(1).hp;
        SetField("shotPosition", Feet(1) + Vector2.up * 2);
        Call("Impact", 1, false);
        Assert.That(before - game.GetActor(1).hp, Is.EqualTo(Mathf.Min(before, game.Classes[1].damage + 15)));
    }

    [UnityTest]
    public IEnumerator RemappedInputActionControlsFire()
    {
        var input = Object.FindAnyObjectByType<FortressInput>();
        var modified = Object.Instantiate(input.actions);
        modified.FindAction("Battle/Fire").ChangeBinding(0).WithPath("<Keyboard>/f");
        input.enabled = false; input.actions = modified; input.enabled = true;
        var oldBackground = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
        var oldEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try
        {
            game.BeginBattle();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space)); yield return null; yield return null;
            Assert.That(game.HasAttacked, Is.False);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
            // Holding the fire key charges; releasing it fires.
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F)); yield return null; yield return null;
            Assert.That(game.IsCharging, Is.True); Assert.That(game.HasAttacked, Is.False);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return null;
            Assert.That(game.HasAttacked, Is.True);
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard); Object.Destroy(modified);
            InputSystem.settings.backgroundBehavior = oldBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorInput;
#endif
        }
    }
}
