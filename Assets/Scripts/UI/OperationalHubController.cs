using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using StateCraft.Core;
using StateCraft.Data;
using StateCraft.Systems;

namespace StateCraft.UI
{
    public class OperationalHubController : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private VisualElement _root;

        private VisualElement _hubExpandedPanel;
        private VisualElement _hubMinimizedStrip;
        private ScrollView _hubScrollContainer;
        private Button _btnCloseHub;

        private VisualElement _iconIntelContainer;
        private VisualElement _iconEconomyContainer;
        private VisualElement _iconSecurityContainer;

        private Label _badgeIntel;
        private Label _badgeEconomy;
        private Label _badgeSecurity;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null)
            {
                _uiDocument = FindObjectOfType<UIDocument>();
            }

            if (_uiDocument != null)
            {
                _root = _uiDocument.rootVisualElement;
                InitializeElements();
            }
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null && GameManager.Instance.TurnSystem != null)
            {
                GameManager.Instance.TurnSystem.OnTurnEnded += RefreshAllData;
            }
            RefreshAllData();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null && GameManager.Instance.TurnSystem != null)
            {
                GameManager.Instance.TurnSystem.OnTurnEnded -= RefreshAllData;
            }
        }

        private void InitializeElements()
        {
            if (_root == null) return;

            _hubExpandedPanel = _root.Q<VisualElement>("HubExpandedPanel");
            _hubMinimizedStrip = _root.Q<VisualElement>("HubMinimizedStrip");
            _hubScrollContainer = _root.Q<ScrollView>("HubScrollContainer");
            _btnCloseHub = _root.Q<Button>("BtnCloseHub");

            _iconIntelContainer = _root.Q<VisualElement>("IconIntelContainer");
            _iconEconomyContainer = _root.Q<VisualElement>("IconEconomyContainer");
            _iconSecurityContainer = _root.Q<VisualElement>("IconSecurityContainer");

            _badgeIntel = _root.Q<Label>("BadgeIntel");
            _badgeEconomy = _root.Q<Label>("BadgeEconomy");
            _badgeSecurity = _root.Q<Label>("BadgeSecurity");

            if (_iconIntelContainer != null)
            {
                _iconIntelContainer.RegisterCallback<ClickEvent>(evt => ExpandHub());
                _iconIntelContainer.RegisterCallback<MouseEnterEvent>(evt => ExpandHub());
            }

            if (_iconEconomyContainer != null)
            {
                _iconEconomyContainer.RegisterCallback<ClickEvent>(evt => ExpandHub());
                _iconEconomyContainer.RegisterCallback<MouseEnterEvent>(evt => ExpandHub());
            }

            if (_iconSecurityContainer != null)
            {
                _iconSecurityContainer.RegisterCallback<ClickEvent>(evt => ExpandHub());
                _iconSecurityContainer.RegisterCallback<MouseEnterEvent>(evt => ExpandHub());
            }

            if (_btnCloseHub != null)
            {
                _btnCloseHub.clicked += CollapseHub;
            }
        }

        private void ExpandHub()
        {
            if (_hubExpandedPanel != null)
            {
                _hubExpandedPanel.RemoveFromClassList("hub-expanded-panel-collapsed");
                RefreshAllData();
            }
        }

        private void CollapseHub()
        {
            if (_hubExpandedPanel != null)
            {
                _hubExpandedPanel.AddToClassList("hub-expanded-panel-collapsed");
            }
        }

        public void RefreshAllData()
        {
            if (GameManager.Instance == null || GameManager.Instance.playerCountry == null) return;

            var country = GameManager.Instance.playerCountry;
            var gm = GameManager.Instance;

            int activeIntelOps = country.activeCovertOps != null ? country.activeCovertOps.Count : 0;

            int macroAlertsCount = 0;
            bool isHighInflation = country.inflationRate >= 0.08f;
            if (isHighInflation) macroAlertsCount++;

            float welfareCost = (country.gdp / 12f) * (country.subsidyLevel / 100f);
            float monthlyGDP = country.gdp / 12f;
            float taxRevenue = monthlyGDP * (country.taxRate / 100f);
            bool isSubsidiesExceedTaxes = welfareCost > taxRevenue;
            if (isSubsidiesExceedTaxes) macroAlertsCount++;

            int unrestCount = 0;
            List<RegionDataSO> unrestRegions = new List<RegionDataSO>();
            if (country.regions != null)
            {
                foreach (var region in country.regions)
                {
                    if (region.insurgencyLevel >= 30f)
                    {
                        unrestCount++;
                        unrestRegions.Add(region);
                    }
                }
            }

            UpdateBadge(_badgeIntel, activeIntelOps);
            UpdateBadge(_badgeEconomy, macroAlertsCount);
            UpdateBadge(_badgeSecurity, unrestCount);

            if (_hubScrollContainer == null) return;
            _hubScrollContainer.Clear();

            if (country.activeCovertOps != null && country.activeCovertOps.Count > 0)
            {
                foreach (var op in country.activeCovertOps)
                {
                    var card = new VisualElement();
                    card.AddToClassList("hub-card");
                    card.AddToClassList("hub-card-cyan");

                    var header = new VisualElement();
                    header.AddToClassList("hub-card-header");
                    
                    var tag = new Label($"[📡 COVERT OPS]") { style = { color = new Color(0, 210/255f, 1f) } };
                    tag.AddToClassList("hub-card-tag");
                    header.Add(tag);
                    
                    var targetName = new Label(op.target.ToUpper()) { style = { color = Color.grey, fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } };
                    header.Add(targetName);
                    card.Add(header);

                    var title = new Label($"INTEL SCAN: {op.target.ToUpper()}") { style = { fontSize = 11, color = Color.white, unityFontStyleAndWeight = FontStyle.Bold } };
                    card.Add(title);

                    string phaseLabel = GetPhaseLabel(op.currentPhase);
                    var phase = new Label($"Phase {op.currentPhase}/{op.totalPhases} ({phaseLabel})") { style = { color = Color.white, fontSize = 9, marginTop = 4 } };
                    card.Add(phase);

                    var time = new Label($"{op.monthsRemaining} Months Left") { style = { color = new Color(0, 210/255f, 1f), fontSize = 9, marginTop = 2, unityFontStyleAndWeight = FontStyle.Bold } };
                    card.Add(time);

                    var riskContainer = new VisualElement() { style = { marginTop = 8 } };
                    var riskRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
                    
                    var riskLabel = new Label("Detection Risk:") { style = { color = Color.grey, fontSize = 8 } };
                    var riskVal = new Label($"{op.detectionRisk:F0}%") { style = { fontSize = 8 } };
                    
                    if (op.detectionRisk >= 60f)
                    {
                        riskVal.text = $"[⚠️ HIGH RISK] {op.detectionRisk:F0}%";
                        riskVal.style.color = new Color(255/255f, 193/255f, 7/255f);
                        riskVal.style.unityFontStyleAndWeight = FontStyle.Bold;
                    }
                    else
                    {
                        riskVal.style.color = new Color(0, 210/255f, 1f);
                    }

                    riskRow.Add(riskLabel);
                    riskRow.Add(riskVal);
                    riskContainer.Add(riskRow);

                    var barBg = new VisualElement() { style = { height = 5, backgroundColor = new Color(0, 0, 0, 0.4f), borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3, overflow = Overflow.Hidden, marginTop = 4 } };
                    var barFill = new VisualElement() { style = { height = 5, width = Length.Percent(op.detectionRisk), borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3 } };
                    
                    if (op.detectionRisk >= 60f)
                    {
                        barFill.style.backgroundColor = new Color(255/255f, 193/255f, 7/255f);
                    }
                    else
                    {
                        barFill.style.backgroundColor = new Color(0, 210/255f, 1f);
                    }
                    barBg.Add(barFill);
                    riskContainer.Add(barBg);
                    card.Add(riskContainer);

                    var actionRow = new VisualElement();
                    actionRow.AddToClassList("hub-card-action-bar");

                    var abortBtn = new Button(() => {
                        country.operativeCount++;
                        country.activeCovertOps.Remove(op);
                        NotificationManager.Instance?.AddNotification(
                            $"OPERATION ABORTED: Agent extracted safely from {op.target}. Surveillance scan cancelled.",
                            Color.cyan);
                        RefreshAllData();
                    }) { text = "ABORT" };
                    abortBtn.AddToClassList("hub-card-btn");
                    abortBtn.AddToClassList("hub-card-btn-abort");
                    actionRow.Add(abortBtn);

                    var riskBtn = new Button(() => {
                        CollapseHub();
                    }) { text = "RISK IT" };
                    riskBtn.AddToClassList("hub-card-btn");
                    riskBtn.AddToClassList("hub-card-btn-risk");
                    actionRow.Add(riskBtn);

                    card.Add(actionRow);
                    _hubScrollContainer.Add(card);
                }
            }

            if (isHighInflation)
            {
                var card = new VisualElement();
                card.AddToClassList("hub-card");
                card.AddToClassList("hub-card-yellow");

                var header = new VisualElement();
                header.AddToClassList("hub-card-header");
                
                var tag = new Label("[⚠️ FISCAL ALERT]") { style = { color = new Color(255/255f, 193/255f, 7/255f) } };
                tag.AddToClassList("hub-card-tag");
                header.Add(tag);
                card.Add(header);

                var title = new Label("CURRENCY INFLATION CRISIS") { style = { fontSize = 11, color = Color.white, unityFontStyleAndWeight = FontStyle.Bold } };
                card.Add(title);

                var detail = new Label($"Inflation spike detected: +{country.inflationRate * 100:F1}% (Currency devaluing). Consider adjusting central interest rates or welfare spending.") { style = { fontSize = 9, color = new Color(176/255f, 190/255f, 197/255f), whiteSpace = WhiteSpace.Normal, marginTop = 4 } };
                card.Add(detail);

                var actionRow = new VisualElement();
                actionRow.AddToClassList("hub-card-action-bar");

                var viewBtn = new Button(() => {
                    var econController = FindObjectOfType<EconomyDashboardController>();
                    if (econController != null) econController.Show();
                    CollapseHub();
                }) { text = "OPEN ECONOMY" };
                viewBtn.AddToClassList("hub-card-btn");
                viewBtn.AddToClassList("hub-card-btn-action");
                actionRow.Add(viewBtn);

                card.Add(actionRow);
                _hubScrollContainer.Add(card);
            }

            if (isSubsidiesExceedTaxes)
            {
                var card = new VisualElement();
                card.AddToClassList("hub-card");
                card.AddToClassList("hub-card-yellow");

                var header = new VisualElement();
                header.AddToClassList("hub-card-header");
                
                var tag = new Label("[📉 TREASURY ALERT]") { style = { color = new Color(255/255f, 193/255f, 7/255f) } };
                tag.AddToClassList("hub-card-tag");
                header.Add(tag);
                card.Add(header);

                var title = new Label("TREASURY DEFICIT TRIGGERED") { style = { fontSize = 11, color = Color.white, unityFontStyleAndWeight = FontStyle.Bold } };
                card.Add(title);

                var detail = new Label($"Welfare subsidies exceeding monthly tax revenue. Running a deficit. Subsidies cost: {welfareCost:F2}B / Tax Income: {taxRevenue:F2}B.") { style = { fontSize = 9, color = new Color(176/255f, 190/255f, 197/255f), whiteSpace = WhiteSpace.Normal, marginTop = 4 } };
                card.Add(detail);

                var actionRow = new VisualElement();
                actionRow.AddToClassList("hub-card-action-bar");

                var viewBtn = new Button(() => {
                    var econController = FindObjectOfType<EconomyDashboardController>();
                    if (econController != null) econController.Show();
                    CollapseHub();
                }) { text = "OPEN ECONOMY" };
                viewBtn.AddToClassList("hub-card-btn");
                viewBtn.AddToClassList("hub-card-btn-action");
                actionRow.Add(viewBtn);

                card.Add(actionRow);
                _hubScrollContainer.Add(card);
            }

            if (unrestRegions.Count > 0)
            {
                foreach (var region in unrestRegions)
                {
                    var card = new VisualElement();
                    card.AddToClassList("hub-card");
                    card.AddToClassList("hub-card-red");

                    var header = new VisualElement();
                    header.AddToClassList("hub-card-header");
                    
                    var tag = new Label("[🔥 UNREST RISK]") { style = { color = new Color(255/255f, 82/255f, 82/255f) } };
                    tag.AddToClassList("hub-card-tag");
                    header.Add(tag);
                    
                    var regionNameTag = new Label(region.regionName.ToUpper()) { style = { color = Color.grey, fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } };
                    header.Add(regionNameTag);
                    card.Add(header);

                    var title = new Label($"Radicalization growing in {region.regionName}") { style = { fontSize = 11, color = Color.white, unityFontStyleAndWeight = FontStyle.Bold } };
                    card.Add(title);

                    int daysToRiot = Mathf.Max(1, Mathf.RoundToInt((100f - region.insurgencyLevel) * 0.25f));
                    var detail = new Label($"Current Threat Level: {region.insurgencyLevel:F0}% | Time to Riot: {daysToRiot} Days") { style = { fontSize = 9, color = new Color(255/255f, 82/255f, 82/255f), unityFontStyleAndWeight = FontStyle.Bold, marginTop = 4 } };
                    card.Add(detail);

                    var info = new Label("Local stability collapses rapidly as radical elements assemble. Immediate suppression required before GDP drop.") { style = { fontSize = 8, color = new Color(176/255f, 190/255f, 197/255f), whiteSpace = WhiteSpace.Normal, marginTop = 2 } };
                    card.Add(info);

                    var actionRow = new VisualElement();
                    actionRow.AddToClassList("hub-card-action-bar");

                    var deployBtn = new Button(() => {
                        float cost = 0.01f;
                        if (gm.playerBudget < cost)
                        {
                            NotificationManager.Instance?.AddNotification(
                                "INSUFFICIENT FUNDS: Deploying local police suppression requires ₦0.01B.",
                                Color.red);
                            return;
                        }

                        gm.playerBudget -= cost;
                        country.treasury = gm.playerBudget;
                        region.security = Mathf.Min(100f, region.security + 15f);
                        region.insurgencyLevel = Mathf.Max(0f, region.insurgencyLevel - 20f);

                        NotificationManager.Instance?.AddNotification(
                            $"POLICE DEPLOYED: Threat suppressed in {region.regionName}! Security +15%, Unrest -20%.",
                            Color.green);
                        
                        RefreshAllData();
                    }) { text = "DEPLOY POLICE" };
                    deployBtn.AddToClassList("hub-card-btn");
                    deployBtn.AddToClassList("hub-card-btn-action");
                    actionRow.Add(deployBtn);

                    card.Add(actionRow);
                    _hubScrollContainer.Add(card);
                }
            }

            if (activeIntelOps == 0 && macroAlertsCount == 0 && unrestCount == 0)
            {
                var emptyLabel = new Label("MONITORS ALL CLEAR\nNo operational alerts active.") 
                { 
                    style = { 
                        color = Color.grey, 
                        fontSize = 10, 
                        unityFontStyleAndWeight = FontStyle.Bold,
                        unityTextAlign = TextAnchor.MiddleCenter, 
                        marginTop = 60
                    } 
                };
                _hubScrollContainer.Add(emptyLabel);
            }
        }

        private void UpdateBadge(Label badge, int count)
        {
            if (badge == null) return;
            if (count > 0)
            {
                badge.text = count.ToString();
                badge.style.display = DisplayStyle.Flex;
            }
            else
            {
                badge.style.display = DisplayStyle.None;
            }
        }

        private string GetPhaseLabel(int phase)
        {
            switch (phase)
            {
                case 1: return "Infiltration Grid";
                case 2: return "Deep Surveillance";
                case 3: return "Data Exfiltration";
                default: return "Espionage Feed";
            }
        }
    }
}
