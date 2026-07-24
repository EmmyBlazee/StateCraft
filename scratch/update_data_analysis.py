# coding=utf-8
import re

with open('C:/Users/Tobi/StateCraft/Assets/Scripts/UI/IntelligenceDashboardController.cs', 'r', encoding='utf-8') as f:
    code = f.read()

# Replace PopulateDataAnalysisTab
new_data_tab = '''        private void PopulateDataAnalysisTab(CountryDataSO country)
        {
            var panel = _dashboardRoot?.Q<VisualElement>("PanelDataAnalysis");
            if (panel == null) return;

            var cRed = new Color(1f, 0.32f, 0.32f);
            var cOrange = new Color(0.95f, 0.77f, 0.06f);
            var cGreen = new Color(0.137f, 0.929f, 0.337f);
            var cPurple = new Color(0.73f, 0.41f, 0.78f);
            var cBlue = new Color(0.18f, 0.6f, 0.95f);
            var cTeal = new Color(0.2f, 0.7f, 0.7f);
            var cWhiteAlpha = new Color(1, 1, 1, 0.1f);

            int intelLvl = Mathf.Clamp((int)country.intelligenceLevel, 10, 100);

            // --- KPIs ---
            panel.Q<Label>("DA_DataSources").text = (intelLvl * 2 + country.satelliteCount * 5).ToString();
            panel.Q<Label>("DA_DataPoints").text = (country.covertBudget * 10f).ToString("N0") + "K";
            panel.Q<Label>("DA_Accuracy").text = Mathf.Clamp((int)country.intelligenceLevel, 10, 99).ToString() + "%";
            panel.Q<Label>("DA_Insights").text = ((int)(country.intelligenceLevel * 0.5f)).ToString();
            
            float budget = country.covertBudget;
            panel.Q<Label>("DA_Budget").text = $"€{Mathf.RoundToInt(budget)}M / month";
            panel.Q<Label>("DA_BudgetRem").text = $"€{Mathf.RoundToInt(budget * 0.4f)}M remaining";

            // --- DATA SOURCES OVERVIEW ---
            var srcList = panel.Q<VisualElement>("DA_SourcesList");
            srcList.Clear();
            string[] sNames = { "Human Intelligence (HUMINT)", "Signals Intelligence (SIGINT)", "Imagery Intelligence (IMINT)", "Open Source Intelligence (OSINT)", "Cyber Intelligence (CYBINT)" };
            int[] sVals = {
                Mathf.Clamp((int)country.intelligenceLevel, 0, 100),
                Mathf.Clamp((int)country.signalInterceptCapability, 0, 100),
                Mathf.Clamp(country.satelliteCount * 25, 0, 100),
                Mathf.Clamp((int)(country.intelligenceLevel * 0.8f + 20), 0, 100),
                Mathf.Clamp((int)country.cyberDefenseLevel, 0, 100)
            };
            Color[] sCols = { cGreen, cBlue, cOrange, cTeal, cPurple };
            for(int i=0; i<5; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 6 } };
                row.Add(new Label(sNames[i]) { style = { color = Color.grey, fontSize = 8, width = 110, whiteSpace = WhiteSpace.Normal } });
                
                var barBg = new VisualElement { style = { flexGrow = 1, height = 4, backgroundColor = cWhiteAlpha, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2, marginRight = 8, marginLeft = 8 } };
                barBg.Add(new VisualElement { style = { width = Length.Percent(sVals[i]), height = 4, backgroundColor = sCols[i], borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } });
                row.Add(barBg);
                
                row.Add(new Label($"{sVals[i]}%") { style = { color = Color.white, fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold, width = 25, unityTextAlign = TextAnchor.MiddleRight } });
                srcList.Add(row);
            }

            // --- INTELLIGENCE TRENDS ---
            var chart = panel.Q<VisualElement>("DA_TrendChart");
            chart.Clear();
            for(int i=0; i<30; i++)
            {
                var barCont = new VisualElement { style = { height = Length.Percent(100), width = 12, justifyContent = Justify.FlexEnd, alignItems = Align.Center } };
                float v1 = UnityEngine.Random.Range(10f, 25f);
                float v2 = UnityEngine.Random.Range(10f, 25f);
                float v3 = UnityEngine.Random.Range(10f, 25f);
                float total = v1 + v2 + v3;
                float targetH = UnityEngine.Random.Range(40f, 100f);
                
                var bar = new VisualElement { style = { width = 6, height = Length.Percent(targetH), flexDirection = FlexDirection.ColumnReverse } };
                bar.Add(new VisualElement { style = { width = 100, height = Length.Percent(v1/total*100), backgroundColor = cBlue } });
                bar.Add(new VisualElement { style = { width = 100, height = Length.Percent(v2/total*100), backgroundColor = cPurple } });
                bar.Add(new VisualElement { style = { width = 100, height = Length.Percent(v3/total*100), backgroundColor = cOrange } });
                
                barCont.Add(bar);
                chart.Add(barCont);
            }
        }'''

code = re.sub(r'        private void PopulateDataAnalysisTab\(CountryDataSO country\)\s*\{.*?(?=        private void PopulateCovertOperationsTab)', new_data_tab + '\n\n', code, flags=re.DOTALL)

with open('C:/Users/Tobi/StateCraft/Assets/Scripts/UI/IntelligenceDashboardController.cs', 'w', encoding='utf-8') as f:
    f.write(code)

print("Updated PopulateDataAnalysisTab")
