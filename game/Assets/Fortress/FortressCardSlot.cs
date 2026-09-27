using UnityEngine;
using UnityEngine.UI;

namespace MiniFortress
{
    // One card in the hand: cost badge, title, art area and description, all editable in the scene.
    public sealed class FortressCardSlot : MonoBehaviour
    {
        public Button button;
        public Text cost, title, description;
        public Image artwork;

        Color costColor, titleColor, descriptionColor;
        bool colorsCached;
        Image[] frameEdges;

        public void Show(string cardTitle, int cardCost, string text, bool playable, Sprite cardArtwork = null)
        {
            if (!artwork && button) artwork = button.transform.Find("Art (drop a sprite here)")?.GetComponent<Image>();
            CacheColors();
            FortressUiFrame.Ensure(ref frameEdges, button.transform, "Card Frame");
            FortressUiFrame.Set(frameEdges, playable ? new Color(.24f, .58f, .64f, .95f) : new Color(.22f, .27f, .3f, .85f), 2.5f);

            if (cost) cost.text = cardCost.ToString();
            if (title) title.text = cardTitle;
            if (description) description.text = text;
            if (artwork)
            {
                artwork.sprite = cardArtwork;
                artwork.enabled = cardArtwork;
            }
            button.interactable = playable;

            SetTextColor(cost, costColor, playable);
            SetTextColor(title, titleColor, playable);
            SetTextColor(description, descriptionColor, playable);
        }

        void CacheColors()
        {
            if (colorsCached) return;
            if (cost) costColor = cost.color;
            if (title) titleColor = title.color;
            if (description) descriptionColor = description.color;
            colorsCached = true;
        }

        static void SetTextColor(Text text, Color original, bool playable)
        {
            if (!text) return;
            text.color = playable ? original : new Color(original.r * 0.7f, original.g * 0.7f, original.b * 0.7f, original.a);
        }
    }

    internal static class FortressUiFrame
    {
        static readonly string[] EdgeNames = { "Top", "Bottom", "Left", "Right" };

        public static void Ensure(ref Image[] edges, Transform parent, string name)
        {
            if (edges != null && edges.Length == EdgeNames.Length && edges[0]) return;

            edges = new Image[EdgeNames.Length];
            for (int i = 0; i < edges.Length; i++)
            {
                var item = new GameObject(name + " " + EdgeNames[i], typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                item.transform.SetParent(parent, false);
                var rect = (RectTransform)item.transform;
                rect.pivot = i < 2 ? new Vector2(.5f, i == 0 ? 1 : 0) : new Vector2(i == 2 ? 0 : 1, .5f);
                if (i == 0) { rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1); }
                else if (i == 1) { rect.anchorMin = rect.anchorMax = Vector2.zero; rect.anchorMax = new Vector2(1, 0); }
                else if (i == 2) { rect.anchorMin = rect.anchorMax = Vector2.zero; rect.anchorMax = new Vector2(0, 1); }
                else { rect.anchorMin = rect.anchorMax = new Vector2(1, 0); rect.anchorMax = Vector2.one; }
                rect.anchoredPosition = Vector2.zero;
                var image = item.GetComponent<Image>(); image.raycastTarget = false;
                edges[i] = image;
            }
        }

        public static void Set(Image[] edges, Color color, float thickness)
        {
            if (edges == null) return;
            for (int i = 0; i < edges.Length; i++)
            {
                if (!edges[i]) continue;
                edges[i].color = color;
                var rect = (RectTransform)edges[i].transform;
                rect.sizeDelta = i < 2 ? new Vector2(0, thickness) : new Vector2(thickness, 0);
            }
        }
    }
}
