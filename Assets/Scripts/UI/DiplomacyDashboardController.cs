using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using StateCraft.Core;
using StateCraft.Data;
using StateCraft.Systems;

namespace StateCraft.UI
{
    [DefaultExecutionOrder(-10)]
    public class DiplomacyDashboardController : MonoBehaviour
    {
        private UIDocument _uiDoc;
        private VisualElement _root;
        private VisualElement _dashboardRoot;
        
        private Button[] _tabs;
        private VisualElement[] _panels;

        // KPI Headers
        private Label _headerAllies, _headerRivals, _headerNeutral, _headerInfluence, _headerPrestige;

        private ScrollView _dipEventsList;
        private VisualElement _overviewTreatiesList, _overviewOrgsList;
        private Label _globalRepValue, _globalRepDesc;

        private VisualElement _bilateralCardsGrid;
        private VisualElement _treatiesCardsList;
        private VisualElement _organizationsCardsGrid;
        
        private Label _embTotal, _embOperational, _embClosed, _embBudget;
        private VisualElement _embassiesCardsGrid;

        private void Awake()
        {
            _uiDoc = GetComponent<UIDocument>();
            if (_uiDoc != null) _root = _uiDoc.rootVisualElement;
            
            var dashboard = _root?.Q<VisualElement>("DiplomacyDashboard");
            if (dashboard != null)
            {
                _dashboardRoot = dashboard;
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
            _tabs = new Button[]
            {
                root.Q<Button>("TabOverview"),
                root.Q<Button>("TabBilateral"),
                root.Q<Button>("TabTreaties"),
                root.Q<Button>("TabOrganizations"),
                root.Q<Button>("TabEmbassies")
            };

            _panels = new VisualElement[]
            {
                root.Q<VisualElement>("PanelOverview"),
                root.Q<VisualElement>("PanelBilateral"),
                root.Q<VisualElement>("PanelTreaties"),
                root.Q<VisualElement>("PanelOrganizations"),
                root.Q<VisualElement>("PanelEmbassies")
            };

            for (int i = 0; i < _tabs.Length; i++)
            {
                int index = i;
                if (_tabs[i] != null)
                    _tabs[i].clicked += () => SwitchTab(index);
            }

            root.Q<Button>("CloseDiplomacy")?.RegisterCallback<ClickEvent>(_ => Hide());

            _headerPrestige = root.Q<Label>("GlobalPrestigeScore");
            _headerAllies = root.Q<Label>("HeaderAlliesCount");
            _headerRivals = root.Q<Label>("HeaderRivalsCount");
            _headerNeutral = root.Q<Label>("HeaderNeutralCount");
            _headerInfluence = root.Q<Label>("HeaderInfluenceVal");

            _dipEventsList = root.Q<ScrollView>("DipEventsList");
            _overviewTreatiesList = root.Q<VisualElement>("OverviewTreatiesList");
            _overviewOrgsList = root.Q<VisualElement>("OverviewOrgsList");
            _globalRepValue = root.Q<Label>("GlobalRepValue");
            _globalRepDesc = root.Q<Label>("GlobalRepDesc");

            _bilateralCardsGrid = root.Q<VisualElement>("BilateralCardsGrid");
            _treatiesCardsList = root.Q<VisualElement>("TreatiesCardsList");
            _organizationsCardsGrid = root.Q<VisualElement>("OrganizationsCardsGrid");
            _embassiesCardsGrid = root.Q<VisualElement>("EmbassiesCardsGrid");

            _embTotal = root.Q<Label>("EmbTotal");
            _embOperational = root.Q<Label>("EmbOperational");
            _embClosed = root.Q<Label>("EmbClosed");
            _embBudget = root.Q<Label>("EmbBudget");
        }

        private void SwitchTab(int index)
        {
            for (int i = 0; i < _panels.Length; i++)
            {
                if (_panels[i] == null) continue;
                bool active = (i == index);
                _panels[i].style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
                if (active) UIStateManager.FixScrollViews(_panels[i]);
            }

            for (int i = 0; i < _tabs.Length; i++)
            {
                if (_tabs[i] == null) continue;
                if (i == index) _tabs[i].AddToClassList("active-tab");
                else _tabs[i].RemoveFromClassList("active-tab");
            }
        }

        public void Show()
        {
            var panel = _dashboardRoot;
            if (panel != null)
            {
                panel.style.display = DisplayStyle.Flex;
                UIStateManager.Instance?.ApplySafeTransition(panel, true, 0.4f);
                RefreshDiplomacyData();
                UIStateManager.FixScrollViews(panel);
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
            var player = gm.playerCountry;

            SeedMockDataIfEmpty(player);

            if (_headerPrestige != null) _headerPrestige.text = $"PRESTIGE: {player.globalPrestige:F0}/100";
            
            int allies = player.activeTreaties.Count(t => t.treatyName == "Mutual Defence Pact");
            if (_headerAllies != null) _headerAllies.text = allies.ToString();
            if (_headerRivals != null) _headerRivals.text = "2"; // Mock
            if (_headerNeutral != null) _headerNeutral.text = (gm.allCountries?.Count - allies - 2 ?? 45).ToString();
            if (_headerInfluence != null) _headerInfluence.text = $"{player.diplomacyInfluence:F0}%";

            PopulateOverviewTab(player);
            PopulateBilateralTab(player, gm);
            PopulateTreatiesTab(player, gm);
            PopulateOrganizationsTab(player);
            PopulateEmbassiesTab(player);
        }

        private void SeedMockDataIfEmpty(CountryDataSO player)
        {
            if (player.diplomacyEvents.Count == 0)
            {
                player.diplomacyEvents.Add(new DiplomaticEvent { eventName = "Trade Agreement Signed", description = "New trade corridors opened with Ghana.", turnOccurred = 1 });
                player.diplomacyEvents.Add(new DiplomaticEvent { eventName = "Relations Improved", description = "Diplomatic mission to Niger successful.", turnOccurred = 1 });
                player.diplomacyEvents.Add(new DiplomaticEvent { eventName = "UN Vote Scheduled", description = "Resolution on regional security pending.", turnOccurred = 2 });
                player.diplomacyEvents.Add(new DiplomaticEvent { eventName = "Border Dispute", description = "Tensions rising along the Cameroon border.", turnOccurred = 2 });
            }

            if (player.embassies.Count == 0 && GameManager.Instance.allCountries != null)
            {
                foreach (var c in GameManager.Instance.allCountries)
                {
                    if (c == player) continue;
                    player.embassies.Add(new Embassy { 
                        countryName = c.countryName, 
                        isOperational = Random.value > 0.15f, 
                        staffCount = Random.Range(10, 50), 
                        influencePercentage = Random.Range(30f, 95f),
                        economicReportQuality = new[] {"Poor", "Average", "High", "Excellent"}[Random.Range(0,4)],
                        politicalReportQuality = new[] {"Poor", "Average", "High", "Excellent"}[Random.Range(0,4)]
                    });
                }
            }

            if (player.internationalOrgs.Count == 0)
            {
                player.internationalOrgs.Add(new InternationalOrganization { orgName = "United Nations", membershipStatus = "Member", votingPower = "Low", currentResolution = "Global Sanctions", voteStatus = "Abstain" });
                player.internationalOrgs.Add(new InternationalOrganization { orgName = "African Union", membershipStatus = "Member", votingPower = "Medium", currentResolution = "Regional Security", voteStatus = "Approve" });
                player.internationalOrgs.Add(new InternationalOrganization { orgName = "ECOWAS", membershipStatus = "Member", votingPower = "High", currentResolution = "Trade Integration", voteStatus = "Approve" });
                player.internationalOrgs.Add(new InternationalOrganization { orgName = "World Bank", membershipStatus = "Observer", votingPower = "None", currentResolution = "Infrastructure Loans", voteStatus = "Pending" });
            }
        }

        private void PopulateOverviewTab(CountryDataSO player)
        {
            if (_dipEventsList != null)
            {
                _dipEventsList.Clear();
                foreach (var ev in player.diplomacyEvents)
                {
                    var row = new Label($"• {ev.description}") { style = { color = Color.white, fontSize = 11, marginBottom = 8 } };
                    _dipEventsList.Add(row);
                }
            }

            if (_overviewTreatiesList != null)
            {
                _overviewTreatiesList.Clear();
                foreach (var t in player.activeTreaties.Take(3))
                    _overviewTreatiesList.Add(new Label($"• {t.treatyName} ({t.partnerCountry})") { style = { color = Color.white, fontSize = 11, marginBottom = 4 } });
            }

            if (_overviewOrgsList != null)
            {
                _overviewOrgsList.Clear();
                foreach (var org in player.internationalOrgs)
                {
                    var pill = new Label(org.orgName) { style = { backgroundColor = new Color(0.2f, 0.4f, 0.8f, 0.5f), paddingBottom = 4, paddingTop = 4, paddingLeft = 8, paddingRight = 8, borderTopLeftRadius = 8, borderTopRightRadius = 8, borderBottomLeftRadius = 8, borderBottomRightRadius = 8, marginRight = 6, marginBottom = 6, color = Color.white, fontSize = 10 } };
                    _overviewOrgsList.Add(pill);
                }
            }
        }

        private void PopulateBilateralTab(CountryDataSO player, GameManager gm)
        {
            if (_bilateralCardsGrid == null) return;
            _bilateralCardsGrid.Clear();

            var others = gm.allCountries?.Where(c => c != player).ToList() ?? new List<CountryDataSO>();
            foreach (var other in others)
            {
                float relation = DashboardLiveContent.ComputeRelation(player, other, gm);
                var card = new VisualElement() { style = { width = Length.Percent(48), backgroundColor = new Color(0.05f, 0.07f, 0.1f, 0.95f), borderTopColor = new Color(1,1,1,0.1f), borderBottomColor = new Color(1,1,1,0.1f), borderLeftColor = new Color(1,1,1,0.1f), borderRightColor = new Color(1,1,1,0.1f), borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopLeftRadius = 6, borderTopRightRadius = 6, borderBottomLeftRadius = 6, borderBottomRightRadius = 6, paddingLeft = 12, paddingRight = 12, paddingTop = 12, paddingBottom = 12, marginBottom = 12, marginRight = 12 } };
                
                var title = new Label($"{player.countryName} — {other.countryName}") { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 12, marginBottom = 8 } };
                card.Add(title);

                var relRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 8 } };
                relRow.Add(new Label("Relations") { style = { color = Color.grey, fontSize = 10 } });
                
                var pbWrap = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                pbWrap.Add(new ProgressBar() { value = relation, highValue = 100, style = { width = 60, height = 4, marginRight = 8 } });
                pbWrap.Add(new Label($"{relation:F0} ({DashboardLiveContent.RelationLabel(relation)})") { style = { color = DashboardLiveContent.RelationColor(relation), fontSize = 10 } });
                relRow.Add(pbWrap);
                card.Add(relRow);

                card.Add(CreateBilateralStat("Trade", "High"));
                card.Add(CreateBilateralStat("Military", "Neutral"));
                card.Add(CreateBilateralStat("Embassy", "Open"));

                var btn = new Button() { text = "OPEN DIPLOMACY" };
                btn.AddToClassList("button-premium");
                btn.style.marginTop = 10;
                btn.style.height = 24;
                card.Add(btn);

                _bilateralCardsGrid.Add(card);
            }
        }

        private VisualElement CreateBilateralStat(string label, string val)
        {
            var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 4 } };
            row.Add(new Label(label) { style = { color = Color.grey, fontSize = 10 } });
            row.Add(new Label(val) { style = { color = Color.white, fontSize = 10 } });
            return row;
        }

        private void PopulateTreatiesTab(CountryDataSO player, GameManager gm)
        {
            if (_treatiesCardsList == null) return;
            _treatiesCardsList.Clear();

            foreach (var t in player.activeTreaties)
            {
                var card = new VisualElement() { style = { backgroundColor = new Color(0.05f, 0.07f, 0.1f, 0.95f), borderTopColor = new Color(1,1,1,0.1f), borderBottomColor = new Color(1,1,1,0.1f), borderLeftColor = new Color(1,1,1,0.1f), borderRightColor = new Color(1,1,1,0.1f), borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopLeftRadius = 6, borderTopRightRadius = 6, borderBottomLeftRadius = 6, borderBottomRightRadius = 6, paddingLeft = 15, paddingRight = 15, paddingTop = 15, paddingBottom = 15, marginBottom = 12 } };
                card.Add(new Label(t.treatyName) { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 14, marginBottom = 4 } });
                
                var statusRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, marginBottom = 8 } };
                statusRow.Add(new Label("ACTIVE") { style = { color = new Color(0, 0.9f, 0.46f), unityFontStyleAndWeight = FontStyle.Bold, fontSize = 10, marginRight = 10 } });
                statusRow.Add(new Label($"Partner: {t.partnerCountry}") { style = { color = Color.grey, fontSize = 10 } });
                card.Add(statusRow);
                
                var line = new VisualElement() { style = { height = 1, backgroundColor = new Color(1,1,1,0.1f), marginBottom = 8 } };
                card.Add(line);
                
                card.Add(new Label(t.terms) { style = { color = Color.white, fontSize = 11 } });
                _treatiesCardsList.Add(card);
            }
        }

        private void PopulateOrganizationsTab(CountryDataSO player)
        {
            if (_organizationsCardsGrid == null) return;
            _organizationsCardsGrid.Clear();

            foreach (var org in player.internationalOrgs)
            {
                var card = new VisualElement() { style = { width = Length.Percent(48), backgroundColor = new Color(0.05f, 0.07f, 0.1f, 0.95f), borderTopColor = new Color(1,1,1,0.1f), borderBottomColor = new Color(1,1,1,0.1f), borderLeftColor = new Color(1,1,1,0.1f), borderRightColor = new Color(1,1,1,0.1f), borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopLeftRadius = 6, borderTopRightRadius = 6, borderBottomLeftRadius = 6, borderBottomRightRadius = 6, paddingLeft = 12, paddingRight = 12, paddingTop = 12, paddingBottom = 12, marginBottom = 12, marginRight = 12 } };
                card.Add(new Label(org.orgName) { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 14, marginBottom = 8 } });
                
                card.Add(CreateBilateralStat("Status", org.membershipStatus));
                card.Add(CreateBilateralStat("Voting Power", org.votingPower));
                
                var line = new VisualElement() { style = { height = 1, backgroundColor = new Color(1,1,1,0.1f), marginTop = 8, marginBottom = 8 } };
                card.Add(line);

                card.Add(new Label("Current Resolution") { style = { color = Color.grey, fontSize = 9, marginBottom = 2 } });
                card.Add(new Label(org.currentResolution) { style = { color = Color.white, fontSize = 11, marginBottom = 8 } });

                var btn = new Button() { text = $"VOTE: {org.voteStatus.ToUpper()}" };
                btn.AddToClassList("button-premium");
                btn.style.height = 24;
                if (org.voteStatus == "Approve") btn.style.backgroundColor = new Color(0, 0.5f, 0.2f);
                card.Add(btn);

                _organizationsCardsGrid.Add(card);
            }
        }

        private void PopulateEmbassiesTab(CountryDataSO player)
        {
            if (_embTotal != null) _embTotal.text = player.embassies.Count.ToString();
            if (_embOperational != null) _embOperational.text = player.embassies.Count(e => e.isOperational).ToString();
            if (_embClosed != null) _embClosed.text = player.embassies.Count(e => !e.isOperational).ToString();
            if (_embBudget != null) _embBudget.text = $"₦{player.embassyBudget:F1}B";

            if (_embassiesCardsGrid == null) return;
            _embassiesCardsGrid.Clear();

            foreach (var emb in player.embassies)
            {
                var card = new VisualElement() { style = { width = Length.Percent(48), backgroundColor = new Color(0.05f, 0.07f, 0.1f, 0.95f), borderTopColor = new Color(1,1,1,0.1f), borderBottomColor = new Color(1,1,1,0.1f), borderLeftColor = new Color(1,1,1,0.1f), borderRightColor = new Color(1,1,1,0.1f), borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopLeftRadius = 6, borderTopRightRadius = 6, borderBottomLeftRadius = 6, borderBottomRightRadius = 6, paddingLeft = 12, paddingRight = 12, paddingTop = 12, paddingBottom = 12, marginBottom = 12, marginRight = 12 } };
                
                var header = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 8 } };
                header.Add(new Label($"Embassy in {emb.countryName}") { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 12 } });
                header.Add(new Label(emb.isOperational ? "OPEN" : "CLOSED") { style = { color = emb.isOperational ? new Color(0, 0.9f, 0.46f) : Color.red, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
                card.Add(header);

                card.Add(CreateBilateralStat("Staff", emb.staffCount.ToString()));
                card.Add(CreateBilateralStat("Influence", $"{emb.influencePercentage:F0}%"));
                card.Add(CreateBilateralStat("Economic Reports", emb.economicReportQuality));
                card.Add(CreateBilateralStat("Political Reports", emb.politicalReportQuality));

                var actions = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginTop = 10 } };
                var btnUp = new Button() { text = "UPGRADE" };
                btnUp.AddToClassList("button-premium");
                btnUp.style.width = Length.Percent(48);
                btnUp.style.height = 24;

                var btnRecall = new Button() { text = "RECALL" };
                btnRecall.AddToClassList("button-premium");
                btnRecall.style.width = Length.Percent(48);
                btnRecall.style.height = 24;
                btnRecall.style.backgroundColor = new Color(0.5f, 0.1f, 0.1f);

                actions.Add(btnUp);
                actions.Add(btnRecall);
                card.Add(actions);

                _embassiesCardsGrid.Add(card);
            }
        }
    }
}
