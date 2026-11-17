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

        private Label _nameLabel, _presidentLabel, _typeLabel, _capitalLabel, _relationBadge, _populationValue;
        private Image _flagImage;
        private Label _intelText;

        private Label _gdpVal, _gdpGrowthVal, _inflationVal, _debtRatioVal;

        private Label _stabilityVal, _securityVal, _threatVal;

        private Label _milPersonnel, _milStrength, _milTanks, _milJets;
        private VisualElement _milEquipContainer;

        private VisualElement _crisisContainer;

        private VisualElement _headlineBox;
        private Label _headlineText;

        private ScrollView _scrollView;

        private Button _btnClose, _btnInvest, _btnSpy, _btnSabotage, _btnWar, _btnAlly;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument != null)
            {
                _uiDocument.sortingOrder = -1;
                _root = _uiDocument.rootVisualElement;
            }

            var panel = _root?.Q<VisualElement>("CountryInfoPanel");
            if (panel == null)
            {
                foreach (var doc in FindObjectsOfType<UIDocument>())
                {
                    panel = doc.rootVisualElement?.Q<VisualElement>("CountryInfoPanel");
                    if (panel != null) { _root = doc.rootVisualElement; _uiDocument = doc; break; }
                }
            }

            if (_root != null)
            {
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
            {
                _scrollView.scrollOffset += new Vector2(0, -scroll * 300f);
            }
        }
        
        public static bool IsPointerOverDossierStatic()
        {
            var inst = FindObjectOfType<CountryInfoToolkitController>();
            if (inst == null || !inst.IsPanelVisible()) return false;
            return Input.mousePosition.x > Screen.width - 440f;
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
            if (country == null) return;
            string currentPlayerName = GameManager.Instance?.playerCountry != null ? GameManager.Instance.playerCountry.countryName : "NONE (NULL)";
            Debug.Log($"[CountryInfo Log] Map clicked: '{country.countryName}' | Current Player Country: '{currentPlayerName}'");

            if (_root == null && _uiDocument != null) _root = _uiDocument.rootVisualElement;
            if (_nameLabel == null || _btnAlly == null) InitializeElements();

            RefreshDisplay(country);

            bool isOwnCountry = GameManager.Instance != null && GameManager.Instance.playerCountry == country;
            if (_btnAlly != null) _btnAlly.style.display = isOwnCountry ? DisplayStyle.None : DisplayStyle.Flex;
            if (_btnWar != null)  _btnWar.style.display  = isOwnCountry ? DisplayStyle.None : DisplayStyle.Flex;

            var panel = _root?.Q<VisualElement>("CountryInfoPanel");
            if (panel == null) panel = _root?.Query<VisualElement>().Name("CountryInfoPanel").First();
            if (panel != null)
            {
                if (_uiDocument != null) _uiDocument.sortingOrder = 10;
                panel.style.display = DisplayStyle.Flex;
                panel.style.opacity = 1;
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

        public void HideDossier()
        {
            if (_uiDocument != null) _uiDocument.sortingOrder = -1;
            var p = _root?.Q<VisualElement>("CountryInfoPanel");
            if (p == null && _uiDocument != null) p = _uiDocument.rootVisualElement?.Q<VisualElement>("CountryInfoPanel");
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
            if (MapManager.Instance != null)
            {
                MapManager.Instance.SelectCountry(null);
            }
        }

        private void InitializeElements()
        {
            if (_root == null) return;
            var panel = _root.Q<VisualElement>("CountryInfoPanel");
            var container = panel ?? _root;

            _nameLabel       = container.Q<Label>("CountryName");
            _presidentLabel  = container.Q<Label>("PresidentName");
            _typeLabel       = container.Q<Label>("GovernmentType");
            _capitalLabel    = container.Q<Label>("CapitalCity");
            _relationBadge   = container.Q<Label>("RelationBadge");
            _populationValue = container.Q<Label>("PopulationValue");
            _intelText       = container.Q<Label>("IntelLevelText");
            _flagImage       = container.Q<Image>("SelectedFlag");
            _scrollView      = container.Q<ScrollView>();

            _gdpVal          = container.Q<Label>("GdpValue");
            _gdpGrowthVal    = container.Q<Label>("GdpGrowthValue");
            _inflationVal    = container.Q<Label>("InflationValue");
            _debtRatioVal    = container.Q<Label>("DebtRatioValue");

            _stabilityVal    = container.Q<Label>("StabilityValue");
            _securityVal     = container.Q<Label>("SecurityValue");
            _threatVal       = container.Q<Label>("ThreatValue");

            _milPersonnel    = container.Q<Label>("MilPersonnel");
            _milStrength     = container.Q<Label>("MilStrength");
            _milTanks        = container.Q<Label>("MilTanks");
            _milJets         = container.Q<Label>("MilJets");
            _milEquipContainer = container.Q<VisualElement>("MilEquipmentContainer");

            _crisisContainer = container.Q<VisualElement>("CrisisContainer");
            _headlineBox  = container.Q<VisualElement>("CountryHeadlineBox");
            _headlineText = container.Q<Label>("CountryHeadlineText");

            _btnClose = container.Q<Button>("ClosePanel");
            if (_btnClose != null) _btnClose.clicked += () => 
            {
                Debug.Log("[CountryInfo] Close button clicked.");
                HideDossier();
            };

            _btnAlly = container.Q<Button>("BtnAlly");
            if (_btnAlly != null) _btnAlly.clicked += () =>
            {
                Debug.Log($"[CountryInfo] Faction Action button clicked for target: {(_currentCountry != null ? _currentCountry.countryName : "NULL")}");
                OnInviteToFactionClicked();
            };

            _btnWar = container.Q<Button>("BtnWar");
            if (_btnWar != null) _btnWar.clicked += () =>
            {
                Debug.Log($"[CountryInfo] 'DECLARE WAR' button clicked for target: {(_currentCountry != null ? _currentCountry.countryName : "NULL")}");
                OnDeclareWarClicked();
            };
        }

        public void RefreshDisplay(CountryDataSO country)
        {
            _currentCountry = country;
            if (country == null) return;

            var player = GameManager.Instance?.playerCountry;

            if (_nameLabel != null) _nameLabel.text = country.countryName.ToUpper();
            if (_presidentLabel != null) _presidentLabel.text = $"PRESIDENT: {country.GetPresidentName().ToUpper()}";
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
                    _flagImage.sprite = country.countryFlag;
                }
            }

            if (_btnAlly != null && player != null)
            {
                bool playerInFaction = !string.IsNullOrEmpty(player.globalFaction) && player.globalFaction != "Independent" && player.globalFaction != "Non-Aligned";
                bool targetInFaction = !string.IsNullOrEmpty(country.globalFaction) && country.globalFaction != "Independent" && country.globalFaction != "Non-Aligned";

                if (playerInFaction && targetInFaction && player.globalFaction == country.globalFaction)
                {
                    _btnAlly.text = $"FACTION MEMBER ({country.globalFaction.ToUpper()})";
                    _btnAlly.style.backgroundColor = new StyleColor(new Color(0f, 230/255f, 118/255f, 0.25f));
                    _btnAlly.style.borderLeftColor  = new StyleColor(new Color(0f, 230/255f, 118/255f, 0.7f));
                    _btnAlly.style.borderRightColor = new StyleColor(new Color(0f, 230/255f, 118/255f, 0.7f));
                    _btnAlly.style.borderTopColor   = new StyleColor(new Color(0f, 230/255f, 118/255f, 0.7f));
                    _btnAlly.style.borderBottomColor= new StyleColor(new Color(0f, 230/255f, 118/255f, 0.7f));
                }
                else if (targetInFaction)
                {
                    _btnAlly.text = $"FACTION: {country.globalFaction.ToUpper()}";
                    _btnAlly.style.backgroundColor = new StyleColor(new Color(255/255f, 193/255f, 7/255f, 0.25f));
                    _btnAlly.style.borderLeftColor  = new StyleColor(new Color(255/255f, 193/255f, 7/255f, 0.7f));
                    _btnAlly.style.borderRightColor = new StyleColor(new Color(255/255f, 193/255f, 7/255f, 0.7f));
                    _btnAlly.style.borderTopColor   = new StyleColor(new Color(255/255f, 193/255f, 7/255f, 0.7f));
                    _btnAlly.style.borderBottomColor= new StyleColor(new Color(255/255f, 193/255f, 7/255f, 0.7f));
                }
                else if (playerInFaction)
                {
                    _btnAlly.text = $"INVITE TO {player.globalFaction.ToUpper()}";
                    _btnAlly.style.backgroundColor = new StyleColor(new Color(0f, 210/255f, 255/255f, 0.25f));
                    _btnAlly.style.borderLeftColor  = new StyleColor(new Color(0f, 210/255f, 255/255f, 0.7f));
                    _btnAlly.style.borderRightColor = new StyleColor(new Color(0f, 210/255f, 255/255f, 0.7f));
                    _btnAlly.style.borderTopColor   = new StyleColor(new Color(0f, 210/255f, 255/255f, 0.7f));
                    _btnAlly.style.borderBottomColor= new StyleColor(new Color(0f, 210/255f, 255/255f, 0.7f));
                }
                else
                {
                    _btnAlly.text = "PROPOSE ALLIANCE";
                    _btnAlly.style.backgroundColor = new StyleColor(new Color(0f, 210/255f, 255/255f, 0.25f));
                    _btnAlly.style.borderLeftColor  = new StyleColor(new Color(0f, 210/255f, 255/255f, 0.7f));
                    _btnAlly.style.borderRightColor = new StyleColor(new Color(0f, 210/255f, 255/255f, 0.7f));
                    _btnAlly.style.borderTopColor   = new StyleColor(new Color(0f, 210/255f, 255/255f, 0.7f));
                    _btnAlly.style.borderBottomColor= new StyleColor(new Color(0f, 210/255f, 255/255f, 0.7f));
                }
            }

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

            Color accent = new Color(255/255f, 152/255f, 0/255f);
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

            var titleLbl = _headlineBox.Q<Label>();
            if (titleLbl != null && titleLbl != _headlineText) titleLbl.style.color = accent;
        }

        private void ApplyIntelLevel(float level)
        {
            if (_currentCountry == null) return;
            var c = _currentCountry;
            var player = GameManager.Instance?.playerCountry;

            int tier = Mathf.Clamp(Mathf.FloorToInt(level / 25f) + 1, 1, 4);

            if (player != null && c != player)
            {
                if (player.satelliteCount == 0 && player.droneCount == 0 && player.operativeCount < 5)
                {
                    tier = 1;
                }
            }

            if (_intelText != null) _intelText.text = $"INTEL LEVEL {tier}: {GetIntelTierName(tier)}";

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

            float milScore = c.GetMilitaryFirepowerScore();
            if (_milPersonnel != null) _milPersonnel.text = tier >= 3 ? $"{c.armyStats.personnel:N0}" : "CLASSIFIED";
            if (_milStrength != null)  _milStrength.text  = tier >= 3 ? $"{milScore:N0}" : "CLASSIFIED";
            if (_milTanks != null)     _milTanks.text     = tier >= 3 ? $"{c.unitTanks:N0}" : "—";
            if (_milJets != null)      _milJets.text      = tier >= 3 ? $"{c.unitFighters:N0}" : "—";

            if (_milEquipContainer != null)
            {
                _milEquipContainer.Clear();
                if (tier >= 3)
                {
                    AddEquipRow(_milEquipContainer, "🔫 Assault Rifles",       $"{c.unitRifles:N0} Units");
                    AddEquipRow(_milEquipContainer, "💥 Heavy Artillery",      $"{c.unitArtillery:N0} Guns");
                    AddEquipRow(_milEquipContainer, "🎖️ Main Battle Tanks",    $"{c.unitTanks:N0} Tanks");
                    AddEquipRow(_milEquipContainer, "🛡️ Armored Carriers",     $"{c.unitAPCs:N0} APCs");
                    AddEquipRow(_milEquipContainer, "✈️ Fighter Jets",         $"{c.unitFighters:N0} Jets");
                    AddEquipRow(_milEquipContainer, "🚁 Attack Helicopters",   $"{c.unitAttackHelis:N0} Helis");
                    AddEquipRow(_milEquipContainer, "⚓ Warships & Frigates",  $"{c.unitDestroyers:N0} Vessels");
                    AddEquipRow(_milEquipContainer, "🌊 Submarines",           $"{c.unitSubmarines:N0} Subs");
                }
                else
                {
                    var lockLbl = new Label("🔒 CLASSIFIED WEAPONS ARSENAL — Requires Intel Level 3+ or satellite coverage to decrypt military equipment inventory.")
                    {
                        style = { color = new Color(255/255f, 193/255f, 7/255f), fontSize = 10, whiteSpace = WhiteSpace.Normal, unityFontStyleAndWeight = FontStyle.Bold }
                    };
                    _milEquipContainer.Add(lockLbl);
                }
            }

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

private void OnInviteToFactionClicked()
        {
            if (GameManager.Instance == null || _currentCountry == null) 
            {
                Debug.LogWarning("[CountryInfo] Faction Invite failed: GameManager or _currentCountry is null.");
                return;
            }
            var player = GameManager.Instance.playerCountry;
            if (player == null)
            {
                Debug.LogWarning("[CountryInfo] Faction Invite failed: playerCountry is null.");
                return;
            }
            
            float rel = DashboardLiveContent.ComputeRelation(player, _currentCountry, GameManager.Instance);
            Debug.Log($"[CountryInfo] Attempting Faction Invite: {player.countryName} -> {_currentCountry.countryName}. Relation Score: {rel:F1}");

            if (rel < 55f) { NotificationManager.Instance?.AddNotification($"Relations too low for faction invite ({rel:F0}/100).", Color.red); return; }
            
            player.globalPrestige = Mathf.Min(100f, player.globalPrestige + 2f);
            NotificationManager.Instance?.AddNotification($"INVITED {_currentCountry.countryName} TO FACTION!", new Color(76/255f, 175/255f, 80/255f));
            NewsSystem.Instance?.PublishNews($"FACTIONS: {player.countryName} formally invites {_currentCountry.countryName} into their geopolitical sphere.");
        }

        private void OnDeclareWarClicked()
        {
            if (GameManager.Instance == null || _currentCountry == null) 
            {
                Debug.LogWarning("[CountryInfo] Declare War failed: GameManager or _currentCountry is null.");
                return;
            }
            var player = GameManager.Instance.playerCountry;
            var warSys = GameManager.Instance.WarSystem;
            if (warSys == null || player == null) 
            {
                Debug.LogWarning($"[CountryInfo] Declare War failed: playerCountry={player?.countryName}, WarSystem={warSys}");
                return;
            }

            Debug.Log($"[CountryInfo] Executing Declare War: {player.countryName} vs {_currentCountry.countryName}");
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

        private void AddEquipRow(VisualElement parent, string category, string value, Color? valueColor = null)
        {
            if (parent == null) return;
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.marginBottom = 4;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = new Color(255/255f, 255/255f, 255/255f, 0.05f);
            row.style.paddingBottom = 2;

            var catLbl = new Label(category) { style = { color = new Color(180/255f, 200/255f, 220/255f), fontSize = 10 } };
            var valLbl = new Label(value) { style = { color = valueColor ?? Color.white, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } };

            row.Add(catLbl);
            row.Add(valLbl);
            parent.Add(row);
        }

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
