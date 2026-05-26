// ============================================================
//  OPHIO — ManagerBootstrapper
//  Fallback: if scene is played directly without going
//  through MainMenu, creates required managers at runtime.
//  Attach to ArenaManager GameObject in Arena scene.
// ============================================================

using UnityEngine;

namespace OPHIO.Core
{
    public class ManagerBootstrapper : MonoBehaviour
    {
        private void Awake()
        {
            // Only create managers if they don't already exist
            // (i.e. game was started directly from Arena scene)
            EnsureManager<AudioManager>("OPHIO_Managers");
            EnsureManager<ObjectPoolManager>("OPHIO_Managers");
            EnsureManager<StatusEffectManager>("OPHIO_Managers");
        }

        private void EnsureManager<T>(string parentName) where T : MonoBehaviour
        {
            if (FindObjectOfType<T>() != null) return;

            // Manager missing — create it
            var go = GameObject.Find(parentName) ?? new GameObject(parentName);
            go.AddComponent<T>();
            Debug.Log($"[Bootstrapper] Created missing {typeof(T).Name} (direct scene play detected).");
        }
    }
}
