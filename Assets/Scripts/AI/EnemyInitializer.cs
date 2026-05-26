// ============================================================
//  OPHIO — EnemyInitializer
//  Enemy AI State Machine
//  Reads an EnemyTypeData asset and configures the EnemyAI
//  component on Awake. Attach to every enemy prefab.
//  Assign the EnemyTypeData SO in the Inspector.
// ============================================================

using UnityEngine;

namespace OPHIO.AI
{
    [RequireComponent(typeof(EnemyAI))]
    public class EnemyInitializer : MonoBehaviour
    {
        [Header("Enemy Configuration")]
        [Tooltip("Assign the EnemyTypeData ScriptableObject for this enemy variant")]
        public EnemyTypeData typeData;

        private void Awake()
        {
            if (typeData == null)
            {
                Debug.LogWarning($"[EnemyInitializer] No EnemyTypeData assigned on {gameObject.name}");
                return;
            }

            var ai = GetComponent<EnemyAI>();
            typeData.ApplyTo(ai);
        }
    }
}
