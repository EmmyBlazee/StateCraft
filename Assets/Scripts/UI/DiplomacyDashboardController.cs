using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using StateCraft.Core;
using StateCraft.Data;
using StateCraft.Systems;
using StateCraft.Interfaces;

namespace StateCraft.UI
{
    public class DiplomacyDashboardController : MonoBehaviour
    {
        private UIDocument _uiDoc;
        private VisualElement _root;
        
        private Button _tabRelations, _tabTreaties, _tabMultilateral, _tabCrisisDesk;
        private VisualElement _panelRelations, _panelTreaties, _panelMultilateral, _panelCrisisDesk;
        private List<VisualElement> _allPanels = new List<VisualElement>();

        private Label _headerPrestige, _headerAlliances, _headerSanctions;
        private ScrollView _globalNationsList;
        private VisualElement _bilateralPanel, _diplomacyWireFeed;
        private Label _selectedCountryName, _selectedRelationText, _selectedTradeVolume, _selectedIncidents;
        private VisualElement _headlineBox;
        private Label _selectedMajorHeadline;
        private VisualElement _treatiesContainer, _crisisContainer;

        private CountryDataSO _selectedCountry;
        private float _selectedRelation;

        private void Awake()
        {
            _uiDoc = GetComponent<UIDocument>();
            if (_uiDoc != null) _root = _uiDoc.rootVisualElement;
            
            var dashboard = _root?.Q<VisualElement>("DiplomacyDashboard");
            if (dashboard != null)
            {
                InitializeElements(dashboard);
                dashboard.style.display = DisplayStyle.None;
            }
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null && GameManager.Instance.TurnSystem != null)
                GameManager.Instance.TurnSystem.OnTurnEnded += RefreshDiplomacyData;
            RefreshDiplomacyData();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null && GameManager.Instance.TurnSystem != null)
                GameManager.Instance.TurnSystem.OnTurnEnded -= RefreshDiplomacyData;
        }

        private void InitializeElements(VisualElement root)
        {
            _tabRelations = root.Q<Button>("TabRelations");
            _tabTreaties = root.Q<Button>("TabTreaties");
            _tabMultilateral = root.Q<Button>("TabMultilateral");
            _tabCrisisDesk = root.Q<Button>("TabCrisisDesk");
            var tabTrade = root.Q<Button>("TabTrade");

            _panelRelations = root.Q<VisualElement>("PanelRelations");
            _panelTreaties = root.Q<VisualElement>("PanelTreaties");
            _panelMultilateral = root.Q<VisualElement>("PanelMultilateral");
            _panelCrisisDesk = root.Q<VisualElement>("PanelCrisisDesk");
            var _panelTrade = root.Q<VisualElement>("PanelTrade");

            _allPanels.Clear();
            _allPanels.AddRange(new[] { _panelRelations, _panelTreaties, _panelTrade, _panelMultilateral, _panelCrisisDesk });

            _tabRelations.clicked += () => SwitchTab(0);
            _tabTreaties.clicked += () => SwitchTab(1);
            if (tabTrade != null) tabTrade.clicked += () => SwitchTab(2);
            _tabMultilateral.clicked += () => SwitchTab(3);
            _tabCrisisDesk.clicked += () => SwitchTab(4);

            var btnAddTrade = root.Q<Button>("BtnAddTrade");
            if (btnAddTrade != null)
            {
                btnAddTrade.clicked += () =>
                {
                    var gm = GameManager.Instance;
                    var player = gm?.playerCountry;

                    // Check if a country is selected and has a Trade Agreement treaty
                    if (_selectedCountry != null && player != null)
                    {
                        bool hasTreaty = StateCraft.Systems.DiplomacySystem
                            .HasSignedTradeAgreementTreaty(player, _selectedCountry);

                        if (!hasTreaty)
                        {
                            NotificationManager.Instance?.AddNotification(
                                $"TRADE BLOCKED: You must sign a Trade Agreement with " +
                                $"{_selectedCountry.countryName} before trading. " +
                                "Select the country in Diplomacy and propose a Trade Agreement first.",
                                UnityEngine.Color.yellow);
                            return;
                        }
                    }

                    var tradeController = FindObjectOfType<TradeAgreementController>();
                    if (tradeController != null) tradeController.Show();
                };
            }

            _headerPrestige = root.Q<Label>("GlobalPrestigeScore");
            _headerAlliances = root.Q<Label>("HeaderAllianceCount");
            _headerSanctions = root.Q<Label>("HeaderSanctionsCount");

            _globalNationsList = root.Q<ScrollView>("GlobalNationsList");
            if (_globalNationsList != null) _globalNationsList.pickingMode = PickingMode.Position;
            _diplomacyWireFeed = root.Q<ScrollView>("DiplomacyWireFeed");
            _bilateralPanel = root.Q<VisualElement>("BilateralPanel");
            _selectedCountryName = root.Q<Label>("SelectedCountryName");
            _selectedRelationText = root.Q<Label>("SelectedRelationText");
            _headlineBox = root.Q<VisualElement>("HeadlineBox");
            _selectedMajorHeadline = root.Q<Label>("SelectedMajorHeadline");
            _selectedTradeVolume = root.Q<Label>("SelectedTradeVolume");
            _selectedIncidents = root.Q<Label>("SelectedIncidents");

            _treatiesContainer = root.Q<ScrollView>("TreatiesContainer");
            _crisisContainer = root.Q<ScrollView>("CrisisContainer");

            // --- Force 3-column horizontal layout in code (overrides any UXML/USS caching) ---
            if (_panelRelations != null)
            {
                _panelRelations.style.flexDirection = FlexDirection.Row;
                _panelRelations.style.height = Length.Percent(100);

                // Wire feed column: narrow, fixed width
                var wireContainer = _diplomacyWireFeed?.parent;
                if (wireContainer != null)
                {
                    wireContainer.style.width = Length.Percent(27);
                    wireContainer.style.minWidth = 160;
                    wireContainer.style.flexShrink = 0;
                    wireContainer.style.marginRight = 10;
                    wireContainer.style.height = Length.Percent(100);
                    if (_diplomacyWireFeed != null)
                    {
                        _diplomacyWireFeed.style.flexGrow = 1;
                        _diplomacyWireFeed.style.maxHeight = StyleKeyword.None; // Remove max-height cap
                    }
                }

                // Nations list column: medium fixed width
                var nationsContainer = _globalNationsList?.parent;
                if (nationsContainer != null)
                {
                    nationsContainer.style.width = Length.Percent(30);
                    nationsContainer.style.minWidth = 160;
                    nationsContainer.style.flexShrink = 0;
                    nationsContainer.style.marginRight = 10;
                    nationsContainer.style.height = Length.Percent(100);
                }

                // Bilateral panel: fills the rest
                if (_bilateralPanel != null)
                {
                    _bilateralPanel.style.flexGrow = 1;
                    _bilateralPanel.style.width = StyleKeyword.Auto;
                    _bilateralPanel.style.paddingTop = 0;
                    _bilateralPanel.style.paddingLeft = 0;
                    _bilateralPanel.style.paddingRight = 0;
                    _bilateralPanel.style.paddingBottom = 0;
                    _bilateralPanel.style.marginLeft = 0;

                    // Wrap inner content in a ScrollView so buttons never get clipped
                    var bilateralScroll = new ScrollView();
                    bilateralScroll.style.flexGrow = 1;
                    bilateralScroll.style.paddingTop = 15;
                    bilateralScroll.style.paddingLeft = 15;
                    bilateralScroll.style.paddingRight = 15;
                    bilateralScroll.style.paddingBottom = 15;
                    var children = new System.Collections.Generic.List<VisualElement>();
                    foreach (var child in _bilateralPanel.Children()) children.Add(child);
                    foreach (var child in children) bilateralScroll.Add(child);
                    _bilateralPanel.Add(bilateralScroll);
                }
            }

            root.Q<Button>("BtnSendAmbassador")?.RegisterCallback<ClickEvent>(_ => OnSendAmbassador());
            root.Q<Button>("BtnOfferTrade")?.RegisterCallback<ClickEvent>(_ => OnOfferTrade());
            root.Q<Button>("BtnUltimatum")?.RegisterCallback<ClickEvent>(_ => OnUltimatum());
            root.Q<Button>("BtnProposeAlliance")?.RegisterCallback<ClickEvent>(_ => OnProposeAlliance());
            root.Q<Button>("BtnProposeCoalition")?.RegisterCallback<ClickEvent>(_ => OnProposeCoalition());
            root.Q<Button>("BtnDeclareWar")?.RegisterCallback<ClickEvent>(_ => OnDeclareWar());
            root.Q<Button>("BtnProposePeace")?.RegisterCallback<ClickEvent>(_ => OnProposePeace());
            root.Q<Button>("BtnDemandReparations")?.RegisterCallback<ClickEvent>(_ => OnDemandReparations());

            root.Q<Button>("CloseDiplomacy").clicked += Hide;
        }

        private void SwitchTab(int index)
        {
            for (int i = 0; i < _allPanels.Count; i++)
            {
                bool active = (i == index);
                _allPanels[i].style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
                _allPanels[i].pickingMode = active ? PickingMode.Position : PickingMode.Ignore;
                // Fix scrollviews in active panel so scrollbars work in builds
                if (active) UIStateManager.FixScrollViews(_allPanels[i]);
            }

            var tabTrade = _root?.Q<Button>("TabTrade");
            var tabs = new[] { _tabRelations, _tabTreaties, tabTrade, _tabMultilateral, _tabCrisisDesk };
            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] == null) continue;
                if (i == index) tabs[i].AddToClassList("active-tab");
                else tabs[i].RemoveFromClassList("active-tab");
            }
        }

        public void Show()
        {
            var panel = _root.Q<VisualElement>("DiplomacyDashboard");
            if (panel != null)
            {
                panel.style.display = DisplayStyle.Flex;
                UIStateManager.Instance?.ApplySafeTransition(panel, true, 0.4f);
                RefreshDiplomacyData();
                UIStateManager.FixScrollViews(panel); // Fix scrollbars in builds after UI refresh
            }
        }

        public void Hide()
        {
            if (MainDashboardToolkitController.Instance != null)
                MainDashboardToolkitController.Instance.HideAllPanels();
        }

        public void RefreshDiplomacyData()
        {
            if (GameManager.Instance == null || GameManager.Instance.playerCountry == null) return;
            var gm = GameManager.Instance;
            var country = gm.playerCountry;

            if (_headerPrestige != null) _headerPrestige.text = $"{country.globalPrestige:F0}/100";
            if (_headerAlliances != null) _headerAlliances.text = country.allianceCount.ToString();
            if (_headerSanctions != null) _headerSanctions.text = $"{country.sanctionIssuedCount} / {country.sanctionReceivedCount}";

            DashboardLiveContent.PopulateFeed(_diplomacyWireFeed, DashboardLiveContent.BuildDiplomacyFeed(country, gm), 6);
            PopulateRelations(country, gm);
            PopulateTreaties(country, gm);
            PopulateTradeDashboard(country, gm);
            PopulateCrises(country, gm);

            if (_selectedCountry != null)
                SelectCountry(_selectedCountry, _selectedRelation);
        }

        private void PopulateRelations(CountryDataSO player, GameManager gm)
        {
            if (_globalNationsList == null) return;
            _globalNationsList.Clear();

            var others = gm.allCountries != null
                ? gm.allCountries.Where(c => c != player).OrderByDescending(c => DashboardLiveContent.ComputeRelation(player, c, gm)).ToList()
                : new List<CountryDataSO>();

            if (others.Count == 0)
            {
                _globalNationsList.Add(new Label("No other nations in simulation.") { style = { color = Color.grey } });
                return;
            }

            foreach (var other in others.Take(24))
            {
                float relation = DashboardLiveContent.ComputeRelation(player, other, gm);
                AddRelationItem(other, relation);
            }
        }

        private void AddRelationItem(CountryDataSO other, float relation)
        {
            // Use VisualElement + ClickEvent instead of Button to avoid unity-dragger blocking clicks inside ScrollView
            var row = new VisualElement();
            row.AddToClassList("sub-panel");
            row.pickingMode = PickingMode.Position;
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.paddingTop = 10;
            row.style.paddingBottom = 10;
            row.style.paddingLeft = 10;
            row.style.paddingRight = 10;
            row.style.marginBottom = 8;

            var left = new VisualElement() { pickingMode = PickingMode.Ignore };
            left.Add(new Label(other.countryName) { style = { color = Color.white, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold }, pickingMode = PickingMode.Ignore });
            left.Add(new Label(DashboardLiveContent.RelationLabel(relation)) { style = { color = DashboardLiveContent.RelationColor(relation), fontSize = 8 }, pickingMode = PickingMode.Ignore });
            row.Add(left);

            var bar = new ProgressBar() { value = relation, highValue = 100, style = { width = 70, height = 4 } };
            bar.pickingMode = PickingMode.Ignore;
            row.Add(bar);

            // Hover highlight
            row.RegisterCallback<MouseEnterEvent>(_ => row.style.backgroundColor = new Color(1f, 1f, 1f, 0.06f));
            row.RegisterCallback<MouseLeaveEvent>(_ => row.style.backgroundColor = StyleKeyword.Null);

            // Click — capture both other and relation so lambda doesn't close over loop variable
            var captureOther = other;
            var captureRelation = relation;
            row.RegisterCallback<ClickEvent>(evt =>
            {
                SelectCountry(captureOther, captureRelation);
                evt.StopPropagation();
            });

            _globalNationsList.Add(row);
        }

        private void SelectCountry(CountryDataSO other, float relation)
        {
            _selectedCountry = other;
            _selectedRelation = relation;
            if (_bilateralPanel == null) return;
            _bilateralPanel.style.display = DisplayStyle.Flex;

            var gm = GameManager.Instance;
            var player = gm.playerCountry;

            _selectedCountryName.text = other.countryName.ToUpper();
            _selectedRelationText.text = DashboardLiveContent.RelationLabel(relation);
            _selectedRelationText.style.color = DashboardLiveContent.RelationColor(relation);

            float tradeVol = other.tradeVolumeUSD > 0 ? other.tradeVolumeUSD : relation * 20e6f;
            _selectedTradeVolume.text = $"${tradeVol / 1e9f:F2}B / Year";

            string incident = "NONE";
            var stage = gm.WarSystem != null ? gm.WarSystem.GetWarStage(other) : WarStage.Peace;
            
            if (stage == WarStage.Conflict)
                incident = $"WAR (Exhaustion: {other.warExhaustion:F0}%)";
            else if (stage >= WarStage.Crisis)
                incident = $"WAR STAGE: {stage}";
            else if (relation < 25f)
                incident = "BORDER TENSION";
            else if (other.stability < 40f)
                incident = "PARTNER UNREST";
            
            _selectedIncidents.text = incident;

            if (_selectedMajorHeadline != null && _headlineBox != null)
            {
                Color boxColor = new Color(0, 210/255f, 255/255f);
                string headline = "";
                bool foundNews = false;

                if (NewsSystem.Instance != null)
                {
                    string latestNews = NewsSystem.Instance.GlobalNewsHistory.Reverse().FirstOrDefault(n => n.IndexOf(other.countryName, System.StringComparison.OrdinalIgnoreCase) >= 0);
                    if (string.IsNullOrEmpty(latestNews))
                        latestNews = NewsSystem.Instance.NewsHistory.Reverse().FirstOrDefault(n => n.IndexOf(other.countryName, System.StringComparison.OrdinalIgnoreCase) >= 0);
                    
                    if (!string.IsNullOrEmpty(latestNews))
                    {
                        headline = latestNews;
                        foundNews = true;
                        
                        if (headline.Contains("WAR") || headline.Contains("MILITARY") || headline.Contains("SKIRMISH") || headline.Contains("CONFLICT") || headline.Contains("ATTACK")) 
                            boxColor = new Color(244/255f, 67/255f, 54/255f);
                        else if (headline.Contains("CRISIS") || headline.Contains("TURMOIL") || headline.Contains("COLLAPSE") || headline.Contains("UNREST") || headline.Contains("TENSION")) 
                            boxColor = new Color(255/255f, 82/255f, 82/255f);
                        else if (headline.Contains("TRADE") || headline.Contains("ECONOMY") || headline.Contains("SURGE") || headline.Contains("PACT") || headline.Contains("ALLY") || headline.Contains("REFORM")) 
                            boxColor = new Color(76/255f, 175/255f, 80/255f);
                        else if (headline.Contains("INTEL") || headline.Contains("SPY") || headline.Contains("CYBER") || headline.Contains("NETWORK")) 
                            boxColor = new Color(186/255f, 104/255f, 200/255f);
                        else if (headline.Contains("ALERT") || headline.Contains("WARNING") || headline.Contains("TENSION"))
                            boxColor = new Color(255/255f, 152/255f, 0/255f);
                    }
                }

                if (!foundNews)
                {
                    headline = $"Situation normal in {other.capitalCity}. Intelligence channels monitoring standard traffic.";

                    if (stage == WarStage.Conflict)
                    {
                        boxColor = new Color(244/255f, 67/255f, 54/255f);
                        headline = $"MAJOR CONFLICT: Full combat mobilization authorized against {other.countryName}.";
                    }
                    else if (other.stability < 35f)
                    {
                        boxColor = new Color(255/255f, 82/255f, 82/255f);
                        headline = $"CRITICAL UNREST: {other.countryName} is experiencing mass protests and failing internal stability (Stability: {other.stability:F0}%).";
                    }
                    else if (other.exposureRisk > 70f)
                    {
                        boxColor = new Color(186/255f, 104/255f, 200/255f);
                        headline = $"COUNTER-INTEL: Severe intelligence leak detected in {other.countryName}. Their covert ops are highly exposed.";
                    }
                    else if (relation > 85f)
                    {
                        boxColor = new Color(76/255f, 175/255f, 80/255f);
                        headline = $"STEADFAST ALLY: Diplomatic ties with {other.countryName} are exceptionally strong. Cooperative frameworks active.";
                    }
                    else if (relation < 20f)
                    {
                        boxColor = new Color(255/255f, 152/255f, 0/255f);
                        headline = $"TENSION ESCALATING: Relations with {other.countryName} are deteriorating rapidly. Diplomatic channels freezing.";
                    }
                    else if (other.foodSecurity < 50f)
                    {
                        boxColor = new Color(255/255f, 152/255f, 0/255f);
                        headline = $"RESOURCE CRISIS: Agricultural failures threatening {other.countryName} (Food Security: {other.foodSecurity:F0}%).";
                    }
                }

                _headlineBox.style.backgroundColor = new Color(boxColor.r, boxColor.g, boxColor.b, 0.15f);
                _headlineBox.style.borderTopColor = new Color(boxColor.r, boxColor.g, boxColor.b, 0.7f);
                _headlineBox.style.borderBottomColor = new Color(boxColor.r, boxColor.g, boxColor.b, 0.7f);
                _headlineBox.style.borderLeftColor = new Color(boxColor.r, boxColor.g, boxColor.b, 0.7f);
                _headlineBox.style.borderRightColor = new Color(boxColor.r, boxColor.g, boxColor.b, 0.7f);
                
                var titleLabel = _headlineBox.Q<Label>(classes: null); // Gets the first label (the title)
                if (titleLabel != null && titleLabel.name != "SelectedMajorHeadline")
                    titleLabel.style.color = boxColor;

                _selectedMajorHeadline.text = headline;
            }

            var btnAmb      = _root?.Q<Button>("BtnSendAmbassador");
            var btnTrade    = _root?.Q<Button>("BtnOfferTrade");
            var btnUlt      = _root?.Q<Button>("BtnUltimatum");
            var btnAlliance = _root?.Q<Button>("BtnProposeAlliance");
            var btnCoalition= _root?.Q<Button>("BtnProposeCoalition");
            var btnWar      = _root?.Q<Button>("BtnDeclareWar");
            var btnPeace    = _root?.Q<Button>("BtnProposePeace");
            var btnRep      = _root?.Q<Button>("BtnDemandReparations");

            // Rename buttons to be clearer about what they do
            if (btnAmb != null)       btnAmb.text       = "🤝  IMPROVE RELATIONS  (+8)";
            if (btnTrade != null)     btnTrade.text     = "📝  PROPOSE TRADE AGREEMENT";
            if (btnUlt != null)       btnUlt.text       = "⚠️  ISSUE SANCTIONS";
            if (btnAlliance != null)  btnAlliance.text  = "🛡  PROPOSE ALLIANCE";
            if (btnCoalition != null) btnCoalition.text = "⚔️  COALITION PACT";
            if (btnWar != null)       btnWar.text       = "💀  DECLARE WAR";
            if (btnPeace != null)     btnPeace.text     = "🕊  PROPOSE PEACE";
            if (btnRep != null)       btnRep.text       = "💰  DEMAND REPARATIONS";

            if (stage == WarStage.Conflict)
            {
                if (btnAmb != null) btnAmb.style.display = DisplayStyle.None;
                if (btnTrade != null) btnTrade.style.display = DisplayStyle.None;
                if (btnUlt != null) btnUlt.style.display = DisplayStyle.None;
                if (btnAlliance != null) btnAlliance.style.display = DisplayStyle.None;
                if (btnCoalition != null) btnCoalition.style.display = DisplayStyle.None;
                if (btnWar != null) btnWar.style.display = DisplayStyle.None;
                
                if (btnPeace != null) btnPeace.style.display = DisplayStyle.Flex;
                if (btnRep != null) btnRep.style.display = DisplayStyle.Flex;
            }
            else
            {
                if (btnAmb != null) btnAmb.style.display = DisplayStyle.Flex;
                if (btnTrade != null) btnTrade.style.display = DisplayStyle.Flex;
                if (btnUlt != null) btnUlt.style.display = DisplayStyle.Flex;
                if (btnAlliance != null) btnAlliance.style.display = DisplayStyle.Flex;
                if (btnCoalition != null) btnCoalition.style.display = DisplayStyle.Flex;
                if (btnWar != null) btnWar.style.display = DisplayStyle.Flex;
                
                if (btnPeace != null) btnPeace.style.display = DisplayStyle.None;
                if (btnRep != null) btnRep.style.display = DisplayStyle.None;
            }
        }

        private void PopulateTreaties(CountryDataSO country, GameManager gm)
        {
            if (_treatiesContainer == null) return;
            _treatiesContainer.Clear();

            bool any = false;

            if (gm.DiplomacySystem is DiplomacySystem diplo)
            {
                foreach (var deal in diplo.ActiveDeals)
                {
                    if (deal.buyerCountry != country.countryName && deal.sellerCountry != country.countryName) continue;
                    any = true;
                    AddTradeDealCard(deal, country, diplo);
                }
            }

            foreach (var treaty in country.activeTreaties)
            {
                any = true;
                AddTreatyCard(treaty);
            }

            if (!any)
                _treatiesContainer.Add(new Label("No active agreements — offer trade from the Relations tab.") { style = { color = Color.grey, marginTop = 30, unityTextAlign = TextAnchor.MiddleCenter } });
        }

        private void PopulateTradeDashboard(CountryDataSO country, GameManager gm)
        {
            var tradeList = _root?.Q<ScrollView>("DashboardActiveAgreementsList");
            if (tradeList == null) return;
            
            tradeList.Clear();
            
            bool any = false;
            foreach (var agr in country.activeTradeAgreements)
            {
                if (agr.status == TradeAgreementStatus.Active)
                {
                    any = true;
                    var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 5, paddingBottom = 5, borderBottomWidth = 1, borderBottomColor = new Color(0.2f, 0.2f, 0.2f) } };
                    
                    string directionStr = agr.isSeller ? "(Exporting to)" : "(Importing from)";
                    var lblInfo = new Label($"{agr.resource} {directionStr} {agr.partnerCountry}") { style = { color = Color.white, fontSize = 12 } };
                    string type = "mo";
                    float usd = agr.monthlyPaymentUSD;
                    float local = usd * country.exchangeRate;
                    string currSym = country.currencySymbol;
                    var lblTerms = new Label($"${usd / 1e6f:F1}M ({currSym}{local / 1e6f:F1}M) | Qty: {agr.resourceQuantity} | {agr.durationMonths} {type}") { style = { color = new Color(0, 0.9f, 0.46f), fontSize = 12 } };
                    
                    row.Add(lblInfo);
                    row.Add(lblTerms);
                    
                    tradeList.Add(row);
                }
            }
            
            if (!any)
                tradeList.Add(new Label("No active trade agreements — click 'Propose New Trade' to start.") { style = { color = Color.grey, marginTop = 30, unityTextAlign = TextAnchor.MiddleCenter } });
        }

        private void AddTradeDealCard(TradeDeal deal, CountryDataSO player, DiplomacySystem diplo)
        {
            var card = new VisualElement();
            card.AddToClassList("sub-panel");
            card.style.paddingTop = 15;
            card.style.paddingBottom = 15;
            card.style.paddingLeft = 15;
            card.style.paddingRight = 15;
            card.style.marginBottom = 10;

            bool isBuyer = deal.buyerCountry == player.countryName;
            card.Add(new Label($"TRADE: {deal.resource}") { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold } });
            card.Add(new Label($"{deal.sellerCountry} → {deal.buyerCountry}") { style = { color = new Color(76/255f, 175/255f, 80/255f), fontSize = 10, marginTop = 4 } });
            card.Add(new Label($"${deal.monthlyPaymentUSD / 1e6f:F1}M/mo · {deal.durationTurns} months · You are {(isBuyer ? "BUYER" : "SELLER")}") { style = { color = Color.grey, fontSize = 9, marginTop = 4 } });

            var footer = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd, marginTop = 10 } };
            var cancel = new Button(() =>
            {
                diplo.CancelTradeDeal(deal.id);
                NotificationManager.Instance?.AddNotification($"Trade deal ({deal.resource}) terminated.", new Color(255/255f, 171/255f, 64/255f));
                RefreshDiplomacyData();
            }) { text = "CANCEL DEAL" };
            cancel.AddToClassList("button-premium");
            cancel.style.height = 22;
            cancel.style.fontSize = 8;
            footer.Add(cancel);
            card.Add(footer);
            _treatiesContainer.Add(card);
        }

        private void AddTreatyCard(DiplomaticTreaty treaty)
        {
            var card = new VisualElement();
            card.AddToClassList("sub-panel");
            card.style.paddingTop = 15;
            card.style.paddingBottom = 15;
            card.style.paddingLeft = 15;
            card.style.paddingRight = 15;
            card.style.marginBottom = 10;

            var header = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            header.Add(new Label(treaty.treatyName) { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold } });
            header.Add(new Label(treaty.partnerCountry) { style = { color = Rgb(76, 175, 80), fontSize = 10 } });
            card.Add(header);
            card.Add(new Label(treaty.terms) { style = { color = Color.grey, fontSize = 9, marginTop = 5 } });
            card.Add(new Label($"Expires in {treaty.monthsRemaining} months") { style = { color = Color.grey, fontSize = 8, marginTop = 8 } });
            _treatiesContainer.Add(card);
        }

        private void PopulateCrises(CountryDataSO country, GameManager gm)
        {
            if (_crisisContainer == null) return;
            _crisisContainer.Clear();

            var crises = new List<GlobalCrisis>(country.activeCrises);
            if (crises.Count == 0)
                crises.AddRange(GenerateAmbientCrises(country, gm));

            if (crises.Count == 0)
            {
                _crisisContainer.Add(new Label("No global emergencies on the desk — situation calm.") { style = { color = Color.grey, marginTop = 40, unityTextAlign = TextAnchor.MiddleCenter } });
                return;
            }

            foreach (var crisis in crises)
                AddCrisisCard(crisis);
        }

        private static List<GlobalCrisis> GenerateAmbientCrises(CountryDataSO c, GameManager gm)
        {
            var list = new List<GlobalCrisis>();
            if (gm.allCountries == null) return list;

            foreach (var other in gm.allCountries)
            {
                if (other == c) continue;
                if (gm.WarSystem != null && gm.WarSystem.GetWarStage(other) == WarStage.Conflict)
                {
                    list.Add(new GlobalCrisis
                    {
                        crisisName = $"Armed conflict: {other.countryName}",
                        affectedRegion = other.capitalCity,
                        severity = "Severe",
                        internationalPressure = 75f
                    });
                    break;
                }
            }

            if (c.foodSecurity < 45f)
                list.Add(new GlobalCrisis { crisisName = "Regional food security emergency", affectedRegion = c.countryName, severity = "Severe", internationalPressure = 55f });

            return list;
        }

        private void AddCrisisCard(GlobalCrisis crisis)
        {
            var card = new VisualElement();
            card.AddToClassList("sub-panel");
            card.style.paddingTop = 20;
            card.style.paddingBottom = 20;
            card.style.paddingLeft = 20;
            card.style.paddingRight = 20;
            card.style.marginBottom = 15;

            card.Add(new Label(crisis.crisisName) { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 14 } });
            card.Add(new Label($"Location: {crisis.affectedRegion} | Severity: {crisis.severity}") { style = { color = Color.grey, fontSize = 10, marginTop = 5 } });
            card.Add(new Label("INTERNATIONAL PRESSURE") { style = { color = Color.grey, fontSize = 9, marginTop = 15 } });
            card.Add(new ProgressBar() { value = crisis.internationalPressure, highValue = 100, style = { height = 4 } });

            var actions = new VisualElement() { style = { flexDirection = FlexDirection.Row, marginTop = 20, justifyContent = Justify.SpaceBetween } };
            AddCrisisAction(actions, "SEND AID", () => NotifyAction("Humanitarian package dispatched."));
            AddCrisisAction(actions, "MEDIATE", () => NotifyAction("Mediation team deployed to talks."));
            AddCrisisAction(actions, "EMBARGO", () => NotifyAction("Embargo measures drafted."));
            AddCrisisAction(actions, "STAY NEUTRAL", () => NotifyAction("Cabinet voted to remain neutral."));
            card.Add(actions);
            _crisisContainer.Add(card);
        }

        private static void AddCrisisAction(VisualElement parent, string label, System.Action onClick)
        {
            var btn = new Button(onClick) { text = label };
            btn.AddToClassList("button-premium");
            btn.style.width = Length.Percent(24);
            btn.style.height = 30;
            btn.style.fontSize = 8;
            parent.Add(btn);
        }

        private static void NotifyAction(string msg)
        {
            NotificationManager.Instance?.AddNotification(msg, new Color(76/255f, 175/255f, 80/255f));
            NewsSystem.Instance?.PublishNews(msg);
        }

        private void OnSendAmbassador()
        {
            if (_selectedCountry == null) { NotifyNeedSelection(); return; }
            var player = GameManager.Instance.playerCountry;

            float cost = 0.005f; // $5M diplomatic cost
            if (GameManager.Instance.playerBudget < cost)
            {
                NotificationManager.Instance?.AddNotification("INSUFFICIENT FUNDS: Need $0.005B to send an ambassador.", Color.red);
                return;
            }
            GameManager.Instance.playerBudget -= cost;
            player.treasury = GameManager.Instance.playerBudget;

            _selectedRelation = Mathf.Min(100f, _selectedRelation + 8f);
            player.globalPrestige = Mathf.Min(100f, player.globalPrestige + 1f);

            NotificationManager.Instance?.AddNotification(
                $"AMBASSADOR SENT: Relations with {_selectedCountry.countryName} +8. Cost: $0.005B.", 
                new Color(0.4f, 0.8f, 1f));
            NewsSystem.Instance?.PublishNews(
                $"DIPLOMACY: {player.countryName} sends ambassador to {_selectedCountry.countryName}.");
            RefreshDiplomacyData();
        }

        private void OnOfferTrade()
        {
            if (_selectedCountry == null) { NotifyNeedSelection(); return; }
            var player = GameManager.Instance.playerCountry;
            
            // Check if already signed
            if (StateCraft.Systems.DiplomacySystem.HasSignedTradeAgreementTreaty(player, _selectedCountry))
            {
                NotificationManager.Instance?.AddNotification(
                    $"TRADE AGREEMENT ALREADY SIGNED: Open the Trade Agreements tab to set up resource deals.", Color.yellow);
                return;
            }

            // Check relations
            if (_selectedRelation < 40f)
            {
                NotificationManager.Instance?.AddNotification(
                    $"REJECTED: Need 40+ relations for a Trade Agreement. Current: {_selectedRelation:F0}.", Color.red);
                return;
            }

            // Deduct cost
            float cost = 0.01f; // $10M diplomatic cost
            if (GameManager.Instance.playerBudget < cost)
            {
                NotificationManager.Instance?.AddNotification("INSUFFICIENT FUNDS: Need $0.01B to open formal trade channels.", Color.red);
                return;
            }
            GameManager.Instance.playerBudget -= cost;
            player.treasury = GameManager.Instance.playerBudget;

            // Sign the treaty
            var treaty = new DiplomaticTreaty
            {
                treatyName    = "Trade Agreement",
                partnerCountry= _selectedCountry.countryName,
                terms         = "Permits the negotiation and signing of bilateral resource deals and economic investments.",
                monthsRemaining = 36
            };
            player.activeTreaties.Add(treaty);
            _selectedCountry.activeTreaties.Add(new DiplomaticTreaty
            {
                treatyName    = "Trade Agreement",
                partnerCountry= player.countryName,
                terms         = treaty.terms,
                monthsRemaining = 36
            });
            
            player.globalPrestige = Mathf.Min(100f, player.globalPrestige + 1f);
            
            NotificationManager.Instance?.AddNotification(
                $"TRADE AGREEMENT SIGNED with {_selectedCountry.countryName}. You can now propose trades! Cost: $0.01B.", 
                new Color(0f, 0.85f, 0.45f));
            NewsSystem.Instance?.PublishNews(
                $"TRADE: {player.countryName} and {_selectedCountry.countryName} sign a historic Trade Agreement.");
                
            RefreshDiplomacyData();
        }

        private void OnUltimatum()
        {
            if (_selectedCountry == null) { NotifyNeedSelection(); return; }
            var player = GameManager.Instance.playerCountry;

            // Sanctions: hurts their economy, costs prestige, drops relations both ways
            _selectedCountry.gdp        = Mathf.Max(0f, _selectedCountry.gdp * 0.97f);        // -3% GDP
            _selectedCountry.stability  = Mathf.Max(0f, _selectedCountry.stability - 5f);      // -5 stability
            _selectedRelation           = Mathf.Max(0f, _selectedRelation - 20f);              // -20 relations
            player.globalPrestige       = Mathf.Max(0f, player.globalPrestige - 3f);           // costs prestige
            player.sanctionIssuedCount++;

            NotificationManager.Instance?.AddNotification(
                $"SANCTIONS IMPOSED: {_selectedCountry.countryName} GDP -3%, Stability -5. Relations drop -20. Your prestige -3.",
                new Color(1f, 0.6f, 0.1f));
            NewsSystem.Instance?.PublishNews(
                $"SANCTIONS: {player.countryName} imposes economic sanctions on {_selectedCountry.countryName}.");
            RefreshDiplomacyData();
        }

        private void OnProposeAlliance()
        {
            if (_selectedCountry == null) { NotifyNeedSelection(); return; }
            var player = GameManager.Instance.playerCountry;

            if (_selectedRelation < 65f)
            {
                NotificationManager.Instance?.AddNotification(
                    $"REJECTED: Need 65+ relations for an alliance. Current: {_selectedRelation:F0}.", Color.red);
                return;
            }

            // Check not already allied
            if (player.activeTreaties.Any(t => t.partnerCountry == _selectedCountry.countryName && t.treatyName == "Mutual Defence Pact"))
            {
                NotificationManager.Instance?.AddNotification(
                    $"Already in a Mutual Defence Pact with {_selectedCountry.countryName}.", Color.yellow);
                return;
            }

            // Alliance costs prestige to establish and commits both nations
            float cost = 0.02f; // $20M diplomatic cost
            if (GameManager.Instance.playerBudget < cost)
            {
                NotificationManager.Instance?.AddNotification("INSUFFICIENT FUNDS: Need $0.02B for alliance negotiations.", Color.red);
                return;
            }
            GameManager.Instance.playerBudget -= cost;
            player.treasury = GameManager.Instance.playerBudget;

            var treaty = new DiplomaticTreaty
            {
                treatyName    = "Mutual Defence Pact",
                partnerCountry= _selectedCountry.countryName,
                terms         = "Both nations pledge military assistance if either is attacked. Grants +10 stability to each party while active.",
                monthsRemaining = 36
            };
            player.activeTreaties.Add(treaty);
            _selectedCountry.activeTreaties.Add(new DiplomaticTreaty
            {
                treatyName    = "Mutual Defence Pact",
                partnerCountry= player.countryName,
                terms         = treaty.terms,
                monthsRemaining = 36
            });

            player.allianceCount++;
            _selectedCountry.allianceCount++;
            player.stability   = Mathf.Min(100f, player.stability + 5f);
            _selectedCountry.stability = Mathf.Min(100f, _selectedCountry.stability + 5f);
            player.globalPrestige = Mathf.Min(100f, player.globalPrestige + 5f);
            _selectedRelation  = Mathf.Min(100f, _selectedRelation + 10f);

            NotificationManager.Instance?.AddNotification(
                $"ALLIANCE FORMED: Mutual Defence Pact with {_selectedCountry.countryName}. Both nations +5 stability, 36-month treaty.", 
                new Color(0f, 0.85f, 0.45f));
            NewsSystem.Instance?.PublishNews(
                $"ALLIANCE: {player.countryName} and {_selectedCountry.countryName} sign a Mutual Defence Pact.");
            RefreshDiplomacyData();
        }

        private void OnProposeCoalition()
        {
            if (_selectedCountry == null) { NotifyNeedSelection(); return; }
            var player = GameManager.Instance.playerCountry;
            var gm = GameManager.Instance;

            // ── Requirement checks ──
            if (_selectedRelation < 80f)
            {
                NotificationManager.Instance?.AddNotification(
                    $"REJECTED: Coalition Pact requires 80+ relations. Current: {_selectedRelation:F0}. " +
                    $"Build relations first via ambassador missions and trade deals.", Color.red);
                return;
            }

            if (player.activeTreaties.Any(t => t.partnerCountry == _selectedCountry.countryName &&
                (t.treatyName == "Coalition Force Pact" || t.treatyName.Contains("Coalition Force"))))
            {
                NotificationManager.Instance?.AddNotification(
                    $"ACTIVE PACT: You already have a Coalition Force Pact with {_selectedCountry.countryName}.", Color.yellow);
                return;
            }

            // ── Cost: Coalition maintenance requires budget commitment ──
            float signingCost = 0.15f; // $150M signing cost
            if (gm.playerBudget < signingCost)
            {
                NotificationManager.Instance?.AddNotification(
                    $"INSUFFICIENT FUNDS: Coalition pact requires $0.15B signing cost.", Color.red);
                return;
            }

            // ── Partner contribution calculation ──
            // Partner contributes troops proportional to their military strength
            int partnerInfantry = Mathf.RoundToInt(_selectedCountry.unitInfantry * 0.15f); // 15% of their forces
            int partnerTanks    = Mathf.RoundToInt(_selectedCountry.unitTanks    * 0.10f);
            int partnerFighters = Mathf.RoundToInt(_selectedCountry.unitFighters * 0.10f);
            bool partnerHasForces = partnerInfantry + partnerTanks > 0;

            if (!partnerHasForces)
            {
                NotificationManager.Instance?.AddNotification(
                    $"WARNING: {_selectedCountry.countryName} has no military capacity to contribute. " +
                    $"This pact provides diplomatic benefits only.", Color.yellow);
            }

            // Deduct signing cost
            gm.playerBudget -= signingCost;
            player.treasury = gm.playerBudget;

            // ── Create treaty with real terms ──
            string terms =
                $"Joint forces committed: {partnerInfantry:N0} infantry, {partnerTanks} tanks, {partnerFighters} fighters from {_selectedCountry.countryName}. " +
                $"Monthly maintenance: $0.02B. Duration: 36 months. " +
                $"Obligations: mutual defence if either party is attacked; joint insurgency suppression; shared intelligence.";

            var playerTreaty = new DiplomaticTreaty
            {
                treatyName    = "Coalition Force Pact",
                partnerCountry= _selectedCountry.countryName,
                terms         = terms,
                monthsRemaining = 36
            };
            player.activeTreaties.Add(playerTreaty);

            _selectedCountry.activeTreaties.Add(new DiplomaticTreaty
            {
                treatyName    = "Coalition Force Pact",
                partnerCountry= player.countryName,
                terms         = terms,
                monthsRemaining = 36
            });

            // ── Partner deploys a contribution to player's military ──
            if (partnerHasForces)
            {
                player.unitInfantry += partnerInfantry;
                player.unitTanks    += partnerTanks;
                player.unitFighters += partnerFighters;
                _selectedCountry.unitInfantry -= partnerInfantry;
                _selectedCountry.unitTanks    -= partnerTanks;
                _selectedCountry.unitFighters -= partnerFighters;
            }

            // ── Diplomatic effects ──
            _selectedRelation = Mathf.Min(100f, _selectedRelation + 10f);
            player.globalPrestige       = Mathf.Min(100f, player.globalPrestige + 8f);
            player.stability            = Mathf.Min(100f, player.stability + 5f);
            _selectedCountry.stability  = Mathf.Min(100f, _selectedCountry.stability + 5f);

            // ── Detailed inbox briefing ──
            InboxSystem.Instance?.SendMessage(player, "JOINT CHIEFS OF STAFF",
                $"⚔️ COALITION FORCE PACT ACTIVE — {_selectedCountry.countryName}",
                $"The Coalition Force Pact with {_selectedCountry.countryName} is now active.\n\n" +
                $"PARTNER CONTRIBUTION RECEIVED:\n" +
                $"• {partnerInfantry:N0} infantry deployed to your forces\n" +
                $"• {partnerTanks} tanks transferred to your command\n" +
                $"• {partnerFighters} fighter jets on standby\n\n" +
                $"WHAT THIS MEANS:\n" +
                $"• If you are attacked, {_selectedCountry.countryName} will automatically send reinforcements\n" +
                $"• Insurgency attacks in your regions are suppressed by 75% via joint patrols\n" +
                $"• Intelligence is shared — their counter-intel score adds to yours\n" +
                $"• If your war exhaustion hits critical levels, coalition forces absorb 30% of casualties\n\n" +
                $"OBLIGATIONS:\n" +
                $"• Monthly maintenance cost: $0.02B deducted automatically\n" +
                $"• If {_selectedCountry.countryName} is attacked, you must send troops or the pact collapses\n" +
                $"• Treaty expires in 36 months — renew via Diplomacy before it lapses\n\n" +
                $"Signing cost paid: $0.15B. Prestige +8. Stability +5.",
                "Important");

            NotificationManager.Instance?.AddNotification(
                $"⚔️ COALITION PACT SIGNED: {_selectedCountry.countryName} deploys {partnerInfantry:N0} troops, " +
                $"{partnerTanks} tanks, {partnerFighters} jets to reinforce your military. " +
                $"Mutual defence active for 36 months.",
                new Color(0f, 0.85f, 1f));

            NewsSystem.Instance?.PublishNews(
                $"COALITION: {player.countryName} and {_selectedCountry.countryName} sign Coalition Force Pact. " +
                $"Joint military command established. Regional security operations begin immediately.");
            NewsSystem.Instance?.PublishNews(
                $"GEOPOLITICS: {_selectedCountry.countryName} troops cross into {player.countryName} as part of coalition agreement.");

            RefreshDiplomacyData();
        }

        private void OnDeclareWar()
        {
            if (_selectedCountry == null) { NotifyNeedSelection(); return; }
            var player = GameManager.Instance.playerCountry;
            var warSys = GameManager.Instance.WarSystem;
            if (warSys == null) return;

            warSys.DeclareWar(player, _selectedCountry);
            _selectedRelation = 0f; // Drop relations immediately to absolute zero
            
            NotificationManager.Instance?.AddNotification($"WAR DECLARED: Mobilizing all combat branches against {_selectedCountry.countryName}!", Color.red);
            NewsSystem.Instance?.PublishNews($"CONFLICT: {player.countryName} has declared war against {_selectedCountry.countryName}! Tactical divisions are mobilizing.");
            
            RefreshDiplomacyData();
        }

        private void OnProposePeace()
        {
            if (_selectedCountry == null) { NotifyNeedSelection(); return; }
            var player = GameManager.Instance.playerCountry;
            var warSys = GameManager.Instance.WarSystem as WarSystem;
            if (warSys == null) return;

            warSys.ProposePeace(player, _selectedCountry);
            RefreshDiplomacyData();
        }

        private void OnDemandReparations()
        {
            if (_selectedCountry == null) { NotifyNeedSelection(); return; }
            var player = GameManager.Instance.playerCountry;
            var warSys = GameManager.Instance.WarSystem as WarSystem;
            if (warSys == null) return;

            warSys.ProposeReparationsPeace(player, _selectedCountry);
            RefreshDiplomacyData();
        }

        private static void NotifyNeedSelection()
        {
            NotificationManager.Instance?.AddNotification("Select a nation from the list first.", Color.yellow);
        }

        private static Color Rgb(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);
    }
}