// ============================================================
//  OPHIO — LoadoutSelectionManager
//  LoadoutBuilder scene mein ability cards ke clicks handle
//  karta hai aur GameFlowManager mein selection save karta hai.
//  LoadoutBuilder scene mein empty GameObject par lagao.
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace OPHIO.UI
{
    public class LoadoutSelectionManager : MonoBehaviour
    {
        [Header("Slot UI Texts (auto-found if null)")] 
        public Text slot1Text;
        public Text slot2Text;
        public Text slot3Text;
        public Text superText;

        // NO hardcoded ability pool here —
        // pool is loaded dynamically from selected character's CharacterData
        private System.Collections.Generic.List<Core.AbilityData> _abilityPool
            = new System.Collections.Generic.List<Core.AbilityData>();

        [Header("Selection State Colors")]
        public Color selectedColor   = new Color(0.10f, 0.80f, 0.95f, 1f);
        public Color deselectedColor = new Color(1f,    1f,    1f,    1f);

        // Internal state
        private Core.AbilityData _pendingAbility; // ability clicked, waiting for slot
        private int              _pendingSlot = -1;

        private Arena.GameFlowManager _flow;

        private void Start()
        {
            _flow = Arena.GameFlowManager.Instance;

            // Auto-find slot texts
            AutoFindSlotTexts();

            // Load ability pool from selected character
            LoadPoolFromCharacter();

            // Update character name in UI
            UpdateCharacterNameDisplay();

            // Load default slots
            if (_flow?.selectedCharacter != null)
                LoadDefaultsFromCharacterData(_flow.selectedCharacter);

            // Rebuild pool cards
            RebuildPoolCards();

            // Wire slot buttons
            WireSlotButtons();

            UpdateAllSlotTexts();
        }

        private void UpdateCharacterNameDisplay()
        {
            if (_flow?.selectedCharacter == null) return;

            // Update "SUBJECT: HAWK" text in EquippedPanel
            var eqPanel = GameObject.Find("EquippedPanel");
            if (eqPanel == null) return;

            var charNameTf = eqPanel.transform.Find("CharNameText");
            if (charNameTf == null) return;

            var txt = charNameTf.GetComponent<UnityEngine.UI.Text>();
            if (txt != null)
                txt.text = $"SUBJECT: {_flow.selectedCharacter.characterName.ToUpper()}";

            // Also update CharInfoPanel right side
            var infoName = GameObject.Find("InfoName");
            if (infoName != null)
            {
                var infoTxt = infoName.GetComponent<UnityEngine.UI.Text>();
                if (infoTxt != null)
                    infoTxt.text = _flow.selectedCharacter.characterName.ToUpper();
            }

            var infoSubject = GameObject.Find("InfoSubject");
            if (infoSubject != null)
            {
                var subTxt = infoSubject.GetComponent<UnityEngine.UI.Text>();
                if (subTxt != null)
                    subTxt.text = _flow.selectedCharacter.subjectNumber;
            }

            var infoRole = GameObject.Find("InfoRole");
            if (infoRole != null)
            {
                var roleTxt = infoRole.GetComponent<UnityEngine.UI.Text>();
                if (roleTxt != null)
                    roleTxt.text = _flow.selectedCharacter.passiveName;
            }

            // Passive box
            var passiveName = GameObject.Find("PassiveName");
            if (passiveName != null)
            {
                var pTxt = passiveName.GetComponent<UnityEngine.UI.Text>();
                if (pTxt != null)
                    pTxt.text = _flow.selectedCharacter.passiveName;
            }

            var passiveDesc = GameObject.Find("PassiveDesc");
            if (passiveDesc != null)
            {
                var dTxt = passiveDesc.GetComponent<UnityEngine.UI.Text>();
                if (dTxt != null)
                    dTxt.text = _flow.selectedCharacter.passiveDesc;
            }
        }

        // --------------------------------------------------
        //  Load pool from selected character's CharacterData
        // --------------------------------------------------
        private void LoadPoolFromCharacter()
        {
            _abilityPool.Clear();

            if (_flow?.selectedCharacter == null)
            {
                Debug.LogWarning("[LoadoutSelectionManager] No character selected — pool empty.");
                return;
            }

            var data = _flow.selectedCharacter;

            // Add all active abilities from pool
            foreach (var a in data.abilityPool)
                if (a != null) _abilityPool.Add(a);

            Debug.Log($"[LoadoutSelectionManager] Pool loaded for {data.characterName}: " +
                      $"{_abilityPool.Count} abilities.");
        }

        // --------------------------------------------------
        //  Step 1 — User clicks ability card in pool
        // --------------------------------------------------
        public void OnAbilityCardClicked(Core.AbilityData ability)
        {
            _pendingAbility = ability;
            Debug.Log($"[Loadout] Ability selected: {ability.abilityName} — now tap a slot.");
            HighlightSlots(true);
            Core.AudioManager.Instance?.PlayAbilityEquip();
        }

        // --------------------------------------------------
        //  Step 2 — User clicks a slot to place ability
        // --------------------------------------------------
        public void OnSlotClicked(int slot)
        {
            if (_pendingAbility == null) return;

            // Super slot only accepts Super category abilities
            if (slot == 3 &&
                _pendingAbility.category != Core.AbilityCategory.Super)
            {
                Debug.Log("[Loadout] Only Super abilities go in the Super slot.");
                return;
            }

            // Active slots don't accept Super abilities
            if (slot < 3 &&
                _pendingAbility.category == Core.AbilityCategory.Super)
            {
                Debug.Log("[Loadout] Super abilities only go in the Super slot (F).");
                return;
            }

            // Save to GameFlowManager
            if (_flow != null)
            {
                switch (slot)
                {
                    case 0: _flow.selectedSlot1 = _pendingAbility; break;
                    case 1: _flow.selectedSlot2 = _pendingAbility; break;
                    case 2: _flow.selectedSlot3 = _pendingAbility; break;
                    case 3: _flow.selectedSuper = _pendingAbility; break;
                }
            }

            UpdateAllSlotTexts();
            HighlightSlots(false);
            _pendingAbility = null;

            Debug.Log($"[Loadout] Slot {slot} set to: {_pendingAbility?.abilityName}");
            Core.AudioManager.Instance?.PlayAbilityEquip();
        }

        // --------------------------------------------------
        //  Update slot text displays
        // --------------------------------------------------
        private void UpdateAllSlotTexts()
        {
            if (_flow == null) return;
            if (slot1Text) slot1Text.text = _flow.selectedSlot1?.abilityName ?? "— EMPTY —";
            if (slot2Text) slot2Text.text = _flow.selectedSlot2?.abilityName ?? "— EMPTY —";
            if (slot3Text) slot3Text.text = _flow.selectedSlot3?.abilityName ?? "— EMPTY —";
            if (superText) superText.text = _flow.selectedSuper?.abilityName ?? "— EMPTY —";
        }

        // --------------------------------------------------
        //  Load defaults from CharacterData ability pool
        // --------------------------------------------------
        private void LoadDefaultsFromCharacterData(Core.CharacterData data)
        {
            if (_flow == null) return;
            if (data.abilityPool.Count > 0 && _flow.selectedSlot1 == null)
                _flow.selectedSlot1 = data.abilityPool[0];
            if (data.abilityPool.Count > 1 && _flow.selectedSlot2 == null)
                _flow.selectedSlot2 = data.abilityPool[1];
            if (data.abilityPool.Count > 2 && _flow.selectedSlot3 == null)
                _flow.selectedSlot3 = data.abilityPool[2];
            if (data.superAbility != null && _flow.selectedSuper == null)
                _flow.selectedSuper = data.superAbility;
        }

        // --------------------------------------------------
        //  Rebuild pool cards dynamically from _abilityPool
        // --------------------------------------------------
        private void RebuildPoolCards()
        {
            var poolPanel = GameObject.Find("AbilityPoolPanel");
            if (poolPanel == null) return;

            // Clear old cards
            for (int i = poolPanel.transform.childCount - 1; i >= 0; i--)
            {
                var child = poolPanel.transform.GetChild(i);
                // Keep title and hint texts, remove PoolCard_ objects
                if (child.name.StartsWith("PoolCard_"))
                    Destroy(child.gameObject);
            }

            // Recreate cards from _abilityPool
            float cardW = 220f, cardH = 90f;
            float padX  = 20f,  padY  = 12f;
            int   cols  = 2;
            float startX= -(cardW + padX) * 0.5f;

            for (int i = 0; i < _abilityPool.Count; i++)
            {
                var ability = _abilityPool[i];
                int col = i % cols;
                int row = i / cols;

                float px = startX + col * (cardW + padX);
                float py = -row   * (cardH + padY * 2f);

                // Card root
                var card       = new GameObject($"PoolCard_{i}");
                card.transform.SetParent(poolPanel.transform, false);
                var rect       = card.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.75f);
                rect.anchorMax = new Vector2(0.5f, 0.75f);
                rect.pivot     = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(cardW, cardH);
                rect.anchoredPosition = new Vector2(px, py - row * 4f);

                var bg   = card.AddComponent<UnityEngine.UI.Image>();
                bg.color = new Color(0.10f, 0.10f, 0.13f, 0.95f);

                // Color bar (left edge — based on damage type)
                Color barColor = GetDamageTypeColor(ability.damageType);
                var   barGO    = new GameObject("ColorBar");
                barGO.transform.SetParent(card.transform, false);
                var barRect    = barGO.AddComponent<RectTransform>();
                barRect.anchorMin = Vector2.zero;
                barRect.anchorMax = new Vector2(0f, 1f);
                barRect.pivot     = new Vector2(0f, 0.5f);
                barRect.sizeDelta = new Vector2(5f, 0f);
                barGO.AddComponent<UnityEngine.UI.Image>().color = barColor;

                // Ability name text
                AddCardText(card.transform, "AbilityName",
                    ability.abilityName.ToUpper(), 15, Color.white,
                    new Vector2(0.07f, 0.55f), new Vector2(0.95f, 0.95f));

                // Category + damage type text
                string subLabel = $"{ability.category}  ·  {ability.damageType}";
                AddCardText(card.transform, "AbilityDesc",
                    subLabel, 12, new Color(0.65f, 0.65f, 0.65f, 1f),
                    new Vector2(0.07f, 0.08f), new Vector2(0.95f, 0.50f));

                // Button
                var btn   = card.AddComponent<UnityEngine.UI.Button>();
                btn.targetGraphic = bg;
                var cols2 = btn.colors;
                cols2.highlightedColor = new Color(0.18f, 0.18f, 0.22f, 1f);
                cols2.pressedColor     = new Color(0.25f, 0.25f, 0.30f, 1f);
                btn.colors = cols2;

                Core.AbilityData captured = ability;
                btn.onClick.AddListener(() => OnAbilityCardClicked(captured));
            }
        }

        private Color GetDamageTypeColor(Core.DamageType type)
        {
            switch (type)
            {
                case Core.DamageType.Electric:  return new Color(0.10f, 0.80f, 0.95f, 1f);
                case Core.DamageType.Fire:       return new Color(1.00f, 0.35f, 0.10f, 1f);
                case Core.DamageType.Explosive:  return new Color(0.95f, 0.70f, 0.10f, 1f);
                case Core.DamageType.Infection:  return new Color(0.40f, 0.85f, 0.20f, 1f);
                default:                         return new Color(0.70f, 0.70f, 0.70f, 1f);
            }
        }

        private void AddCardText(Transform parent, string name, string content,
                                  int size, Color color,
                                  Vector2 anchorMin, Vector2 anchorMax)
        {
            var go   = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin        = anchorMin;
            rect.anchorMax        = anchorMax;
            rect.sizeDelta        = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            var txt  = go.AddComponent<UnityEngine.UI.Text>();
            txt.text      = content;
            txt.fontSize  = size;
            txt.color     = color;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow   = VerticalWrapMode.Overflow;
        }

        // --------------------------------------------------
        //  Wire slot click buttons (separate from pool cards)
        // --------------------------------------------------
        private void WireSlotButtons()
        {
            var eqPanel = GameObject.Find("EquippedPanel");
            if (eqPanel == null) return;

            for (int i = 0; i < 4; i++)
            {
                var slotTf = eqPanel.transform.Find($"SlotBg_{i}");
                if (slotTf == null) continue;

                var btn = slotTf.GetComponent<UnityEngine.UI.Button>();
                if (btn == null)
                    btn = slotTf.gameObject.AddComponent<UnityEngine.UI.Button>();

                var img = slotTf.GetComponent<UnityEngine.UI.Image>();
                if (img != null) btn.targetGraphic = img;

                int captured = i;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnSlotClicked(captured));
            }
        }

        // --------------------------------------------------
        //  Highlight / dehighlight slot panels
        // --------------------------------------------------
        private void HighlightSlots(bool on)
        {
            var eqPanel = GameObject.Find("EquippedPanel");
            if (eqPanel == null) return;
            for (int i = 0; i < 4; i++)
            {
                var slotTf = eqPanel.transform.Find($"SlotBg_{i}");
                if (slotTf == null) continue;
                var img = slotTf.GetComponent<Image>();
                if (img != null)
                    img.color = on
                        ? new Color(0.12f, 0.20f, 0.30f, 0.95f)
                        : new Color(0.08f, 0.08f, 0.10f, 0.90f);
            }
        }

        // --------------------------------------------------
        //  Auto-find slot texts
        // --------------------------------------------------
        private void AutoFindSlotTexts()
        {
            var eqPanel = GameObject.Find("EquippedPanel");
            if (eqPanel == null) return;

            string[] slotNames = { "SlotBg_0", "SlotBg_1", "SlotBg_2", "SlotBg_3" };
            Text[] refs = { slot1Text, slot2Text, slot3Text, superText };

            for (int i = 0; i < 4; i++)
            {
                if (refs[i] != null) continue;
                var slotTf = eqPanel.transform.Find(slotNames[i]);
                if (slotTf == null) continue;
                var txt = slotTf.Find("AbilityName")?.GetComponent<Text>();
                switch (i)
                {
                    case 0: slot1Text = txt; break;
                    case 1: slot2Text = txt; break;
                    case 2: slot3Text = txt; break;
                    case 3: superText = txt; break;
                }
            }
        }
    }
}
