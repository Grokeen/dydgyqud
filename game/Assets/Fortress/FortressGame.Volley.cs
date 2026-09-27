using System.Collections.Generic;
using UnityEngine;

namespace MiniFortress
{
    // Extra arrows from multi-shot cards leave right behind the first one (volleyInterval apart) and fly at the
    // same time, instead of each waiting for the previous arrow to land. The turn resolves once all have landed.
    public sealed partial class FortressGame
    {
        sealed class VolleyArrow
        {
            public Transform view;
            public Vector2 position, velocity;
            public float angle, delay, age, accumulator;
            public bool launched, critical;
            public ShotMods mods;
        }
        readonly List<VolleyArrow> volleyArrows = new List<VolleyArrow>();
        readonly List<(Transform view, float time)> volleyPuffs = new List<(Transform, float)>();
        const float PuffDuration = .35f;

        bool VolleyInFlight => volleyArrows.Count > 0;

        void QueueVolley()
        {
            for (int shot = 1; shot <= volleyRemaining; shot++)
                volleyArrows.Add(new VolleyArrow { angle = fighters[0].angle + VolleyAngleOffset(shot), delay = shot * arena.rules.volleyInterval });
            volleyRemaining = 0;
        }

        void UpdateVolley(float dt)
        {
            for (int i = volleyArrows.Count - 1; i >= 0; i--)
            {
                var shot = volleyArrows[i];
                if (!shot.launched)
                {
                    shot.delay -= dt;
                    if (shot.delay > 0) continue;
                    LaunchVolleyArrow(shot);
                }
                shot.accumulator += dt;
                bool done = false;
                while (shot.accumulator >= ShotStep && !done)
                {
                    shot.accumulator -= ShotStep; Integrate(ref shot.position, ref shot.velocity); shot.age += ShotStep;
                    int hit = HitFighter(shot.position, 0);
                    if (hit >= 0 || HitsTerrain(shot.position))
                    {
                        hitNote = "";
                        int blocked = 0, total = ApplyBlast(0, shot.position, hit, shot.critical, ref blocked, shot.mods);
                        if (total > 0) message = (shot.critical ? "치명타! " : "") + "연사 명중! 피해 " + total + hitNote;
                        else PlayerArrowMissed(shot.position, shot.velocity, true);
                        Puff(shot.position);
                        done = true;
                    }
                    else if (ShotExpired(shot.position, shot.age)) { PlayerArrowMissed(shot.position, shot.velocity, false); done = true; }
                }
                shot.view.position = shot.position;
                shot.view.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(shot.velocity.y, shot.velocity.x) * Mathf.Rad2Deg);
                if (done) { Destroy(shot.view.gameObject); volleyArrows.RemoveAt(i); }
            }
            for (int i = volleyPuffs.Count - 1; i >= 0; i--)
            {
                var (view, time) = volleyPuffs[i];
                time -= dt;
                if (time <= 0) { Destroy(view.gameObject); volleyPuffs.RemoveAt(i); continue; }
                float t = time / PuffDuration;
                view.localScale = Vector3.one * Mathf.Lerp(fighters[0].definition.blastRadius * 1.6f, .4f, t);
                view.GetComponent<SpriteRenderer>().color = new Color(1, .6f, .2f, .7f * t);
                volleyPuffs[i] = (view, time);
            }
        }

        void LaunchVolleyArrow(VolleyArrow shot)
        {
            var player = fighters[0];
            shot.launched = true;
            shot.position = Origin(0, shot.angle);
            shot.velocity = Direction(0, shot.angle) * player.power;
            shot.critical = Random.Range(0, 100) < shotCriticalChance;
            shot.mods = NextArrowMods();
            shot.view = ArtObject(transform, "Volley arrow", player.definition.projectile, shot.position, 30);
            shot.view.localScale = Vector3.one * player.root.localScale.x;
        }

        void Puff(Vector2 at)
        {
            var view = ArtObject(transform, "Volley impact", arena.effectSprite, at, 31);
            volleyPuffs.Add((view, PuffDuration));
        }

        void ClearVolley()
        {
            foreach (var shot in volleyArrows) if (shot.view) Destroy(shot.view.gameObject);
            foreach (var (view, _) in volleyPuffs) if (view) Destroy(view.gameObject);
            volleyArrows.Clear(); volleyPuffs.Clear();
            ClearFallenArrows();
        }
    }
}
