using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using StateCraft.Core;
using StateCraft.Data;

namespace StateCraft.UI
{
    [DefaultExecutionOrder(-10)]
    public class GlobalDashboardController : MonoBehaviour
    {
        private UIDocument _uiDoc;
        private VisualElement _root;
        private VisualElement _dashboardRoot;
        
        private Label _kpiWorldStab, _kpiAfrStab, _kpiOil, _kpiInflation, _kpiWars;
        private VisualElement _liveEventsList, _leadersList, _topEconomiesList, _topMilitaryList, _regionalSummariesGrid;

        private void Awake()
        {
            _uiDoc = GetComponent<UIDocument>();
            if (_uiDoc != null) _root = _uiDoc.rootVisualElement;
            
            var dashboard = _root?.Q<VisualElement>("GlobalDashboard");
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
                GameManager.Instance.TurnSystem.OnTurnEnded += RefreshDashboard;
            RefreshDashboard();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null && GameManager.Instance.TurnSystem != null)
                GameManager.Instance.TurnSystem.OnTurnEnded -= RefreshDashboard;
        }

        private void InitializeElements(VisualElement root)
        {
            root.Q<Button>("BtnCloseGlobalDashboard")?.RegisterCallback<ClickEvent>(_ => Hide());

            _kpiWorldStab = root.Q<Label>("KpiWorldStab");
            _kpiAfrStab = root.Q<Label>("KpiAfrStab");
            _kpiOil = root.Q<Label>("KpiOil");
            _kpiInflation = root.Q<Label>("KpiInflation");
            _kpiWars = root.Q<Label>("KpiWars");

            _liveEventsList = root.Q<VisualElement>("LiveEventsList");
            _leadersList = root.Q<VisualElement>("LeadersList");
            _topEconomiesList = root.Q<VisualElement>("TopEconomiesList");
            _topMilitaryList = root.Q<VisualElement>("TopMilitaryList");
            _regionalSummariesGrid = root.Q<VisualElement>("RegionalSummariesGrid");
        }

        public void Show()
        {
            if (_dashboardRoot == null || _dashboardRoot.panel == null)
            {
                _dashboardRoot = MainDashboardToolkitController.Instance?.GetPanelVisualElement("Global")
                              ?? _root?.Q<VisualElement>("GlobalDashboard");
            }

            var panel = _dashboardRoot;
            if (panel != null)
            {
                panel.style.display = DisplayStyle.Flex;
                UIStateManager.Instance?.ApplySafeTransition(panel, true, 0.4f);
                RefreshDashboard();
                UIStateManager.FixScrollViews(panel);
            }
        }

        public void Hide()
        {
            if (MainDashboardToolkitController.Instance != null)
                MainDashboardToolkitController.Instance.HideAllPanels();
        }

        public void RefreshDashboard()
        {
            if (_dashboardRoot == null || _dashboardRoot.panel == null)
            {
                _dashboardRoot = MainDashboardToolkitController.Instance?.GetPanelVisualElement("Global")
                              ?? _root?.Q<VisualElement>("GlobalDashboard")
                              ?? _dashboardRoot;
            }

            if (GameManager.Instance == null || GameManager.Instance.allCountries == null) return;
            var all = GameManager.Instance.allCountries;

            float worldStab = all.Average(c => c.stability);
            float afrStab = all.Where(c => !string.IsNullOrEmpty(c.countryName)).Average(c => c.stability); // Mock filter

            if (_kpiWorldStab != null) _kpiWorldStab.text = $"{worldStab:F0}%";
            if (_kpiAfrStab != null) _kpiAfrStab.text = $"{afrStab:F0}%";
            
            // Mock Global metrics
            if (_kpiOil != null) _kpiOil.text = $"${Random.Range(70f, 95f):F1}";
            if (_kpiInflation != null) _kpiInflation.text = $"{Random.Range(2f, 8f):F1}%";
            if (_kpiWars != null) _kpiWars.text = Random.Range(1, 4).ToString();

            PopulateLiveEvents(all);
            PopulateLeaders(all);
            PopulateRankings(all);
            PopulateRegional(all);
        }

        private void PopulateLiveEvents(List<CountryDataSO> all)
        {
            if (_liveEventsList == null) return;
            _liveEventsList.Clear();
            
            string[] eventTypes = { "Civil War", "Election", "Economic Crisis", "Military Coup", "Diplomatic Summit", "Trade Agreement" };
            
            for(int i = 0; i < 5; i++)
            {
                var country = all[Random.Range(0, all.Count)];
                string ev = eventTypes[Random.Range(0, eventTypes.Length)];
                
                var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, paddingBottom = 8, marginBottom = 8, borderBottomWidth = 1, borderBottomColor = new Color(1,1,1,0.05f) } };
                row.Add(new Label(ev) { style = { color = new Color(0, 0.8f, 1f), fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } });
                row.Add(new Label(country.countryName) { style = { color = Color.white, fontSize = 11 } });
                _liveEventsList.Add(row);
            }
        }

        private void PopulateLeaders(List<CountryDataSO> all)
        {
            if (_leadersList == null) return;
            _leadersList.Clear();

            var mostStable = all.OrderByDescending(c => c.stability).FirstOrDefault();
            var leastStable = all.OrderBy(c => c.stability).FirstOrDefault();
            var topPrestige = all.OrderByDescending(c => c.globalPrestige).FirstOrDefault();

            AddLeaderRow("Most Stable Nation", mostStable?.countryName ?? "Unknown");
            AddLeaderRow("Highest GDP Growth", topPrestige?.countryName ?? "Unknown");
            AddLeaderRow("Most Corrupt Nation", leastStable?.countryName ?? "Unknown");
        }

        private void AddLeaderRow(string title, string value)
        {
            var row = new VisualElement() { style = { paddingBottom = 6, marginBottom = 6, borderBottomWidth = 1, borderBottomColor = new Color(1,1,1,0.05f) } };
            row.Add(new Label(title) { style = { color = Color.grey, fontSize = 10, marginBottom = 2 } });
            row.Add(new Label(value) { style = { color = Color.white, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold } });
            _leadersList.Add(row);
        }

        private void PopulateRankings(List<CountryDataSO> all)
        {
            if (_topEconomiesList != null)
            {
                _topEconomiesList.Clear();
                var econs = all.OrderByDescending(c => c.gdp).Take(10).ToList();
                for(int i = 0; i < econs.Count; i++)
                {
                    AddRankRow(_topEconomiesList, i + 1, econs[i].countryName, $"${econs[i].gdp:F1}B");
                }
            }

            if (_topMilitaryList != null)
            {
                _topMilitaryList.Clear();
                var mils = all.OrderByDescending(c => c.security).Take(10).ToList();
                for(int i = 0; i < mils.Count; i++)
                {
                    AddRankRow(_topMilitaryList, i + 1, mils[i].countryName, $"PWR: {mils[i].security:F0}");
                }
            }
        }

        private void AddRankRow(VisualElement parent, int rank, string name, string val)
        {
            var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, paddingBottom = 4, paddingTop = 4, borderBottomWidth = 1, borderBottomColor = new Color(1,1,1,0.05f) } };
            
            var left = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
            left.Add(new Label($"{rank}.") { style = { color = Color.grey, fontSize = 11, width = 20 } });
            left.Add(new Label(name) { style = { color = Color.white, fontSize = 11 } });
            
            row.Add(left);
            row.Add(new Label(val) { style = { color = new Color(0, 0.9f, 0.46f), fontSize = 11 } });
            
            parent.Add(row);
        }

        private void PopulateRegional(List<CountryDataSO> all)
        {
            if (_regionalSummariesGrid == null) return;
            _regionalSummariesGrid.Clear();

            string[] regions = { "West Africa", "North Africa", "East Africa", "Central Africa", "Southern Africa" };
            
            foreach (var r in regions)
            {
                var card = new VisualElement() { style = { width = Length.Percent(18), backgroundColor = new Color(0.1f, 0.12f, 0.15f, 0.9f), borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, paddingLeft = 8, paddingRight = 8, paddingTop = 8, paddingBottom = 8 } };
                card.Add(new Label(r) { style = { color = Color.white, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4, unityTextAlign = TextAnchor.MiddleCenter } });
                
                float stab = Random.Range(30f, 90f);
                Color c = new Color(241/255f, 196/255f, 15/255f);
                string desc = "Political Tension";
                if (stab > 70) { c = new Color(0, 0.9f, 0.46f); desc = "Stable"; }
                else if (stab < 40) { c = new Color(255/255f, 82/255f, 82/255f); desc = "Conflict Escalating"; }

                card.Add(new Label(desc) { style = { color = c, fontSize = 9, unityTextAlign = TextAnchor.MiddleCenter, whiteSpace = WhiteSpace.Normal } });
                _regionalSummariesGrid.Add(card);
            }
        }
    }
}
