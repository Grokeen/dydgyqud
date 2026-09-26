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
        public bool IsSelecting => phase == Phase.Selecting;
        public bool IsFinished => phase == Phase.Finished;
        public bool HasAttacked => playerHasAttacked;
        public bool CanAim => ready && phase == Phase.Aim && !playerHasAttacked;
        public bool CanFire => CanAim && grounded;
        public bool CanEndTurn => ready && phase == Phase.Aim && grounded;
        public bool CanJump => CanEndTurn && moveRemaining >= arena.rules.jumpCost;
        public float MovementRemaining => moveRemaining;
        public float PlayerMovementLimit => MoveLimit;
        public float Angle => fighters[0].angle;
        public float Power => fighters[0].power;
        public string Message => message;
        public string CurrentAttackName => AttackName;
        public struct ActorInfo
        {
            public string name;
            public int hp, maxHp;
            public Sprite portrait;
            public Vector3 head;
        }
        public ActorInfo GetActor(int index)
        {
            var f = fighters[index];
            return new ActorInfo { name = f.name, hp = f.hp, maxHp = f.maxHp, portrait = f.definition.portrait,
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
        public int CardCost(int index) => hand[index].cost;
        public bool CanPlayCard(int index) => CanPlay(index);
        public void RequestPlayCard(int index) { if (ready) PlayCard(index); }
        public void SetAngle(float value)
        { if (phase == Phase.Aim && !playerHasAttacked) fighters[0].angle = Mathf.Clamp(value, Rules.angleLimits.x, Rules.angleLimits.y); }
        public void SetPower(float value)
        { if (phase == Phase.Aim && !playerHasAttacked) fighters[0].power = Mathf.Clamp(value, Rules.powerLimits.x, Rules.powerLimits.y); }
        public void SetMoveInput(float value) => mouseMove = Mathf.Clamp(value, -1, 1);
        public void RequestFire() { if (ready) Fire(0); }
        public void RequestJump() { if (ready) Jump(); }
        public void RequestDrop() { if (ready) DropFromBridge(); }
        public void RequestEndTurn() { if (ready) EndPlayerTurn(); }
        public void RequestRestart() { if (ready && phase != Phase.Selecting) Restart(); }
    }
}
