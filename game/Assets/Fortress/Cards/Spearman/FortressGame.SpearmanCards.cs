namespace MiniFortress
{
    // Spearman card effects. The spearman's current cards use only the shared effects (Cards/Common);
    // add spear-only effects here. Returns false for effects the spearman does not handle.
    public sealed partial class FortressGame
    {
        bool PlaySpearmanCard(FortressCardEntry card)
        {
            switch (card.effect)
            {
                default: return false;
            }
        }
    }
}
