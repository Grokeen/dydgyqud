using UnityEngine;
using UnityEngine.Rendering;

namespace MiniFortress
{
    // Oblique orthographic projection for the 2.5D look. Points on the gameplay plane (z = planeZ) project exactly
    // as with the plain orthographic camera, so aiming, HUD anchors and camera framing are unchanged; anything
    // deeper than the plane shifts up/right by `shear` per metre, which reveals platform tops and fighters' depth.
    // Runs after FortressGame's camera framing (LateUpdate) and before FortressHud reads WorldToViewportPoint.
    [DefaultExecutionOrder(50)]
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public sealed class FortressObliqueCamera : MonoBehaviour
    {
        // Zero keeps a true side view (platform fronts only, no tops). Raise y (e.g. .3) to reveal platform tops.
        [Tooltip("평면보다 1m 깊은 곳이 화면에서 오른쪽(x)·위쪽(y)으로 밀리는 양. 0이면 완전 측면")]
        public Vector2 shear = Vector2.zero;
        public float planeZ;
        Camera target;

        public Vector2 ScreenShift(float depthBeyondPlane) => shear * depthBeyondPlane;

        void OnEnable()
        {
            target = GetComponent<Camera>();
            RenderPipelineManager.beginCameraRendering += BeforeRender;
            Apply();
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeforeRender;
            if (target) target.ResetProjectionMatrix();
        }

        void LateUpdate() => Apply();

        void BeforeRender(ScriptableRenderContext context, Camera rendering) { if (rendering == target) Apply(); }

        public void Apply()
        {
            if (!target) return;
            // Rebuild from orthographicSize/aspect every frame so zoom and trajectory framing keep working.
            target.ResetProjectionMatrix();
            float distance = planeZ - target.transform.position.z;
            var s = Matrix4x4.identity;
            // View space looks down -Z: a point on the plane has z = -distance, which these terms cancel.
            s.m02 = -shear.x; s.m03 = -shear.x * distance;
            s.m12 = -shear.y; s.m13 = -shear.y * distance;
            target.projectionMatrix = target.projectionMatrix * s;
        }
    }
}
