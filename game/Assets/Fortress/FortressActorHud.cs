using UnityEngine;
using UnityEngine.UI;

namespace MiniFortress
{
    public sealed class FortressActorHud : MonoBehaviour
    {
        public Image portrait, healthFill, highlight;
        public Text label;
        float targetHealthFill;
        bool healthFillInitialized;

        public void Show(Sprite sprite, string caption, int health, int maximum, bool active)
        {
            if (portrait) { portrait.sprite = sprite; portrait.color = health > 0 ? Color.white : Color.gray; }
            if (label) label.text = caption;
            if (healthFill)
            {
                targetHealthFill = maximum > 0 ? Mathf.Clamp01(health / (float)maximum) : 0;
                if (!healthFillInitialized)
                {
                    healthFill.fillAmount = targetHealthFill;
                    healthFillInitialized = true;
                }
            }
            if (highlight) highlight.enabled = active;
        }

        void Update()
        {
            if (healthFill && healthFillInitialized)
                healthFill.fillAmount = Mathf.MoveTowards(healthFill.fillAmount, targetHealthFill, Time.unscaledDeltaTime * 1.6f);
        }
    }
}
