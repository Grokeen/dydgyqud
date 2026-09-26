using UnityEngine;

namespace MiniFortress
{
    [RequireComponent(typeof(BoxCollider2D))]
    [DisallowMultipleComponent]
    public sealed class FortressTerrain : MonoBehaviour
    {
        [Tooltip("S 키로 이 발판 아래로 내려갈 수 있습니다.")]
        public bool allowDropThrough;

        // The collider is an authoring tool. The existing deterministic solver reads its rectangle.
        public Rect WorldRect
        {
            get
            {
                var box = GetComponent<BoxCollider2D>();
                Vector2 center = transform.TransformPoint(box.offset);
                Vector2 size = Vector2.Scale(box.size, new Vector2(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y)));
                return new Rect(center - size * .5f, size);
            }
        }

        void OnDrawGizmos()
        {
            Rect r = WorldRect;
            Gizmos.color = allowDropThrough ? new Color(1, .7f, .2f, .16f) : new Color(.1f, 1, .55f, .12f);
            Gizmos.DrawCube(r.center, new Vector3(r.width, r.height, .01f));
            Gizmos.color = allowDropThrough ? new Color(1, .7f, .2f, .8f) : new Color(.1f, 1, .55f, .7f);
            Gizmos.DrawWireCube(r.center, new Vector3(r.width, r.height, .01f));
        }
    }
}
