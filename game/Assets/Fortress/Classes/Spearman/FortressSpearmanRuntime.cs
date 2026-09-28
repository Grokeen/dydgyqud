namespace MiniFortress
{
    // The spearman's per-battle state. Every hook keeps the shared default for now.
    public sealed class FortressSpearmanRuntime : FortressClassRuntime
    {
        public FortressSpearmanRuntime(IFortressBattle battle, FortressCharacterDefinition definition) : base(battle, definition) { }

        // Spear-only card effects go here; return false for the rest.
        public override bool PlayCard(FortressCardEntry card) => false;
    }
}
