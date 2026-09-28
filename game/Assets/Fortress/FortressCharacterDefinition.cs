using UnityEngine;

namespace MiniFortress
{
    public enum FortressWeapon { Bow, Spear }
    // Colour of the placeholder capsule built by FortressFighterModel. Auto picks one from the asset/prefab name and weapon.
    public enum FortressModelStyle { Auto, Archer, Spearman, Goblin, Captain }

    [CreateAssetMenu(menuName = "Mini Fortress/Character", fileName = "Character")]
    public sealed class FortressCharacterDefinition : ScriptableObject
    {
        public string displayName;
        [TextArea] public string description;
        public FortressFighterView prefab;
        public Sprite portrait;
        public Sprite projectile;
        public FortressWeapon weapon;
        [Header("3D 외형 (비워 두면 기본 도형 모델)")]
        [Tooltip("캐릭터 3D 모델(FBX/Prefab). +X가 앞, 발이 원점. 비워 두면 Model Style 색의 캡슐 도형을 씁니다.")]
        public GameObject modelPrefab;
        [Tooltip("기본 캡슐 색 (궁수 초록, 창병 주황, 경비병 보라, 대장 빨강). Auto는 에셋 이름과 무기로 고릅니다.")]
        public FortressModelStyle modelStyle;
        [Tooltip("모델 높이를 Height에 맞춰 크기를 자동 조절합니다.")]
        public bool fitModelToHeight = true;
        public Vector3 modelOffset;
        [Tooltip("기본값 Y 28°: 카메라 쪽으로 살짝 돌려 입체감이 보이게 합니다.")]
        public Vector3 modelRotation = new Vector3(0, 28, 0);
        [Min(.01f)] public float modelScale = 1;
        [Tooltip("켜 두면 기본 Animator 클립(호흡·기울기·피격 흔들림)이 모델 전체를 움직입니다. 자체 애니메이션이 있는 모델은 끄세요.")]
        public bool useBuiltInBodyMotion = true;
        [Tooltip("손에 든 무기 모델. 원점 = 어깨(조준 회전축), +X = 조준 방향. 비워 두면 기본 활 / 창")]
        public GameObject weaponModelPrefab;
        [Tooltip("장전·비행·바닥에 떨어진 화살/창 모델. 원점 = 촉 끝, 자루는 -X 방향. 비워 두면 기본 화살 / 창")]
        public GameObject projectileModelPrefab;
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
        [Header("직업")]
        [Tooltip("직업 모듈(Assets/FortressContent/Classes/<직업>/<직업> Class). 고유 능력·수치·카드 효과를 담당합니다. 비워 두면 무기별 기본 직업(활 → 궁수, 창 → 창병). 적은 쓰지 않습니다.")]
        public FortressClassBehaviour classBehaviour;
        [Header("카드")]
        [Tooltip("이 직업의 카드 세트(Assets/FortressContent/Classes/<직업>/<직업> Cards). 비워 두면 무기별 기본 카드를 사용합니다.")]
        public FortressCardSet cardSet;
        public FortressClassBehaviour ClassBehaviour => classBehaviour ? classBehaviour : FortressBuiltInClasses.For(weapon);
        public string AttackName => !string.IsNullOrWhiteSpace(ClassBehaviour.attackName) ? ClassBehaviour.attackName
            : weapon == FortressWeapon.Spear ? "창 투척" : "화살 발사";
        public FortressCardSet CardSet => cardSet ? cardSet : FortressBuiltInCards.For(weapon);
    }
}
