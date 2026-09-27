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
        [Min(0), Tooltip("방어도는 일반 사격 피해를 먼저 막습니다. 출혈/파열은 방어도를 무시합니다.")] public int armor;
        [Min(0.01f)] public float blastRadius = 3.5f;
        [Min(0)] public float movementPerTurn = 10;
        [Min(0.01f)] public float movementSpeed = 5;
        [Min(0.01f)] public float releaseTime = .16f;
        [Min(0.01f)] public float height = 4.2f;
        [Min(0.01f)] public float halfWidth = .7f;
        [Min(0)] public float shoulderHeight = 2.5f;
        [Min(0)] public float muzzleDistance = 1.6f;
        [Tooltip("비워 두면 무기별 기본 덱을 사용합니다.")]
        public FortressCardEntry[] deck;
        [Tooltip("보상/실험용 카드 풀. 비워 둔 궁수는 우선 구현 20장 풀을 사용합니다.")]
        public FortressCardEntry[] cardPool;
        [Tooltip("궁수의 20장 우선 구현 풀을 이번 전투 덱 전체로 사용합니다. 기본 덱 9장 규칙을 바꾸는 테스트 옵션입니다.")]
        public bool useCardPoolAsTestDeck;
        public string AttackName => weapon == FortressWeapon.Spear ? "창 투척" : "화살 발사";
        public FortressCardEntry[] CardPool => cardPool != null && cardPool.Length > 0 ? cardPool :
            weapon == FortressWeapon.Bow ? FortressCardEntry.ArcherPrototypePool() : Deck;
        public FortressCardEntry[] Deck => deck != null && deck.Length > 0 ? deck :
            useCardPoolAsTestDeck && weapon == FortressWeapon.Bow ? CardPool : FortressCardEntry.DefaultDeck(weapon);
    }
}
