using System.Collections.Generic;
using UnityEngine;

namespace MiniFortress
{
    // One battlefield. Put map assets in Assets/Resources/FortressMaps (or list them on the Arena) and they appear
    // in the main menu, sorted by `order`. Collision comes only from `platforms`; art never changes gameplay.
    [CreateAssetMenu(menuName = "Mini Fortress/Map", fileName = "Map")]
    public sealed class FortressMapDefinition : ScriptableObject
    {
        [Tooltip("메인 메뉴에서의 순서(작은 값이 먼저)")]
        public int order;
        public string displayName;
        [TextArea] public string description;
        [Tooltip("메인 메뉴 상단 부제. 이 맵을 고르면 바뀝니다. 비워 두면 공통 문구")]
        public string menuTagline;
        [Tooltip("메인 메뉴 상단 설명. 이 맵을 고르면 바뀝니다. 비워 두면 공통 문구")]
        [TextArea] public string menuDescription;

        [Header("이미지")]
        [Tooltip("전투 배경. 카메라를 따라가며 화면을 덮습니다.")]
        public Sprite background;
        [Tooltip("메인 메뉴 카드 이미지. 비워 두면 배경을 사용합니다.")]
        public Sprite preview;
        [Tooltip("기본 발판 색에 곱하는 색조")]
        public Color tint = Color.white;

        [Header("발판 외형 (비워 두면 기본 벽돌 / 판자)")]
        public FortressPlatformLook groundLook = new FortressPlatformLook();
        [Tooltip("S로 내려갈 수 있는 발판의 외형")]
        public FortressPlatformLook dropThroughLook = new FortressPlatformLook();

        [Header("배치 (월드 좌표, m)")]
        public List<FortressMapPlatform> platforms = new List<FortressMapPlatform>();
        public Vector2 playerSpawn;
        [Tooltip("목록 순서가 적 턴 순서입니다.")]
        public List<FortressMapEnemy> enemies = new List<FortressMapEnemy>();

        public Sprite PreviewImage => preview ? preview : background;
        public string Title => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    }

    [System.Serializable]
    public sealed class FortressMapPlatform
    {
        public string name;
        [Tooltip("충돌 영역: 왼쪽·오른쪽 x, 윗면·아랫면 y. 캐릭터는 윗면(top)에 섭니다.")]
        public float left, right, top, bottom;
        [Tooltip("S 키로 아래로 내려갈 수 있습니다.")]
        public bool dropThrough;
        [Tooltip("이 발판에만 쓸 3D 모델. 비워 두면 맵의 발판 외형을 씁니다.")]
        public GameObject model;

        public Rect Rect => new Rect(left, bottom, right - left, top - bottom);

        public FortressMapPlatform() { }
        public FortressMapPlatform(string name, float left, float right, float top, float bottom, bool dropThrough)
        { this.name = name; this.left = left; this.right = right; this.top = top; this.bottom = bottom; this.dropThrough = dropThrough; }
    }

    [System.Serializable]
    public sealed class FortressMapEnemy
    {
        public string displayName;
        [Tooltip("발 위치")]
        public Vector2 position;
        [Tooltip("비워 두면 씬의 첫 번째 적 스폰 캐릭터를 사용합니다.")]
        public FortressCharacterDefinition character;

        public FortressMapEnemy() { }
        public FortressMapEnemy(string displayName, Vector2 position, FortressCharacterDefinition character = null)
        { this.displayName = displayName; this.position = position; this.character = character; }
    }

    // Stretch: scale the model to the platform's width and thickness.
    // PivotAtTopCentre: place the model's origin at the middle of the platform's top edge, unscaled.
    public enum FortressModelFit { Stretch, PivotAtTopCentre }

    [System.Serializable]
    public sealed class FortressPlatformLook
    {
        [Tooltip("발판 전체를 이 3D 모델로 그립니다. 비워 두면 상자 모양을 생성합니다.")]
        public GameObject model;
        public FortressModelFit fit = FortressModelFit.Stretch;
        [Tooltip("상자 본체 재질. 비워 두면 아래 질감·색으로 만듭니다.")]
        public Material bodyMaterial;
        [Tooltip("상자 윗면 테두리 재질")]
        public Material capMaterial;
        [Tooltip("상자에 반복해서 입힐 질감. 비워 두면 기본 벽돌 / 판자")]
        public Texture texture;
        [Tooltip("질감이 한 번 반복되는 크기(m)")]
        [Min(.1f)] public float textureTile = 2;
        public bool overrideColors;
        public Color bodyColor = new Color(.5f, .54f, .6f), capColor = new Color(.74f, .72f, .66f);
    }
}
