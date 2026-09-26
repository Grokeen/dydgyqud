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
            fighter.animator.SetFloat(SpeedParameter, speed);
            fighter.animator.SetBool(GroundedParameter, onGround);
            fighter.animator.SetFloat(VerticalSpeedParameter, index == 0 ? fallSpeed : 0);
            fighter.animator.SetBool(AimingParameter, current == index &&
                ((phase == Phase.Aim && !playerHasAttacked) || phase == Phase.EnemyAim));
        }

        void PlayDamageReaction(Fighter fighter)
        {
            if (fighter.hp <= 0)
            {
                fighter.deathRemaining = DeathVisualDuration;
                fighter.animator.ResetTrigger(BowAttackParameter);
                fighter.animator.ResetTrigger(SpearAttackParameter);
                fighter.animator.ResetTrigger(HitParameter);
                fighter.animator.SetBool(DeadParameter, true);
            }
            else fighter.animator.SetTrigger(HitParameter);
        }

        void ResetFighterAnimation(Fighter fighter)
        {
            fighter.root.gameObject.SetActive(true);
            fighter.deathRemaining = 0;
            fighter.previousFeet = fighter.feet;
            fighter.animator.Rebind();
            fighter.animator.SetFloat(SpeedParameter, 0);
            fighter.animator.SetFloat(VerticalSpeedParameter, 0);
            fighter.animator.SetBool(GroundedParameter, true);
            fighter.animator.SetBool(AimingParameter, false);
            fighter.animator.SetBool(DeadParameter, false);
            fighter.animator.ResetTrigger(BowAttackParameter);
            fighter.animator.ResetTrigger(SpearAttackParameter);
            fighter.animator.ResetTrigger(HitParameter);
            fighter.animator.Play(IdleState, 0, 0);
            fighter.animator.Update(0);
        }
    }
}
