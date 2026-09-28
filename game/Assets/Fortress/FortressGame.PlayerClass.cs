using UnityEngine;

namespace MiniFortress
{
    // The bridge between the shared battle and the player's class module (Assets/Fortress/Classes). The battle calls
    // PlayerClass at fixed moments and never names a class; the class reaches back only through IFortressBattle.
    public sealed partial class FortressGame : IFortressBattle
    {
        FortressClassRuntime playerClassRuntime;

        // Created for the player's current character; replaced when a different class is picked.
        FortressClassRuntime PlayerClass
        {
            get
            {
                var definition = fighters[0].definition;
                if (playerClassRuntime == null || playerClassRuntime.Definition != definition)
                {
                    playerClassRuntime?.ClearVisuals();
                    playerClassRuntime = definition.ClassBehaviour.CreateRuntime(this, definition);
                }
                return playerClassRuntime;
            }
        }

        int IFortressBattle.FighterCount => fighters.Count;
        int IFortressBattle.Round => round;
        FortressCharacterDefinition IFortressBattle.PlayerDefinition => fighters[0].definition;
        Vector2 IFortressBattle.FighterFeet(int index) => fighters[index].feet;
        int IFortressBattle.FighterHp(int index) => fighters[index].hp;
        int IFortressBattle.FighterMaxHp(int index) => fighters[index].maxHp;
        string IFortressBattle.FighterName(int index) => fighters[index].name;
        void IFortressBattle.DrawCards(int count) => DrawCards(count);
        void IFortressBattle.AddNextAttackDamage(int amount) => bonusDamage += amount;
        void IFortressBattle.AddNextAttackShots(int amount) => bonusShots += amount;
        void IFortressBattle.AddHitNote(string note) => hitNote += note;

        void IFortressBattle.DamageFighter(int index, int amount)
        {
            var f = fighters[index];
            if (amount <= 0 || f.hp <= 0) return;
            f.hp = Mathf.Max(0, f.hp - amount); PlayDamageReaction(f);
        }

        Transform IFortressBattle.SpawnProjectileView(string name, Vector2 position, float angleDegrees)
        {
            var view = ProjectileObject(name, fighters[0].definition, position);
            view.rotation = Quaternion.Euler(0, 0, angleDegrees);
            view.localScale = Vector3.one * fighters[0].root.localScale.x;
            return view;
        }
    }
}
