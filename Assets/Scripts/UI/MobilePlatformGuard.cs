// ============================================================
//  OPHIO - MobilePlatformGuard
//  Keeps Invector mobile controls alive in Editor and builds.
// ============================================================

using UnityEngine;

namespace OPHIO.UI
{
    public class MobilePlatformGuard : MonoBehaviour
    {
        [Header("Mobile UI Root")]
        [Tooltip("Auto-uses this GameObject if null")]
        public GameObject mobileUIRoot;

        private void Awake()
        {
            if (mobileUIRoot == null)
                mobileUIRoot = gameObject;

            mobileUIRoot.SetActive(true);
            Debug.Log("[MobilePlatformGuard] Mobile UI ready.");
        }
    }
}
