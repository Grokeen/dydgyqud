using UnityEngine;
using UnityEngine.UI;

namespace MiniFortress
{
    public sealed class FortressActorHud : MonoBehaviour
    {
        public Image portrait, healthFill, highlight;
        public Text label;
        public void Show(Sprite sprite, string caption, int health, int maximum, bool active)
        {
            if (portrait) { portrait.sprite = sprite; portrait.color = health > 0 ? Color.white : Color.gray; }
            if (label) label.text = caption;
            if (healthFill) healthFill.fillAmount = maximum > 0 ? health / (float)maximum : 0;
            if (highlight) highlight.enabled = active;
        }
    }
}
