using UnityEngine;

namespace MiniFortress
{
    // Public presentation interface. Canvas and input components do not mutate combat state directly.
    public sealed partial class FortressGame
    {
        public bool Ready => ready;
        public FortressBattleRules Rules => arena.rules;
        public FortressCharacterDefinition[] Classes => arena.playerClasses;
        public RenderTexture BattlefieldTexture => pixelFrame;
        public Camera WorldCamera => worldCamera;
        public int SelectedClass => highlightedClass;
        public int FighterCount => fighters.Count;
        public int CurrentActor => current;
        public int Round => round;
        public int LivingEnemies => EnemiesAlive();
        // Enemies fielded this stage; the others are left out of the turn order.
        public int EnemyCount => EnemiesPresent();
        public bool IsPresent(int index) => index < present.Length && present[index];
        public bool IsSelecting => phase == Phase.Selecting;
        public bool IsFinished => phase == Phase.Finished;
        public bool HasAttacked => playerHasAttacked;
        public bool CanAim => ready && phase == Phase.Aim && !playerHasAttacked;
        public bool CanFire => CanAim && grounded;
        public bool CanEndTurn => ready && phase == Phase.Aim && grounded;
        public bool CanJump => CanEndTurn && moveRemaining >= arena.rules.jumpCost;
        public float MovementRemaining => moveRemaining;
        public float PlayerMovementLimit => MoveLimit;
        public bool IsCharging => charging;
        // 0 at the start of a charge, 1 at full power; 0 while not charging.
        public float ChargeFraction => charging ? Mathf.InverseLerp(Rules.powerLimits.x, Rules.powerLimits.y, fighters[0].power) : 0;
        // Previous shot's power (0..1), kept across turns and stages of a run; -1 before the first shot.
        public float LastPowerFraction => lastPowerFraction;
        public float Angle => fighters[0].angle;
        public float Power => fighters[0].power;
        public string Message => message;
        public string CurrentAttackName => AttackName;
        public struct ActorInfo
        {
            public string name;
            public int hp, maxHp;
            // Class-specific status of an enemy (e.g. the archer's bleed), empty when none.
            public string status;
            public Sprite portrait;
            public Vector3 head;
        }
        public ActorInfo GetActor(int index)
        {
            var f = fighters[index];
            return new ActorInfo { name = f.name, hp = f.hp, maxHp = f.maxHp, status = index > 0 ? PlayerClass.ActorStatusText(index) : "", portrait = f.definition.portrait,
                head = f.feet + Vector2.up * (Height(f) + .45f) };
        }
        public int CardEnergy => energy;
        public int MaxCardEnergy => arena.rules.cardEnergy;
        public int Block => block;
        public int HandCount => hand.Count;
        public int DrawPileCount => drawPile.Count;
        public int DiscardPileCount => discardPile.Count;
        public string PendingAttackBuffs => PendingBuffText();
        public string CardTitle(int index) => hand[index].title;
        public string CardDescription(int index) => hand[index].Description;
        public Sprite CardArtwork(int index) => FortressCardArt.For(hand[index], fighters[0].definition.CardSet);
        public int CardCost(int index) => EffectiveCost(hand[index]);
        // The player's class module (Assets/Fortress/Classes) and the texts it shows in the HUD.
        public FortressClassRuntime PlayerClassRuntime => PlayerClass;
        public string AttackResourceText => PlayerClass.AttackResourceText;
        public string PlayerClassStatus => PlayerClass.PlayerStatusText;
        public bool CanPlayCard(int index) => CanPlay(index);
        public void RequestPlayCard(int index) { if (ready) PlayCard(index); }
        public void SetAngle(float value)
        { if (phase == Phase.Aim && !playerHasAttacked) fighters[0].angle = Mathf.Clamp(value, Rules.angleLimits.x, Rules.angleLimits.y); }
        // CodexCode: retain the requested camera zoom as its resting size after trajectory framing.
        public void AdjustCameraZoom(float scale, float minimum, float maximum)
        {
            if (!ready || worldCamera == null || !worldCamera.orthographic) return;
            cameraSize = Mathf.Clamp(cameraSize * scale, minimum, maximum);
            worldCamera.orthographicSize = cameraSize;
        }
        public void SetPower(float value)
        { if (phase == Phase.Aim && !playerHasAttacked) fighters[0].power = Mathf.Clamp(value, Rules.powerLimits.x, Rules.powerLimits.y); }
        public void SetMoveInput(float value) => mouseMove = Mathf.Clamp(value, -1, 1);
        public void RequestFire() { if (ready) Fire(0); }
        public void RequestBeginCharge() { if (ready) BeginCharge(); }
        public void RequestReleaseCharge() { if (ready) ReleaseCharge(); }
        public void RequestJump() { if (ready) Jump(); }
        public void RequestDrop() { if (ready) DropFromBridge(); }
        public void RequestEndTurn() { if (ready) EndPlayerTurn(); }
        // Run and stage rewards. While IsChoosingReward, IsFinished is also true.
        public int Stage => stage;
        public int DeckSize => runDeck.Count;
        public bool IsChoosingReward => choosingReward;
        public int RewardCount => choosingReward ? rewards.Count : 0;
        public string RewardTitle(int index) => rewards[index].title;
        public string RewardDescription(int index) => rewards[index].Description;
        public int RewardCost(int index) => rewards[index].cost;
        public FortressCardRarity RewardRarity(int index) => rewards[index].rarity;
        public Sprite RewardArtwork(int index) => FortressCardArt.For(rewards[index], fighters[0].definition.CardSet);
        public void RequestChooseReward(int index) { if (ready) ChooseReward(index); }
        public void RequestSkipReward() { if (ready) SkipReward(); }
        public void RequestRestart() { if (ready && phase != Phase.Selecting) Restart(); }
    }
}
