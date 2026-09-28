using UnityEngine;

namespace MiniFortress
{
    // Player classes are modules. The shared battle code (FortressGame) never names a class: it calls the player's
    // FortressClassRuntime at fixed moments (battle start, turn start, attack, launch, hit, miss, card play, HUD),
    // and each class keeps its own state, rules and card effects in its folder, Assets/Fortress/Classes/<Class>.
    //
    // A class has two parts:
    //  - FortressClassBehaviour: a ScriptableObject asset with the class's tuning (Assets/FortressContent/Classes/<Class>),
    //    linked from the character's Class field;
    //  - FortressClassRuntime: the per-battle state it creates, living as long as the player keeps that class.

    // What a class module may ask of the battle. FortressGame implements it; index 0 is the player, 1+ are enemies.
    public interface IFortressBattle
    {
        int FighterCount { get; }
        int Round { get; }
        FortressCharacterDefinition PlayerDefinition { get; }
        Vector2 FighterFeet(int index);
        int FighterHp(int index);
        int FighterMaxHp(int index);
        string FighterName(int index);
        // Damage outside a shot (bleed bursts ...), with the hit/death reaction. Ignored for fallen fighters.
        void DamageFighter(int index, int amount);
        void DrawCards(int count);
        // Adds to the player's banked "next attack" damage (the shared ShotDamage card pool).
        void AddNextAttackDamage(int amount);
        // Adds to the player's banked extra projectiles for the next attack (the shared ExtraShot card pool).
        void AddNextAttackShots(int amount);
        // Appended to the hit message of the shot being resolved.
        void AddHitNote(string note);
        // A 3D projectile of the player's weapon, left on the battlefield (fallen arrows ...). The class destroys it.
        Transform SpawnProjectileView(string name, Vector2 position, float angleDegrees);
    }

    // What one projectile carries when it lands. Classes subclass it for their own on-hit effects.
    public class FortressShotMods
    {
        public int extraDamage;
    }

    public abstract class FortressClassBehaviour : ScriptableObject
    {
        [Tooltip("공격 버튼과 메시지에 쓰는 공격 이름. 비워 두면 무기 기본값(화살 발사 / 창 투척)")]
        public string attackName;

        public abstract FortressClassRuntime CreateRuntime(IFortressBattle battle, FortressCharacterDefinition definition);
    }

    // Every hook defaults to "no class-specific behaviour", so a new class overrides only what it changes.
    public abstract class FortressClassRuntime
    {
        protected readonly IFortressBattle battle;
        public FortressCharacterDefinition Definition { get; }

        protected FortressClassRuntime(IFortressBattle battle, FortressCharacterDefinition definition)
        { this.battle = battle; Definition = definition; }

        // A battle (stage or restart) begins; fighter indices are fixed until the next call.
        public virtual void OnBattleStart() { }
        public virtual void OnPlayerTurnStart() { }
        // The player committed an attack: lock in banked "next attack" effects.
        public virtual void OnAttackCommitted() { }
        // Projectiles this attack looses, including shared card extras.
        public virtual int ShotsForAttack(int extraShots) => 1 + extraShots;
        // Multiplies the player's base shot damage for the committed attack.
        public virtual int DamageMultiplier => 1;
        // Called once per player projectile at launch.
        public virtual FortressShotMods NextShotMods() => null;
        // Extra damage against a specific enemy.
        public virtual int TargetBonus(int target, FortressShotMods mods) => 0;
        // An enemy's shot damage before it hits the player (weaken ...).
        public virtual int AdjustEnemyShotDamage(int enemy, int damage) => damage;
        // The player's projectile damaged `target` (the enemy that took the most damage).
        public virtual void OnHit(int target, FortressShotMods mods) { }
        // The player's projectile damaged nobody; `landed` is false when it left the map.
        public virtual void OnMiss(Vector2 at, Vector2 velocity, bool landed) { }
        // Class-specific card effects; return false for effects this class does not handle.
        public virtual bool PlayCard(FortressCardEntry card) => false;
        // HUD texts: banked attack effects, the line under the attack button, the player status line, an enemy's status.
        public virtual string PendingAttackText => "";
        public virtual string AttackResourceText => "";
        public virtual string PlayerStatusText => "";
        public virtual string ActorStatusText(int index) => "";
        // Remove anything the class left on the battlefield.
        public virtual void ClearVisuals() { }
    }

    // Class used when a character has no Class asset: the archer for bows, the spearman for spears.
    public static class FortressBuiltInClasses
    {
        static FortressClassBehaviour archer, spearman;

        public static FortressClassBehaviour For(FortressWeapon weapon)
        {
            if (weapon == FortressWeapon.Spear)
            {
                if (!spearman) spearman = ScriptableObject.CreateInstance<FortressSpearmanClass>();
                return spearman;
            }
            if (!archer) archer = ScriptableObject.CreateInstance<FortressArcherClass>();
            return archer;
        }
    }
}
