# coding=utf-8
import re

with open('C:/Users/Tobi/StateCraft/Assets/Scripts/UI/IntelligenceDashboardController.cs', 'r', encoding='utf-8') as f:
    code = f.read()

# Replace PopulateThreatAssessmentTab to use actual system
new_threat_tab = '''        private void PopulateThreatAssessmentTab(CountryDataSO country)
        {
            var panel = _dashboardRoot?.Q<VisualElement>("PanelThreatBoard");
            if (panel == null) return;

            var cRed = new Color(1f, 0.32f, 0.32f);
            var cOrange = new Color(0.95f, 0.77f, 0.06f);
            var cGreen = new Color(0.137f, 0.929f, 0.337f);
            var cPurple = new Color(0.73f, 0.41f, 0.78f);
            var cBlue = new Color(0.18f, 0.6f, 0.95f);
            var cWhiteAlpha = new Color(1, 1, 1, 0.1f);

            var gm = StateCraft.Core.GameManager.Instance;
            var threats = gm.ThreatAssessmentSystem.GenerateThreats(country, gm);

            // --- KPIs ---
            panel.Q<Label>("TA_ActiveThreats").text = threats.Count.ToString();
            int criticals = 0;
            foreach (var t in threats) if (t.impact == "Critical") criticals++;
            panel.Q<Label>("TA_CriticalThreats").text = criticals.ToString();
            panel.Q<Label>("TA_CountriesConcern").text = gm.allCountries != null ? (gm.allCountries.Count / 3).ToString() : "0";

            // --- THREAT MATRIX ---
            var matrix = panel.Q<VisualElement>("TA_MatrixGrid");
            matrix.Clear();
            Color[] mColors = { cGreen, cGreen, cOrange, cOrange,
                                cGreen, cOrange, cOrange, cRed,
                                cOrange, cOrange, cRed, cRed,
                                cOrange, cRed, cRed, cRed };
            for(int i=0; i<16; i++)
            {
                var box = new VisualElement { style = { width = Length.Percent(23), height = 24, marginRight = Length.Percent(2), marginBottom = 6, backgroundColor = mColors[i], borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2, alignItems = Align.Center, justifyContent = Justify.Center } };
                if (i == 3 || i == 7 || i == 11 || i == 15) box.style.marginRight = 0; // last column
                box.Add(new Label($"{i+1}") { style = { color = new Color(1,1,1,0.5f), fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold } });
                matrix.Add(box);
            }

            // --- TOP THREATS ---
            var tList = panel.Q<ScrollView>("TA_TopThreatsList");
            tList.Clear();
            for(int i=0; i<threats.Count; i++)
            {
                var t = threats[i];
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingBottom = 6, paddingTop = 6, borderBottomWidth = 1, borderBottomColor = cWhiteAlpha } };
                row.Add(new Label((i+1).ToString()) { style = { color = Color.grey, fontSize = 8, width = 16 } });
                row.Add(new Label(t.title) { style = { color = Color.white, fontSize = 8, width = 124 } });
                row.Add(new Label(t.region) { style = { color = Color.grey, fontSize = 8, flexGrow = 1 } });
                
                // Impact dots
                var impCol = new VisualElement { style = { flexDirection = FlexDirection.Row, width = 60 } };
                for(int d=0; d<4; d++) impCol.Add(new VisualElement { style = { width = 6, height = 6, borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3, backgroundColor = (d < (t.impact == "Critical" ? 4 : t.impact == "High" ? 3 : 2)) ? (t.impact == "Critical" ? cRed : cOrange) : cWhiteAlpha, marginRight = 2 } });
                row.Add(impCol);
                
                // Likelihood dots
                var likCol = new VisualElement { style = { flexDirection = FlexDirection.Row, width = 60 } };
                for(int d=0; d<4; d++) likCol.Add(new VisualElement { style = { width = 6, height = 6, borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3, backgroundColor = (d < (int)(t.probability/25f)) ? (t.impact == "Critical" ? cRed : cOrange) : cWhiteAlpha, marginRight = 2 } });
                row.Add(likCol);
                
                // Trend
                row.Add(new Label(t.impact == "Medium" ? "->" : "^") { style = { color = t.impact == "Medium" ? cOrange : cRed, fontSize = 10, width = 40, unityTextAlign = TextAnchor.MiddleCenter, unityFontStyleAndWeight = FontStyle.Bold } });
                
                // Risk Level
                row.Add(new Label(t.impact.ToUpper()) { style = { color = t.impact == "Critical" ? cRed : cOrange, fontSize = 8, width = 60, unityTextAlign = TextAnchor.MiddleRight, unityFontStyleAndWeight = FontStyle.Bold } });
                tList.Add(row);
            }

            // --- CATEGORIES ---
            panel.Q<Label>("TA_CatDonut").text = threats.Count.ToString();
            var cList = panel.Q<VisualElement>("TA_CategoryList");
            cList.Clear();
            string[] cNames = { "Terrorism", "Political", "Economic", "Cyber" };
            Color[] cColors = { cRed, cOrange, cOrange, cPurple };
            string[] cVals = { $"{criticals} ({(threats.Count>0?criticals*100/threats.Count:0)}%)", "0 (0%)", "0 (0%)", "0 (0%)" };
            for(int i=0; i<4; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 4 } };
                var rL = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                rL.Add(new VisualElement { style = { width = 6, height = 6, backgroundColor = cColors[i], marginRight = 6 } });
                rL.Add(new Label(cNames[i]) { style = { color = Color.white, fontSize = 8 } });
                row.Add(rL);
                row.Add(new Label(cVals[i]) { style = { color = Color.grey, fontSize = 8 } });
                cList.Add(row);
            }

            // --- REGIONAL THREAT OVERVIEW ---
            var rList = panel.Q<VisualElement>("TA_RegionList");
            rList.Clear();
            if (country.regions != null && country.regions.Count > 0)
            {
                for(int i=0; i<country.regions.Count && i<6; i++)
                {
                    var reg = country.regions[i];
                    var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 6 } };
                    
                    var rL = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, width = 110 } };
                    rL.Add(new Label(reg.regionName) { style = { color = Color.grey, fontSize = 8, whiteSpace = WhiteSpace.Normal } });
                    row.Add(rL);
                    
                    var barBg = new VisualElement { style = { flexGrow = 1, height = 4, backgroundColor = cWhiteAlpha, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2, marginRight = 8, marginLeft = 8 } };
                    float risk = Mathf.Clamp(100f - reg.security, 0f, 100f);
                    barBg.Add(new VisualElement { style = { width = Length.Percent(risk), height = 4, backgroundColor = risk > 70f ? cRed : risk > 40f ? cOrange : cGreen, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } });
                    row.Add(barBg);
                    
                    string rLvl = risk > 70f ? "HIGH" : risk > 40f ? "MEDIUM" : "LOW";
                    row.Add(new Label(rLvl) { style = { color = risk > 70f ? cRed : risk > 40f ? cOrange : cGreen, fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold, width = 50, unityTextAlign = TextAnchor.MiddleRight } });
                    rList.Add(row);
                }
            }
        }'''

code = re.sub(r'        private void PopulateThreatAssessmentTab\(CountryDataSO country\)\s*\{.*?(?=        private void PopulateDataAnalysisTab)', new_threat_tab + '\n\n', code, flags=re.DOTALL)

with open('C:/Users/Tobi/StateCraft/Assets/Scripts/UI/IntelligenceDashboardController.cs', 'w', encoding='utf-8') as f:
    f.write(code)

print("Updated PopulateThreatAssessmentTab")
