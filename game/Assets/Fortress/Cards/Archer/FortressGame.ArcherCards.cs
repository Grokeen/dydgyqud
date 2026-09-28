namespace MiniFortress
{
    // Archer card effects. Bleed effects live in FortressGame.Bleed.cs, quiver/recovery effects in
    // FortressGame.Arrows.cs (both in this folder). Returns false for effects the archer does not handle.
    public sealed partial class FortressGame
    {
        bool PlayArcherCard(FortressCardEntry card) => PlayBleedCard(card) || PlayArrowCard(card);
    }
}
