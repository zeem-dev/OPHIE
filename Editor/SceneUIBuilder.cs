using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine.Events;
using System.IO;
using System.Collections.Generic;
using OPHIO.Core;

public class SceneUIBuilder : EditorWindow
{
    private static readonly string MainMenuScenePath        = "Assets/Scenes/MainMenu.unity";
    private static readonly string CharacterSelectScenePath = "Assets/Scenes/CharacterSelect.unity";
    private static readonly string LoadoutBuilderScenePath  = "Assets/Scenes/LoadoutBuilder.unity";
    private static readonly string ArenaScenePath           = "Assets/Scenes/Arena.unity";
    private static readonly string EndScreenScenePath       = "Assets/Scenes/EndScreen.unity";

    // Invector mobile prefab path (no-inventory variant — OPHIO does not use Invector inventory)
    private static readonly string MobileUIPrefabPath =
        "Assets/Invector-3rdPersonController/Add-ons/Controller_Mobile/" +
        "Shooter (Require Shooter Template)/Prefabs/MobileUI_ShooterMelee_NoInventory.prefab";

    // Ability button prefab path (OPHIO custom — created below if missing)
    private static readonly string AbilityButtonPrefabPath =
        "Assets/Prefabs/UI/AbilityButtonMobile.prefab";

    // Color palette
    private static readonly Color DarkBgColor      = new Color(0.08f, 0.08f, 0.10f, 0.95f);
    private static readonly Color PanelColor        = new Color(0.12f, 0.12f, 0.15f, 0.85f);
    private static readonly Color GoldenAmber       = new Color(0.95f, 0.70f, 0.10f, 1.00f);
    private static readonly Color NeonCyan          = new Color(0.10f, 0.80f, 0.95f, 1.00f);
    private static readonly Color NeonRed           = new Color(0.90f, 0.20f, 0.20f, 1.00f);
    private static readonly Color ButtonNormalColor = new Color(0.18f, 0.18f, 0.22f, 1.00f);

    // Mobile ability button colors
    private static readonly Color AbilityBtnColor   = new Color(0.10f, 0.10f, 0.14f, 0.90f);
    private static readonly Color SuperBtnColor     = new Color(0.50f, 0.10f, 0.10f, 0.95f);

    // -------------------------------------------------------------------------
    //  Menu entries
    // -------------------------------------------------------------------------
    [MenuItem("OPHIO/UI/Build All Mock Scene UIs")]
    public static void BuildAllScenes()
    {
        Debug.Log("[SceneUIBuilder] Starting full scene UI build...");
        AddScenesToBuildSettings();
        BuildMainMenu();
        BuildCharacterSelect();
        BuildLoadoutBuilder();
        BuildArenaHUD();
        BuildEndScreen();
        Debug.Log("[SceneUIBuilder] All 5 scenes built successfully.");
    }

    [MenuItem("OPHIO/UI/Rebuild Arena HUD Only")]
    public static void RebuildArenaOnly()
    {
        Debug.Log("[SceneUIBuilder] Rebuilding Arena HUD...");
        BuildArenaHUD();
        Debug.Log("[SceneUIBuilder] Arena HUD rebuild complete.");
    }

    // -------------------------------------------------------------------------
    //  Build settings helper
    // -------------------------------------------------------------------------
    private static void AddScenesToBuildSettings()
    {
        var buildScenes = new List<EditorBuildSettingsScene>();
        string[] paths  = new string[]
        {
            MainMenuScenePath, CharacterSelectScenePath,
            LoadoutBuilderScenePath, ArenaScenePath, EndScreenScenePath
        };
        foreach (string path in paths)
            if (File.Exists(Path.GetFullPath(path)))
                buildScenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = buildScenes.ToArray();
    }

    // -------------------------------------------------------------------------
    //  Scene bootstrap
    // -------------------------------------------------------------------------
    private static void ClearCanvasAndSetupScene(string scenePath,
                                                  out GameObject canvasObj,
                                                  out SceneNavigator navigator)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        GameObject old = GameObject.Find("Mock_UI_Canvas");
        if (old != null) DestroyImmediate(old);

        if (GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        canvasObj        = new GameObject("Mock_UI_Canvas");
        Canvas canvas    = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler          = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode           = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution   = new Vector2(1920, 1080);
        scaler.screenMatchMode       = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight    = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
        navigator = canvasObj.AddComponent<SceneNavigator>();
    }

    private static void SaveAndCloseActiveScene()
    {
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
    }

    // -------------------------------------------------------------------------
    //  UI helpers
    // -------------------------------------------------------------------------
    private static Font GetDefaultFont()
    {
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return f != null ? f : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 sizeDelta, Vector2 anchoredPosition)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin        = anchorMin;
        rect.anchorMax        = anchorMax;
        rect.pivot            = pivot;
        rect.sizeDelta        = sizeDelta;
        rect.anchoredPosition = anchoredPosition;
        go.AddComponent<Image>().color = color;
        return go;
    }

    private static GameObject CreateText(Transform parent, string name, string content,
        int fontSize, Color color, TextAnchor alignment,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 sizeDelta, Vector2 anchoredPosition)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin        = anchorMin;
        rect.anchorMax        = anchorMax;
        rect.pivot            = pivot;
        rect.sizeDelta        = sizeDelta;
        rect.anchoredPosition = anchoredPosition;
        var t = go.AddComponent<Text>();
        t.text               = content;
        t.font               = GetDefaultFont();
        t.fontSize           = fontSize;
        t.color              = color;
        t.alignment          = alignment;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
        return go;
    }

    private static GameObject CreateButton(Transform parent, string name, string label,
        Color normalColor,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 sizeDelta, Vector2 anchoredPosition)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin        = anchorMin;
        rect.anchorMax        = anchorMax;
        rect.pivot            = pivot;
        rect.sizeDelta        = sizeDelta;
        rect.anchoredPosition = anchoredPosition;
        var img = go.AddComponent<Image>();
        img.color = normalColor;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        CreateText(go.transform, "Label", label, 22, Color.white, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        return go;
    }

    // =========================================================================
    //  ARENA HUD  (fully updated — adds Invector mobile UI + OPHIO ability bar)
    // =========================================================================
    private static void BuildArenaHUD()
    {
        ClearCanvasAndSetupScene(ArenaScenePath, out GameObject canvas, out SceneNavigator navigator);

        // ------------------------------------------------------------------
        //  Resolve player spawn point
        // ------------------------------------------------------------------
        Vector3    spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        GameObject existingSpawn = GameObject.Find("PlayerSpawnPoint");
        if (existingSpawn != null)
        {
            spawnPos = existingSpawn.transform.position;
            spawnRot = existingSpawn.transform.rotation;
            DestroyImmediate(existingSpawn);
        }

        // Destroy stale static player objects
        var toDestroy = new List<GameObject>();
        foreach (var root in UnityEngine.SceneManagement.SceneManager
                     .GetActiveScene().GetRootGameObjects())
        {
            if (root.CompareTag("Player")                      ||
                root.name.ToLower().Contains("player")         ||
                root.name.Contains("Hawk")  || root.name.Contains("GOON") ||
                root.name.Contains("MAC")   || root.name.Contains("GUST") ||
                root.name.Contains("FLEX"))
                toDestroy.Add(root);
        }
        foreach (var obj in toDestroy) DestroyImmediate(obj);

        // Spawn point marker
        var spawnPointObj       = new GameObject("PlayerSpawnPoint");
        spawnPointObj.transform.position = spawnPos;
        spawnPointObj.transform.rotation = spawnRot;

        // Player spawner
        GameObject oldSpawner = GameObject.Find("PlayerSpawner");
        if (oldSpawner != null) DestroyImmediate(oldSpawner);
        var spawnerObj  = new GameObject("PlayerSpawner");
        var spawnerComp = spawnerObj.AddComponent<OPHIO.Arena.PlayerSpawner>();
        spawnerComp.hawkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Players/Hawk - Electric Melee Fighter.prefab");
        spawnerComp.goonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Players/GOON - Fire Attacker.prefab");
        spawnerComp.macPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Players/MAC - Energy Tank.prefab");
        spawnerComp.gustPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Players/GUST - Spore Summoner.prefab");
        spawnerComp.flexPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Players/FLEX - Melee Brawler.prefab");
        spawnerComp.spawnPoint = spawnPointObj.transform;

        // ------------------------------------------------------------------
        //  1. HUD — health / energy bars (top-left)
        // ------------------------------------------------------------------
        var hudPanel = CreatePanel(canvas.transform, "HUD_Overlay", Color.clear,
            new Vector2(0.02f, 0.90f), new Vector2(0.02f, 0.90f),
            new Vector2(0f, 1f), new Vector2(420, 100), Vector2.zero);

        var hpBorder = CreatePanel(hudPanel.transform, "HP_Border", Color.black,
            new Vector2(0f, 0.55f), new Vector2(1f, 0.55f),
            new Vector2(0f, 0.5f), new Vector2(0, 32), Vector2.zero);
        CreatePanel(hpBorder.transform, "HP_Fill", NeonRed,
            Vector2.zero, new Vector2(0.85f, 1f),
            new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
        CreateText(hpBorder.transform, "HPText", "HP  85 / 100", 14, Color.white,
            TextAnchor.MiddleCenter, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        var energyBorder = CreatePanel(hudPanel.transform, "Energy_Border", Color.black,
            new Vector2(0f, 0.1f), new Vector2(1f, 0.1f),
            new Vector2(0f, 0.5f), new Vector2(0, 28), Vector2.zero);
        CreatePanel(energyBorder.transform, "Energy_Fill", NeonCyan,
            Vector2.zero, new Vector2(0.60f, 1f),
            new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
        CreateText(energyBorder.transform, "EnergyText", "ENERGY  60 / 100", 13,
            Color.black, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        // ------------------------------------------------------------------
        //  2. Wave display (top-center)
        // ------------------------------------------------------------------
        var wavePanel = CreatePanel(canvas.transform, "WavePanel", PanelColor,
            new Vector2(0.5f, 0.94f), new Vector2(0.5f, 0.94f),
            new Vector2(0.5f, 0.5f), new Vector2(280, 52), Vector2.zero);
        CreateText(wavePanel.transform, "WaveText", "WAVE 1 / 3  |  03s", 18, GoldenAmber,
            TextAnchor.MiddleCenter, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        // ------------------------------------------------------------------
        //  3. Mock end-game trigger panel (top-right, editor only)
        // ------------------------------------------------------------------
        var testPanel = CreatePanel(canvas.transform, "TestPanel", PanelColor,
            new Vector2(0.98f, 0.98f), new Vector2(0.98f, 0.98f),
            new Vector2(1f, 1f), new Vector2(240, 120), Vector2.zero);
        CreateText(testPanel.transform, "TestTitle", "MOCK END TRIGGERS", 13, Color.gray,
            TextAnchor.MiddleCenter, new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f),
            new Vector2(0.5f, 0.5f), new Vector2(220, 20), Vector2.zero);

        var triggerMethod = typeof(SceneNavigator)
            .GetMethod("TriggerEndGame", new System.Type[] { typeof(bool) });
        var winAction = System.Delegate.CreateDelegate(
            typeof(UnityAction<bool>), navigator, triggerMethod) as UnityAction<bool>;

        var vicBtn = CreateButton(testPanel.transform, "VictoryBtn", "MOCK VICTORY",
            GoldenAmber, new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f),
            new Vector2(0.5f, 0.5f), new Vector2(200, 32), Vector2.zero);
        vicBtn.GetComponentInChildren<Text>().color = Color.black;
        UnityEventTools.AddBoolPersistentListener(
            vicBtn.GetComponent<Button>().onClick, winAction, true);

        var defBtn = CreateButton(testPanel.transform, "DefeatBtn", "MOCK DEFEAT",
            NeonRed, new Vector2(0.5f, 0.20f), new Vector2(0.5f, 0.20f),
            new Vector2(0.5f, 0.5f), new Vector2(200, 32), Vector2.zero);
        UnityEventTools.AddBoolPersistentListener(
            defBtn.GetComponent<Button>().onClick, winAction, false);

        // ------------------------------------------------------------------
        //  4. INVECTOR MOBILE UI — instantiate prefab from Add-ons
        // ------------------------------------------------------------------
        AddInvectorMobileUI(canvas);

        // ------------------------------------------------------------------
        //  5. OPHIO ABILITY BAR — 3 active + 1 super (bottom-right, above joystick)
        // ------------------------------------------------------------------
        AddOphioAbilityBar(canvas.transform, navigator);

        SaveAndCloseActiveScene();
        Debug.Log("[SceneUIBuilder] Arena HUD with mobile controls built successfully.");
    }

    // -------------------------------------------------------------------------
    //  Invector Mobile UI
    // -------------------------------------------------------------------------
    private static void AddInvectorMobileUI(GameObject canvas)
    {
        // Remove old mobile UI if present
        var old = canvas.transform.Find("MobileUI_ShooterMelee_NoInventory");
        if (old != null) DestroyImmediate(old.gameObject);

        var mobilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MobileUIPrefabPath);
        if (mobilePrefab == null)
        {
            Debug.LogWarning(
                "[SceneUIBuilder] Invector Mobile UI prefab not found at:\n" +
                MobileUIPrefabPath +
                "\nCheck path or enable mobile add-on in Invector settings.");
            CreateFallbackMobileUI(canvas.transform);
            return;
        }

        // Instantiate under the scene canvas
        var mobileUI = (GameObject)PrefabUtility.InstantiatePrefab(mobilePrefab);
        mobileUI.transform.SetParent(canvas.transform, false);
        mobileUI.name = "MobileUI_ShooterMelee_NoInventory";

        // Make sure it renders above everything (high sort order sibling)
        mobileUI.transform.SetAsLastSibling();

        // Guard: disable on Windows/Editor, enable only on Android/iOS
        mobileUI.AddComponent<OPHIO.UI.MobilePlatformGuard>();

        // Canvas Scaler already handles resolution — just confirm RectTransform fills screen
        var rect = mobileUI.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin        = Vector2.zero;
            rect.anchorMax        = Vector2.one;
            rect.offsetMin        = Vector2.zero;
            rect.offsetMax        = Vector2.zero;
        }

        Debug.Log("[SceneUIBuilder] Invector MobileUI_ShooterMelee_NoInventory added to Arena canvas.");
    }

    // -------------------------------------------------------------------------
    //  OPHIO Ability Bar  (4 buttons: Q E R F)
    //  Placed bottom-right, ABOVE the Invector joystick area
    // -------------------------------------------------------------------------
    private static void AddOphioAbilityBar(Transform canvasTransform,
                                            SceneNavigator navigator)
    {
        // Remove old ability bar if present
        var old = canvasTransform.Find("OPHIO_AbilityBar");
        if (old != null) DestroyImmediate(old.gameObject);

        // Container — anchored bottom-right, sitting above Invector attack buttons
        // Typical Invector mobile layout: joystick bottom-left, action buttons bottom-right.
        // We push the ability bar up by ~220px so it doesn't overlap.
        var bar = new GameObject("OPHIO_AbilityBar");
        bar.transform.SetParent(canvasTransform, false);
        var barRect           = bar.AddComponent<RectTransform>();
        barRect.anchorMin     = new Vector2(1f, 0f);
        barRect.anchorMax     = new Vector2(1f, 0f);
        barRect.pivot         = new Vector2(1f, 0f);
        barRect.sizeDelta     = new Vector2(400, 100);
        barRect.anchoredPosition = new Vector2(-20f, 230f); // above Invector buttons

        // Add a faint background panel
        var bgImg   = bar.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.45f);

        // Slot data: label, key hint, color
        var slots = new (string label, string key, Color col)[]
        {
            ("ARC\nSLASH",  "Q", NeonCyan),
            ("DISC\nHARGE", "E", NeonCyan),
            ("SURGE",       "R", NeonCyan),
            ("OVER\nLOAD",  "F", SuperBtnColor),
        };

        float btnSize   = 88f;
        float spacing   = 8f;
        float startX    = -(slots.Length * btnSize + (slots.Length - 1) * spacing) / 2f
                          + btnSize / 2f;

        for (int i = 0; i < slots.Length; i++)
        {
            var (label, key, col) = slots[i];
            float posX = startX + i * (btnSize + spacing);

            // Slot root
            var slotGO   = new GameObject($"AbilitySlot_{i}");
            slotGO.transform.SetParent(bar.transform, false);
            var slotRect = slotGO.AddComponent<RectTransform>();
            slotRect.anchorMin        = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax        = new Vector2(0.5f, 0.5f);
            slotRect.pivot            = new Vector2(0.5f, 0.5f);
            slotRect.sizeDelta        = new Vector2(btnSize, btnSize);
            slotRect.anchoredPosition = new Vector2(posX, 0f);

            // Background circle
            var bg     = slotGO.AddComponent<Image>();
            bg.color   = AbilityBtnColor;

            // Make it a button so touch works
            var btn    = slotGO.AddComponent<Button>();
            btn.targetGraphic = bg;
            var colors = btn.colors;
            colors.highlightedColor = col * 1.3f;
            colors.pressedColor     = col;
            btn.colors = colors;

            // Colored border ring (child image)
            var ring     = new GameObject("Ring");
            ring.transform.SetParent(slotGO.transform, false);
            var ringRect = ring.AddComponent<RectTransform>();
            ringRect.anchorMin        = Vector2.zero;
            ringRect.anchorMax        = Vector2.one;
            ringRect.sizeDelta        = new Vector2(6f, 6f);  // slight inset
            ringRect.anchoredPosition = Vector2.zero;
            var ringImg   = ring.AddComponent<Image>();
            ringImg.color = col * 0.85f;
            // Nested: make ring slightly smaller to show as border
            ringRect.sizeDelta = Vector2.zero;
            ringRect.offsetMin = new Vector2(-3f, -3f);
            ringRect.offsetMax = new Vector2( 3f,  3f);
            ringImg.color      = new Color(col.r, col.g, col.b, 0.6f);

            // Ability name label
            var nameLbl = new GameObject("NameLabel");
            nameLbl.transform.SetParent(slotGO.transform, false);
            var nameLblRect = nameLbl.AddComponent<RectTransform>();
            nameLblRect.anchorMin        = new Vector2(0f, 0.35f);
            nameLblRect.anchorMax        = new Vector2(1f, 0.90f);
            nameLblRect.sizeDelta        = Vector2.zero;
            nameLblRect.anchoredPosition = Vector2.zero;
            var nameTxt  = nameLbl.AddComponent<Text>();
            nameTxt.text             = label;
            nameTxt.font             = GetDefaultFont();
            nameTxt.fontSize         = 13;
            nameTxt.color            = Color.white;
            nameTxt.alignment        = TextAnchor.MiddleCenter;
            nameTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            nameTxt.verticalOverflow   = VerticalWrapMode.Overflow;

            // Key hint label (bottom)
            var keyLbl  = new GameObject("KeyLabel");
            keyLbl.transform.SetParent(slotGO.transform, false);
            var keyRect = keyLbl.AddComponent<RectTransform>();
            keyRect.anchorMin        = new Vector2(0f, 0.05f);
            keyRect.anchorMax        = new Vector2(1f, 0.30f);
            keyRect.sizeDelta        = Vector2.zero;
            keyRect.anchoredPosition = Vector2.zero;
            var keyTxt  = keyLbl.AddComponent<Text>();
            keyTxt.text    = key;
            keyTxt.font    = GetDefaultFont();
            keyTxt.fontSize= 16;
            keyTxt.color   = col;
            keyTxt.alignment = TextAnchor.MiddleCenter;

            // Cooldown overlay (dark semi-transparent fill — runtime script controls it)
            var cdOverlay     = new GameObject("CooldownOverlay");
            cdOverlay.transform.SetParent(slotGO.transform, false);
            var cdRect        = cdOverlay.AddComponent<RectTransform>();
            cdRect.anchorMin  = Vector2.zero;
            cdRect.anchorMax  = Vector2.one;
            cdRect.sizeDelta  = Vector2.zero;
            cdRect.anchoredPosition = Vector2.zero;
            var cdImg         = cdOverlay.AddComponent<Image>();
            cdImg.color       = new Color(0f, 0f, 0f, 0.65f);
            cdImg.type        = Image.Type.Filled;
            cdImg.fillMethod  = Image.FillMethod.Radial360;
            cdImg.fillAmount  = 0f;   // 0 = not on cooldown, runtime sets to 1→0
            cdImg.raycastTarget = false;

            // Tag the slot so MobileAbilityHUD runtime script can find it
            slotGO.tag = "Untagged";
            slotGO.name = $"AbilitySlot_{i}";
        }

        Debug.Log("[SceneUIBuilder] OPHIO ability bar (4 slots) added to Arena canvas.");
    }

    // -------------------------------------------------------------------------
    //  Fallback: if Invector mobile prefab not found,
    //  build a minimal joystick + buttons layout using plain UI
    // -------------------------------------------------------------------------
    private static void CreateFallbackMobileUI(Transform canvasTransform)
    {
        Debug.Log("[SceneUIBuilder] Building fallback mobile UI (no Invector prefab).");

        // Left joystick zone hint
        var leftZone    = CreatePanel(canvasTransform, "FallbackJoystickZone",
            new Color(1f, 1f, 1f, 0.06f),
            new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(0f, 0f), new Vector2(200, 200),
            new Vector2(30f, 30f));
        CreateText(leftZone.transform, "JoystickHint", "MOVE\n(Joystick)", 16,
            new Color(1f, 1f, 1f, 0.5f), TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);

        // Right side: attack + aim buttons
        var rightZone   = CreatePanel(canvasTransform, "FallbackActionZone",
            new Color(1f, 1f, 1f, 0.04f),
            new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(1f, 0f), new Vector2(220, 200),
            new Vector2(-30f, 30f));

        CreateButton(rightZone.transform, "FallbackAttackBtn", "ATK",
            new Color(0.6f, 0.1f, 0.1f, 0.9f),
            new Vector2(0.65f, 0.5f), new Vector2(0.65f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(80, 80), Vector2.zero);

        CreateButton(rightZone.transform, "FallbackAimBtn", "AIM",
            new Color(0.1f, 0.3f, 0.6f, 0.9f),
            new Vector2(0.25f, 0.7f), new Vector2(0.25f, 0.7f),
            new Vector2(0.5f, 0.5f), new Vector2(72, 72), Vector2.zero);

        CreateButton(rightZone.transform, "FallbackJumpBtn", "JUMP",
            new Color(0.2f, 0.5f, 0.2f, 0.9f),
            new Vector2(0.25f, 0.3f), new Vector2(0.25f, 0.3f),
            new Vector2(0.5f, 0.5f), new Vector2(72, 72), Vector2.zero);

        Debug.Log("[SceneUIBuilder] Fallback mobile UI created.");
    }

    // =========================================================================
    //  Persistent Managers (MainMenu scene only — DontDestroyOnLoad)
    // =========================================================================
    private static void SetupPersistentManagers()
    {
        // Remove old managers if rebuilding
        DestroyIfExists("OPHIO_Managers");
        DestroyIfExists("GameManagers");

        // ── OPHIO_Managers ── AudioManager + ObjectPoolManager + StatusEffectManager
        var managersGO = new GameObject("OPHIO_Managers");
        managersGO.AddComponent<OPHIO.Core.AudioManager>();
        managersGO.AddComponent<OPHIO.Core.ObjectPoolManager>();
        managersGO.AddComponent<OPHIO.Core.StatusEffectManager>();
        managersGO.AddComponent<OPHIO.UI.MainMenuAudio>();  // plays menu music on Start

        // ── GameManagers ── GameFlowManager + ProgressionManager
        var gameManagersGO = new GameObject("GameManagers");
        gameManagersGO.AddComponent<OPHIO.Arena.GameFlowManager>();
        gameManagersGO.AddComponent<OPHIO.Arena.ProgressionManager>();

        Debug.Log("[SceneUIBuilder] Persistent managers created in MainMenu scene.");
    }

    private static void DestroyIfExists(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) DestroyImmediate(go);
    }

    // =========================================================================
    //  Remaining scenes (unchanged logic, just reformatted)
    // =========================================================================

    private static void BuildMainMenu()
    {
        ClearCanvasAndSetupScene(MainMenuScenePath, out var canvas, out var navigator);

        // ── Singleton managers (DontDestroyOnLoad — only in MainMenu) ──
        SetupPersistentManagers();

        // Play menu music
        // (AudioManager is now in scene — will auto-start on Play)

        CreatePanel(canvas.transform, "BgPanel", DarkBgColor, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        CreateText(canvas.transform, "SubtitleText", "H A R V E S T   G A M E S", 20, GoldenAmber, TextAnchor.MiddleCenter, new Vector2(0.5f,0.75f), new Vector2(0.5f,0.75f), new Vector2(0.5f,0.5f), new Vector2(800,50), Vector2.zero);
        CreateText(canvas.transform, "TitleText", "OPHIO", 110, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f,0.65f), new Vector2(0.5f,0.65f), new Vector2(0.5f,0.5f), new Vector2(800,150), Vector2.zero);
        var btnContainer = CreatePanel(canvas.transform, "ButtonsPanel", Color.clear, new Vector2(0.5f,0.35f), new Vector2(0.5f,0.35f), new Vector2(0.5f,0.5f), new Vector2(400,200), Vector2.zero);
        var playBtn = CreateButton(btnContainer.transform, "PlayButton", "PLAY HARVEST GAMES", GoldenAmber, Vector2.zero, Vector2.zero, new Vector2(0,0), new Vector2(400,60), new Vector2(0,110));
        playBtn.GetComponentInChildren<Text>().color = Color.black;
        var playComp = playBtn.GetComponent<Button>();
        UnityEventTools.AddPersistentListener(playComp.onClick, System.Delegate.CreateDelegate(typeof(UnityAction), navigator, typeof(SceneNavigator).GetMethod("LoadCharacterSelect")) as UnityAction);
        var quitBtn = CreateButton(btnContainer.transform, "QuitButton", "QUIT GAME", ButtonNormalColor, Vector2.zero, Vector2.zero, new Vector2(0,0), new Vector2(400,60), new Vector2(0,30));
        UnityEventTools.AddPersistentListener(quitBtn.GetComponent<Button>().onClick, System.Delegate.CreateDelegate(typeof(UnityAction), navigator, typeof(SceneNavigator).GetMethod("QuitGame")) as UnityAction);
        CreateText(canvas.transform, "DevDesc", "Subject containment test terminal v2.0 — OTTO Corp.", 14, Color.gray, TextAnchor.MiddleCenter, new Vector2(0.5f,0.05f), new Vector2(0.5f,0.05f), new Vector2(0.5f,0.5f), new Vector2(800,40), Vector2.zero);
        SaveAndCloseActiveScene();
    }

    private static void BuildCharacterSelect()
    {
        ClearCanvasAndSetupScene(CharacterSelectScenePath, out var canvas, out var navigator);
        CreatePanel(canvas.transform, "BgPanel", DarkBgColor, Vector2.zero, Vector2.one, new Vector2(0.5f,0.5f), Vector2.zero, Vector2.zero);
        CreateText(canvas.transform, "CS_HeaderText", "SELECT YOUR SUBJECT", 32, GoldenAmber, TextAnchor.MiddleCenter, new Vector2(0.5f,0.9f), new Vector2(0.5f,0.9f), new Vector2(0.5f,0.5f), new Vector2(1000,60), Vector2.zero);
        var cardRow = CreatePanel(canvas.transform, "CardRow", Color.clear, new Vector2(0.5f,0.55f), new Vector2(0.5f,0.55f), new Vector2(0.5f,0.5f), new Vector2(1500,400), Vector2.zero);
        string[] subjects = { "Hawk","Goon","Mac","Gust","Flex" };
        string[] roles    = { "Electric Fighter","Fire Attacker","Energy Tank","Spore Summoner","Melee Brawler" };
        float cw = 240f, sp = 40f;
        float total = cw*5+sp*4;
        float sx = -total/2f+cw/2f;
        var selectMethod = typeof(SceneNavigator).GetMethod("SelectCharacter", new System.Type[]{typeof(string)});
        var selectAction = System.Delegate.CreateDelegate(typeof(UnityAction<string>), navigator, selectMethod) as UnityAction<string>;
        for (int i=0;i<5;i++)
        {
            float px = sx + i*(cw+sp);
            var card = CreatePanel(cardRow.transform, subjects[i]+"_Card", PanelColor, new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(cw,340), new Vector2(px,0));
            CreateText(card.transform,"Name",subjects[i].ToUpper(),28,Color.white,TextAnchor.MiddleCenter,new Vector2(0.5f,0.8f),new Vector2(0.5f,0.8f),new Vector2(0.5f,0.5f),new Vector2(200,40),Vector2.zero);
            CreateText(card.transform,"Role",roles[i],16,GoldenAmber,TextAnchor.MiddleCenter,new Vector2(0.5f,0.6f),new Vector2(0.5f,0.6f),new Vector2(0.5f,0.5f),new Vector2(200,30),Vector2.zero);
            var selBtn = CreateButton(card.transform,"SelectBtn","CHOOSE",ButtonNormalColor,new Vector2(0.5f,0.15f),new Vector2(0.5f,0.15f),new Vector2(0.5f,0.5f),new Vector2(180,45),Vector2.zero);
            UnityEventTools.AddStringPersistentListener(selBtn.GetComponent<Button>().onClick, selectAction, subjects[i]);
        }
        var selPanel = CreatePanel(canvas.transform,"SelectionDisplay",PanelColor,new Vector2(0.5f,0.25f),new Vector2(0.5f,0.25f),new Vector2(0.5f,0.5f),new Vector2(500,60),Vector2.zero);
        navigator.selectedCharacterText = CreateText(selPanel.transform,"SelectionText","SELECTED: HAWK",22,Color.white,TextAnchor.MiddleCenter,Vector2.zero,Vector2.one,new Vector2(0.5f,0.5f),Vector2.zero,Vector2.zero).GetComponent<Text>();
        var confirmBtn = CreateButton(canvas.transform,"ConfirmBtn","CONFIRM SELECTION",GoldenAmber,new Vector2(0.5f,0.12f),new Vector2(0.5f,0.12f),new Vector2(0.5f,0.5f),new Vector2(400,60),Vector2.zero);
        confirmBtn.GetComponentInChildren<Text>().color = Color.black;
        UnityEventTools.AddPersistentListener(confirmBtn.GetComponent<Button>().onClick, System.Delegate.CreateDelegate(typeof(UnityAction), navigator, typeof(SceneNavigator).GetMethod("LoadLoadoutBuilder")) as UnityAction);
        SaveAndCloseActiveScene();
    }

    private static void BuildLoadoutBuilder()
    {
        ClearCanvasAndSetupScene(LoadoutBuilderScenePath, out var canvas, out var navigator);

        // Background
        CreatePanel(canvas.transform, "BgPanel", DarkBgColor,
            Vector2.zero, Vector2.one, new Vector2(0.5f,0.5f), Vector2.zero, Vector2.zero);

        // Header
        CreateText(canvas.transform, "LB_Header", "TACTICAL ABILITY LOADOUT", 32, GoldenAmber,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f,0.94f), new Vector2(0.5f,0.94f), new Vector2(0.5f,0.5f),
            new Vector2(1000,55), Vector2.zero);

        // ── LEFT PANEL — Equipped Slots ─────────────────────
        var eqPanel = CreatePanel(canvas.transform, "EquippedPanel", PanelColor,
            new Vector2(0.18f,0.5f), new Vector2(0.18f,0.5f), new Vector2(0.5f,0.5f),
            new Vector2(340,600), Vector2.zero);

        CreateText(eqPanel.transform, "EquippedTitle", "EQUIPPED", 22, GoldenAmber,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f,0.93f), new Vector2(0.5f,0.93f), new Vector2(0.5f,0.5f),
            new Vector2(300,36), Vector2.zero);

        navigator.selectedCharacterText = CreateText(eqPanel.transform, "CharNameText",
            "SUBJECT: HAWK", 18, Color.white, TextAnchor.MiddleCenter,
            new Vector2(0.5f,0.84f), new Vector2(0.5f,0.84f), new Vector2(0.5f,0.5f),
            new Vector2(300,30), Vector2.zero).GetComponent<Text>();

        // Slot rows
        float[] slotY = { 0.68f, 0.53f, 0.38f, 0.20f };
        string[] slotLabels = { "SLOT 1  [Q]", "SLOT 2  [E]", "SLOT 3  [R]", "SUPER   [F]" };
        Color[]  slotColors = { NeonCyan, NeonCyan, NeonCyan, NeonRed };
        string[] defaults   = { "ARC SLASH", "DISCHARGE", "NEURAL SURGE", "TOTAL OVERLOAD" };

        for (int i = 0; i < 4; i++)
        {
            // Slot bg
            var slotBg = CreatePanel(eqPanel.transform, $"SlotBg_{i}",
                new Color(0.08f,0.08f,0.10f,0.9f),
                new Vector2(0.5f, slotY[i]), new Vector2(0.5f, slotY[i]),
                new Vector2(0.5f,0.5f), new Vector2(300, 56), Vector2.zero);

            // Key label
            CreateText(slotBg.transform, "KeyLabel", slotLabels[i], 13, slotColors[i],
                TextAnchor.MiddleLeft,
                new Vector2(0.05f,0.7f), new Vector2(0.5f,1f),
                new Vector2(0f,0.5f), Vector2.zero, Vector2.zero);

            // Ability name text (updatable)
            var abilityTxt = CreateText(slotBg.transform, "AbilityName", defaults[i], 16,
                Color.white, TextAnchor.MiddleLeft,
                new Vector2(0.05f,0.05f), new Vector2(0.95f,0.55f),
                new Vector2(0f,0.5f), Vector2.zero, Vector2.zero).GetComponent<Text>();

            // Wire to navigator text refs
            switch (i)
            {
                case 0: navigator.active1Text = abilityTxt; break;
                case 1: navigator.active2Text = abilityTxt; break;
                case 2: navigator.active3Text = abilityTxt; break;
                case 3: navigator.superText   = abilityTxt; break;
            }
        }

        // ── CENTER PANEL — Ability Pool ──────────────────────
        var poolPanel = CreatePanel(canvas.transform, "AbilityPoolPanel", PanelColor,
            new Vector2(0.5f,0.52f), new Vector2(0.5f,0.52f), new Vector2(0.5f,0.5f),
            new Vector2(520,600), Vector2.zero);

        CreateText(poolPanel.transform, "PoolTitle", "ABILITY POOL", 22, GoldenAmber,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f,0.93f), new Vector2(0.5f,0.93f), new Vector2(0.5f,0.5f),
            new Vector2(460,36), Vector2.zero);

        CreateText(poolPanel.transform, "PoolHint",
            "Tap ability → then tap slot to equip", 13,
            new Color(0.6f,0.6f,0.6f,1f), TextAnchor.MiddleCenter,
            new Vector2(0.5f,0.86f), new Vector2(0.5f,0.86f), new Vector2(0.5f,0.5f),
            new Vector2(460,26), Vector2.zero);

        // Hawk ability pool (5 abilities in grid)
        string[] poolAbilities = {
            "Arc Slash", "Discharge", "Neural Surge",
            "Volt Dash", "Energy Siphon" };
        string[] poolDescs = {
            "Electric chain slash", "Stored charge burst", "Speed + attack buff",
            "Dash + electric trail", "Melee charge drain" };
        Color[] poolColors = {
            NeonCyan, NeonCyan, NeonCyan, NeonCyan, NeonCyan };

        int cols = 2;
        float cardW = 220f, cardH = 90f, padX = 20f, padY = 12f;
        float startX = -(cardW + padX) * 0.5f;
        float startYPool = 0.75f;

        for (int i = 0; i < poolAbilities.Length; i++)
        {
            int col = i % cols;
            int row = i / cols;
            float px = startX + col * (cardW + padX);
            float py = -row * (cardH + padY * 2f);

            var card = CreatePanel(poolPanel.transform, $"PoolCard_{i}",
                new Color(0.10f,0.10f,0.13f,0.95f),
                new Vector2(0.5f, startYPool), new Vector2(0.5f, startYPool),
                new Vector2(0.5f,0.5f), new Vector2(cardW, cardH),
                new Vector2(px, py - row * 4f));

            // Colored left bar
            var bar = CreatePanel(card.transform, "ColorBar", poolColors[i],
                Vector2.zero, new Vector2(0f,1f), new Vector2(0f,0.5f),
                new Vector2(5f, 0f), Vector2.zero);

            // Ability name
            CreateText(card.transform, "AbilityName", poolAbilities[i].ToUpper(),
                16, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0.07f,0.55f), new Vector2(0.95f,0.95f),
                new Vector2(0f,0.5f), Vector2.zero, Vector2.zero);

            // Description
            CreateText(card.transform, "AbilityDesc", poolDescs[i],
                13, new Color(0.65f,0.65f,0.65f,1f), TextAnchor.MiddleLeft,
                new Vector2(0.07f,0.08f), new Vector2(0.95f,0.5f),
                new Vector2(0f,0.5f), Vector2.zero, Vector2.zero);

            // Select button (covers whole card)
            var btn = card.AddComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = card.GetComponent<UnityEngine.UI.Image>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.18f,0.18f,0.22f,1f);
            colors.pressedColor     = new Color(0.25f,0.25f,0.30f,1f);
            btn.colors = colors;
        }

        // Last pool card — odd one out (center bottom)
        // Already handled above (index 4 goes to col 0, row 2)

        // ── RIGHT PANEL — Character Info ─────────────────────
        var infoPanel = CreatePanel(canvas.transform, "CharInfoPanel", PanelColor,
            new Vector2(0.82f,0.5f), new Vector2(0.82f,0.5f), new Vector2(0.5f,0.5f),
            new Vector2(280,600), Vector2.zero);

        CreateText(infoPanel.transform, "InfoTitle", "CHARACTER", 20, GoldenAmber,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f,0.93f), new Vector2(0.5f,0.93f), new Vector2(0.5f,0.5f),
            new Vector2(240,36), Vector2.zero);

        CreateText(infoPanel.transform, "InfoName", "HAWK", 28, Color.white,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f,0.82f), new Vector2(0.5f,0.82f), new Vector2(0.5f,0.5f),
            new Vector2(240,40), Vector2.zero);

        CreateText(infoPanel.transform, "InfoSubject", "Subject #4", 16, GoldenAmber,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f,0.73f), new Vector2(0.5f,0.73f), new Vector2(0.5f,0.5f),
            new Vector2(240,28), Vector2.zero);

        CreateText(infoPanel.transform, "InfoRole", "Electric Melee Fighter", 15,
            new Color(0.7f,0.7f,0.7f,1f), TextAnchor.MiddleCenter,
            new Vector2(0.5f,0.65f), new Vector2(0.5f,0.65f), new Vector2(0.5f,0.5f),
            new Vector2(240,26), Vector2.zero);

        // Passive box
        var passiveBox = CreatePanel(infoPanel.transform, "PassiveBox",
            new Color(0.08f,0.08f,0.10f,0.9f),
            new Vector2(0.5f,0.48f), new Vector2(0.5f,0.48f), new Vector2(0.5f,0.5f),
            new Vector2(248,100), Vector2.zero);

        CreateText(passiveBox.transform, "PassiveLabel", "PASSIVE", 11,
            GoldenAmber, TextAnchor.UpperLeft,
            new Vector2(0.05f,0.85f), new Vector2(0.95f,1f),
            new Vector2(0f,0.5f), Vector2.zero, Vector2.zero);

        CreateText(passiveBox.transform, "PassiveName", "Living Conductor", 15,
            Color.white, TextAnchor.UpperLeft,
            new Vector2(0.05f,0.55f), new Vector2(0.95f,0.82f),
            new Vector2(0f,0.5f), Vector2.zero, Vector2.zero);

        CreateText(passiveBox.transform, "PassiveDesc",
            "Absorbs electric energy\nto boost attack speed", 12,
            new Color(0.65f,0.65f,0.65f,1f), TextAnchor.UpperLeft,
            new Vector2(0.05f,0.08f), new Vector2(0.95f,0.52f),
            new Vector2(0f,0.5f), Vector2.zero, Vector2.zero);

        // ── LAUNCH BUTTON ─────────────────────────────────────
        var launchBtn = CreateButton(canvas.transform, "LaunchBtn",
            "LAUNCH HARVEST ARENA", GoldenAmber,
            new Vector2(0.5f,0.05f), new Vector2(0.5f,0.05f), new Vector2(0.5f,0.5f),
            new Vector2(420,58), Vector2.zero);
        launchBtn.GetComponentInChildren<Text>().color = Color.black;
        UnityEventTools.AddPersistentListener(
            launchBtn.GetComponent<Button>().onClick,
            System.Delegate.CreateDelegate(typeof(UnityAction), navigator,
                typeof(SceneNavigator).GetMethod("LoadArena")) as UnityAction);

        SaveAndCloseActiveScene();
    }

    private static void BuildEndScreen()
    {
        ClearCanvasAndSetupScene(EndScreenScenePath, out var canvas, out var navigator);
        CreatePanel(canvas.transform,"BgPanel",DarkBgColor,Vector2.zero,Vector2.one,new Vector2(0.5f,0.5f),Vector2.zero,Vector2.zero);
        CreateText(canvas.transform,"ES_SubText","H A R V E S T   G A M E   C O N C L U S I O N",20,Color.gray,TextAnchor.MiddleCenter,new Vector2(0.5f,0.85f),new Vector2(0.5f,0.85f),new Vector2(0.5f,0.5f),new Vector2(800,40),Vector2.zero);
        navigator.resultText = CreateText(canvas.transform,"OutcomeText","VICTORY ACHIEVED",65,GoldenAmber,TextAnchor.MiddleCenter,new Vector2(0.5f,0.75f),new Vector2(0.5f,0.75f),new Vector2(0.5f,0.5f),new Vector2(800,100),Vector2.zero).GetComponent<Text>();
        var statsCard = CreatePanel(canvas.transform,"StatsCard",PanelColor,new Vector2(0.5f,0.45f),new Vector2(0.5f,0.45f),new Vector2(0.5f,0.5f),new Vector2(600,300),Vector2.zero);
        CreateText(statsCard.transform,"StatsTitle","TEST REPORT DATA",20,GoldenAmber,TextAnchor.MiddleCenter,new Vector2(0.5f,0.88f),new Vector2(0.5f,0.88f),new Vector2(0.5f,0.5f),new Vector2(500,30),Vector2.zero);
        CreateText(statsCard.transform,"StatLine1","Entities Purged: 16",16,Color.white,TextAnchor.MiddleLeft,new Vector2(0.1f,0.65f),new Vector2(0.9f,0.65f),new Vector2(0.5f,0.5f),new Vector2(0,25),Vector2.zero);
        CreateText(statsCard.transform,"StatLine2","Performance Score: [SSS]",16,NeonCyan,TextAnchor.MiddleLeft,new Vector2(0.1f,0.45f),new Vector2(0.9f,0.45f),new Vector2(0.5f,0.5f),new Vector2(0,25),Vector2.zero);
        CreateText(statsCard.transform,"StatLine3","OTTO Facility Status: Secure",16,Color.gray,TextAnchor.MiddleLeft,new Vector2(0.1f,0.25f),new Vector2(0.9f,0.25f),new Vector2(0.5f,0.5f),new Vector2(0,25),Vector2.zero);
        var btnContainer = CreatePanel(canvas.transform,"EndButtons",Color.clear,new Vector2(0.5f,0.18f),new Vector2(0.5f,0.18f),new Vector2(0.5f,0.5f),new Vector2(600,60),Vector2.zero);
        var retryBtn = CreateButton(btnContainer.transform,"PlayAgainBtn","RETRY MISSION",GoldenAmber,new Vector2(0,0.5f),new Vector2(0,0.5f),new Vector2(0,0.5f),new Vector2(280,55),Vector2.zero);
        retryBtn.GetComponentInChildren<Text>().color = Color.black;
        UnityEventTools.AddPersistentListener(retryBtn.GetComponent<Button>().onClick, System.Delegate.CreateDelegate(typeof(UnityAction), navigator, typeof(SceneNavigator).GetMethod("LoadCharacterSelect")) as UnityAction);
        var menuBtn = CreateButton(btnContainer.transform,"MainMenuBtn","MAIN MENU",ButtonNormalColor,new Vector2(1,0.5f),new Vector2(1,0.5f),new Vector2(1,0.5f),new Vector2(280,55),Vector2.zero);
        UnityEventTools.AddPersistentListener(menuBtn.GetComponent<Button>().onClick, System.Delegate.CreateDelegate(typeof(UnityAction), navigator, typeof(SceneNavigator).GetMethod("LoadMainMenu")) as UnityAction);
        SaveAndCloseActiveScene();
    }
}
