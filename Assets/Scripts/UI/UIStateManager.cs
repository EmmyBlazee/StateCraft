using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using StateCraft.Core;
using StateCraft.Data;

namespace StateCraft.UI
{
    /// <summary>
    /// Coordinates the overall UI state, transitions, and global input blocking.
    /// </summary>
    public class UIStateManager : MonoBehaviour
    {
        public static UIStateManager Instance { get; private set; }

        [Header("UI Documents")]
        public UIDocument dashboardDoc;
        public UIDocument countrySelectorDoc;
        public UIDocument countryPanelDoc;
        public UIDocument newsTickerDoc;
        private UIDocument _uiDoc;

        private VisualElement _dashboard;
        private VisualElement _countryPanel;
        private VisualElement _newsTicker;
        private VisualElement _selector;

        private void Awake()
        {
            // Ensure CriticalDispatchController exists on this GameObject for Critical inbox popups
            if (CriticalDispatchController.Instance == null)
            {
                if (GetComponent<CriticalDispatchController>() == null)
                    gameObject.AddComponent<CriticalDispatchController>();
            }
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private IEnumerator Start()
        {
            yield return StartCoroutine(DeferredInitialization());
        }

        private IEnumerator DeferredInitialization()
        {
            yield return new WaitForEndOfFrame();
            InitializeElements();
            StartCoroutine(GameStartSequence());
        }

        private void InitializeElements()
        {
            _uiDoc = GetComponent<UIDocument>();
            if (_uiDoc == null) _uiDoc = FindObjectOfType<UIDocument>();
            
            if (_uiDoc == null) 
            {
                Debug.LogError("UI: UIStateManager requires a UIDocument component!");
                return;
            }

            var root = _uiDoc.rootVisualElement;
            if (root == null) return;

            _dashboard = root.Q<VisualElement>("MainDashboard");
            _countryPanel = root.Q<VisualElement>("CountryInfoPanel");
            _newsTicker = root.Q<VisualElement>("NewsTickerContainer");
            _selector = root.Q<VisualElement>("SelectorRoot");

            // GLOBAL FALLBACK: If separate documents are used
            var allDocs = FindObjectsOfType<UIDocument>();
            if (_countryPanel == null) {
                foreach(var doc in allDocs) {
                    var found = doc.rootVisualElement?.Q<VisualElement>("CountryInfoPanel");
                    if (found != null) { _countryPanel = found; break; }
                }
            }
            if (_newsTicker == null) {
                foreach(var doc in allDocs) {
                    var found = doc.rootVisualElement?.Q<VisualElement>("NewsTickerContainer");
                    if (found != null) { _newsTicker = found; break; }
                }
            }
            if (_selector == null) {
                foreach(var doc in allDocs) {
                    var found = doc.rootVisualElement?.Q<VisualElement>("SelectorRoot");
                    if (found != null) { _selector = found; break; }
                }
            }

            // Initial states: Hide everything except the main dashboard sequence
            if (_dashboard != null) {
                _dashboard.style.display = DisplayStyle.Flex;
                _dashboard.style.opacity = 1;
            }
            
            // Explicitly hide these to prevent them from blocking clicks
            if (_countryPanel != null) {
                _countryPanel.style.display = DisplayStyle.None;
                _countryPanel.style.opacity = 0;
            }
            if (_newsTicker != null) {
                _newsTicker.style.display = DisplayStyle.None;
                _newsTicker.style.opacity = 0;
            }
            
            // Selector always starts hidden — selection now happens in SplashScene.
            // It will only be shown manually if needed (e.g., in-editor fallback).
            if (_selector != null) {
                _selector.style.display = DisplayStyle.None;
                _selector.style.opacity = 0;
                _selector.pickingMode = PickingMode.Ignore;
            }

            // Cleanup Legacy UI
            var allCanvases = Resources.FindObjectsOfTypeAll<Canvas>();
            foreach (var canvas in allCanvases)
            {
                if (canvas.gameObject.scene.name != null && canvas.GetComponent<UIDocument>() == null)
                {
                    canvas.gameObject.SetActive(false);
}
            }

            var allRaycasters = Resources.FindObjectsOfTypeAll<UnityEngine.UI.GraphicRaycaster>();
            foreach (var ray in allRaycasters)
            {
                if (ray.gameObject.scene.name != null && ray.GetComponent<UIDocument>() == null)
                {
                    ray.enabled = false;
                }
            }

            // Cleanup Redundant UIDocuments (Legacy check)
            string[] legacyUxmls = { "CountryInfo_Legacy", "NewsTicker_Old" }; // Only nuke truly old stuff
            
            foreach (var doc in allDocs)
            {
                if (doc.gameObject == _uiDoc.gameObject) continue;
                if (doc.gameObject.scene.name == null) continue;

                if (doc.visualTreeAsset != null && System.Array.Exists(legacyUxmls, name => doc.visualTreeAsset.name.Contains(name)))
                {
doc.gameObject.SetActive(false);
                }
            }

}

        private IEnumerator GameStartSequence()
        {
            // ── Bootstrap from GameContext (set by SplashScene) ───────────────
            if (GameManager.Instance != null)
            {
                // Apply player name
                if (!string.IsNullOrEmpty(GameContext.PlayerName))
                    GameManager.Instance.playerName = GameContext.PlayerName;

                // Apply selected country
                if (GameManager.Instance.playerCountry == null && !string.IsNullOrEmpty(GameContext.SelectedCountryName))
                {
                    CountryDataSO found = null;

#if UNITY_EDITOR
                    // Editor: scan Assets/Data/Countries directly
                    var guids = UnityEditor.AssetDatabase.FindAssets("t:CountryDataSO", new[] { "Assets/Data/Countries" });
                    foreach (var guid in guids)
                    {
                        var path  = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                        var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<CountryDataSO>(path);
                        if (asset != null && asset.countryName == GameContext.SelectedCountryName)
                        { found = asset; break; }
                    }
#else
                    // Build: load via CountryRegistry (created by SplashSceneBuilder)
                    var registry = Resources.Load<StateCraft.Data.CountryRegistry>("CountryRegistry");
                    if (registry != null)
                        found = registry.FindByName(GameContext.SelectedCountryName);
                    else
                        Debug.LogError("[UIStateManager] CountryRegistry not found in Resources/! Run StateCraft → Build Splash Scene in the editor first.");
#endif
                    if (found != null)
                    {
                        GameManager.Instance.playerCountry = found;
                        Debug.Log($"[UIStateManager] Player country set to: {found.countryName}");
                    }
                    else
                    {
                        Debug.LogError($"[UIStateManager] Could not find CountryDataSO for '{GameContext.SelectedCountryName}'! Check Assets/Data/Countries.");
                    }
                }

                // Handle Load Game
                if (GameContext.ShouldLoadSave)
                {
                    GameContext.ShouldLoadSave = false;
                    GameManager.Instance.LoadGame();
                    yield return null; // allow LoadGame to set playerCountry
                }

                // ── Editor fallback: if no country set, show selector ─────────
                if (GameManager.Instance.playerCountry == null)
                {
                    Debug.LogWarning("[UIStateManager] No country in GameContext. Showing selector as fallback.");
                    if (_selector != null)
                    {
                        _selector.style.display  = DisplayStyle.Flex;
                        _selector.style.opacity  = 1;
                        _selector.pickingMode    = PickingMode.Position;
                    }
                    while (GameManager.Instance.playerCountry == null) yield return null;
                    if (_selector != null) ApplySafeTransition(_selector, false, 0.5f);
                    yield return new WaitForSeconds(0.5f);
                }
            }

            // ── Phase 1: Fade in HUD ──────────────────────────────────────────
            var root    = _uiDoc.rootVisualElement;
            var topBar  = root.Q<VisualElement>("TopBar");
            var sidebar = root.Q<VisualElement>("Sidebar");

            if (topBar  != null) ApplySafeTransition(topBar,  true, 1.0f);
            if (sidebar != null) ApplySafeTransition(sidebar, true, 1.0f);

            yield return new WaitForSeconds(0.5f);

            // ── Phase 2: Scenario Briefing ────────────────────────────────────
            var player = GameManager.Instance?.playerCountry;
            if (player != null && !player.hasShownBriefing)
            {
                yield return StartCoroutine(ShowScenarioBriefing(root, player));
                player.hasShownBriefing = true;
            }

            // ── Phase 3: Fade in News Ticker ──────────────────────────────────
            if (_newsTicker != null) ApplySafeTransition(_newsTicker, true, 0.8f);
        }

        // ─── Scenario Briefing Screen ─────────────────────────────────────────
        // Full-screen overlay shown once at game start. Pulls text from
        // CountryDataSO.scenarioTitle / scenarioContext / scenarioPriorities.
        // Falls back to generic text if the fields are empty.
        private IEnumerator ShowScenarioBriefing(VisualElement root, StateCraft.Data.CountryDataSO country)
        {
            bool dismissed = false;

            // ── Resolve display text ──────────────────────────────────────────
            string sym      = string.IsNullOrEmpty(country.currencySymbol) ? "$" : country.currencySymbol;
            string title    = string.IsNullOrEmpty(country.scenarioTitle)
                ? $"{country.countryName.ToUpper()} — ASSUME THE PRESIDENCY"
                : country.scenarioTitle.ToUpper();

            string context  = string.IsNullOrEmpty(country.scenarioContext)
                ? $"You have assumed the presidency of {country.countryName}. " +
                  $"The nation faces a complex web of economic pressures, regional tensions, " +
                  $"and political expectations. Every decision you make will shape the future."
                : country.scenarioContext;

            string priorities = string.IsNullOrEmpty(country.scenarioPriorities)
                ? "• Monitor your treasury — ensure income exceeds spending\n" +
                  "• Check stability across your regions — unrest spreads fast\n" +
                  "• Watch the election timer — approval rating determines your survival"
                : country.scenarioPriorities;

            // ── Spawn Dedicated Host Document ─────────────────────────────────
            var hostGO = new GameObject("ScenarioBriefingHost");
            var hostDoc = hostGO.AddComponent<UIDocument>();
            hostDoc.panelSettings = _uiDoc.panelSettings;
            hostDoc.sortingOrder = 100;
            var hostRoot = hostDoc.rootVisualElement;

            // ── Build full-screen overlay ─────────────────────────────────────
            var overlay = new VisualElement();
            overlay.name = "ScenarioBriefingOverlay";
            overlay.style.position        = Position.Absolute;
            overlay.style.top = overlay.style.left = overlay.style.right = overlay.style.bottom = 0;
            overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.92f);
            overlay.style.justifyContent  = Justify.Center;
            overlay.style.alignItems      = Align.Center;
            overlay.style.flexDirection   = FlexDirection.Column;
            overlay.pickingMode           = PickingMode.Position;
            hostRoot.Add(overlay);

            // ── Card ──────────────────────────────────────────────────────────
            var card = new VisualElement();
            card.style.width           = new StyleLength(new Length(58f, LengthUnit.Percent));
            card.style.maxWidth        = 680;
            card.style.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 1f);
            card.style.borderTopWidth  = card.style.borderBottomWidth =
            card.style.borderLeftWidth = card.style.borderRightWidth = 2;
            var gold = new Color(0.95f, 0.78f, 0.2f);
            card.style.borderTopColor  = card.style.borderBottomColor =
            card.style.borderLeftColor = card.style.borderRightColor = gold;
            card.style.borderTopLeftRadius    = card.style.borderTopRightRadius    = 8;
            card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 8;
            card.style.paddingBottom = 28;
            card.pickingMode = PickingMode.Position;
            overlay.Add(card);

            // Gold top stripe
            var stripe = new VisualElement();
            stripe.style.height          = 5;
            stripe.style.backgroundColor = gold;
            card.Add(stripe);

            // Header area
            var header = new VisualElement();
            header.style.paddingTop    = 24;
            header.style.paddingLeft   = header.style.paddingRight  = 28;
            header.style.paddingBottom = 16;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = new Color(1f, 1f, 1f, 0.08f);
            card.Add(header);

            // CLASSIFIED stamp label
            var stamp = new Label("CLASSIFIED SITUATION BRIEFING");
            stamp.style.color                  = gold;
            stamp.style.fontSize               = 9;
            stamp.style.unityFontStyleAndWeight = FontStyle.Bold;
            stamp.style.letterSpacing          = 3;
            stamp.style.marginBottom           = 10;
            header.Add(stamp);

            // Country title
            var titleLabel = new Label(title);
            titleLabel.style.color                  = Color.white;
            titleLabel.style.fontSize               = 22;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.whiteSpace             = WhiteSpace.Normal;
            titleLabel.style.marginBottom           = 6;
            header.Add(titleLabel);

            // Date subtitle
            string gameDate = GameManager.Instance?.TurnSystem?.CurrentDate ?? "YEAR ONE";
            var dateLabel = new Label(gameDate.ToUpper());
            dateLabel.style.color    = new Color(0.6f, 0.6f, 0.6f);
            dateLabel.style.fontSize = 10;
            header.Add(dateLabel);

            // Body area
            var body = new VisualElement();
            body.style.paddingTop    = 20;
            body.style.paddingLeft   = body.style.paddingRight  = 28;
            body.style.paddingBottom = 0;
            card.Add(body);

            // Situation context paragraph
            var contextLabel = new Label(context);
            contextLabel.style.color      = new Color(0.82f, 0.82f, 0.82f);
            contextLabel.style.fontSize   = 12;
            contextLabel.style.whiteSpace = WhiteSpace.Normal;
            contextLabel.style.marginBottom = 20;
            body.Add(contextLabel);

            // Divider
            var divider = new VisualElement();
            divider.style.height          = 1;
            divider.style.backgroundColor = new Color(1f, 1f, 1f, 0.1f);
            divider.style.marginBottom    = 16;
            body.Add(divider);

            // Priorities header
            var priHeader = new Label("YOUR IMMEDIATE PRIORITIES");
            priHeader.style.color                  = gold;
            priHeader.style.fontSize               = 10;
            priHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            priHeader.style.letterSpacing          = 2;
            priHeader.style.marginBottom           = 10;
            body.Add(priHeader);

            // Priority bullets
            var priLabel = new Label(priorities);
            priLabel.style.color      = new Color(0.88f, 0.88f, 0.88f);
            priLabel.style.fontSize   = 12;
            priLabel.style.whiteSpace = WhiteSpace.Normal;
            priLabel.style.marginBottom = 24;
            body.Add(priLabel);

            // Stats row — quick snapshot
            var statsRow = new VisualElement();
            statsRow.style.flexDirection    = FlexDirection.Row;
            statsRow.style.justifyContent   = Justify.SpaceAround;
            statsRow.style.backgroundColor  = new Color(1f, 1f, 1f, 0.04f);
            statsRow.style.borderTopWidth   = statsRow.style.borderBottomWidth = 1;
            statsRow.style.borderTopColor   = statsRow.style.borderBottomColor = new Color(1f, 1f, 1f, 0.08f);
            statsRow.style.paddingTop       = statsRow.style.paddingBottom = 12;
            statsRow.style.marginBottom     = 20;
            body.Add(statsRow);

            void AddStat(string label, string value, Color col)
            {
                var col_ = new VisualElement();
                col_.style.alignItems = Align.Center;
                var valLbl = new Label(value);
                valLbl.style.color = col;
                valLbl.style.fontSize = 14;
                valLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                var lbl = new Label(label);
                lbl.style.color = new Color(0.55f, 0.55f, 0.55f);
                lbl.style.fontSize = 9;
                col_.Add(valLbl);
                col_.Add(lbl);
                statsRow.Add(col_);
            }

            string treasuryStr = $"{sym}{country.treasury:F1}B";
            string stabilityStr = $"{country.stability:F0}%";
            string approvalStr  = $"{country.publicApproval:F0}%";
            Color  approvalCol  = country.publicApproval >= 60f ? Color.green
                                : country.publicApproval >= 45f ? new Color(1f, 0.72f, 0.1f)
                                : new Color(1f, 0.3f, 0.3f);

            AddStat("TREASURY",  treasuryStr,  new Color(0.3f, 1f, 0.55f));
            AddStat("STABILITY", stabilityStr, country.stability >= 60f ? new Color(0.3f, 1f, 0.55f) : new Color(1f, 0.72f, 0.1f));
            AddStat("APPROVAL",  approvalStr,  approvalCol);
            AddStat("POPULATION", $"{(country.population / 1_000_000f):F0}M", Color.white);

            // Begin button
            var footer = new VisualElement();
            footer.style.paddingLeft = footer.style.paddingRight = 28;
            card.Add(footer);

            var beginBtn = new Button(() => { dismissed = true; });
            beginBtn.text = "▶  ASSUME THE PRESIDENCY";
            beginBtn.style.height          = 44;
            beginBtn.style.fontSize        = 13;
            beginBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            beginBtn.style.backgroundColor = new Color(0.95f, 0.78f, 0.2f, 0.18f);
            beginBtn.style.borderTopWidth  = beginBtn.style.borderBottomWidth =
            beginBtn.style.borderLeftWidth = beginBtn.style.borderRightWidth = 1;
            beginBtn.style.borderTopColor  = beginBtn.style.borderBottomColor =
            beginBtn.style.borderLeftColor = beginBtn.style.borderRightColor = gold;
            beginBtn.style.color           = gold;
            beginBtn.style.borderTopLeftRadius    = beginBtn.style.borderTopRightRadius    = 4;
            beginBtn.style.borderBottomLeftRadius = beginBtn.style.borderBottomRightRadius = 4;
            footer.Add(beginBtn);

            // ── Wait for dismissal ────────────────────────────────────────────
            while (!dismissed) yield return null;

            // Fade out overlay
            float t = 0f;
            while (t < 0.4f)
            {
                t += UnityEngine.Time.deltaTime;
                overlay.style.opacity = Mathf.Lerp(1f, 0f, t / 0.4f);
                yield return null;
            }
            Destroy(hostGO);
        }

        public IEnumerator FadeIn(VisualElement element, float duration)
        {
            if (element == null) yield break;
            
            element.style.display = DisplayStyle.Flex;
            float time = 0;
            // Force start at 0 if it's currently invisible
            float start = element.style.opacity.value;
            if (float.IsNaN(start)) start = 0;

            while (time < duration) {
                time += Time.deltaTime;
                element.style.opacity = Mathf.Lerp(start, 1, time / duration);
                yield return null;
            }
            element.style.opacity = 1;
        }

        public IEnumerator FadeOut(VisualElement element, float duration)
        {
            if (element == null) yield break;
            
            float time = 0;
            float start = element.style.opacity.value;
            if (float.IsNaN(start)) start = 1;

            while (time < duration) {
                time += Time.deltaTime;
                element.style.opacity = Mathf.Lerp(start, 0, time / duration);
                yield return null;
            }
            element.style.opacity = 0;
            element.style.display = DisplayStyle.None;
        }

        public void HideCountryPanel()
        {
            if (_countryPanel != null) StartCoroutine(FadeOut(_countryPanel, 0.3f));
        }

        public void ApplySafeTransition(VisualElement element, bool show, float duration)
        {
            if (element == null) return;
            element.pickingMode = show ? PickingMode.Position : PickingMode.Ignore;

            // Toggle children picking mode — but SKIP ScrollView internal elements
            // (unity-scroller, unity-slider, unity-dragger) which manage their own picking
            element.Query<VisualElement>().ForEach(ve =>
            {
                string n = ve.name ?? "";
                string cls = ve.GetType().Name;
                // Never override ScrollView internals or they break in builds
                bool isScrollInternal = n.StartsWith("unity-") || 
                                        cls == "ScrollView" || 
                                        cls == "Scroller" ||
                                        cls == "Slider";
                if (!isScrollInternal)
                    ve.pickingMode = show ? PickingMode.Position : PickingMode.Ignore;
            });
            StartCoroutine(show ? FadeIn(element, duration) : FadeOut(element, duration));
        }

        /// <summary>
        /// Fix all ScrollViews in a panel — call after Show() to ensure scrollbars work in builds.
        /// </summary>
        // ─────────────────────────────────────────────────────────
        // GAME OVER SCREEN
        // ─────────────────────────────────────────────────────────

        public void ShowGameOver(StateCraft.Data.CountryDataSO country, float approval,
                                  int monthsInOffice, bool isCoup = false)
        {
            if (_uiDoc == null || _uiDoc.rootVisualElement == null) return;
            var root = _uiDoc.rootVisualElement;

            // Pause everything
            if (GameManager.Instance != null)
                GameManager.Instance.isRealTime = false;

            // Build full-screen game over overlay
            var overlay = new VisualElement();
            overlay.name = "GameOverOverlay";
            overlay.style.position        = Position.Absolute;
            overlay.style.top = 0; overlay.style.left  = 0;
            overlay.style.right= 0; overlay.style.bottom= 0;
            overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.92f);
            overlay.style.justifyContent  = Justify.Center;
            overlay.style.alignItems      = Align.Center;
            overlay.style.flexDirection   = FlexDirection.Column;
            overlay.pickingMode           = PickingMode.Position;
            overlay.BringToFront();

            // Title
            string title     = isCoup ? "COUP D'ÉTAT" : "ELECTORAL DEFEAT";
            string subtitle  = isCoup ? "Your government has been overthrown"
                                      : "You have been voted out of office";
            Color accentColor= isCoup ? new Color(1f, 0.25f, 0.25f)
                                      : new Color(0.95f, 0.72f, 0.1f);

            // Accent line
            var topLine = new VisualElement();
            topLine.style.width           = new StyleLength(new Length(40f, LengthUnit.Percent));
            topLine.style.height          = 3;
            topLine.style.backgroundColor = accentColor;
            topLine.style.marginBottom    = 30;
            overlay.Add(topLine);

            var titleLabel = new Label(title);
            titleLabel.style.fontSize    = 36;
            titleLabel.style.color       = accentColor;
            titleLabel.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
            titleLabel.style.marginBottom= 8;
            overlay.Add(titleLabel);

            var subLabel = new Label(subtitle);
            subLabel.style.fontSize  = 14;
            subLabel.style.color     = new Color(0.7f, 0.7f, 0.75f);
            subLabel.style.marginBottom = 40;
            overlay.Add(subLabel);

            // Stats card
            var card = new VisualElement();
            card.style.width            = new StyleLength(new Length(40f, LengthUnit.Percent));
            card.style.backgroundColor  = new Color(0.06f, 0.06f, 0.10f, 0.98f);
            card.style.paddingTop       = 24; card.style.paddingBottom = 24;
            card.style.paddingLeft      = 28; card.style.paddingRight  = 28;
            card.style.borderTopWidth   = 1; card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth  = 1; card.style.borderRightWidth  = 1;
            Color borderCol             = new Color(accentColor.r * 0.5f, accentColor.g * 0.5f, accentColor.b * 0.5f, 0.6f);
            card.style.borderTopColor   = borderCol; card.style.borderBottomColor = borderCol;
            card.style.borderLeftColor  = borderCol; card.style.borderRightColor  = borderCol;
            card.style.borderTopLeftRadius    = 6; card.style.borderTopRightRadius    = 6;
            card.style.borderBottomLeftRadius = 6; card.style.borderBottomRightRadius = 6;
            card.style.marginBottom     = 32;

            // Legacy header
            var legacyHeader = new Label("YOUR LEGACY");
            legacyHeader.style.color    = new Color(0.5f, 0.5f, 0.65f);
            legacyHeader.style.fontSize = 9;
            legacyHeader.style.letterSpacing = 2f;
            legacyHeader.style.marginBottom  = 16;
            card.Add(legacyHeader);

            void AddStat(string label, string value, UnityEngine.Color col)
            {
                var row = new VisualElement();
                row.style.flexDirection  = FlexDirection.Row;
                row.style.justifyContent = Justify.SpaceBetween;
                row.style.marginBottom   = 10;
                row.Add(new Label(label) { style = { color = new Color(0.6f, 0.6f, 0.7f), fontSize = 10 } });
                row.Add(new Label(value) { style = { color = col, fontSize = 10,
                    unityFontStyleAndWeight = UnityEngine.FontStyle.Bold } });
                card.Add(row);
            }

            int years  = monthsInOffice / 12;
            int months = monthsInOffice % 12;
            AddStat("TIME IN OFFICE",     $"{years}y {months}m",                    Color.white);
            AddStat("FINAL APPROVAL",     $"{approval:F0}%",
                approval >= 50f ? new Color(0.3f, 1f, 0.5f) : new Color(1f, 0.4f, 0.4f));
            AddStat("FINAL STABILITY",    $"{country.stability:F0}%",
                country.stability >= 50f ? Color.green : Color.red);
            AddStat("FINAL PRESTIGE",     $"{country.globalPrestige:F0}/100",        Color.white);
            AddStat("ALLIANCES FORMED",   $"{country.allianceCount}",               Color.white);
            AddStat("TOTAL KIA",          $"{country.totalKIA:N0}",
                country.totalKIA > 5000 ? new Color(1f, 0.4f, 0.4f) : Color.white);
            AddStat("TRADE AGREEMENTS",   $"{country.activeTradeAgreements?.Count ?? 0}", Color.white);

            overlay.Add(card);

            // Verdict
            string verdict = approval >= 70f ? "A RESPECTED LEADER — history will remember you fondly." :
                             approval >= 55f ? "A CAPABLE ADMINISTRATOR — mixed results, mixed legacy." :
                             approval >= 40f ? "A FLAWED LEADER — good intentions, poor execution." :
                             isCoup         ? "OVERTHROWN — you lost control of the nation." :
                                             "REJECTED BY THE PEOPLE — the electorate has spoken.";

            var verdictLabel = new Label($"\u201C{verdict}\u201D");
            verdictLabel.style.color     = new Color(0.65f, 0.65f, 0.75f);
            verdictLabel.style.fontSize  = 11;
            verdictLabel.style.whiteSpace = WhiteSpace.Normal;
            verdictLabel.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
            verdictLabel.style.marginBottom   = 32;
            verdictLabel.style.width = new StyleLength(new Length(42f, LengthUnit.Percent));
            overlay.Add(verdictLabel);

            // Buttons row
            var btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;

            var restartBtn = new Button(() =>
            {
                // Return to Main Menu
                UnityEngine.SceneManagement.SceneManager.LoadScene("SplashScene");
            }) { text = "🏠  MAIN MENU" };

            restartBtn.style.height      = 40;
            restartBtn.style.paddingLeft = 28; restartBtn.style.paddingRight = 28;
            restartBtn.style.fontSize    = 11;
            restartBtn.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
            restartBtn.style.backgroundColor = new Color(0.12f, 0.35f, 0.18f, 0.95f);
            restartBtn.style.borderTopWidth = 1; restartBtn.style.borderBottomWidth = 1;
            restartBtn.style.borderLeftWidth= 1; restartBtn.style.borderRightWidth  = 1;
            restartBtn.style.borderTopColor    = new Color(0.2f, 0.75f, 0.35f, 0.8f);
            restartBtn.style.borderBottomColor = new Color(0.2f, 0.75f, 0.35f, 0.8f);
            restartBtn.style.borderLeftColor   = new Color(0.2f, 0.75f, 0.35f, 0.8f);
            restartBtn.style.borderRightColor  = new Color(0.2f, 0.75f, 0.35f, 0.8f);
            restartBtn.style.borderTopLeftRadius    = 4; restartBtn.style.borderTopRightRadius    = 4;
            restartBtn.style.borderBottomLeftRadius = 4; restartBtn.style.borderBottomRightRadius = 4;
            restartBtn.style.color = Color.white;
            restartBtn.style.marginRight = 12;
            btnRow.Add(restartBtn);

            var quitBtn = new Button(() =>
            {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                UnityEngine.Application.Quit();
                #endif
            }) { text = "✕  QUIT TO DESKTOP" };

            quitBtn.style.height      = 40;
            quitBtn.style.paddingLeft = 28; quitBtn.style.paddingRight = 28;
            quitBtn.style.fontSize    = 11;
            quitBtn.style.backgroundColor = new Color(0.25f, 0.08f, 0.08f, 0.95f);
            quitBtn.style.borderTopWidth = 1; quitBtn.style.borderBottomWidth = 1;
            quitBtn.style.borderLeftWidth= 1; quitBtn.style.borderRightWidth  = 1;
            quitBtn.style.borderTopColor    = new Color(0.65f, 0.18f, 0.18f, 0.8f);
            quitBtn.style.borderBottomColor = new Color(0.65f, 0.18f, 0.18f, 0.8f);
            quitBtn.style.borderLeftColor   = new Color(0.65f, 0.18f, 0.18f, 0.8f);
            quitBtn.style.borderRightColor  = new Color(0.65f, 0.18f, 0.18f, 0.8f);
            quitBtn.style.borderTopLeftRadius    = 4; quitBtn.style.borderTopRightRadius    = 4;
            quitBtn.style.borderBottomLeftRadius = 4; quitBtn.style.borderBottomRightRadius = 4;
            quitBtn.style.color = Color.white;
            btnRow.Add(quitBtn);

            overlay.Add(btnRow);
            root.Add(overlay);
        }

        public static void FixScrollViews(VisualElement root)
        {
            if (root == null) return;
            root.Query<ScrollView>().ForEach(sv =>
            {
                sv.pickingMode = PickingMode.Position;
                sv.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
                sv.scrollDecelerationRate = 0.135f;
                sv.elasticity = 0.1f;
                // Ensure all scrollbar children are pickable
                sv.Query<VisualElement>().ForEach(ve =>
                {
                    string n = ve.name ?? "";
                    if (n.StartsWith("unity-"))
                        ve.pickingMode = PickingMode.Position;
                });
            });
        }

        /// <summary>
        /// Logs any VisualElement whose resolved layout width or height exceeds its parent's.
        /// Call after a panel becomes visible so layout has been resolved.
        /// Uses GeometryChangedEvent to wait for the first layout pass.
        /// </summary>
        public static void LogOverflowingElements(VisualElement root, string panelLabel = "")
        {
            if (root == null) return;

            // Wait for the first geometry pass so resolvedStyle dimensions are valid
            root.RegisterCallback<GeometryChangedEvent>(OnLayout);

            void OnLayout(GeometryChangedEvent _)
            {
                root.UnregisterCallback<GeometryChangedEvent>(OnLayout);
                int found = 0;

                root.Query<VisualElement>().ForEach(ve =>
                {
                    var parent = ve.parent;
                    if (parent == null) return;

                    // Skip internal Unity elements (scrollbars, sliders, etc.)
                    string n = ve.name ?? "";
                    if (n.StartsWith("unity-")) return;

                    float veW = ve.resolvedStyle.width;
                    float veH = ve.resolvedStyle.height;
                    float pW  = parent.resolvedStyle.width;
                    float pH  = parent.resolvedStyle.height;

                    // Skip unresolved (NaN / 0) layouts
                    if (float.IsNaN(veW) || float.IsNaN(veH) || pW <= 0 || pH <= 0) return;

                    // Check if the element resides inside a ScrollView
                    bool insideScrollView = false;
                    for (VisualElement current = parent; current != null; current = current.parent)
                    {
                        if (current is ScrollView)
                        {
                            insideScrollView = true;
                            break;
                        }
                    }

                    bool overflowsX = veW > pW + 2f;  // 2px tolerance for rounding
                    bool overflowsY = veH > pH + 2f;

                    // Ignore vertical overflows inside ScrollViews since they are meant to scroll
                    if (insideScrollView) overflowsY = false;

                    if (overflowsX || overflowsY)
                    {
                        string label  = panelLabel != "" ? $"[{panelLabel}] " : "";
                        string elName = string.IsNullOrEmpty(ve.name) ? "(unnamed)" : ve.name;
                        string cls    = ve.GetClasses() is var c ? string.Join(" ", c) : "";
                        string dir    = (overflowsX && overflowsY) ? "X+Y"
                                      : overflowsX ? "X" : "Y";

                        Debug.LogWarning(
                            $"[UI Overflow] {label}{dir} | " +
                            $"elem='{elName}' classes='{cls}' " +
                            $"size=({veW:F0}x{veH:F0}) " +
                            $"parent='{(string.IsNullOrEmpty(parent.name) ? "(unnamed)" : parent.name)}' " +
                            $"parentSize=({pW:F0}x{pH:F0})");
                        found++;
                    }
                });

                string prefix = panelLabel != "" ? $"[{panelLabel}] " : "";
                if (found == 0)
                    Debug.Log($"[UI Overflow] {prefix}No overflowing elements found.");
                else
                    Debug.LogWarning($"[UI Overflow] {prefix}{found} overflowing element(s) found — see warnings above.");
            }
        }


        // Static version for global UI utility calls
        public static void ApplySafeTransition(VisualElement element, string[] properties, float duration)
        {
            if (element == null) return;
            List<StylePropertyName> props = new List<StylePropertyName>();
            foreach (var p in properties) props.Add(new StylePropertyName(p));
            element.style.transitionProperty = props;
            element.style.transitionDuration = new List<TimeValue> { new TimeValue(duration) };
        }

        /// <summary>
        /// Global check for map interaction. Returns true if pointer is over interactive UI.
        /// </summary>
        public bool IsPointerOverUI()
        {
            Vector2 mousePos = Input.mousePosition;
            Vector2 pointerPos = new Vector2(mousePos.x, Screen.height - mousePos.y);

            // Check all documents from top sorting order down
            var docs = new List<UIDocument> { countrySelectorDoc, countryPanelDoc, newsTickerDoc, dashboardDoc };
            
            foreach (var doc in docs)
            {
                if (doc == null || doc.rootVisualElement == null) continue;
                
                // CRITICAL: If the root is hidden or transparent, skip the whole document
                if (doc.rootVisualElement.resolvedStyle.display == DisplayStyle.None || 
                    doc.rootVisualElement.resolvedStyle.opacity < 0.01f) continue;

                var panel = doc.rootVisualElement.panel;
                if (panel == null) continue;

                // Use the built-in screen-to-panel conversion correctly
                Vector2 screenPos = new Vector2(mousePos.x, Screen.height - mousePos.y);
                VisualElement picked = doc.rootVisualElement.panel.Pick(screenPos);

                if (picked != null && picked != doc.rootVisualElement)
                {
                    if (picked.pickingMode == PickingMode.Position)
                    {
                        // Ignore invisible flex containers
                        bool isTransparentContainer = picked.resolvedStyle.backgroundColor.a == 0 && 
                                                      picked.resolvedStyle.backgroundImage.texture == null &&
                                                      !(picked is TextElement) &&
                                                      !(picked is ScrollView);

                        if (!isTransparentContainer)
                        {
                            // Final safety check: skip if the element itself is effectively hidden
                            if (picked.resolvedStyle.display == DisplayStyle.None || picked.resolvedStyle.opacity < 0.1f) continue;
                            
return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
// Force re-import: 2026-05-14 10:14