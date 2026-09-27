using UnityEngine;

namespace MiniFortress
{
    [DisallowMultipleComponent]
    public sealed class FortressCameraBackdrop : MonoBehaviour
    {
        Camera targetCamera;
        SpriteRenderer backdrop;

        public void Follow(Camera cameraToFollow)
        {
            targetCamera = cameraToFollow;
            if (!backdrop) backdrop = GetComponent<SpriteRenderer>();
        }

        void LateUpdate()
        {
            if (!targetCamera || !targetCamera.orthographic || !backdrop || !backdrop.sprite) return;

            Vector3 position = transform.position;
            Vector3 cameraPosition = targetCamera.transform.position;
            transform.position = new Vector3(cameraPosition.x, cameraPosition.y, position.z);

            float viewHeight = targetCamera.orthographicSize * 2f * 1.03f;
            float viewWidth = viewHeight * targetCamera.aspect;
            Vector2 spriteSize = backdrop.sprite.bounds.size;
            float scale = Mathf.Max(viewWidth / spriteSize.x, viewHeight / spriteSize.y);
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
