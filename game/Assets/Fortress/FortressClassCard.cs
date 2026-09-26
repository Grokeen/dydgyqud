using UnityEngine;
using UnityEngine.UI;

namespace MiniFortress
{
    public sealed class FortressClassCard : MonoBehaviour
    {
        public Image portrait, border;
        public Text title, stats, description;
        public Button button;
        public Color selectedColor = new Color(.09f, .2f, .25f);
        public Color normalColor = new Color(.035f, .07f, .095f);
        public void Show(FortressCharacterDefinition definition, bool selected)
        {
            portrait.sprite = definition.portrait;
            title.text = definition.displayName + (selected ? " · 선택됨" : "");
            stats.text = $"체력 {definition.health}\n직격 피해 {definition.damage}\n범위 {definition.blastRadius:0.#} m\n이동 {definition.movementPerTurn:0.#} m / 턴";
            description.text = definition.description;
            border.color = selected ? selectedColor : normalColor;
            var outline = border.GetComponent<Outline>();
            if (outline) outline.effectColor = selected ? new Color(.8f, .65f, .3f) : new Color(.25f, .35f, .4f);
        }
    }
}
