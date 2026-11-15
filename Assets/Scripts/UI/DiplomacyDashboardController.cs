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

        private Label _headerAllies, _headerRivals, _headerNeutral, _headerInfluence, _headerPrestige;

        private ScrollView _dipEventsList;
        private VisualElement _overviewTreatiesList, _overviewOrgsList;
        private Label _globalRepValue, _globalRepDesc;
        private Label _pendingVotesBadge;

        private VisualElement _bilateralCardsGrid;
        private VisualElement _treatiesCardsList;
        private VisualElement _organizationsCardsGrid;

        private Label _embTotal, _embOperational, _embClosed, _embBudget;
        private VisualElement _embassiesCardsGrid;

        private enum BilateralFilter { Actionable, Allies, Rivals, Neutral, All }
        private BilateralFilter _currentBilateralFilter = BilateralFilter.All;
        private Button _btnFilterActive, _btnFilterAllies, _btnFilterRivals, _btnFilterNeutral, _btnFilterAll;

        private VisualElement _panelBilateral;
        private VisualElement _panelBilateralDetail;
        private Label _detailCountryTitle, _detailRelationsScore, _detailRelationsLabel, _detailWarStage;
        private ProgressBar _detailRelationsBar;
        private VisualElement _detailStatsList, _detailTreatiesList, _detailActionsList;

        private TradeAgreementController _tradeController;

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
            if (GameManager.Instance?.TurnSystem != null)
                GameManager.Instance.TurnSystem.OnTurnEnded += RefreshDiplomacyData;
            RefreshDiplomacyData();
        }

        private void OnDisable()
        {
            if (GameManager.Instance?.TurnSystem != null)
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
                root.Q<Button>("TabEmbassies"),
                root.Q<Button>("TabTrade")
            };

            _panels = new VisualElement[]
            {
                root.Q<VisualElement>("PanelOverview"),
                root.Q<VisualElement>("PanelBilateral"),
                root.Q<VisualElement>("PanelTreaties"),
                root.Q<VisualElement>("PanelOrganizations"),
                root.Q<VisualElement>("PanelEmbassies"),
                root.Q<VisualElement>("PanelTrade")
            };

            for (int i = 0; i < _tabs.Length; i++)
            {
                int idx = i;
                if (_tabs[i] != null) _tabs[i].clicked += () => SwitchTab(idx);
            }

            root.Q<Button>("CloseDiplomacy")?.RegisterCallback<ClickEvent>(_ => Hide());

            _headerPrestige  = root.Q<Label>("GlobalPrestigeScore");
            _headerAllies    = root.Q<Label>("HeaderAlliesCount");
            _headerRivals    = root.Q<Label>("HeaderRivalsCount");
            _headerNeutral   = root.Q<Label>("HeaderNeutralCount");
            _headerInfluence = root.Q<Label>("HeaderInfluenceVal");

            _dipEventsList        = root.Q<ScrollView>("DipEventsList");
            _overviewTreatiesList = root.Q<VisualElement>("OverviewTreatiesList");
            _overviewOrgsList     = root.Q<VisualElement>("OverviewOrgsList");
            _globalRepValue       = root.Q<Label>("GlobalRepValue");
            _globalRepDesc        = root.Q<Label>("GlobalRepDesc");

            _bilateralCardsGrid     = root.Q<VisualElement>("BilateralCardsGrid");
            _treatiesCardsList      = root.Q<VisualElement>("TreatiesCardsList");
            _organizationsCardsGrid = root.Q<VisualElement>("OrganizationsCardsGrid");
            _embassiesCardsGrid     = root.Q<VisualElement>("EmbassiesCardsGrid");

            _embTotal       = root.Q<Label>("EmbTotal");
            _embOperational = root.Q<Label>("EmbOperational");
            _embClosed      = root.Q<Label>("EmbClosed");
            _embBudget      = root.Q<Label>("EmbBudget");

            _btnFilterActive  = root.Q<Button>("BtnFilterActive");
            _btnFilterAll     = root.Q<Button>("BtnFilterAll");
            _btnFilterAllies  = root.Q<Button>("BtnFilterAllies");
            _btnFilterRivals  = root.Q<Button>("BtnFilterRivals");
            _btnFilterNeutral = root.Q<Button>("BtnFilterNeutral");

            if (_btnFilterActive  != null) _btnFilterActive.clicked  += () => SetBilateralFilter(BilateralFilter.Actionable);
            if (_btnFilterAllies  != null) _btnFilterAllies.clicked  += () => SetBilateralFilter(BilateralFilter.Allies);
            if (_btnFilterRivals  != null) _btnFilterRivals.clicked  += () => SetBilateralFilter(BilateralFilter.Rivals);
            if (_btnFilterNeutral != null) _btnFilterNeutral.clicked += () => SetBilateralFilter(BilateralFilter.Neutral);
            if (_btnFilterAll     != null) _btnFilterAll.clicked     += () => SetBilateralFilter(BilateralFilter.All);

            _panelBilateral       = root.Q<VisualElement>("PanelBilateral");
            _panelBilateralDetail = root.Q<VisualElement>("PanelBilateralDetail");
            _detailCountryTitle   = root.Q<Label>("DetailCountryTitle");
            _detailRelationsScore = root.Q<Label>("DetailRelationsScore");
            _detailRelationsBar   = root.Q<ProgressBar>("DetailRelationsBar");
            _detailRelationsLabel = root.Q<Label>("DetailRelationsLabel");
            _detailWarStage       = root.Q<Label>("DetailWarStage");
            _detailStatsList      = root.Q<VisualElement>("DetailStatsList");
            _detailTreatiesList   = root.Q<VisualElement>("DetailTreatiesList");
            _detailActionsList    = root.Q<VisualElement>("DetailActionsList");

            root.Q<Button>("BtnBackToCountries")?.RegisterCallback<ClickEvent>(_ => ShowBilateralGrid());

root.Q<Button>("BtnOpenTrade")?.RegisterCallback<ClickEvent>(_ =>
            {
                SwitchTab(5);
                if (_tradeController == null) _tradeController = UnityEngine.Object.FindObjectOfType<TradeAgreementController>();
                _tradeController?.Show();
            });

            root.Q<Button>("BtnAddTrade")?.RegisterCallback<ClickEvent>(_ =>
            {
                SwitchTab(5);
                if (_tradeController == null) _tradeController = UnityEngine.Object.FindObjectOfType<TradeAgreementController>();
                _tradeController?.Show();
            });

            root.Q<Button>("BtnOfferTrade")?.RegisterCallback<ClickEvent>(_ =>
            {
                SwitchTab(5);
                if (_tradeController == null) _tradeController = UnityEngine.Object.FindObjectOfType<TradeAgreementController>();
                if (_detailCountryTitle != null && !string.IsNullOrEmpty(_detailCountryTitle.text))
                {
                    _tradeController?.Show();
                }
                else
                {
                    _tradeController?.Show();
                }
            });
        }

        public void SwitchTabByName(string name)
        {
            if (name == "TabOverview") SwitchTab(0);
            else if (name == "TabBilateral") SwitchTab(1);
            else if (name == "TabTreaties") SwitchTab(2);
            else if (name == "TabOrganizations") SwitchTab(3);
            else if (name == "TabEmbassies") SwitchTab(4);
            else if (name == "TabTrade") SwitchTab(5);
        }

        private void SwitchTab(int index)
        {
            if (_panelBilateralDetail != null)
                _panelBilateralDetail.style.display = DisplayStyle.None;

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
                _tabs[i].EnableInClassList("active-tab", i == index);
            }

            if (index == 5)
            {
                if (_tradeController == null) _tradeController = UnityEngine.Object.FindObjectOfType<TradeAgreementController>();
                _tradeController?.Show();
            }
        }

        private void SetBilateralFilter(BilateralFilter filter)
        {
            _currentBilateralFilter = filter;
            if (_btnFilterActive  != null) _btnFilterActive.EnableInClassList("active-tab",  filter == BilateralFilter.Actionable);
            if (_btnFilterAllies  != null) _btnFilterAllies.EnableInClassList("active-tab",  filter == BilateralFilter.Allies);
            if (_btnFilterRivals  != null) _btnFilterRivals.EnableInClassList("active-tab",  filter == BilateralFilter.Rivals);
            if (_btnFilterNeutral != null) _btnFilterNeutral.EnableInClassList("active-tab", filter == BilateralFilter.Neutral);
            if (_btnFilterAll     != null) _btnFilterAll.EnableInClassList("active-tab",     filter == BilateralFilter.All);

            var gm = GameManager.Instance;
            if (gm?.playerCountry != null) PopulateBilateralTab(gm.playerCountry, gm);
        }

        public void Show()
        {
            if (_dashboardRoot == null || _dashboardRoot.panel == null)
            {
                _dashboardRoot = MainDashboardToolkitController.Instance?.GetPanelVisualElement("Diplomacy")
                              ?? _root?.Q<VisualElement>("DiplomacyDashboard");
                if (_dashboardRoot != null) _root = _dashboardRoot;
            }

            if (_dashboardRoot == null) return;
            _dashboardRoot.style.display = DisplayStyle.Flex;
            UIStateManager.Instance?.ApplySafeTransition(_dashboardRoot, true, 0.4f);
            RefreshDiplomacyData();
            UIStateManager.FixScrollViews(_dashboardRoot);
        }

        public void Hide()
        {
            MainDashboardToolkitController.Instance?.HideAllPanels();
        }

        public void RefreshDiplomacyData()
        {
            if (_dashboardRoot == null || _dashboardRoot.panel == null)
            {
                _dashboardRoot = MainDashboardToolkitController.Instance?.GetPanelVisualElement("Diplomacy")
                              ?? _root?.Q<VisualElement>("DiplomacyDashboard")
                              ?? _dashboardRoot;
                if (_dashboardRoot != null) _root = _dashboardRoot;
            }

            var gm = GameManager.Instance;
            if (gm?.playerCountry == null) return;
            var player = gm.playerCountry;

            SeedMockDataIfEmpty(player);

            if (_headerPrestige  != null) _headerPrestige.text  = $"PRESTIGE: {player.globalPrestige:F0}/100";

            int allies  = player.activeTreaties.Count(t => t.treatyName == "Mutual Defence Pact");
            int rivals  = 2;
            int neutral = Mathf.Max(0, (gm.allCountries?.Count ?? 50) - allies - rivals - 1);
            if (_headerAllies    != null) _headerAllies.text    = allies.ToString();
            if (_headerRivals    != null) _headerRivals.text    = rivals.ToString();
            if (_headerNeutral   != null) _headerNeutral.text   = neutral.ToString();
            if (_headerInfluence != null) _headerInfluence.text = $"{player.diplomacyInfluence:F0}%";

            PopulateOverviewTab(player);
            PopulateBilateralTab(player, gm);
            PopulateTreatiesTab(player, gm);
            PopulateTradeTab(player, gm);
            PopulateOrganizationsTab(player);
            PopulateEmbassiesTab(player);
        }

private void PopulateTradeTab(CountryDataSO player, GameManager gm)
        {
            var list = _dashboardRoot?.Q<ScrollView>("DashboardActiveAgreementsList");
            if (list == null) return;
            list.Clear();

            if (player.activeTradeAgreements == null || player.activeTradeAgreements.Count == 0)
            {
                list.Add(new Label("No active trade agreements.") { style = { color = Color.grey, fontSize = 11, unityFontStyleAndWeight = FontStyle.Italic, marginTop = 10, marginLeft = 10 } });
                return;
            }

            foreach (var t in player.activeTradeAgreements)
            {
                var card = new VisualElement { style = { backgroundColor = new Color(0.1f, 0.14f, 0.2f), paddingLeft = 12, paddingRight = 12, paddingTop = 12, paddingBottom = 12, marginBottom = 8, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, borderLeftWidth = 3, borderLeftColor = new Color(1f, 0.6f, 0f) } };
                card.Add(new Label($"{t.partnerCountry} — {t.resource}") { style = { color = Color.white, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 } });
                string directionStr = t.isSeller ? "EXPORT" : "IMPORT";
                card.Add(new Label($"Direction: {directionStr} | Quantity: {t.resourceQuantity:F0} ({t.durationMonths} turns left)") { style = { color = Color.grey, fontSize = 10, marginBottom = 4 } });
                float totalValue = (t.monthlyPaymentUSD * t.durationMonths) / 1000f;
                card.Add(new Label($"Strategic Value: ${totalValue:F2}B") { style = { color = new Color(0.14f, 0.93f, 0.34f), fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
                list.Add(card);
            }
        }

        private void SeedMockDataIfEmpty(CountryDataSO player)
        {
            if (player.diplomacyEvents.Count == 0)
            {
                player.diplomacyEvents.Add(new DiplomaticEvent { eventName = "Trade Agreement Signed",  description = "New trade corridors opened with Ghana.",      turnOccurred = 1 });
                player.diplomacyEvents.Add(new DiplomaticEvent { eventName = "Relations Improved",      description = "Diplomatic mission to Niger successful.",      turnOccurred = 1 });
                player.diplomacyEvents.Add(new DiplomaticEvent { eventName = "UN Vote Scheduled",       description = "Resolution on regional security pending.",     turnOccurred = 2 });
                player.diplomacyEvents.Add(new DiplomaticEvent { eventName = "Border Dispute",          description = "Tensions rising along the Cameroon border.",   turnOccurred = 2 });
            }

            if (player.embassies.Count == 0 && GameManager.Instance?.allCountries != null)
            {
                foreach (var c in GameManager.Instance.allCountries)
                {
                    if (c == player) continue;
                    player.embassies.Add(new Embassy
                    {
                        countryName            = c.countryName,
                        isOperational          = Random.value > 0.15f,
                        staffCount             = Random.Range(10, 50),
                        influencePercentage    = Random.Range(30f, 95f),
                        economicReportQuality  = new[] { "Poor", "Average", "High", "Excellent" }[Random.Range(0, 4)],
                        politicalReportQuality = new[] { "Poor", "Average", "High", "Excellent" }[Random.Range(0, 4)]
                    });
                }
            }

            if (player.internationalOrgs.Count == 0)
            {
                player.internationalOrgs.Add(new InternationalOrganization { orgName = "United Nations", membershipStatus = "Member",   votingPower = "Low",    currentResolution = "Global Sanctions",     voteStatus = "Pending" });
                player.internationalOrgs.Add(new InternationalOrganization { orgName = "African Union",  membershipStatus = "Member",   votingPower = "Medium", currentResolution = "Regional Security",    voteStatus = "Pending" });
                player.internationalOrgs.Add(new InternationalOrganization { orgName = "ECOWAS",         membershipStatus = "Member",   votingPower = "High",   currentResolution = "Trade Integration",    voteStatus = "Approve" });
                player.internationalOrgs.Add(new InternationalOrganization { orgName = "World Bank",     membershipStatus = "Observer", votingPower = "None",   currentResolution = "Infrastructure Loans", voteStatus = "Abstain" });
            }
        }

        private void PopulateOverviewTab(CountryDataSO player)
        {
            if (_dipEventsList != null)
            {
                _dipEventsList.Clear();
                foreach (var ev in player.diplomacyEvents)
                    _dipEventsList.Add(new Label($"• {ev.description}") { style = { color = Color.white, fontSize = 11, marginBottom = 8 } });
            }

            if (_overviewTreatiesList != null)
            {
                _overviewTreatiesList.Clear();
                foreach (var t in player.activeTreaties.Take(3))
                    _overviewTreatiesList.Add(new Label($"• {t.treatyName} ({t.partnerCountry})") { style = { color = Color.white, fontSize = 11, marginBottom = 4 } });
                if (player.activeTreaties.Count == 0)
                    _overviewTreatiesList.Add(new Label("No active treaties.") { style = { color = Color.grey, fontSize = 10, unityFontStyleAndWeight = FontStyle.Italic } });
            }

            int pendingVotes = player.internationalOrgs.Count(o => o.voteStatus == "Pending");
            if (_overviewOrgsList != null)
            {
                _overviewOrgsList.Clear();

                if (pendingVotes > 0)
                {
                    var alertPillFix = new Label($"⚠ {pendingVotes} VOTE{(pendingVotes > 1 ? "S" : "")} PENDING");
                    alertPillFix.style.backgroundColor = new Color(0.8f, 0.5f, 0f, 0.35f);
                    alertPillFix.style.color = new Color(1f, 0.75f, 0.3f);
                    alertPillFix.style.paddingBottom = 4; alertPillFix.style.paddingTop = 4;
                    alertPillFix.style.paddingLeft = 10; alertPillFix.style.paddingRight = 10;
                    alertPillFix.style.borderTopLeftRadius = 8; alertPillFix.style.borderTopRightRadius = 8;
                    alertPillFix.style.borderBottomLeftRadius = 8; alertPillFix.style.borderBottomRightRadius = 8;
                    alertPillFix.style.marginRight = 6; alertPillFix.style.marginBottom = 6;
                    alertPillFix.style.fontSize = 10;
                    alertPillFix.style.unityFontStyleAndWeight = FontStyle.Bold;
                    _overviewOrgsList.Add(alertPillFix);
                }

                foreach (var org in player.internationalOrgs)
                {
                    var pill = new Label(org.orgName);
                    bool isPending = org.voteStatus == "Pending";
                    pill.style.backgroundColor = isPending
                        ? new Color(0.6f, 0.4f, 0f, 0.4f)
                        : new Color(0.2f, 0.4f, 0.8f, 0.5f);
                    pill.style.color = Color.white;
                    pill.style.paddingBottom = 4; pill.style.paddingTop = 4;
                    pill.style.paddingLeft = 8; pill.style.paddingRight = 8;
                    pill.style.borderTopLeftRadius = 8; pill.style.borderTopRightRadius = 8;
                    pill.style.borderBottomLeftRadius = 8; pill.style.borderBottomRightRadius = 8;
                    pill.style.marginRight = 6; pill.style.marginBottom = 6;
                    pill.style.fontSize = 10;
                    if (isPending)
                    {
                        pill.text = $"{org.orgName} ●";
                        pill.style.color = new Color(1f, 0.85f, 0.5f);
                    }
                    _overviewOrgsList.Add(pill);
                }
            }
        }

        private void ShowBilateralGrid()
        {
            if (_panelBilateralDetail != null) _panelBilateralDetail.style.display = DisplayStyle.None;
            if (_panelBilateral       != null) _panelBilateral.style.display       = DisplayStyle.Flex;
        }

        private void PopulateBilateralTab(CountryDataSO player, GameManager gm)
        {
            if (_bilateralCardsGrid == null) return;
            _bilateralCardsGrid.Clear();

            var others = gm.allCountries?.Where(c => c != player).ToList() ?? new List<CountryDataSO>();
            var scored = others
                .Select(o => new { Country = o, Score = DashboardLiveContent.ComputeRelation(player, o, gm) })
                .ToList();

            var filtered = scored.AsEnumerable();
            switch (_currentBilateralFilter)
            {
                case BilateralFilter.Actionable:
                    filtered = scored.Where(r => r.Score >= 70 || r.Score <= 30);
                    break;
                case BilateralFilter.Allies:
                    filtered = scored.Where(r => r.Score >= 70);
                    break;
                case BilateralFilter.Rivals:
                    filtered = scored.Where(r => r.Score <= 30);
                    break;
                case BilateralFilter.Neutral:
                    filtered = scored.Where(r => r.Score > 30 && r.Score < 70);
                    break;
                case BilateralFilter.All:
                    filtered = scored;
                    break;
            }

            var sorted = (_currentBilateralFilter == BilateralFilter.Actionable || _currentBilateralFilter == BilateralFilter.All)
                ? filtered.OrderByDescending(r => Mathf.Abs(r.Score - 50f)).ThenByDescending(r => r.Score).ToList()
                : filtered.OrderByDescending(r => r.Score).ToList();

            if (!sorted.Any())
            {
                _bilateralCardsGrid.Add(new Label("No countries match this filter.") { style = { color = Color.grey, fontSize = 11, unityFontStyleAndWeight = FontStyle.Italic, marginTop = 20 } });
                return;
            }

            foreach (var item in sorted)
            {
                CountryDataSO other = item.Country;
                float relation      = item.Score;
                bool isAtWar        = gm.WarSystem?.GetWarStage(other) == Interfaces.WarStage.Conflict;

                var card = MakeCard(100, 12);
                if (isAtWar)
                {
                    card.style.borderLeftWidth = 3;
                    card.style.borderLeftColor = new Color(1f, 0.32f, 0.32f);
                }

                card.Add(new Label($"{other.countryName}") { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 12, marginBottom = 6 } });

                var relRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 8 } };
                var pb = new ProgressBar { value = relation, highValue = 100 };
                pb.style.flexGrow = 1;
                pb.style.height   = 5;
                pb.style.marginRight = 8;
                relRow.Add(pb);
                relRow.Add(new Label($"{relation:F0}") { style = { color = DashboardLiveContent.RelationColor(relation), fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold, marginRight = 4 } });
                relRow.Add(new Label(isAtWar ? "⚔ WAR" : DashboardLiveContent.RelationLabel(relation)) { style = { color = isAtWar ? new Color(1f, 0.32f, 0.32f) : DashboardLiveContent.RelationColor(relation), fontSize = 9 } });
                card.Add(relRow);

                CountryDataSO captureOther = other;
                float captureRelation = relation;

                var btn = new Button(() => OpenBilateralDetail(player, captureOther, captureRelation, gm)) { text = "OPEN DIPLOMACY" };
                btn.AddToClassList("econ-small-button");
                btn.style.height  = 22;
                btn.style.fontSize = 9;
                btn.style.marginTop = 4;
                card.Add(btn);

                _bilateralCardsGrid.Add(card);
            }
        }

        private void OpenBilateralDetail(CountryDataSO player, CountryDataSO target, float relation, GameManager gm)
        {
            if (_panelBilateral       != null) _panelBilateral.style.display       = DisplayStyle.None;
            if (_panelBilateralDetail != null) _panelBilateralDetail.style.display = DisplayStyle.Flex;

            if (_detailCountryTitle   != null) _detailCountryTitle.text   = target.countryName.ToUpper();
            if (_detailRelationsScore != null) _detailRelationsScore.text = $"Relations: {relation:F0}/100";
            if (_detailRelationsBar   != null) _detailRelationsBar.value  = relation;
            if (_detailRelationsLabel != null)
            {
                _detailRelationsLabel.text       = DashboardLiveContent.RelationLabel(relation);
                _detailRelationsLabel.style.color = DashboardLiveContent.RelationColor(relation);
            }

            if (_detailWarStage != null && gm?.WarSystem != null)
            {
                var stage = gm.WarSystem.GetWarStage(target);
                switch (stage)
                {
                    case Interfaces.WarStage.Conflict:
                        _detailWarStage.text = "⚔ AT WAR";
                        _detailWarStage.style.color = new Color(1f, 0.32f, 0.32f); break;
                    case Interfaces.WarStage.Crisis:
                        _detailWarStage.text = "⚠ CRISIS";
                        _detailWarStage.style.color = new Color(1f, 0.67f, 0.25f); break;
                    case Interfaces.WarStage.Tension:
                        _detailWarStage.text = "TENSION";
                        _detailWarStage.style.color = Color.yellow; break;
                    default:
                        _detailWarStage.text = "PEACE";
                        _detailWarStage.style.color = new Color(0.14f, 0.93f, 0.34f); break;
                }
            }

            if (_detailStatsList != null)
            {
                _detailStatsList.Clear();
                AddDetailStat("GDP",           $"${target.gdp:F1}B");
                AddDetailStat("Stability",     $"{target.stability:F0}%");
                AddDetailStat("Military",      $"{(target.unitInfantry + target.unitTanks):N0} units");
                AddDetailStat("Prestige",      $"{target.globalPrestige:F0}/100");
                if (target.hasNuclearWeapon)
                    AddDetailStat("NUCLEAR ARMED", "⚛ Confirmed", new Color(1f, 0.67f, 0.25f));
            }

            if (_detailTreatiesList != null)
            {
                _detailTreatiesList.Clear();
                if (target.globalFaction != "Independent")
                {
                    _detailTreatiesList.Add(new Label($"🛡 Member of: {target.globalFaction}") { style = { color = new Color(0.14f, 0.93f, 0.34f), fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 } });
                    if (player.globalFaction == target.globalFaction)
                        _detailTreatiesList.Add(new Label($"✓ Free Trade Active\n✓ Mutual Defence Active") { style = { color = new Color(0.14f, 0.93f, 0.34f), fontSize = 10, marginBottom = 4 } });
                }
                else
                {
                    _detailTreatiesList.Add(new Label("No active faction alignment.") { style = { color = Color.grey, fontSize = 10, unityFontStyleAndWeight = FontStyle.Italic } });
                }
            }

            if (_detailActionsList != null)
            {
                _detailActionsList.Clear();

                if (player.globalFaction != target.globalFaction && target.globalFaction != "Independent" && target.globalFaction != "Non-Aligned")
                {
                    AddAction($"JOIN {target.globalFaction.ToUpper()}", new Color(0.18f, 0.6f, 0.95f), () =>
                    {
                        if (relation >= 30)
                        {
                            if (gm?.DiplomacySystem is StateCraft.Systems.DiplomacySystem dipSys)
                                dipSys.JoinFaction(player, target.globalFaction);
                            OpenBilateralDetail(player, target, DashboardLiveContent.ComputeRelation(player, target, gm), gm);
                        }
                        else
                        {
                            NotificationManager.Instance?.AddNotification($"Requires at least 30 Relations. Current: {relation:F0}", Color.red);
                        }
                    });
                }
                if (player.globalFaction != "Independent" && target.globalFaction == "Independent")
                {
                    AddAction($"INVITE TO {player.globalFaction.ToUpper()}", new Color(0.18f, 0.6f, 0.95f), () =>
                    {
                        if (relation >= 50)
                        {
                            if (gm?.DiplomacySystem is StateCraft.Systems.DiplomacySystem dipSys)
                                dipSys.JoinFaction(target, player.globalFaction);
                            OpenBilateralDetail(player, target, DashboardLiveContent.ComputeRelation(player, target, gm), gm);
                        }
                        else
                        {
                            NotificationManager.Instance?.AddNotification($"Requires at least 50 Relations. Current: {relation:F0}", Color.red);
                        }
                    });
                }

AddAction("TRADE RESOURCES", new Color(0.95f, 0.72f, 0.05f), () =>
                {
                    if (_tradeController == null) _tradeController = UnityEngine.Object.FindObjectOfType<TradeAgreementController>();
                    if (_tradeController != null) _tradeController.ShowWithPartner(target.countryName);
                    else NotificationManager.Instance?.AddNotification("TRADE DESK: Could not find TradeAgreementController in scene.", Color.red);
                });
                AddAction("OFFER AID PACKAGE", new Color(0.14f, 0.93f, 0.34f), () =>
                {
                    var g = GameManager.Instance;
                    if (g == null) return;
                    if (g.playerBudget < 0.1f) { NotificationManager.Instance?.AddNotification("INSUFFICIENT FUNDS: Need ≥$0.1B for aid.", Color.red); return; }
                    g.playerBudget -= 0.1f;
                    player.treasury = g.playerBudget;
                    g.Relations?.Adjust(player.countryName, target.countryName, 8f, "Aid package offered", g.TurnSystem?.CurrentTurn ?? 0);
                    NotificationManager.Instance?.AddNotification($"AID SENT: $0.1B to {target.countryName}. Relations +8.", Color.green);
                    OpenBilateralDetail(player, target, DashboardLiveContent.ComputeRelation(player, target, g), g);
                });

                AddAction("IMPOSE SANCTIONS",  new Color(1f, 0.67f, 0.25f), () =>
                {
                    var g = GameManager.Instance;
                    if (g == null) return;
                    g.Relations?.Adjust(player.countryName, target.countryName, -12f, "Sanctions imposed", g.TurnSystem?.CurrentTurn ?? 0);
                    NotificationManager.Instance?.AddNotification($"SANCTIONS: Restrictions imposed on {target.countryName}. Relations −12.", new Color(1f, 0.67f, 0.25f));
                    OpenBilateralDetail(player, target, DashboardLiveContent.ComputeRelation(player, target, g), g);
                });

                var sep = new VisualElement { style = { height = 1, backgroundColor = new Color(1, 1, 1, 0.08f), marginTop = 14, marginBottom = 14 } };
                _detailActionsList.Add(sep);

                bool atWar = gm?.WarSystem?.GetWarStage(target) == Interfaces.WarStage.Conflict;
                if (atWar)
                {
                    AddAction("PROPOSE CEASEFIRE",   new Color(0.4f, 0.8f, 1f), () =>
                    {
                        var g = GameManager.Instance;
                        if (g?.WarSystem is WarSystem ws) ws.ProposePeace(player, target);
                        if (g != null) OpenBilateralDetail(player, target, DashboardLiveContent.ComputeRelation(player, target, g), g);
                    });
                    AddAction("DEMAND CAPITULATION", new Color(1f, 0.32f, 0.32f), () =>
                    {
                        var g = GameManager.Instance;
                        if (g?.WarSystem is WarSystem ws) ws.ProposeReparationsPeace(player, target);
                        if (g != null) OpenBilateralDetail(player, target, DashboardLiveContent.ComputeRelation(player, target, g), g);
                    });
                }
                else
                {
                    var warBtn = new Button(() => ShowDeclareWarModal(player, target, relation, gm)) { text = "DECLARE WAR" };
                    warBtn.AddToClassList("econ-small-button");
                    warBtn.style.height = 36; warBtn.style.fontSize = 11; warBtn.style.marginTop = 4;
                    warBtn.style.color = new Color(1f, 0.32f, 0.32f);
                    warBtn.style.backgroundColor = new Color(0.4f, 0.05f, 0.05f, 0.5f);
                    warBtn.style.borderTopColor  = warBtn.style.borderBottomColor =
                    warBtn.style.borderLeftColor = warBtn.style.borderRightColor  = new Color(1f, 0.32f, 0.32f);
                    warBtn.style.borderTopWidth  = warBtn.style.borderBottomWidth =
                    warBtn.style.borderLeftWidth = warBtn.style.borderRightWidth  = 1;
                    warBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
                    _detailActionsList.Add(warBtn);

                    string hint = relation > 30
                        ? "⚠ Attacking a non-hostile nation will severely damage your prestige and approval."
                        : "Relations are already hostile — standard war costs apply.";
                    _detailActionsList.Add(new Label(hint) { style = { color = relation > 30 ? new Color(1f, 0.67f, 0.25f) : new Color(0.55f, 0.55f, 0.65f), fontSize = 9, whiteSpace = WhiteSpace.Normal, marginTop = 6 } });
                }
            }
        }

        private void ShowDeclareWarModal(CountryDataSO player, CountryDataSO target, float relation, GameManager gm)
        {
            bool isBetrayal = relation > 30;
            bool isNuclearBlocked = target.hasNuclearWeapon;
            var cRed    = new Color(1f, 0.32f, 0.32f);
            var cOrange = new Color(1f, 0.67f, 0.25f);

            var overlay = MakeOverlay();
            var modal   = new VisualElement
            {
                style =
                {
                    width = 500,
                    backgroundColor = new Color(0.07f, 0.04f, 0.04f, 1f),
                    borderTopLeftRadius = 10, borderTopRightRadius = 10,
                    borderBottomLeftRadius = 10, borderBottomRightRadius = 10,
                    paddingTop = 28, paddingBottom = 28, paddingLeft = 28, paddingRight = 28
                }
            };
            SetBorder(modal, cRed, 1);
            overlay.Add(modal);

            modal.Add(new Label("⚔  DECLARE WAR") { style = { color = cRed, fontSize = 20, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4, unityTextAlign = TextAnchor.MiddleCenter } });
            modal.Add(new Label($"vs.  {target.countryName.ToUpper()}") { style = { color = Color.white, fontSize = 14, marginBottom = 20, unityTextAlign = TextAnchor.MiddleCenter } });

            if (isNuclearBlocked)
            {
                var nukeBox = new VisualElement { style = { backgroundColor = new Color(0.4f, 0f, 0.4f, 0.5f), paddingTop = 14, paddingBottom = 14, paddingLeft = 14, paddingRight = 14, borderTopLeftRadius = 6, borderTopRightRadius = 6, borderBottomLeftRadius = 6, borderBottomRightRadius = 6, marginBottom = 16 } };
                nukeBox.Add(new Label("⚛  NUCLEAR DETERRENT — ACTION BLOCKED") { style = { color = new Color(0.85f, 0.4f, 1f), fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 6, unityTextAlign = TextAnchor.MiddleCenter } });
                nukeBox.Add(new Label($"{target.countryName}'s nuclear arsenal provides absolute deterrence. Invading would trigger a global nuclear crisis. This declaration cannot proceed.") { style = { color = new Color(0.85f, 0.75f, 0.9f), fontSize = 10, whiteSpace = WhiteSpace.Normal, unityTextAlign = TextAnchor.MiddleCenter } });
                modal.Add(nukeBox);
            }
            else
            {
                if (isBetrayal)
                {
                    var bannerBox = new VisualElement { style = { backgroundColor = new Color(0.5f, 0.2f, 0f, 0.6f), paddingTop = 10, paddingBottom = 10, paddingLeft = 12, paddingRight = 12, borderTopLeftRadius = 6, borderTopRightRadius = 6, borderBottomLeftRadius = 6, borderBottomRightRadius = 6, marginBottom = 16 } };
                    bannerBox.Add(new Label("⚠  ACT OF BETRAYAL") { style = { color = cOrange, fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 } });
                    bannerBox.Add(new Label($"Your relations with {target.countryName} stand at {relation:F0}/100. The international community will view this as unprovoked aggression.") { style = { color = new Color(1f, 0.85f, 0.6f), fontSize = 10, whiteSpace = WhiteSpace.Normal } });
                    modal.Add(bannerBox);
                }

                var costsBox = new VisualElement { style = { backgroundColor = new Color(0.1f, 0.06f, 0.06f, 0.8f), paddingTop = 12, paddingBottom = 12, paddingLeft = 14, paddingRight = 14, borderTopLeftRadius = 6, borderTopRightRadius = 6, borderBottomLeftRadius = 6, borderBottomRightRadius = 6, marginBottom = 18 } };
                costsBox.Add(new Label("EXPECTED CONSEQUENCES") { style = { color = new Color(0.6f, 0.6f, 0.7f), fontSize = 9, letterSpacing = 1, marginBottom = 8 } });
                AddCostRow(costsBox, "Mobilisation Cost",   "$0.5B deducted from treasury",         cRed);
                AddCostRow(costsBox, "GDP Impact",          "Aggressor −6%,  Defender −8%",          cRed);
                AddCostRow(costsBox, "Your Stability",      "−8 pts",                                cRed);
                AddCostRow(costsBox, "Prestige",            "−10 pts  (international outcry)",       cOrange);
                AddCostRow(costsBox, "World Tension",       "+30 pts",                               cOrange);
                AddCostRow(costsBox, "Relations",           "−40 pts with target",                   cRed);
                if (isBetrayal)
                    AddCostRow(costsBox, "Betrayal Penalty", "−5 prestige per allied nation of target", cOrange);
                modal.Add(costsBox);
            }

            var btnRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };

            var cancelBtn = new Button(() => overlay.parent?.Remove(overlay)) { text = "CANCEL" };
            cancelBtn.AddToClassList("econ-small-button");
            cancelBtn.style.width = Length.Percent(45); cancelBtn.style.height = 36; cancelBtn.style.fontSize = 11;
            btnRow.Add(cancelBtn);

            var confirmBtn = new Button { text = "CONFIRM — DECLARE WAR" };
            confirmBtn.AddToClassList("econ-small-button");
            confirmBtn.style.width = Length.Percent(50); confirmBtn.style.height = 36; confirmBtn.style.fontSize = 11;

            if (isNuclearBlocked)
            {
                confirmBtn.SetEnabled(false);
                confirmBtn.text = "⚛ BLOCKED — NUCLEAR DETERRENT";
                confirmBtn.style.opacity = 0.35f;
                confirmBtn.style.color = new Color(0.85f, 0.4f, 1f);
                confirmBtn.style.backgroundColor = new Color(0.15f, 0f, 0.2f, 0.5f);
                SetBorder(confirmBtn, new Color(0.6f, 0.3f, 0.8f), 1);
            }
            else
            {
                confirmBtn.style.color = cRed;
                confirmBtn.style.backgroundColor = new Color(0.5f, 0.05f, 0.05f, 0.9f);
                confirmBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
                SetBorder(confirmBtn, cRed, 1);
                confirmBtn.clicked += () =>
                {
                    overlay.parent?.Remove(overlay);
                    gm?.WarSystem?.DeclareWar(player, target);
                    var g2 = GameManager.Instance;
                    if (g2 != null) OpenBilateralDetail(player, target, DashboardLiveContent.ComputeRelation(player, target, g2), g2);
                };
            }
            btnRow.Add(confirmBtn);
            modal.Add(btnRow);

            _dashboardRoot?.Add(overlay);
        }

        private void ShowRecallConfirmModal(CountryDataSO player, Embassy emb)
        {
            var cRed  = new Color(1f, 0.32f, 0.32f);
            var overlay = MakeOverlay();
            var modal   = new VisualElement
            {
                style =
                {
                    width = 400,
                    backgroundColor = new Color(0.07f, 0.04f, 0.04f, 1f),
                    borderTopLeftRadius = 8, borderTopRightRadius = 8,
                    borderBottomLeftRadius = 8, borderBottomRightRadius = 8,
                    paddingTop = 24, paddingBottom = 24, paddingLeft = 24, paddingRight = 24
                }
            };
            SetBorder(modal, cRed, 1);
            overlay.Add(modal);

            modal.Add(new Label("RECALL AMBASSADOR") { style = { color = cRed, fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 8 } });
            modal.Add(new Label($"Embassy in {emb.countryName}") { style = { color = Color.grey, fontSize = 10, marginBottom = 16 } });

            var infoBox = new VisualElement { style = { backgroundColor = new Color(0.12f, 0.06f, 0.06f, 0.8f), paddingTop = 12, paddingBottom = 12, paddingLeft = 14, paddingRight = 14, borderTopLeftRadius = 6, borderTopRightRadius = 6, borderBottomLeftRadius = 6, borderBottomRightRadius = 6, marginBottom = 18 } };
            infoBox.Add(new Label("CONSEQUENCES OF RECALL") { style = { color = new Color(0.6f, 0.6f, 0.7f), fontSize = 9, letterSpacing = 1, marginBottom = 8 } });
            AddCostRow(infoBox, "Intel Feed",      $"All econ/political reports from {emb.countryName} will stop.", cRed);
            AddCostRow(infoBox, "Staff",           $"{emb.staffCount} personnel repatriated.",                      new Color(0.7f, 0.7f, 0.8f));
            AddCostRow(infoBox, "Relations",       "−5 pts (diplomatic slight).",                                   new Color(1f, 0.67f, 0.25f));
            AddCostRow(infoBox, "Reversibility",   "Re-opening costs time and budget.",                             new Color(0.7f, 0.7f, 0.8f));
            modal.Add(infoBox);

            var btnRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };

            var cancelBtn = new Button(() => overlay.parent?.Remove(overlay)) { text = "KEEP EMBASSY" };
            cancelBtn.AddToClassList("econ-small-button");
            cancelBtn.style.width = Length.Percent(45); cancelBtn.style.height = 34;
            btnRow.Add(cancelBtn);

            var confirmBtn = new Button(() =>
            {
                overlay.parent?.Remove(overlay);
                emb.isOperational = false;
                emb.staffCount    = 0;
                var gm = GameManager.Instance;
                gm?.Relations?.Adjust(player.countryName, emb.countryName, -5f, "Ambassador recalled", gm.TurnSystem?.CurrentTurn ?? 0);
                NotificationManager.Instance?.AddNotification($"EMBASSY CLOSED: Ambassador recalled from {emb.countryName}. Intel reporting stopped.", cRed);
                NewsSystem.Instance?.PublishNews($"DIPLOMACY: {player.countryName} recalls its ambassador from {emb.countryName}.");
                RefreshDiplomacyData();
            })
            { text = "CONFIRM RECALL" };
            confirmBtn.AddToClassList("econ-small-button");
            confirmBtn.style.width = Length.Percent(50); confirmBtn.style.height = 34;
            confirmBtn.style.color = cRed;
            confirmBtn.style.backgroundColor = new Color(0.4f, 0.05f, 0.05f, 0.9f);
            SetBorder(confirmBtn, cRed, 1);
            btnRow.Add(confirmBtn);
            modal.Add(btnRow);

            _dashboardRoot?.Add(overlay);
        }

        private void PopulateTreatiesTab(CountryDataSO player, GameManager gm)
        {
            if (_treatiesCardsList == null) return;
            _treatiesCardsList.Clear();
            
            var label = new Label("The Treaties system has been replaced by the Global Factions system.\n\nAllies in the same faction automatically receive Free Trade and Mutual Defence.") 
            { 
                style = { color = Color.white, fontSize = 14, whiteSpace = WhiteSpace.Normal, marginTop = 20, marginLeft = 20, marginRight = 20 } 
            };
            _treatiesCardsList.Add(label);
            
            if (player.globalFaction != "Independent" && player.globalFaction != "Non-Aligned")
            {
                var factionLabel = new Label($"You are currently a member of: {player.globalFaction}") 
                { 
                    style = { color = new Color(0.14f, 0.93f, 0.34f), fontSize = 14, unityFontStyleAndWeight = FontStyle.Bold, marginTop = 20, marginLeft = 20 } 
                };
                _treatiesCardsList.Add(factionLabel);

                var btnLeave = new Button(() => {
                    if (gm?.DiplomacySystem is StateCraft.Systems.DiplomacySystem dipSys)
                    {
                        dipSys.LeaveFaction(player);
                        RefreshDiplomacyData();
                    }
                }) { text = "LEAVE FACTION" };
                btnLeave.AddToClassList("econ-small-button");
                btnLeave.style.width = 200; btnLeave.style.height = 36;
                btnLeave.style.marginTop = 20; btnLeave.style.marginLeft = 20;
                btnLeave.style.backgroundColor = new Color(0.95f, 0.2f, 0.2f, 0.8f);
                SetBorder(btnLeave, new Color(0.95f, 0.2f, 0.2f), 1);
                _treatiesCardsList.Add(btnLeave);
            }
            else
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 20, marginLeft = 20 } };

                var btnCreate = new Button(() => ShowCreateFactionModal(player, gm)) { text = "CREATE NEW FACTION" };
                btnCreate.AddToClassList("econ-small-button");
                btnCreate.style.width = 200; btnCreate.style.height = 36;
                btnCreate.style.backgroundColor = new Color(0.18f, 0.6f, 0.95f, 0.8f);
                SetBorder(btnCreate, new Color(0.18f, 0.6f, 0.95f), 1);
                row.Add(btnCreate);

                var btnJoin = new Button(() => ShowJoinFactionModal(player, gm)) { text = "JOIN FACTION" };
                btnJoin.AddToClassList("econ-small-button");
                btnJoin.style.width = 200; btnJoin.style.height = 36;
                btnJoin.style.marginLeft = 10;
                btnJoin.style.backgroundColor = new Color(0.2f, 0.75f, 0.4f, 0.8f);
                SetBorder(btnJoin, new Color(0.2f, 0.75f, 0.4f), 1);
                row.Add(btnJoin);

                _treatiesCardsList.Add(row);
            }
        }
        
        private void ShowCreateFactionModal(CountryDataSO player, GameManager gm)
        {
            var overlay = MakeOverlay();
            var modal = new VisualElement
            {
                style =
                {
                    width = 400,
                    backgroundColor = new Color(0.07f, 0.04f, 0.04f, 1f),
                    borderTopLeftRadius = 8, borderTopRightRadius = 8,
                    borderBottomLeftRadius = 8, borderBottomRightRadius = 8,
                    paddingTop = 24, paddingBottom = 24, paddingLeft = 24, paddingRight = 24
                }
            };
            SetBorder(modal, new Color(0.18f, 0.6f, 0.95f), 1);
            overlay.Add(modal);

            modal.Add(new Label("CREATE A GLOBAL FACTION") { style = { color = new Color(0.18f, 0.6f, 0.95f), fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 8 } });
            modal.Add(new Label("Founding a new faction will establish your nation as its leader. You can invite other Independent nations to join your faction from their diplomacy page.") { style = { color = Color.grey, fontSize = 10, marginBottom = 16, whiteSpace = WhiteSpace.Normal } });

            var inputField = new UnityEngine.UIElements.TextField("Faction Name:");
            inputField.style.marginBottom = 20;
            modal.Add(inputField);

            var btnRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };

            var cancelBtn = new Button(() => overlay.parent?.Remove(overlay)) { text = "CANCEL" };
            cancelBtn.AddToClassList("econ-small-button");
            cancelBtn.style.width = Length.Percent(45); cancelBtn.style.height = 34;
            btnRow.Add(cancelBtn);

            var confirmBtn = new Button(() =>
            {
                if (string.IsNullOrWhiteSpace(inputField.value))
                {
                    NotificationManager.Instance?.AddNotification("Faction name cannot be empty.", Color.red);
                    return;
                }
                
                string factionName = inputField.value.Trim();
                
                if (gm?.DiplomacySystem is StateCraft.Systems.DiplomacySystem dipSys)
                {
                    dipSys.JoinFaction(player, factionName);
                    NotificationManager.Instance?.AddNotification($"You have founded the {factionName}.", new Color(0.18f, 0.6f, 0.95f));
                    player.globalPrestige = UnityEngine.Mathf.Min(100f, player.globalPrestige + 10f);
                }
                
                overlay.parent?.Remove(overlay);
                RefreshDiplomacyData();
            })
            { text = "FOUND FACTION" };
            confirmBtn.AddToClassList("econ-small-button");
            confirmBtn.style.width = Length.Percent(50); confirmBtn.style.height = 34;
            confirmBtn.style.color = new Color(0.18f, 0.6f, 0.95f);
            confirmBtn.style.backgroundColor = new Color(0.1f, 0.3f, 0.5f, 0.9f);
            btnRow.Add(confirmBtn);

            modal.Add(btnRow);
            _dashboardRoot?.Add(overlay);
        }

        private void ShowJoinFactionModal(CountryDataSO player, GameManager gm)
        {
            var overlay = MakeOverlay();
            var modal = new VisualElement
            {
                style =
                {
                    width = 400,
                    backgroundColor = new Color(0.07f, 0.04f, 0.04f, 1f),
                    borderTopLeftRadius = 8, borderTopRightRadius = 8,
                    borderBottomLeftRadius = 8, borderBottomRightRadius = 8,
                    paddingTop = 24, paddingBottom = 24, paddingLeft = 24, paddingRight = 24
                }
            };
            SetBorder(modal, new Color(0.2f, 0.75f, 0.4f), 1);
            overlay.Add(modal);

            modal.Add(new Label("JOIN A GLOBAL FACTION") { style = { color = new Color(0.2f, 0.75f, 0.4f), fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 8 } });
            
            System.Collections.Generic.HashSet<string> existingFactions = new System.Collections.Generic.HashSet<string>();
            foreach (var c in gm.allCountries)
            {
                if (c.globalFaction != "Independent" && c.globalFaction != "Non-Aligned" && !string.IsNullOrEmpty(c.globalFaction))
                {
                    existingFactions.Add(c.globalFaction);
                }
            }

            if (existingFactions.Count == 0)
            {
                modal.Add(new Label("There are no global factions currently established in the world. You must create one.") { style = { color = Color.grey, fontSize = 12, marginBottom = 16, whiteSpace = WhiteSpace.Normal } });
                
                var cancelBtn = new Button(() => overlay.parent?.Remove(overlay)) { text = "CLOSE" };
                cancelBtn.AddToClassList("econ-small-button");
                cancelBtn.style.width = Length.Percent(100); cancelBtn.style.height = 34;
                modal.Add(cancelBtn);
            }
            else
            {
                modal.Add(new Label("Select an existing global faction to join.") { style = { color = Color.grey, fontSize = 10, marginBottom = 16, whiteSpace = WhiteSpace.Normal } });
                
                var dropdown = new UnityEngine.UIElements.DropdownField("Faction:");
                dropdown.choices = new System.Collections.Generic.List<string>(existingFactions);
                dropdown.index = 0;
                dropdown.style.marginBottom = 20;
                modal.Add(dropdown);

                var btnRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };

                var cancelBtn = new Button(() => overlay.parent?.Remove(overlay)) { text = "CANCEL" };
                cancelBtn.AddToClassList("econ-small-button");
                cancelBtn.style.width = Length.Percent(45); cancelBtn.style.height = 34;
                btnRow.Add(cancelBtn);

                var confirmBtn = new Button(() =>
                {
                    string factionName = dropdown.value;
                    
                    if (gm?.DiplomacySystem is StateCraft.Systems.DiplomacySystem dipSys)
                    {
                        dipSys.JoinFaction(player, factionName);
                    }
                    
                    overlay.parent?.Remove(overlay);
                    RefreshDiplomacyData();
                })
                { text = "JOIN" };
                confirmBtn.AddToClassList("econ-small-button");
                confirmBtn.style.width = Length.Percent(50); confirmBtn.style.height = 34;
                confirmBtn.style.color = new Color(0.2f, 0.75f, 0.4f);
                confirmBtn.style.backgroundColor = new Color(0.1f, 0.4f, 0.2f, 0.9f);
                btnRow.Add(confirmBtn);

                modal.Add(btnRow);
            }

            _dashboardRoot?.Add(overlay);
        }

        private void PopulateOrganizationsTab(CountryDataSO player)
        {
            if (_organizationsCardsGrid == null) return;
            _organizationsCardsGrid.Clear();

            foreach (var org in player.internationalOrgs)
            {
                bool isPending = org.voteStatus == "Pending";
                var card = MakeCard(48, 12);
                if (isPending)
                {
                    card.style.borderLeftWidth = 3;
                    card.style.borderLeftColor = new Color(1f, 0.75f, 0.3f);
                }

                var titleRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 8 } };
                titleRow.Add(new Label(org.orgName) { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 13 } });
                if (isPending)
                    titleRow.Add(new Label("VOTE DUE") { style = { color = new Color(1f, 0.75f, 0.3f), fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold, backgroundColor = new Color(0.8f, 0.5f, 0f, 0.25f), paddingLeft = 5, paddingRight = 5, paddingTop = 2, paddingBottom = 2, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4 } });
                card.Add(titleRow);

                card.Add(KVRow("Status",       org.membershipStatus));
                card.Add(KVRow("Voting Power", org.votingPower));
                card.Add(new VisualElement { style = { height = 1, backgroundColor = new Color(1, 1, 1, 0.1f), marginTop = 8, marginBottom = 8 } });
                card.Add(new Label("Current Resolution") { style = { color = Color.grey, fontSize = 9, marginBottom = 2 } });
                card.Add(new Label(org.currentResolution) { style = { color = Color.white, fontSize = 11, marginBottom = 10, whiteSpace = WhiteSpace.Normal } });

                var voteRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexStart } };
                foreach (var option in new[] { "Approve", "Abstain", "Reject" })
                {
                    string capOpt = option;
                    Color optColor = option == "Approve" ? new Color(0.14f, 0.93f, 0.34f)
                                   : option == "Reject"  ? new Color(1f, 0.32f, 0.32f)
                                   : Color.grey;
                    var vBtn = new Button(() =>
                    {
                        org.voteStatus = capOpt;
                        NotificationManager.Instance?.AddNotification($"VOTE CAST: {capOpt} on '{org.currentResolution}' in {org.orgName}.", Color.cyan);
                        RefreshDiplomacyData();
                    }) { text = option.ToUpper() };
                    vBtn.AddToClassList("econ-small-button");
                    vBtn.style.height = 22; vBtn.style.fontSize = 8;
                    vBtn.style.marginRight = 4;
                    if (org.voteStatus == option)
                    {
                        vBtn.style.backgroundColor = new Color(optColor.r, optColor.g, optColor.b, 0.3f);
                        SetBorder(vBtn, optColor, 1);
                        vBtn.style.color = optColor;
                    }
                    voteRow.Add(vBtn);
                }
                card.Add(voteRow);
                _organizationsCardsGrid.Add(card);
            }
        }

        private void PopulateEmbassiesTab(CountryDataSO player)
        {
            if (_embTotal       != null) _embTotal.text       = player.embassies.Count.ToString();
            if (_embOperational != null) _embOperational.text = player.embassies.Count(e => e.isOperational).ToString();
            if (_embClosed      != null) _embClosed.text      = player.embassies.Count(e => !e.isOperational).ToString();
            if (_embBudget      != null) _embBudget.text      = $"₦{player.embassyBudget:F1}B";

            if (_embassiesCardsGrid == null) return;
            _embassiesCardsGrid.Clear();

            foreach (var emb in player.embassies)
            {
                Embassy captureEmb = emb;
                var card = MakeCard(48, 12);

                var header = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 8 } };
                header.Add(new Label($"Embassy in {emb.countryName}") { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 12 } });
                var sc = emb.isOperational ? new Color(0.13f, 0.92f, 0.33f) : new Color(1f, 0.32f, 0.32f);
                header.Add(new Label(emb.isOperational ? "OPEN" : "CLOSED") { style = { color = sc, fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold, backgroundColor = new Color(sc.r, sc.g, sc.b, 0.18f), paddingLeft = 5, paddingRight = 5, paddingTop = 2, paddingBottom = 2, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4 } });
                card.Add(header);

                var stats = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 8 } };
                stats.Add(KVRow("Staff",     emb.staffCount.ToString()));
                stats.Add(KVRow("Influence", $"{emb.influencePercentage:F0}%"));
                card.Add(stats);

                var reports = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 10 } };
                reports.Add(MiniPill($"Econ: {emb.economicReportQuality}",  Color.grey));
                reports.Add(MiniPill($"Pol: {emb.politicalReportQuality}", Color.grey));
                card.Add(reports);

                var actions = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginTop = 6 } };
                var btnUp = new Button { text = "UPGRADE" };
                btnUp.AddToClassList("econ-small-button"); btnUp.style.width = Length.Percent(48); btnUp.style.height = 24;

                var btnRecall = new Button(() => ShowRecallConfirmModal(player, captureEmb)) { text = "RECALL" };
                btnRecall.AddToClassList("econ-small-button");
                btnRecall.style.width  = Length.Percent(48); btnRecall.style.height = 24;
                btnRecall.style.color  = new Color(1f, 0.32f, 0.32f);
                btnRecall.style.backgroundColor = new Color(0.35f, 0.05f, 0.05f, 0.6f);
                SetBorder(btnRecall, new Color(1f, 0.32f, 0.32f), 1);

                actions.Add(btnUp);
                actions.Add(btnRecall);
                card.Add(actions);

                _embassiesCardsGrid.Add(card);
            }
        }

        private VisualElement MakeOverlay() => new VisualElement
        {
            style =
            {
                position = Position.Absolute, left = 0, top = 0, right = 0, bottom = 0,
                backgroundColor = new Color(0, 0, 0, 0.88f),
                justifyContent = Justify.Center, alignItems = Align.Center
            }
        };

        private VisualElement MakeCard(float widthPct, float margin)
        {
            var card = new VisualElement();
            card.style.width = Length.Percent(widthPct);
            card.style.backgroundColor = new Color(0.05f, 0.07f, 0.1f, 0.95f);
            card.style.borderTopWidth = card.style.borderBottomWidth =
            card.style.borderLeftWidth = card.style.borderRightWidth = 1;
            card.style.borderTopColor = card.style.borderBottomColor =
            card.style.borderLeftColor = card.style.borderRightColor = new Color(1, 1, 1, 0.09f);
            card.style.borderTopLeftRadius = card.style.borderTopRightRadius =
            card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 6;
            card.style.paddingLeft = card.style.paddingRight =
            card.style.paddingTop  = card.style.paddingBottom = margin;
            card.style.marginBottom = margin; card.style.marginRight = margin;
            return card;
        }

        private void SetBorder(VisualElement el, Color c, float w)
        {
            el.style.borderTopColor = el.style.borderBottomColor =
            el.style.borderLeftColor = el.style.borderRightColor = new Color(c.r, c.g, c.b, 0.6f);
            el.style.borderTopWidth = el.style.borderBottomWidth =
            el.style.borderLeftWidth = el.style.borderRightWidth = w;
        }

        private VisualElement KVRow(string label, string value)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 4 } };
            row.Add(new Label(label) { style = { color = Color.grey, fontSize = 10 } });
            row.Add(new Label(value) { style = { color = Color.white, fontSize = 10 } });
            return row;
        }

        private VisualElement MiniPill(string text, Color color)
        {
            var lbl = new Label(text);
            lbl.style.color = color; lbl.style.fontSize = 9;
            lbl.style.backgroundColor = new Color(1, 1, 1, 0.05f);
            lbl.style.paddingLeft = 4; lbl.style.paddingRight = 4;
            lbl.style.borderTopLeftRadius = lbl.style.borderTopRightRadius =
            lbl.style.borderBottomLeftRadius = lbl.style.borderBottomRightRadius = 4;
            lbl.style.marginRight = 6;
            return lbl;
        }

        private void AddDetailStat(string label, string value, Color? valueColor = null)
        {
            if (_detailStatsList == null) return;
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 6 } };
            row.Add(new Label(label) { style = { color = Color.grey, fontSize = 10 } });
            row.Add(new Label(value) { style = { color = valueColor ?? Color.white, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
            _detailStatsList.Add(row);
        }

        private void AddAction(string text, Color accentColor, System.Action onClick)
        {
            var btn = new Button(onClick) { text = text };
            btn.AddToClassList("econ-small-button");
            btn.style.height = 30; btn.style.fontSize = 10; btn.style.marginBottom = 8;
            btn.style.color = accentColor;
            btn.style.backgroundColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.1f);
            SetBorder(btn, accentColor, 1);
            _detailActionsList.Add(btn);
        }

        private void AddCostRow(VisualElement parent, string label, string value, Color col)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 5 } };
            row.Add(new Label(label) { style = { color = new Color(0.7f, 0.7f, 0.75f), fontSize = 10 } });
            row.Add(new Label(value) { style = { color = col, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold, whiteSpace = WhiteSpace.Normal } });
            parent.Add(row);
        }
    }
}
