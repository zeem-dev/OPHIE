// ============================================================
//  OPHIO — AbilityCooldownUI
//  HUD + UI
//  Single ability slot HUD element with cooldown ring.
//  Shows icon, fill radial for cooldown, energy cost text.
//  Attach to each ability slot UI element.
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OPHIO.UI
{
    public class AbilityCooldownUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Ability icon image")]
        public Image iconImage;
        [Tooltip("Radial fill image overlaid on the icon (Image Type = Filled)")]
        public Image cooldownOverlay;
        [Tooltip("Text showing remaining cooldown seconds")]
        public TextMeshProUGUI  cooldownText;
        [Tooltip("Text showing the key bind (Q/E/R/F)")]
        public TextMeshProUGUI keyBindText;
        [Tooltip("Border highlight when ability is ready")]
        public Image readyBorder;

        [Header("Colors")]
        public Color readyColor    = new Color(1f, 1f, 1f, 1f);
        public Color cooldownColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        public Color noEnergyColor = new Color(0.2f, 0.2f, 0.6f, 0.8f);

        private int _slotIndex;
        private Core.AbilityExecutor _executor;
        private Core.EnergyManager   _energy;
        private Core.AbilityLoadout  _loadout;

        public void Init(int slotIndex, Core.AbilityExecutor executor,
                         Core.EnergyManager energy, Core.AbilityLoadout loadout)
        {
            _slotIndex = slotIndex;
            _executor  = executor;
            _energy    = energy;
            _loadout   = loadout;

            // Set key bind text
            string[] keys = { "Q", "E", "R", "F" };
            if (keyBindText != null && slotIndex < keys.Length)
                keyBindText.text = keys[slotIndex];

            UpdateIcon();
        }

        private void Update()
        {
            if (_executor == null) return;

            var ability = _loadout?.GetSlot(_slotIndex);
            if (ability == null)
            {
                if (cooldownOverlay != null) cooldownOverlay.fillAmount = 1f;
                if (iconImage       != null) iconImage.color = cooldownColor;
                return;
            }

            float remaining = _executor.GetCooldownRemaining(_slotIndex);
            float max       = _executor.GetCooldownMax(_slotIndex);
            bool  onCD      = _executor.IsOnCooldown(_slotIndex);
            bool  hasEnergy = _energy != null && _energy.HasEnergy(ability.energyCost);

            // Cooldown overlay (radial fill — 1=full cover, 0=ready)
            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = onCD
                    ? remaining / Mathf.Max(max, 0.01f)
                    : 0f;
            }

            // Cooldown text
            if (cooldownText != null)
            {
                if (onCD)
                {
                    cooldownText.text    = remaining.ToString("F1");
                    cooldownText.enabled = true;
                }
                else
                {
                    cooldownText.enabled = false;
                }
            }

            // Icon tint
            if (iconImage != null)
            {
                if (onCD)
                    iconImage.color = cooldownColor;
                else if (!hasEnergy)
                    iconImage.color = noEnergyColor;
                else
                    iconImage.color = readyColor;
            }

            // Ready border pulse
            if (readyBorder != null)
            {
                readyBorder.enabled = !onCD && hasEnergy;
                if (readyBorder.enabled)
                {
                    float alpha = 0.5f + Mathf.Sin(Time.time * 3f) * 0.3f;
                    readyBorder.color = new Color(1f, 1f, 1f, alpha);
                }
            }
        }

        public void UpdateIcon()
        {
            var ability = _loadout?.GetSlot(_slotIndex);
            if (iconImage != null && ability != null && ability.icon != null)
                iconImage.sprite = ability.icon;
        }
    }
}
