using UnityEngine;

namespace MiniFortress
{
    // Archer tuning (Assets/FortressContent/Classes/Archer/Archer Class.asset). The archer's state and card effects
    // live in FortressArcherRuntime (arrows) and FortressArcherRuntime.Bleed (bleed, weaken) in this folder.
    [CreateAssetMenu(menuName = "Mini Fortress/Classes/Archer", fileName = "Archer Class")]
    public sealed class FortressArcherClass : FortressClassBehaviour
    {
        [Header("화살")]
        [Tooltip("매 턴 시작 화살 수. 공격 한 번에 가진 화살을 모두 연속으로 쏩니다.")]
        [Min(1)] public int baseArrows = 1;
        [Tooltip("화살 수 상한. 카드로 추가해도 이 수를 넘지 않습니다(최대치 증가 카드로만 늘어남).")]
        [Min(1)] public int baseMaxArrows = 10;
        [Header("출혈")]
        [Tooltip("출혈이 이만큼 쌓이면 폭발합니다.")]
        [Min(1)] public int bleedThreshold = 10;
        [Min(0)] public int bleedBurstDamage = 20;
        [Tooltip("연쇄 출혈이 퍼지는 거리")]
        [Min(0)] public float chainBleedRange = 8;

        public override FortressClassRuntime CreateRuntime(IFortressBattle battle, FortressCharacterDefinition definition)
            => new FortressArcherRuntime(this, battle, definition);
    }
}
