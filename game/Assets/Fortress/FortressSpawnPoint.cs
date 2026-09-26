using UnityEngine;

namespace MiniFortress
{
    public sealed class FortressSpawnPoint : MonoBehaviour
    {
        public FortressCharacterDefinition character;
        [Tooltip("비워두면 Character의 Display Name을 사용합니다.")]
        public string displayName;
        void OnDrawGizmos()
        {
            var data = character;
            var arena = GetComponentInParent<FortressArena>();
            if (arena && transform == arena.playerSpawn && arena.playerClasses != null && arena.playerClasses.Length > 0)
                data = arena.playerClasses[0];
            float scale = data && data.prefab ? data.prefab.transform.localScale.x : 1;
            float height = data ? data.height * scale : 4.2f;
            float halfWidth = data ? data.halfWidth * scale : .7f;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, .5f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * (height * .5f), new Vector3(halfWidth * 2, height, .05f));
        }
    }
}
