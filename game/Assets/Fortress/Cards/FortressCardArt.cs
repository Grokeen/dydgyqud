using System.Collections.Generic;
using UnityEngine;

namespace MiniFortress
{
    // Card pictures. A card's own `art` wins; otherwise an image in Resources/FortressCardArt/<class art folder>
    // named after the card title (e.g. Archer/방어.png), then after its effect (e.g. Archer/Defense.png).
    // Each class has its own folder, so same-named cards of different classes keep different art.
    // Images imported as plain textures work too. Returns null when nothing matches; the card shows no picture.
    public static class FortressCardArt
    {
        const string Root = "FortressCardArt/";
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite For(FortressCardEntry card, FortressCardSet set)
        {
            if (card == null) return null;
            if (card.art) return card.art;
            if (!set || string.IsNullOrWhiteSpace(set.artFolder)) return null;
            string folder = Root + set.artFolder.Trim('/') + "/";
            return Find(folder + card.title) ?? Find(folder + card.effect);
        }

        static Sprite Find(string path)
        {
            if (cache.TryGetValue(path, out var sprite)) return sprite ? sprite : null;
            sprite = Resources.Load<Sprite>(path);
            if (!sprite)
            {
                var texture = Resources.Load<Texture2D>(path);
                if (texture) sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100f);
            }
            cache[path] = sprite;
            return sprite ? sprite : null;
        }
    }
}
