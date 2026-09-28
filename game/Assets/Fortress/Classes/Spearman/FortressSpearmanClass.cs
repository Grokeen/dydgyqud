using UnityEngine;

namespace MiniFortress
{
    // Spearman tuning (Assets/FortressContent/Classes/Spearman/Spearman Class.asset). The spearman has no
    // class-specific mechanics yet: one spear per attack plus shared card extras, and only shared card effects.
    // Add spear-only settings here and spear-only state and card effects in FortressSpearmanRuntime.
    [CreateAssetMenu(menuName = "Mini Fortress/Classes/Spearman", fileName = "Spearman Class")]
    public sealed class FortressSpearmanClass : FortressClassBehaviour
    {
        public override FortressClassRuntime CreateRuntime(IFortressBattle battle, FortressCharacterDefinition definition)
            => new FortressSpearmanRuntime(battle, definition);
    }
}
