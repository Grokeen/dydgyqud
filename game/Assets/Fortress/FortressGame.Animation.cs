using UnityEngine;

namespace MiniFortress
{
    public sealed partial class FortressGame
    {
        // Keep the terminal death pose visible before hiding the fighter.
        const float DeathVisualDuration = 0.9f;
        static readonly int SpeedParameter = Animator.StringToHash("Speed");
        static readonly int GroundedParameter = Animator.StringToHash("Grounded");
        static readonly int VerticalSpeedParameter = Animator.StringToHash("VerticalSpeed");
        static readonly int AimingParameter = Animator.StringToHash("Aiming");
        static readonly int DeadParameter = Animator.StringToHash("Dead");
        static readonly int BowAttackParameter = Animator.StringToHash("BowAttack");
        static readonly int SpearAttackParameter = Animator.StringToHash("SpearAttack");
        static readonly int HitParameter = Animator.StringToHash("Hit");
        static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
        // Every gameplay animation parameter goes to the prefab's Animator and, when the character uses its own
        // 3D model with an Animator, to that model as well (FortressFighterModel skips parameters it lacks).
        static void AnimFloat(Fighter f, int id, float value) { f.animator.SetFloat(id, value); if (f.model) f.model.SetFloat(id, value); }
        static void AnimBool(Fighter f, int id, bool value) { f.animator.SetBool(id, value); if (f.model) f.model.SetBool(id, value); }
        static void AnimTrigger(Fighter f, int id) { f.animator.SetTrigger(id); if (f.model) f.model.SetTrigger(id); }
        static void AnimResetTrigger(Fighter f, int id) { f.animator.ResetTrigger(id); if (f.model) f.model.ResetTrigger(id); }

        void UpdateFighterAnimation(int index, float dt)
        {
            Fighter fighter = fighters[index];
            if (fighter.hp <= 0)
            {
                // Keep the visual alive long enough for the terminal Death state to finish.
                if (dt > 0) fighter.deathRemaining = Mathf.Max(0, fighter.deathRemaining - Time.deltaTime);
                fighter.root.gameObject.SetActive(fighter.deathRemaining > 0);
                return;
            }
            fighter.root.gameObject.SetActive(true);
            float speed = dt > 0 ? Mathf.Abs(fighter.feet.x - fighter.previousFeet.x) / dt : 0;
            fighter.previousFeet = fighter.feet;
            bool onGround = index != 0 || grounded;
            AnimFloat(fighter, SpeedParameter, speed);
            AnimBool(fighter, GroundedParameter, onGround);
            AnimFloat(fighter, VerticalSpeedParameter, index == 0 ? fallSpeed : 0);
            AnimBool(fighter, AimingParameter, current == index &&
                ((phase == Phase.Aim && !playerHasAttacked) || phase == Phase.EnemyAim));
        }

        void PlayDamageReaction(Fighter fighter)
        {
            if (fighter.hp <= 0)
            {
                fighter.deathRemaining = DeathVisualDuration;
                AnimResetTrigger(fighter, BowAttackParameter);
                AnimResetTrigger(fighter, SpearAttackParameter);
                AnimResetTrigger(fighter, HitParameter);
                AnimBool(fighter, DeadParameter, true);
            }
            else AnimTrigger(fighter, HitParameter);
        }

        void ResetFighterAnimation(Fighter fighter)
        {
            fighter.root.gameObject.SetActive(true);
            fighter.deathRemaining = 0;
            fighter.previousFeet = fighter.feet;
            fighter.animator.Rebind();
            AnimFloat(fighter, SpeedParameter, 0);
            AnimFloat(fighter, VerticalSpeedParameter, 0);
            AnimBool(fighter, GroundedParameter, true);
            AnimBool(fighter, AimingParameter, false);
            AnimBool(fighter, DeadParameter, false);
            AnimResetTrigger(fighter, BowAttackParameter);
            AnimResetTrigger(fighter, SpearAttackParameter);
            AnimResetTrigger(fighter, HitParameter);
            fighter.animator.Play(IdleState, 0, 0);
            fighter.animator.Update(0);
            if (fighter.model) fighter.model.ResetAnimation();
        }
    }
}
