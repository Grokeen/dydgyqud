using UnityEngine;

namespace MiniFortress
{
    public sealed partial class FortressGame
    {
        Transform Shape(string name, Vector2 position, Vector2 size, Color color, int order)
        {
            var item = ArtObject(transform, name, square, position, order);
            item.localScale = new Vector3(size.x, size.y, 1);
            item.GetComponent<SpriteRenderer>().color = color;
            return item;
        }
        Transform ArtObject(Transform parent, string name, Sprite sprite, Vector2 position, int order)
        {
            var item = new GameObject(name); item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            var renderer = item.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingOrder = order;
            return item.transform;
        }
        Fighter CreateFighter(FortressCharacterDefinition definition, Vector2 feet, string displayName)
        {
            var view = Instantiate(definition.prefab, feet, Quaternion.identity, transform);
            view.name = string.IsNullOrWhiteSpace(displayName) ? definition.displayName : displayName;
            return new Fighter {
                definition = definition, name = view.name, feet = feet, previousFeet = feet,
                hp = definition.health, maxHp = definition.health, root = view.transform,
                motion = view.motion, aimPivot = view.aimPivot, weaponMotion = view.weaponMotion,
                weapon = view.weapon, loadedArrow = view.loadedProjectile, body = view.body, animator = view.animator
            };
        }
        void AddFighter(FortressCharacterDefinition definition, Vector2 feet, string displayName)
            => fighters.Add(CreateFighter(definition, feet, displayName));
        public void OpenSelection()
        {
            if (fighters.Count == 0) return;
            phase = Phase.Selecting; highlightedClass = playerClass; mouseMove = 0;
            arrow.gameObject.SetActive(false); burst.gameObject.SetActive(false);
            foreach (var fighter in fighters) fighter.root.gameObject.SetActive(false);
            foreach (var dot in guide) dot.gameObject.SetActive(false);
        }
        public void SelectClass(int index)
        {
            if (phase == Phase.Selecting && index >= 0 && index < arena.playerClasses.Length) highlightedClass = index;
        }
        public void BeginBattle()
        {
            if (phase != Phase.Selecting) return;
            playerClass = highlightedClass;
            var previous = fighters[0].root.gameObject; previous.SetActive(false); Destroy(previous);
            fighters[0] = CreateFighter(arena.playerClasses[playerClass], starts[0], null);
            Restart();
            if (EnemiesAlive() == 0) Finish(true);
        }
    }
}
