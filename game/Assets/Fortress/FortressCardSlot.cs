using UnityEngine;
using UnityEngine.UI;

namespace MiniFortress
{
    // One card in the hand: cost badge, title, art area and description, all editable in the scene.
    public sealed class FortressCardSlot : MonoBehaviour
    {
        public Button button;
        public Text cost, title, description;
        public void Show(string cardTitle, int cardCost, string text, bool playable)
        {
            if (cost) cost.text = cardCost.ToString();
            if (title) title.text = cardTitle;
            if (description) description.text = text;
            button.interactable = playable;
        }
    }
}
