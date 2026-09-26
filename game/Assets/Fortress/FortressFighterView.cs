using UnityEngine;

namespace MiniFortress
{
    public sealed class FortressFighterView : MonoBehaviour
    {
        public Transform motion, aimPivot, weaponMotion, weapon, loadedProjectile;
        public SpriteRenderer body;
        public Animator animator;
        public bool IsConfigured => motion && aimPivot && weaponMotion && weapon && loadedProjectile && body && animator && animator.runtimeAnimatorController;
    }
}
