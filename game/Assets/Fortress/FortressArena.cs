using System.Collections.Generic;
using UnityEngine;

namespace MiniFortress
{
    public sealed class FortressArena : MonoBehaviour
    {
        public FortressBattleRules rules;
        public Camera worldCamera;
        public SpriteRenderer background;
        public Transform terrainRoot;
        public Transform playerSpawn;
        public Transform enemySpawns;
        public FortressCharacterDefinition[] playerClasses;
        public Sprite effectSprite;
        [Tooltip("높은 탄도를 따라 카메라를 확장합니다. 기본 위치와 크기는 Camera에서 편집하세요.")]
        public bool followHighShots = true;

        public List<string> ValidateSetup()
        {
            var errors = new List<string>();
            if (!rules) errors.Add("Battle Rules를 연결하세요.");
            if (!worldCamera || !background || !terrainRoot || !playerSpawn || !enemySpawns || !effectSprite)
                errors.Add("Camera / Background / Terrain / Spawn / Effect Sprite 참조를 확인하세요.");
            if (playerClasses == null || playerClasses.Length == 0) errors.Add("플레이어 직업이 필요합니다.");
            else foreach (var definition in playerClasses) CheckCharacter(definition, errors);
            if (enemySpawns)
                foreach (var spawn in enemySpawns.GetComponentsInChildren<FortressSpawnPoint>()) CheckCharacter(spawn.character, errors);
            if (terrainRoot)
            {
                var platforms = terrainRoot.GetComponentsInChildren<FortressTerrain>();
                if (platforms.Length == 0) errors.Add("바닥 영역이 필요합니다.");
                foreach (var platform in platforms)
                {
                    if (Quaternion.Angle(platform.transform.rotation, Quaternion.identity) > .01f)
                        errors.Add(platform.name + ": 바닥 회전은 지원하지 않습니다. Position과 Collider Size를 사용하세요.");
                    if (platform.WorldRect.width <= 0 || platform.WorldRect.height <= 0)
                        errors.Add(platform.name + ": 바닥 크기는 0보다 커야 합니다.");
                }
            }
            return errors;
        }

        static void CheckCharacter(FortressCharacterDefinition definition, List<string> errors)
        {
            if (!definition || !definition.prefab || !definition.prefab.IsConfigured || !definition.projectile || !definition.portrait)
                errors.Add("캐릭터 데이터의 Prefab / Animator / Portrait / Projectile 연결을 확인하세요.");
        }
    }
}
