using UnityEngine;

namespace MiniFortress
{
    [CreateAssetMenu(menuName = "Mini Fortress/Battle Rules", fileName = "BattleRules")]
    public sealed class FortressBattleRules : ScriptableObject
    {
        [Min(.01f)] public float gravity = 12;
        [Min(.001f)] public float shotStep = .0125f;
        [Min(0)] public float jumpSpeed = 11;
        [Min(0)] public float jumpCost = 1;
        [Min(0)] public int fallDamage = 15;
        public float fallRespawnY = -10;
        public Vector2 horizontalLimits = new Vector2(0, 102);
        public Rect projectileLimits = new Rect(-6, -13, 112, 160);
        [Min(.1f)] public float shotLifetime = 10;
        public Vector2 angleLimits = new Vector2(10, 80);
        public Vector2 powerLimits = new Vector2(10, 38);
        public float defaultAngle = 48, defaultPower = 26;
        [Min(0)] public float angleSpeed = 35, powerSpeed = 12;
        [Min(0)] public float enemyAngleError = 2.8f, enemyPowerError = .8f;
        [Header("Stages")]
        [Tooltip("이 스테이지까지는 약한 적만 일부 출전합니다.")]
        [Min(0)] public int earlyStages = 2;
        [Tooltip("초반 스테이지에 출전하는 적 수 (체력이 낮은 적부터, 같으면 Hierarchy 순서)")]
        [Min(1)] public int earlyStageEnemies = 2;
        [Header("Cards")]
        [Min(0)] public int cardEnergy = 3;
        [Min(1)] public int handSize = 5;
        [Tooltip("카드를 추가로 뽑을 때 손패가 이 수를 넘지 않습니다.")]
        [Min(1)] public int maxHandSize = 8;
        [Min(1)] public float criticalMultiplier = 2;
        [Tooltip("연사 시 화살마다 벌어지는 각도")]
        [Min(0)] public float volleySpread = 4f;
    }
}
