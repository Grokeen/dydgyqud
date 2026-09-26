using UnityEngine;

namespace MiniFortress
{
    public enum FortressWeapon { Bow, Spear }

    [CreateAssetMenu(menuName = "Mini Fortress/Character", fileName = "Character")]
    public sealed class FortressCharacterDefinition : ScriptableObject
    {
        public string displayName;
        [TextArea] public string description;
        public FortressFighterView prefab;
        public Sprite portrait;
        public Sprite projectile;
        public FortressWeapon weapon;
        [Min(1)] public int health = 120;
        [Min(0)] public int damage = 32;
        [Min(0.01f)] public float blastRadius = 3.5f;
        [Min(0)] public float movementPerTurn = 10;
        [Min(0.01f)] public float movementSpeed = 5;
        [Min(0.01f)] public float releaseTime = .16f;
        [Min(0.01f)] public float height = 4.2f;
        [Min(0.01f)] public float halfWidth = .7f;
        [Min(0)] public float shoulderHeight = 2.5f;
        [Min(0)] public float muzzleDistance = 1.6f;
        public string AttackName => weapon == FortressWeapon.Spear ? "창 투척" : "화살 발사";
    }
}
