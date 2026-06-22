using UnityEngine;
using UnityEngine.UIElements;
using StateCraft.Data;
using StateCraft.Core;
using StateCraft.Map;
using StateCraft.Systems;

namespace StateCraft.UI
{
    public class CountryInfoToolkitController : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private VisualElement _root;
        private CountryDataSO _currentCountry;

        // Header
        private Label _nameLabel, _typeLabel, _capitalLabel, _relationBadge, _populationValue;
        private Image _flagImage;
        private Label _intelText;

        // Economic stats
        private Label _gdpVal, _gdpGrowthVal, _inflationVal, _debtRatioVal;

        // Political stats
        private Label _stabilityVal, _securityVal, _threatVal;

        // Military stats
        private Label _milPersonnel, _milStrength, _milTanks, _milJets;

        // Crisis container
        private VisualElement _crisisContainer;

        // News headline
        private VisualElement _headlineBox;
        private Label _headlineText;

        // Scroll
        private ScrollView _scrollView;

        // Buttons
        private Button _btnClose, _btnInvest, _btnSpy, _btnSabotage, _btnWar, _btnAlly;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument != null)
            {
                // CRITICAL: keep this document BELOW the main dashboard so it
                // never intercepts raycast clicks when the panel is hidden.
                _uiDocument.sortingOrder = -1;
                _root = _uiDocument.rootVisualElement;
            }

            var panel = _root?.Q<VisualElement>("CountryInfoPanel");
            if (panel == null)
            {
                foreach (var doc in FindObjectsOfType<UIDocument>())
                {
                    panel = doc.rootVisualElement?.Q<VisualElement>("CountryInfoPanel");
                    if (panel != null) { _root = doc.rootVisualElement; break; }
                }
            }

            if (_root != null)
            {
                // The document root must ALWAYS be Ignore — clicks go through it
                // to the main dashboard. Only the panel element itself becomes
                // Position when the dossier is shown.
                _root.pickingMode = PickingMode.Ignore;

                var p = _root.Q<VisualElement>("CountryInfoPanel");
                if (p != null)
                {
                    p.style.display = DisplayStyle.None;
                    p.pickingMode = PickingMode.Ignore;
                }

                InitializeElements();
            }
        }

        private bool IsPanelVisible()
        {
            var p = _root?.Q<VisualElement>("CountryInfoPanel");
            return p != null && p.resolvedStyle.opacity > 0.01f;
        }

        private void Update()
        {
            if (_scrollView == null || !IsPanelVisible()) return;
            bool mouseOverDossier = Input.mousePosition.x > Screen.width - 440f;
            if (!mouseOverDossier) return;
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
                _scrollView.scrollOffset += new Vector2(0, -scroll * 300f);
        }

        private void OnEnable()
        {
            if (MapManager.Instance != null)
                MapManager.Instance.OnCountrySelected += HandleCountrySelected;
        }

        private void OnDisable()
        {
            if (MapManager.Instance != null)
                MapManager.Instance.OnCountrySelected -= HandleCountrySelected;
        }

        private void HandleCountrySelected(CountryDataSO country)
        {
            if (GameManager.Instance == null || GameManager.Instance.playerCountry == null || GameManager.Instance.playerCountry == country)
            {
                // Hide the panel — set display none AND picking ignore so nothing bleeds through
                var p = _root?.Q<VisualElement>("CountryInfoPanel");
                if (p != null)
                {
                    p.pickingMode = PickingMode.Ignore;
                    p.Query<VisualElement>().ForEach(ve => ve.pickingMode = PickingMode.Ignore);
                    if (UIStateManager.Instance != null)
                        UIStateManager.Instance.ApplySafeTransition(p, false, 0.3f);
                    else
                    {
                        p.style.opacity = 0;
                        p.style.display = DisplayStyle.None;
                    }
                }
                return;
            }

            RefreshDisplay(country);
            var panel = _root?.Q<VisualElement>("CountryInfoPanel");
            if (panel == null) panel = _root?.Query<VisualElement>().Name("CountryInfoPanel").First();
            if (panel != null)
            {
                panel.style.display = DisplayStyle.Flex;
                // Re-enable picking on the panel and its children
                panel.pickingMode = PickingMode.Position;
                panel.Query<VisualElement>().ForEach(ve =>
                {
                    string n = ve.name ?? "";
                    string cls = ve.GetType().Name;
                    bool isScrollInternal = n.StartsWith("unity-") || cls == "Scroller" || cls == "Slider";
                    if (!isScrollInternal) ve.pickingMode = PickingMode.Position;
                });
                UIStateManager.Instance?.ApplySafeTransition(panel, true, 0.4f);
                UIStateManager.FixScrollViews(panel);
            }
        }

        private void InitializeElements()
        {
            _nameLabel       = _root.Q<Label>("CountryName");
            _typeLabel       = _root.Q<Label>("GovernmentType");
            _capitalLabel    = _root.Q<Label>("CapitalCity");
            _relationBadge   = _root.Q<Label>("RelationBadge");
            _populationValue = _root.Q<Label>("PopulationValue");
            _intelText       = _root.Q<Label>("IntelLevelText");
            _flagImage       = _root.Q<Image>("SelectedFlag");
            _scrollView      = _root.Q<ScrollView>();

            _gdpVal          = _root.Q<Label>("GdpValue");
            _gdpGrowthVal    = _root.Q<Label>("GdpGrowthValue");
            _inflationVal    = _root.Q<Label>("InflationValue");
            _debtRatioVal    = _root.Q<Label>("DebtRatioValue");

            _stabilityVal    = _root.Q<Label>("StabilityValue");
            _securityVal     = _root.Q<Label>("SecurityValue");
            _threatVal       = _root.Q<Label>("ThreatValue");

            _milPersonnel    = _root.Q<Label>("MilPersonnel");
            _milStrength     = _root.Q<Label>("MilStrength");
            _milTanks        = _root.Q<Label>("MilTanks");
            _milJets         = _root.Q<Label>("MilJets");

            _crisisContainer = _root.Q<VisualElement>("CrisisContainer");
            _headlineBox  = _root.Q<VisualElement>("CountryHeadlineBox");
            _headlineText = _root.Q<Label>("CountryHeadlineText");

            _btnClose = _root.Q<Button>("ClosePanel");
            if (_btnClose != null) _btnClose.clicked += () => UIStateManager.Instance?.HideCountryPanel();

            _btnInvest = _root.Q<Button>("BtnTrade");
            if (_btnInvest != null) _btnInvest.clicked += OnInvestClicked;

            _btnAlly = _root.Q<Button>("BtnAlly");
            if (_btnAlly != null) _btnAlly.clicked += OnProposeAllianceClicked;

            _btnSpy = _root.Q<Button>("BtnSpy");
            if (_btnSpy != null) _btnSpy.clicked += OnSpyClicked;

            _btnSabotage = _root.Q<Button>("BtnSabotage");
            if (_btnSabotage != null) _btnSabotage.clicked += OnSabotageClicked;

            _btnWar = _root.Q<Button>("BtnWar");
            if (_btnWar != null) _btnWar.clicked += OnDeclareWarClicked;

            _root.Q<Button>("BtnReinforce")?.RegisterCallback<ClickEvent>(_ => OnSendAidClicked());
        }

        public void RefreshDisplay(CountryDataSO country)
        {
            _currentCountry = country;
            if (country == null) return;

            var player = GameManager.Instance?.playerCountry;

            if (_nameLabel != null) _nameLabel.text = country.countryName.ToUpper();
            if (_flagImage != null)
            {
                string flagName = country.countryName.Replace(" ", "_");
                Sprite flagSprite = UnityEngine.Resources.Load<Sprite>($"Flags/{flagName}");
                if (flagSprite != null)
                {
                    _flagImage.sprite = flagSprite;
                }
                else if (country.countryFlag != null)
                {
                    _flagImage.sprite = country.countryFlag; // Fallback to serialized flag
                }
            }

            // Derive a government descriptor from available data (no governmentType field on SO)
            string govType = country.stability > 70f ? "STABLE DEMOCRACY" :
                             country.stability > 45f ? "TRANSITIONAL GOVERNMENT" :
                             country.militaryStrength > 300f ? "MILITARY-LED REGIME" : "FRAGILE STATE";
            if (_typeLabel != null) _typeLabel.text = govType;

            if (_capitalLabel != null) _capitalLabel.text = string.IsNullOrEmpty(country.capitalCity) ? "" : $"CAPITAL: {country.capitalCity.ToUpper()}";
            if (_populationValue != null) _populationValue.text = $"POP: {FormatPopulation(country.population)} {FormatPopulationSigned(country.populationChangeLastTurn)}";

            if (_relationBadge != null && player != null)
            {
                float rel = DashboardLiveContent.ComputeRelation(player, country, GameManager.Instance);
                string relLabel = DashboardLiveContent.RelationLabel(rel);
                Color relColor = DashboardLiveContent.RelationColor(rel);
                _relationBadge.text = relLabel;
                _relationBadge.style.color = relColor;
                _relationBadge.style.backgroundColor = new StyleColor(new Color(relColor.r, relColor.g, relColor.b, 0.1f));
            }

            ApplyIntelLevel(country.intelligenceLevel);
            ApplyHeadline(country);
        }

        private void ApplyHeadline(CountryDataSO country)
        {
            if (_headlineBox == null || _headlineText == null) return;

            string headline = null;

            // Search the global news history for a recent mention of this country
            if (NewsSystem.Instance != null)
            {
                headline = System.Linq.Enumerable.FirstOrDefault(
                    System.Linq.Enumerable.Reverse(NewsSystem.Instance.GlobalNewsHistory),
                    n => n.IndexOf(country.countryName, System.StringComparison.OrdinalIgnoreCase) >= 0);

                if (string.IsNullOrEmpty(headline))
                    headline = System.Linq.Enumerable.FirstOrDefault(
                        System.Linq.Enumerable.Reverse(NewsSystem.Instance.NewsHistory),
                        n => n.IndexOf(country.countryName, System.StringComparison.OrdinalIgnoreCase) >= 0);
            }

            // Determine border color from headline keywords, or fall back to stat-based brief
            Color accent = new Color(255/255f, 152/255f, 0/255f); // amber default
            if (!string.IsNullOrEmpty(headline))
            {
                if (headline.Contains("WAR") || headline.Contains("MILITARY") || headline.Contains("CONFLICT") || headline.Contains("SKIRMISH"))
                    accent = new Color(244/255f, 67/255f, 54/255f);
                else if (headline.Contains("CRISIS") || headline.Contains("TURMOIL") || headline.Contains("COLLAPSE") || headline.Contains("UNREST"))
                    accent = new Color(255/255f, 82/255f, 82/255f);
                else if (headline.Contains("TRADE") || headline.Contains("ECONOMY") || headline.Contains("REFORM") || headline.Contains("SURGE"))
                    accent = new Color(76/255f, 175/255f, 80/255f);
                else if (headline.Contains("INTEL") || headline.Contains("SPY") || headline.Contains("CYBER"))
                    accent = new Color(186/255f, 104/255f, 200/255f);
            }
            else
            {
                // Fallback: derive a brief from live stats
                var gm = GameManager.Instance;
                var warStage = gm?.WarSystem != null ? gm.WarSystem.GetWarStage(country) : StateCraft.Interfaces.WarStage.Peace;

                if (warStage == StateCraft.Interfaces.WarStage.Conflict)
                {
                    accent   = new Color(244/255f, 67/255f, 54/255f);
                    headline = $"CONFLICT ACTIVE: {country.countryName} armed forces engaged on multiple fronts.";
                }
                else if (country.stability < 35f)
                {
                    accent   = new Color(255/255f, 82/255f, 82/255f);
                    headline = $"POLITICAL INSTABILITY: Internal unrest rising in {country.countryName} (Stability {country.stability:F0}%).";
                }
                else if (country.foodSecurity < 50f)
                {
                    accent   = new Color(255/255f, 152/255f, 0/255f);
                    headline = $"RESOURCE PRESSURE: Food security crisis reported across {country.countryName} regions.";
                }
                else
                {
                    accent   = new Color(0, 210/255f, 255/255f);
                    headline = $"No breaking developments. {country.countryName} frequencies nominal.";
                }
            }

            _headlineText.text = headline;
            _headlineBox.style.backgroundColor = new Color(accent.r, accent.g, accent.b, 0.08f);
            _headlineBox.style.borderTopColor    = new Color(accent.r, accent.g, accent.b, 0.5f);
            _headlineBox.style.borderBottomColor = new Color(accent.r, accent.g, accent.b, 0.5f);
            _headlineBox.style.borderLeftColor   = new Color(accent.r, accent.g, accent.b, 0.5f);
            _headlineBox.style.borderRightColor  = new Color(accent.r, accent.g, accent.b, 0.5f);

            // Tint the "▶ LATEST HEADLINE" label too
            var titleLbl = _headlineBox.Q<Label>();
            if (titleLbl != null && titleLbl != _headlineText) titleLbl.style.color = accent;
        }

        private void ApplyIntelLevel(float level)
        {
            if (_currentCountry == null) return;
            var c = _currentCountry;

            int tier = Mathf.Clamp(Mathf.FloorToInt(level / 25f) + 1, 1, 4);
            if (_intelText != null) _intelText.text = $"INTEL LEVEL {tier}: {GetIntelTierName(tier)}";

            // ECONOMICS
            string sym = c.currencySymbol ?? "$";
            if (_gdpVal != null)     _gdpVal.text     = tier >= 1 ? UiFormat.Money(c.gdp * c.exchangeRate, sym) : "REDACTED";
            if (_gdpGrowthVal != null)
            {
                _gdpGrowthVal.text = tier >= 1 ? UiFormat.Percent(c.gdpGrowthRate) : "—";
                _gdpGrowthVal.style.color = c.gdpGrowthRate >= 0 ? new Color(0, 230/255f, 118/255f) : new Color(255/255f, 82/255f, 82/255f);
            }
            if (_inflationVal != null) _inflationVal.text = tier >= 1 ? UiFormat.Percent(c.inflationRate) : "—";
            if (_debtRatioVal != null)
            {
                float ratio = c.gdp > 0 ? c.debt / c.gdp : 0f;
                _debtRatioVal.text = tier >= 1 ? $"{ratio * 100f:F0}%" : "—";
                _debtRatioVal.style.color = ratio > 0.8f ? new Color(255/255f, 82/255f, 82/255f) : Color.white;
            }

            // STABILITY
            if (_stabilityVal != null)
            {
                _stabilityVal.text = tier >= 2 ? $"{c.stability:F0}%" : "UNCERTAIN";
                _stabilityVal.style.color = c.stability > 60 ? new Color(0, 230/255f, 118/255f) : c.stability > 35 ? new Color(255/255f, 193/255f, 7/255f) : new Color(255/255f, 82/255f, 82/255f);
            }
            if (_securityVal != null)
            {
                _securityVal.text = tier >= 2 ? $"{c.security:F0}%" : "CLASSIFIED";
                _securityVal.style.color = c.security > 60 ? new Color(0, 230/255f, 118/255f) : new Color(255/255f, 82/255f, 82/255f);
            }
            if (_threatVal != null)
            {
                string threat = c.security < 30 ? "CRITICAL" : c.security < 55 ? "ELEVATED" : c.security < 75 ? "GUARDED" : "LOW";
                Color tc = c.security < 30 ? new Color(255/255f, 23/255f, 68/255f) : c.security < 55 ? new Color(255/255f, 152/255f, 0f) : c.security < 75 ? new Color(255/255f, 235/255f, 59/255f) : new Color(0, 230/255f, 118/255f);
                _threatVal.text  = tier >= 2 ? threat : "SCANNING...";
                _threatVal.style.color = tc;
            }

            // MILITARY
            if (_milPersonnel != null) _milPersonnel.text = tier >= 3 ? $"{c.armyStats.personnel:N0}" : "CLASSIFIED";
            if (_milStrength != null)  _milStrength.text  = tier >= 3 ? $"{c.militaryStrength:F0}" : "CLASSIFIED";
            if (_milTanks != null)     _milTanks.text     = tier >= 3 ? $"{c.unitTanks:N0}" : "—";
            if (_milJets != null)      _milJets.text      = tier >= 3 ? $"{c.unitFighters:N0}" : "—";

            // CRISES
            if (_crisisContainer != null)
            {
                _crisisContainer.Clear();
                if (tier >= 2 && c.activeCrises != null && c.activeCrises.Count > 0)
                {
                    foreach (var crisis in c.activeCrises)
                    {
                        var row = new VisualElement();
                        row.style.flexDirection = FlexDirection.Row;
                        row.style.marginBottom = 5;
                        row.style.backgroundColor = new Color(255/255f, 82/255f, 82/255f, 0.07f);
                        row.style.paddingTop = 6;
                        row.style.paddingBottom = 6;
                        row.style.paddingLeft = 8;
                        row.style.paddingRight = 8;
                        row.Add(new Label("!") { style = { color = new Color(255/255f, 82/255f, 82/255f), marginRight = 6, fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } });
                        row.Add(new Label(crisis.crisisName) { style = { color = Color.white, fontSize = 9, flexGrow = 1, whiteSpace = WhiteSpace.Normal } });
                        _crisisContainer.Add(row);
                    }
                }
                else
                {
                    string msg = tier >= 2 ? "No active crises detected." : "Insufficient intel to assess.";
                    _crisisContainer.Add(new Label(msg) { style = { color = Color.grey, fontSize = 9 } });
                }
            }
        }

        // ---- ACTIONS ----

        private void OnInvestClicked()
        {
            if (GameManager.Instance == null || _currentCountry == null) return;
            float cost = 2.0f;
            if (GameManager.Instance.playerBudget < cost) { NotificationManager.Instance?.AddNotification("Insufficient funds to invest.", Color.red); return; }
            GameManager.Instance.playerBudget -= cost;
            _currentCountry.stability = Mathf.Min(100f, _currentCountry.stability + 3f);
            _currentCountry.gdp *= 1.01f;
            string sym = GameManager.Instance.playerCountry?.currencySymbol ?? "N";
            NotificationManager.Instance?.AddNotification($"INVESTED {sym}2B in {_currentCountry.countryName}: +3% stability, +1% GDP.", new Color(0, 230/255f, 118/255f));
            NewsSystem.Instance?.PublishNews($"TRADE: {GameManager.Instance.playerCountry?.countryName} expands economic footprint in {_currentCountry.countryName}.", true);
        }

        private void OnProposeAllianceClicked()
        {
            if (GameManager.Instance == null || _currentCountry == null) return;
            var player = GameManager.Instance.playerCountry;
            if (player == null) return;
            float rel = DashboardLiveContent.ComputeRelation(player, _currentCountry, GameManager.Instance);
            if (rel < 55f) { NotificationManager.Instance?.AddNotification($"Relations too low for alliance ({rel:F0}/100). Send ambassadors first.", Color.red); return; }
            player.allianceCount++;
            player.globalPrestige = Mathf.Min(100f, player.globalPrestige + 2f);
            NotificationManager.Instance?.AddNotification($"ALLIANCE PROPOSED with {_currentCountry.countryName}!", new Color(76/255f, 175/255f, 80/255f));
            NewsSystem.Instance?.PublishNews($"ALLIANCE: Mutual defence pact proposed between {player.countryName} and {_currentCountry.countryName}.");
        }

        private void OnSpyClicked()
        {
            if (GameManager.Instance == null || _currentCountry == null) return;
            float cost = 0.5f;
            if (GameManager.Instance.playerBudget < cost) { NotificationManager.Instance?.AddNotification("Insufficient funds to deploy assets.", Color.red); return; }
            GameManager.Instance.playerBudget -= cost;
            _currentCountry.intelligenceLevel = Mathf.Min(100f, _currentCountry.intelligenceLevel + 20f);
            NotificationManager.Instance?.AddNotification($"ASSETS DEPLOYED to {_currentCountry.countryName}. Intel level improved.", new Color(255/255f, 193/255f, 7/255f));
            RefreshDisplay(_currentCountry);
        }

        private void OnSabotageClicked()
        {
            if (GameManager.Instance == null || _currentCountry == null) return;
            float cost = 1.0f;
            if (GameManager.Instance.playerBudget < cost) { NotificationManager.Instance?.AddNotification("Insufficient funds for covert op.", Color.red); return; }
            GameManager.Instance.playerBudget -= cost;
            _currentCountry.stability -= 8f;
            _currentCountry.security  -= 5f;
            _currentCountry.gdp      *= 0.98f;
            NotificationManager.Instance?.AddNotification($"SABOTAGE: Infrastructure degraded in {_currentCountry.countryName}.", new Color(255/255f, 82/255f, 82/255f));
            NewsSystem.Instance?.PublishNews($"INTEL: Mysterious infrastructure failures in {_currentCountry.countryName}. Sabotage suspected.", true);
        }

        private void OnDeclareWarClicked()
        {
            if (GameManager.Instance == null || _currentCountry == null) return;
            var player = GameManager.Instance.playerCountry;
            var warSys = GameManager.Instance.WarSystem;
            if (warSys == null || player == null) return;
            warSys.DeclareWar(player, _currentCountry);
            NotificationManager.Instance?.AddNotification($"WAR DECLARED against {_currentCountry.countryName}! Forces mobilizing.", Color.red);
            NewsSystem.Instance?.PublishNews($"WAR: {player.countryName} declares war on {_currentCountry.countryName}!");
        }

        private void OnSendAidClicked()
        {
            if (GameManager.Instance == null || _currentCountry == null) return;
            float cost = 1.5f;
            if (GameManager.Instance.playerBudget < cost) { NotificationManager.Instance?.AddNotification("Insufficient funds to send aid.", Color.red); return; }
            GameManager.Instance.playerBudget -= cost;
            _currentCountry.stability = Mathf.Min(100f, _currentCountry.stability + 5f);
            _currentCountry.globalPrestige = Mathf.Min(100f, _currentCountry.globalPrestige + 2f);
            GameManager.Instance.playerCountry.globalPrestige = Mathf.Min(100f, GameManager.Instance.playerCountry.globalPrestige + 1.5f);
            NotificationManager.Instance?.AddNotification($"AID SENT to {_currentCountry.countryName}: +5 stability.", new Color(76/255f, 175/255f, 80/255f));
        }

        // ---- HELPERS ----

        private static string FormatPopulation(long pop)
        {
            if (pop >= 1_000_000_000) return $"{pop / 1_000_000_000f:F1}B";
            if (pop >= 1_000_000)     return $"{pop / 1_000_000f:F1}M";
            if (pop >= 1_000)         return $"{pop / 1_000f:F0}K";
            return pop.ToString("N0");
        }

        private static string FormatPopulationSigned(long pop)
        {
            if (pop == 0) return "(0)";
            string sign = pop > 0 ? "+" : "-";
            long absPop = System.Math.Abs(pop);
            return $"({sign}{FormatPopulation(absPop)})";
        }

        private static string GetIntelTierName(int tier) => tier switch
        {
            1 => "SATELLITE RECON",
            2 => "HUMAN ASSETS",
            3 => "SIGNAL INTERCEPT",
            4 => "DEEP INFILTRATION",
            _ => "UNKNOWN"
        };
    }
}
