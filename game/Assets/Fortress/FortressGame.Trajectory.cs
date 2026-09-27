using UnityEngine;

namespace MiniFortress
{
    public sealed partial class FortressGame
    {
        // The aim guide is a protractor at the shooter: a 0-90 degree arc with ticks, and a needle whose
        // direction is the angle and whose length is the power. It never shows where the shot will land.
        const int TrailPointLimit = 64, ArcSegments = 32, TickStep = 10;
        const float TrailFadeDuration = .45f, ProtractorScale = 1.3f, MinimumNeedle = .25f;
        readonly Vector3[] trailPoints = new Vector3[TrailPointLimit];
        readonly Vector3[] arcPoints = new Vector3[ArcSegments + 1];
        readonly Vector3[] linePoints = new Vector3[2];
        LineRenderer aimLine, aimOutline, powerTrack, protractorArc, shotTrail, shotTrailOutline;
        LineRenderer[] protractorTicks;
        Material trajectoryMaterial, flowingTrajectoryMaterial;
        SpriteRenderer aimTip, shotGlow;
        int trailCount;
        bool guideVisible, trailRecording;
        float predictedPeak = float.NegativeInfinity, previewMinimum = float.PositiveInfinity, trailFade;
        Vector2 needleTip;
        Color trailColor;
        readonly Color trajectoryCyan = new Color(.15f, 1, .97f);
        readonly Color trajectoryGold = new Color(1, .79f, .18f);
        readonly Color trajectoryOutline = new Color(.015f, .035f, .07f, .85f);

        void BuildTrajectoryEffects()
        {
            var shader = Resources.Load<Shader>("FortressEffects/Trajectory");
            if (shader == null) throw new System.InvalidOperationException("Fortress trajectory shader is missing.");
            trajectoryMaterial = new Material(shader) { name = "Trajectory solid" };
            flowingTrajectoryMaterial = new Material(shader) { name = "Trajectory flowing light" };
            flowingTrajectoryMaterial.SetFloat("_FlowStrength", 1);
            ownedAssets.Add(trajectoryMaterial); ownedAssets.Add(flowingTrajectoryMaterial);
            protractorArc = TrajectoryLine("Aim protractor", 21, trajectoryMaterial);
            protractorArc.startColor = protractorArc.endColor = new Color(1, 1, 1, .45f);
            protractorTicks = new LineRenderer[90 / TickStep + 1];
            for (int i = 0; i < protractorTicks.Length; i++)
            {
                protractorTicks[i] = TrajectoryLine("Protractor tick " + i * TickStep, 21, trajectoryMaterial);
                protractorTicks[i].startColor = protractorTicks[i].endColor = new Color(1, 1, 1, i * TickStep % 30 == 0 ? .8f : .45f);
            }
            powerTrack = TrajectoryLine("Aim power track", 22, trajectoryMaterial);
            powerTrack.startColor = powerTrack.endColor = new Color(1, 1, 1, .22f);
            aimOutline = TrajectoryLine("Aim needle outline", 23, trajectoryMaterial);
            aimLine = TrajectoryLine("Aim needle", 24, flowingTrajectoryMaterial);
            aimLine.colorGradient = TrajectoryGradient(trajectoryCyan, trajectoryGold, .95f, 1);
            aimOutline.startColor = aimOutline.endColor = trajectoryOutline;
            Transform tip = Shape("Aim needle tip", Vector2.zero, Vector2.one, Color.white, 25);
            aimTip = tip.GetComponent<SpriteRenderer>(); aimTip.sprite = arena.effectSprite;
            shotTrailOutline = TrajectoryLine("Projectile trail outline", 27, trajectoryMaterial);
            shotTrail = TrajectoryLine("Projectile luminous trail", 28, trajectoryMaterial);
            Transform glow = Shape("Projectile glow", Vector2.zero, Vector2.one, Color.white, 29);
            shotGlow = glow.GetComponent<SpriteRenderer>(); shotGlow.sprite = arena.effectSprite;
            ClearTrajectoryEffects();
        }

        LineRenderer TrajectoryLine(string name, int order, Material material)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.numCapVertices = 6; line.numCornerVertices = 4;
            line.sortingOrder = order;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.positionCount = 0; line.enabled = false;
            return line;
        }

        static Gradient TrajectoryGradient(Color start, Color end, float startAlpha, float endAlpha)
        {
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(start, 0), new GradientColorKey(end, 1) },
                new[] { new GradientAlphaKey(startAlpha, 0), new GradientAlphaKey(endAlpha, 1) });
            return gradient;
        }

        void UpdateTrajectoryPreview()
        {
            guideVisible = grounded && ((phase == Phase.Aim && !playerHasAttacked) || (phase == Phase.Attack && current == 0));
            SetGuideVisible(guideVisible);
            if (!guideVisible) return;
            var shooter = fighters[0];
            Vector2 pivot = shooter.feet + Vector2.up * (shooter.definition.shoulderHeight * shooter.root.localScale.x);
            float radius = Height(shooter) * ProtractorScale;
            for (int i = 0; i <= ArcSegments; i++) arcPoints[i] = pivot + Direction(0, 90f * i / ArcSegments) * radius;
            SetLinePoints(protractorArc, arcPoints, arcPoints.Length);
            for (int i = 0; i < protractorTicks.Length; i++)
            {
                Vector2 tick = Direction(0, i * TickStep);
                SetSegment(protractorTicks[i], pivot + tick * radius * (i * TickStep % 30 == 0 ? .78f : .88f), pivot + tick * radius);
            }
            Vector2 aim = Direction(0, shooter.angle);
            needleTip = pivot + aim * radius * Mathf.Lerp(MinimumNeedle, 1, ChargeFraction);
            SetSegment(powerTrack, pivot, pivot + aim * radius);
            SetSegment(aimOutline, pivot, needleTip);
            SetSegment(aimLine, pivot, needleTip);
            // Only the protractor needs to stay framed; the camera no longer chases a predicted arc.
            predictedPeak = pivot.y + radius; previewMinimum = shooter.feet.y;
        }

        bool ShotExpired(Vector2 point, float age) => ShotOutside(point) || age > arena.rules.shotLifetime;

        void SetGuideVisible(bool visible)
        {
            aimLine.enabled = aimOutline.enabled = powerTrack.enabled = protractorArc.enabled = visible;
            foreach (var tick in protractorTicks) tick.enabled = visible;
            aimTip.gameObject.SetActive(visible);
        }

        void SetSegment(LineRenderer line, Vector2 from, Vector2 to)
        {
            linePoints[0] = from; linePoints[1] = to;
            SetLinePoints(line, linePoints, 2);
        }

        static void SetLinePoints(LineRenderer line, Vector3[] points, int count)
        {
            line.positionCount = count;
            for (int i = 0; i < count; i++) line.SetPosition(i, points[i]);
        }

        void FrameTrajectoryCamera(float blend)
        {
            // Resting frame is the scene camera; the arc band spans 24%..86% of the view height.
            float homeHeight = cameraSize * 2;
            float low = cameraHome.y - .26f * homeHeight, high = low + .62f * homeHeight;
            // Keep the preview framing through draw, release and impact to avoid a zoom pop.
            if (guideVisible || (current == 0 && (phase == Phase.Attack || phase == Phase.Flight || phase == Phase.Impact)))
            {
                low = Mathf.Min(low, previewMinimum);
                high = Mathf.Max(high, predictedPeak);
            }
            if (phase == Phase.Flight)
            {
                high = Mathf.Max(high, shotPosition.y);
                low = Mathf.Min(low, shotPosition.y);
            }
            // Reserve the top turn bar and bottom control panels when fitting the arc.
            float height = Mathf.Max(homeHeight, (high - low) / .62f);
            float centre = low + .26f * height;
            worldCamera.orthographicSize = Mathf.Lerp(worldCamera.orthographicSize, height * .5f, blend);
            worldCamera.transform.position = Vector3.Lerp(worldCamera.transform.position, new Vector3(cameraHome.x, centre, cameraHome.z), blend);
        }

        void AnimateTrajectoryEffects(float time)
        {
            // Widths are logical screen pixels, so zooming out never makes the arc disappear.
            float unit = worldCamera.orthographicSize * 2 / 900;
            aimLine.widthMultiplier = 4 * unit; aimOutline.widthMultiplier = 8 * unit; powerTrack.widthMultiplier = 4 * unit;
            protractorArc.widthMultiplier = 2 * unit;
            foreach (var tick in protractorTicks) tick.widthMultiplier = 2 * unit;
            shotTrail.widthMultiplier = 5 * unit; shotTrailOutline.widthMultiplier = 8 * unit;
            if (guideVisible)
            {
                aimTip.transform.position = needleTip;
                aimTip.transform.localScale = Vector3.one * ((12 + 2 * Mathf.Sin(time * 6)) * unit);
                aimTip.color = Color.Lerp(trajectoryGold, Color.white, .5f);
            }
            if (shotGlow.gameObject.activeSelf)
            {
                shotGlow.transform.position = shotPosition;
                shotGlow.transform.localScale = Vector3.one * (19 + 3 * Mathf.Sin(time * 22)) * unit;
            }
        }

        void BeginShotTrail()
        {
            guideVisible = false; SetGuideVisible(false);
            trailCount = 0; trailFade = 0; trailRecording = true;
            trailColor = current > 0 ? new Color(1, .32f, .18f) : fighters[0].definition.weapon == FortressWeapon.Spear ? trajectoryGold : trajectoryCyan;
            shotTrail.colorGradient = TrajectoryGradient(trailColor, Color.Lerp(trailColor, Color.white, .7f), 0, 1);
            shotTrailOutline.colorGradient = TrajectoryGradient(trajectoryOutline, trajectoryOutline, 0, .8f);
            shotTrail.widthCurve = shotTrailOutline.widthCurve = AnimationCurve.Linear(0, .12f, 1, 1);
            shotGlow.color = new Color(trailColor.r, trailColor.g, trailColor.b, .85f);
            shotGlow.gameObject.SetActive(true);
            RecordShotTrail(shotPosition);
        }

        void RecordShotTrail(Vector2 point)
        {
            if (!trailRecording) return;
            if (trailCount == trailPoints.Length)
            {
                System.Array.Copy(trailPoints, 1, trailPoints, 0, trailPoints.Length - 1);
                trailCount--;
            }
            trailPoints[trailCount++] = point;
            SetLinePoints(shotTrail, trailPoints, trailCount);
            SetLinePoints(shotTrailOutline, trailPoints, trailCount);
            shotTrail.enabled = shotTrailOutline.enabled = trailCount > 1;
        }

        void StopShotTrail()
        {
            trailRecording = false; trailFade = TrailFadeDuration;
            shotGlow.gameObject.SetActive(false);
        }

        void UpdateShotTrail(float dt)
        {
            if (trailRecording || trailFade <= 0) return;
            trailFade = Mathf.Max(0, trailFade - dt);
            float opacity = trailFade / TrailFadeDuration;
            shotTrail.startColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0);
            Color head = Color.Lerp(trailColor, Color.white, .7f); head.a = opacity;
            shotTrail.endColor = head;
            Color outline = trajectoryOutline; outline.a = .8f * opacity;
            shotTrailOutline.endColor = outline;
            if (trailFade == 0) shotTrail.enabled = shotTrailOutline.enabled = false;
        }

        void ClearTrajectoryEffects()
        {
            guideVisible = false; trailCount = 0;
            trailRecording = false; trailFade = 0;
            predictedPeak = float.NegativeInfinity; previewMinimum = float.PositiveInfinity;
            SetGuideVisible(false);
            aimLine.positionCount = aimOutline.positionCount = powerTrack.positionCount = 0;
            shotTrail.positionCount = shotTrailOutline.positionCount = 0;
            shotTrail.enabled = shotTrailOutline.enabled = false;
            shotGlow.gameObject.SetActive(false);
        }
    }
}
