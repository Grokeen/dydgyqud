using UnityEngine;

namespace MiniFortress
{
    [DisallowMultipleComponent]
    public sealed class FortressCameraBackdrop : MonoBehaviour
    {
        Camera targetCamera;
        SpriteRenderer backdrop;
        FortressObliqueCamera oblique;

        public void Follow(Camera cameraToFollow)
        {
            targetCamera = cameraToFollow;
            if (!backdrop) backdrop = GetComponent<SpriteRenderer>();
            oblique = cameraToFollow ? cameraToFollow.GetComponent<FortressObliqueCamera>() : null;
        }

        void LateUpdate()
        {
            if (!targetCamera || !targetCamera.orthographic || !backdrop || !backdrop.sprite) return;

            Vector3 position = transform.position;
            Vector3 cameraPosition = targetCamera.transform.position;
            float viewHeight = targetCamera.orthographicSize * 2f * 1.03f;
            float viewWidth = viewHeight * targetCamera.aspect;
            Bounds bounds = backdrop.sprite.bounds;
            float scale = Mathf.Max(viewWidth / bounds.size.x, viewHeight / bounds.size.y);
            transform.localScale = new Vector3(scale, scale, 1f);

            // Centre the image itself, not its pivot: the map paintings pivot at their bottom edge.
            // The backdrop also sits far behind the 3D platforms, so undo the oblique camera's depth shift.
            Vector2 shift = oblique ? oblique.ScreenShift(position.z - oblique.planeZ) : Vector2.zero;
            Vector2 centre = (Vector2)bounds.center * scale;
            transform.position = new Vector3(cameraPosition.x - shift.x - centre.x, cameraPosition.y - shift.y - centre.y, position.z);
        }
    }
}
