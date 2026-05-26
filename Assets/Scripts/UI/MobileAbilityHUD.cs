// ============================================================
//  OPHIO — MobileAbilityHUD (Runtime Spawn Compatible)
//  Player runtime pe spawn hota hai — HUD event se notify
//  hota hai jab player ready ho.
//  Attach to OPHIO_AbilityBar GameObject.
// ============================================================

using UnityEngine;
using UnityEngine.UI;

namespace OPHIO.UI
{
    public class MobileAbilityHUD : MonoBehaviour
    {
        // Static events
        public static System.Action<Core.AbilityExecutor> OnPlayerSpawned;
        public static System.Action<Core.AbilityLoadout>  OnLoadoutApplied;

        [Header("Ability Slots (auto-found from children)")]
        public Button slot0Button;
        public Button slot1Button;
        public Button slot2Button;
        public Button slot3Button;

        [Header("Cooldown Overlays")]
        public Image slot0CooldownOverlay;
        public Image slot1CooldownOverlay;
        public Image slot2CooldownOverlay;
        public Image slot3CooldownOverlay;

        private Core.AbilityExecutor _executor;
        private bool                 _wired;

        // --------------------------------------------------
        //  Lifecycle
        // --------------------------------------------------
        private void Awake()
        {
            AutoFindSlots();
        }

        private void OnEnable()
        {
            OnPlayerSpawned  += HandlePlayerSpawned;
            OnLoadoutApplied += HandleLoadoutApplied;
        }

        private void OnDisable()
        {
            OnPlayerSpawned  -= HandlePlayerSpawned;
            OnLoadoutApplied -= HandleLoadoutApplied;
        }

        private void Start()
        {
            // Player already in scene (non-spawner setup) — find immediately
            TryFindExecutorNow();
        }

        private void Update()
        {
            if (_executor == null) return;
            UpdateOverlay(slot0CooldownOverlay, 0);
            UpdateOverlay(slot1CooldownOverlay, 1);
            UpdateOverlay(slot2CooldownOverlay, 2);
            UpdateOverlay(slot3CooldownOverlay, 3);
        }

        // --------------------------------------------------
        //  Called by PlayerSpawner after loadout is applied
        //  Updates button name labels in Arena HUD
        // --------------------------------------------------
        private void HandleLoadoutApplied(Core.AbilityLoadout loadout)
        {
            if (loadout == null) return;
            UpdateButtonLabel(slot0Button, loadout.GetSlot(0));
            UpdateButtonLabel(slot1Button, loadout.GetSlot(1));
            UpdateButtonLabel(slot2Button, loadout.GetSlot(2));
            UpdateButtonLabel(slot3Button, loadout.GetSlot(3));
            Debug.Log("[MobileAbilityHUD] Button names updated from loadout.");
        }

        private void UpdateButtonLabel(Button btn, Core.AbilityData ability)
        {
            if (btn == null || ability == null) return;
            var lbl = btn.GetComponentInChildren<Text>();
            if (lbl == null) return;
            // Show short name (max 8 chars to fit button)
            string name = ability.abilityName;
            lbl.text    = name.Length > 10 ? name.Substring(0, 9) + "." : name;
        }

        // --------------------------------------------------
        //  Called by PlayerSpawner after player is spawned
        // --------------------------------------------------
        private void HandlePlayerSpawned(Core.AbilityExecutor executor)
        {
            if (executor == null) return;
            _executor = executor;
            WireButtons();
            Debug.Log($"[MobileAbilityHUD] Player spawned — buttons wired to: {executor.gameObject.name}");
        }

        // --------------------------------------------------
        //  Fallback: find executor if already in scene
        // --------------------------------------------------
        private void TryFindExecutorNow()
        {
            if (_wired) return;
            var executor = FindObjectOfType<Core.AbilityExecutor>();
            if (executor != null)
                HandlePlayerSpawned(executor);
        }

        // --------------------------------------------------
        //  Wire buttons
        // --------------------------------------------------
        private void WireButtons()
        {
            if (_wired) return;
            WireSlot(slot0Button, 0);
            WireSlot(slot1Button, 1);
            WireSlot(slot2Button, 2);
            WireSlot(slot3Button, 3);
            _wired = true;
        }

        private void WireSlot(Button btn, int slot)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            int s = slot;
            btn.onClick.AddListener(() =>
            {
                if (_executor == null) TryFindExecutorNow();
                _executor?.TryActivate(s);
            });
        }

        // --------------------------------------------------
        //  Cooldown overlay
        // --------------------------------------------------
        private void UpdateOverlay(Image overlay, int slot)
        {
            if (overlay == null) return;
            float max       = _executor.GetCooldownMax(slot);
            float remaining = _executor.GetCooldownRemaining(slot);
            overlay.fillAmount = max > 0f ? remaining / max : 0f;
        }

        // --------------------------------------------------
        //  Auto-find slots from children
        // --------------------------------------------------
        private void AutoFindSlots()
        {
            TryFindSlot(0, ref slot0Button, ref slot0CooldownOverlay);
            TryFindSlot(1, ref slot1Button, ref slot1CooldownOverlay);
            TryFindSlot(2, ref slot2Button, ref slot2CooldownOverlay);
            TryFindSlot(3, ref slot3Button, ref slot3CooldownOverlay);
        }

        private void TryFindSlot(int i, ref Button btn, ref Image overlay)
        {
            var t = transform.Find($"AbilitySlot_{i}");
            if (t == null) return;
            if (btn     == null) btn     = t.GetComponent<Button>();
            if (overlay == null) overlay = t.Find("CooldownOverlay")?.GetComponent<Image>();
        }
    }
}
