using UnityEngine;

namespace MiniFortress
{
    public sealed partial class FortressGame
    {
        const int PreviewSteps = 801, FlowLightCount = 8, TrailPointLimit = 64;
        const float TrailFadeDuration = .45f;
        readonly Vector3[] previewPoints = new Vector3[PreviewSteps + 1];
        readonly Vector3[] trailPoints = new Vector3[TrailPointLimit];
        readonly SpriteRenderer[] flowLights = new SpriteRenderer[FlowLightCount];
        readonly Vector3[] ringPoints = new Vector3[49];
        LineRenderer aimLine, aimOutline, landingRing, shotTrail, shotTrailOutline;
        Material trajectoryMaterial, flowingTrajectoryMaterial;
        SpriteRenderer shotGlow;
        int previewCount, trailCount;
        bool guideVisible, predictedImpact, trailRecording;
        float predictedPeak = 43, previewMinimum = 6.9f, trailFade;
        Vector2 predictedPoint;
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
            aimOutline = TrajectoryLine("Aim outline", 23, trajectoryMaterial);
            aimLine = TrajectoryLine("Aim arc", 24, flowingTrajectoryMaterial);
            aimLine.colorGradient = TrajectoryGradient(trajectoryCyan, trajectoryGold, .95f, 1);
            aimOutline.startColor = aimOutline.endColor = trajectoryOutline;
            landingRing = TrajectoryLine("Predicted impact ring", 26, trajectoryMaterial);
            for (int i = 0; i < flowLights.Length; i++)
            {
                Transform light = Shape("Trajectory flow light", Vector2.zero, Vector2.one, Color.white, 25);
                flowLights[i] = light.GetComponent<SpriteRenderer>();
                flowLights[i].sprite = softCircle;
                light.gameObject.SetActive(false);
            }
            shotTrailOutline = TrajectoryLine("Projectile trail outline", 27, trajectoryMaterial);
            shotTrail = TrajectoryLine("Projectile luminous trail", 28, trajectoryMaterial);
            Transform glow = Shape("Projectile glow", Vector2.zero, Vector2.one, Color.white, 29);
            shotGlow = glow.GetComponent<SpriteRenderer>(); shotGlow.sprite = softCircle;
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
            Vector2 point = Origin(0, fighters[0].angle), velocity = Direction(0, fighters[0].angle) * fighters[0].power;
            previewCount = 1; previewPoints[0] = point;
            float age = 0;
            predictedPeak = previewMinimum = point.y;
            predictedImpact = false;
            // Use the very same integration, hit order and lifetime as the real projectile.
            for (int step = 1; step <= PreviewSteps; step++)
            {
                Integrate(ref point, ref velocity);
                age += ShotStep;
                predictedPeak = Mathf.Max(predictedPeak, point.y);
                previewMinimum = Mathf.Min(previewMinimum, point.y);
                predictedImpact = HitFighter(point, 0) >= 0 || HitsTerrain(point);
                bool end = predictedImpact || ShotExpired(point, age) || step == PreviewSteps;
                if (step % 4 == 0 || end) previewPoints[previewCount++] = point;
                if (end) break;
            }
            predictedPoint = point;
            SetLinePoints(aimLine, previewPoints, previewCount);
            SetLinePoints(aimOutline, previewPoints, previewCount);
            landingRing.enabled = predictedImpact;
        }

        static bool ShotExpired(Vector2 point, float age) => point.x < -6 || point.x > 106 || point.y < -13 || age > 10;

        void SetGuideVisible(bool visible)
        {
            aimLine.enabled = aimOutline.enabled = visible;
            if (!visible) landingRing.enabled = false;
            foreach (SpriteRenderer light in flowLights) light.gameObject.SetActive(visible);
        }

        static void SetLinePoints(LineRenderer line, Vector3[] points, int count)
        {
            line.positionCount = count;
            for (int i = 0; i < count; i++) line.SetPosition(i, points[i]);
        }

        void FrameTrajectoryCamera(float blend)
        {
            float low = 6.9f, high = 43;
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
            float height = Mathf.Max(60, (high - low) / .62f);
            float centre = low + .26f * height;
            worldCamera.orthographicSize = Mathf.Lerp(worldCamera.orthographicSize, height * .5f, blend);
            worldCamera.transform.position = Vector3.Lerp(worldCamera.transform.position, new Vector3(50, centre, -30), blend);
        }

        void AnimateTrajectoryEffects(float time)
        {
            // Widths are logical screen pixels, so zooming out never makes the arc disappear.
            float unit = worldCamera.orthographicSize * 2 / 900;
            aimLine.widthMultiplier = 3.5f * unit; aimOutline.widthMultiplier = 7.5f * unit;
            landingRing.widthMultiplier = 3 * unit;
            shotTrail.widthMultiplier = 5 * unit; shotTrailOutline.widthMultiplier = 8 * unit;
            if (guideVisible && previewCount > 1)
            {
                for (int i = 0; i < flowLights.Length; i++)
                {
                    float progress = Mathf.Repeat(time * .32f + i / (float)flowLights.Length, 1);
                    float sample = progress * (previewCount - 1);
                    int first = Mathf.Min((int)sample, previewCount - 2);
                    SpriteRenderer light = flowLights[i];
                    light.transform.position = Vector3.Lerp(previewPoints[first], previewPoints[first + 1], sample - first);
                    light.transform.localScale = Vector3.one * (13 * unit);
                    light.color = Color.Lerp(Color.Lerp(trajectoryCyan, trajectoryGold, progress), Color.white, .7f);
                }
                if (predictedImpact)
                {
                    float radius = (10 + 2 * Mathf.Sin(time * 5)) * unit;
                    for (int i = 0; i < ringPoints.Length; i++)
                    {
                        float radians = i * Mathf.PI * 2 / (ringPoints.Length - 1);
                        ringPoints[i] = predictedPoint + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
                    }
                    landingRing.startColor = landingRing.endColor = Color.Lerp(trajectoryGold, Color.white, .25f + .2f * Mathf.Sin(time * 5));
                    SetLinePoints(landingRing, ringPoints, ringPoints.Length);
                }
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
            trailColor = current > 0 ? new Color(1, .32f, .18f) : IsSpearman ? trajectoryGold : trajectoryCyan;
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
            guideVisible = false; previewCount = trailCount = 0;
            predictedImpact = trailRecording = false; trailFade = 0;
            predictedPeak = 43; previewMinimum = 6.9f;
            SetGuideVisible(false);
            aimLine.positionCount = aimOutline.positionCount = 0;
            shotTrail.positionCount = shotTrailOutline.positionCount = 0;
            shotTrail.enabled = shotTrailOutline.enabled = false;
            shotGlow.gameObject.SetActive(false);
        }
    }
}
