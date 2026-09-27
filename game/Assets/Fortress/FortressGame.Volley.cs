using System.Collections.Generic;
using UnityEngine;

namespace MiniFortress
{
    // Extra arrows follow the first one volleyInterval apart, each with a small random wobble in angle and power
    // so they scatter a little around the aimed spot. They fly at the same time instead of each waiting for the
    // previous arrow to land, and the turn resolves once all have landed.
    public sealed partial class FortressGame
    {
        sealed class VolleyArrow
        {
            public Transform view;
            public Vector2 position, velocity;
            public float angle, delay, age, accumulator;
            public bool launched, critical;
            public ShotMods mods;
            public VolleyTrail trail;
        }
        // The same glowing tail and head the first arrow gets, one per extra arrow; the tail fades after landing.
        sealed class VolleyTrail
        {
            public LineRenderer line, outline;
            public SpriteRenderer glow;
            public readonly Vector3[] points = new Vector3[TrailPointLimit];
            public int count;
            public float fade;
        }
        readonly List<VolleyTrail> fadingTrails = new List<VolleyTrail>();
        readonly List<VolleyArrow> volleyArrows = new List<VolleyArrow>();
        readonly List<(Transform view, float time)> volleyPuffs = new List<(Transform, float)>();
        const float PuffDuration = .35f;

        bool VolleyInFlight => volleyArrows.Count > 0;
        bool VolleyEffectsActive => volleyArrows.Count > 0 || volleyPuffs.Count > 0 || fadingTrails.Count > 0;

        void QueueVolley()
        {
            for (int shot = 1; shot <= volleyRemaining; shot++)
                volleyArrows.Add(new VolleyArrow { angle = fighters[0].angle, delay = shot * arena.rules.volleyInterval });
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
                    RecordTrail(shot.trail, shot.position);
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
                shot.trail.glow.transform.position = shot.position;
                if (done)
                {
                    Destroy(shot.view.gameObject); Destroy(shot.trail.glow.gameObject);
                    shot.trail.fade = TrailFadeDuration; fadingTrails.Add(shot.trail);
                    volleyArrows.RemoveAt(i);
                }
            }
            UpdateVolleyTrails(dt);
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
            float wobble = arena.rules.volleyAngleJitter, spread = arena.rules.volleyPowerJitter;
            shot.angle = Mathf.Clamp(shot.angle + Random.Range(-wobble, wobble), Rules.angleLimits.x, Rules.angleLimits.y);
            shot.position = Origin(0, shot.angle);
            shot.velocity = Direction(0, shot.angle) * player.power * (1 + Random.Range(-spread, spread));
            shot.critical = Random.Range(0, 100) < shotCriticalChance;
            shot.mods = NextArrowMods();
            shot.view = ArtObject(transform, "Volley arrow", player.definition.projectile, shot.position, 30);
            shot.view.localScale = Vector3.one * player.root.localScale.x;
            shot.trail = CreateVolleyTrail();
            RecordTrail(shot.trail, shot.position);
        }

        VolleyTrail CreateVolleyTrail()
        {
            // Same colours as the first arrow's trail (BeginShotTrail) for the player's weapon.
            Color color = fighters[0].definition.weapon == FortressWeapon.Spear ? trajectoryGold : trajectoryCyan;
            var trail = new VolleyTrail
            {
                outline = TrajectoryLine("Volley trail outline", 27, trajectoryMaterial),
                line = TrajectoryLine("Volley trail", 28, trajectoryMaterial),
                glow = ArtObject(transform, "Volley glow", arena.effectSprite, Vector2.zero, 29).GetComponent<SpriteRenderer>()
            };
            trail.line.colorGradient = TrajectoryGradient(color, Color.Lerp(color, Color.white, .7f), 0, 1);
            trail.outline.colorGradient = TrajectoryGradient(trajectoryOutline, trajectoryOutline, 0, .8f);
            trail.line.widthCurve = trail.outline.widthCurve = AnimationCurve.Linear(0, .12f, 1, 1);
            trail.glow.color = new Color(color.r, color.g, color.b, .85f);
            return trail;
        }

        static void RecordTrail(VolleyTrail trail, Vector2 point)
        {
            if (trail.count == trail.points.Length)
            {
                System.Array.Copy(trail.points, 1, trail.points, 0, trail.points.Length - 1);
                trail.count--;
            }
            trail.points[trail.count++] = point;
            SetLinePoints(trail.line, trail.points, trail.count);
            SetLinePoints(trail.outline, trail.points, trail.count);
            trail.line.enabled = trail.outline.enabled = trail.count > 1;
        }

        // Widths follow the camera like the first arrow's; landed tails fade out and are removed.
        void UpdateVolleyTrails(float dt)
        {
            float unit = worldCamera.orthographicSize * 2 / 900;
            foreach (var shot in volleyArrows)
            {
                if (!shot.launched) continue;
                shot.trail.line.widthMultiplier = 5 * unit; shot.trail.outline.widthMultiplier = 8 * unit;
                shot.trail.glow.transform.localScale = Vector3.one * (19 + 3 * Mathf.Sin(Time.time * 22)) * unit;
            }
            for (int i = fadingTrails.Count - 1; i >= 0; i--)
            {
                var trail = fadingTrails[i];
                trail.fade = Mathf.Max(0, trail.fade - dt);
                float opacity = trail.fade / TrailFadeDuration;
                Color head = trail.line.endColor; head.a = opacity; trail.line.endColor = head;
                Color outline = trajectoryOutline; outline.a = .8f * opacity; trail.outline.endColor = outline;
                if (trail.fade > 0) continue;
                DestroyTrail(trail); fadingTrails.RemoveAt(i);
            }
        }

        static void DestroyTrail(VolleyTrail trail)
        {
            if (trail.line) Destroy(trail.line.gameObject);
            if (trail.outline) Destroy(trail.outline.gameObject);
            if (trail.glow) Destroy(trail.glow.gameObject);
        }

        void Puff(Vector2 at)
        {
            var view = ArtObject(transform, "Volley impact", arena.effectSprite, at, 31);
            volleyPuffs.Add((view, PuffDuration));
        }

        void ClearVolley()
        {
            foreach (var shot in volleyArrows) { if (shot.view) Destroy(shot.view.gameObject); if (shot.trail != null) DestroyTrail(shot.trail); }
            foreach (var trail in fadingTrails) DestroyTrail(trail);
            fadingTrails.Clear();
            foreach (var (view, _) in volleyPuffs) if (view) Destroy(view.gameObject);
            volleyArrows.Clear(); volleyPuffs.Clear();
            ClearFallenArrows();
        }
    }
}
