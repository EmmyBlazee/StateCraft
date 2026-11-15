import re

with open('C:/Users/Tobi/StateCraft/Assets/Scripts/UI/IntelligenceDashboardController.cs', 'r', encoding='utf-8') as f:
    code = f.read()

# Replace PopulateCovertOperationsTab
new_covert_ops_tab = '''        private void PopulateCovertOperationsTab(CountryDataSO country)
        {
            var panel = _root.Q<VisualElement>("PanelCovertOperations");
            if (panel == null) return;

            // Colors
            var cGreen = new Color(0.137f, 0.929f, 0.337f);
            var cRed = new Color(1f, 0.32f, 0.32f);
            var cOrange = new Color(0.95f, 0.77f, 0.06f);
            var cBlue = new Color(0.18f, 0.6f, 0.95f);
            var cPurple = new Color(0.73f, 0.41f, 0.78f);
            var cWhiteAlpha = new Color(1, 1, 1, 0.1f);

            int intelLvl = Mathf.Clamp((int)country.intelligenceLevel, 10, 100);
            float budget = country.covertBudget;

            // --- KPIs ---
            int totalOps = country.activeCovertOps.Count;
            int inProgress = 0;
            int planning = 0;
            int onHold = 0;
            int completed = 0;
            int highRiskOps = 0;
            
            Dictionary<string, int> categoryCounts = new Dictionary<string, int>() {
                {"Political Operations", 0}, {"Economic Operations", 0}, {"Military Operations", 0}, {"Influence Operations", 0}, {"Cyber Operations", 0}
            };

            foreach (var op in country.activeCovertOps)
            {
                if (op.status == "ACTIVE" || op.status == "In Progress") inProgress++;
                else if (op.status == "PLANNING" || op.status == "Planning") planning++;
                else if (op.status == "ON HOLD" || op.status == "On Hold") onHold++;
                else completed++;

                if (op.riskLevel == "High" || op.riskLevel == "Extreme") highRiskOps++;

                string cat = "Influence Operations";
                if (op.type == "Assassination" || op.type == "Political Disruption") cat = "Political Operations";
                else if (op.type == "Sabotage" || op.type == "Arms Supply") cat = "Military Operations";
                else if (op.type == "Economic Disruption") cat = "Economic Operations";
                else if (op.type == "Cyber Attack") cat = "Cyber Operations";

                if (categoryCounts.ContainsKey(cat)) categoryCounts[cat]++;
                else categoryCounts[cat] = 1;
            }

            panel.Q<Label>("CO_ActiveOps").text = inProgress.ToString();
            panel.Q<Label>("CO_HighRiskOps").text = $"{highRiskOps} High Risk";
            panel.Q<Label>("CO_SuccessRate").text = $"{65f + (country.intelligenceLevel * 0.3f):F0}%";
            panel.Q<Label>("CO_SuccessDelta").text = "→ -";
            panel.Q<Label>("CO_Budget").text = $"€{Mathf.RoundToInt(budget)}M";
            panel.Q<Label>("CO_BudgetRemaining").text = $"€{Mathf.RoundToInt(budget * 0.28f)}M remaining";
            
            var riskLbl = panel.Q<Label>("CO_RiskLabel");
            riskLbl.text = highRiskOps > 2 ? "HIGH" : highRiskOps > 0 ? "MEDIUM" : "LOW";
            riskLbl.style.color = highRiskOps > 2 ? cRed : highRiskOps > 0 ? cOrange : cGreen;
            panel.Q<VisualElement>("CO_RiskDot").style.backgroundColor = riskLbl.style.color;

            // --- DONUT CHART ---
            panel.Q<Label>("CO_TotalOpsDonut").text = totalOps.ToString();
            
            var statusList = panel.Q<VisualElement>("CO_StatusList");
            statusList.Clear();
            string[] sNames = { "In Progress", "Planning", "On Hold", "Completed" };
            int[] sVals = { inProgress, planning, onHold, completed };
            Color[] sColors = { cGreen, cBlue, cOrange, cPurple };
            
            for(int i=0; i<4; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 4, alignItems = Align.Center } };
                var rL = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                rL.Add(new VisualElement { style = { width = 6, height = 6, backgroundColor = sColors[i], marginRight = 6 } });
                rL.Add(new Label(sNames[i]) { style = { color = Color.white, fontSize = 8 } });
                row.Add(rL);
                row.Add(new Label($"{sVals[i]} ({(totalOps > 0 ? Mathf.RoundToInt(sVals[i]*100f/totalOps) : 0)}%)") { style = { color = Color.grey, fontSize = 8 } });
                statusList.Add(row);
            }

            // --- CATEGORIES ---
            var catList = panel.Q<VisualElement>("CO_CategoryList");
            catList.Clear();
            string[] cNames = { "Political Operations", "Economic Operations", "Military Operations", "Influence Operations", "Cyber Operations" };
            string[] cIcons = { "\uf0c0", "\uf153", "\uf132", "\uf0a1", "\uf108" };
            for(int i=0; i<5; i++)
            {
                int val = categoryCounts[cNames[i]];
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 6, alignItems = Align.Center } };
                var rL = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                rL.Add(new Label(cIcons[i]) { style = { color = Color.grey, fontSize = 10, marginRight = 8, width = 14 } }.WithClass("fa-icon"));
                rL.Add(new Label(cNames[i]) { style = { color = Color.grey, fontSize = 8 } });
                row.Add(rL);
                row.Add(new Label(val.ToString()) { style = { color = Color.white, fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold } });
                catList.Add(row);
            }

            // --- ACTIVE OPERATIONS LIST ---
            var opsList = panel.Q<ScrollView>("CO_OpsList");
            opsList.Clear();
            
            foreach (var op in country.activeCovertOps)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingBottom = 8, paddingTop = 8, borderBottomWidth = 1, borderBottomColor = cWhiteAlpha } };
                
                var rCol = new VisualElement { style = { width = 160 } };
                rCol.Add(new Label(op.opName) { style = { color = Color.white, fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
                rCol.Add(new Label(op.target) { style = { color = Color.grey, fontSize = 7, marginTop = 2 } });
                row.Add(rCol);
                
                row.Add(new Label(op.type) { style = { color = Color.grey, fontSize = 8, width = 110 } });
                row.Add(new Label(op.operativeName) { style = { color = Color.grey, fontSize = 8, flexGrow = 1 } });
                
                var stCol = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, width = 90 } };
                stCol.Add(new VisualElement { style = { width = 6, height = 6, borderRadius = 3, backgroundColor = op.status == "ACTIVE" || op.status == "In Progress" ? cGreen : cOrange, marginRight = 6 } });
                stCol.Add(new Label(op.status.ToUpper()) { style = { color = op.status == "ACTIVE" || op.status == "In Progress" ? cGreen : cOrange, fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold } });
                row.Add(stCol);
                
                var pCol = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, width = 100 } };
                pCol.Add(new Label($"PHASE {op.currentPhase}/{op.totalPhases}") { style = { color = Color.white, fontSize = 8, width = 45 } });
                var pBar = new VisualElement { style = { flexGrow = 1, height = 4, backgroundColor = cWhiteAlpha, borderRadius = 2, marginLeft = 4 } };
                pBar.Add(new VisualElement { style = { width = Length.Percent((float)op.currentPhase/op.totalPhases*100f), height = 4, backgroundColor = cBlue, borderRadius = 2 } });
                pCol.Add(pBar);
                row.Add(pCol);

                opsList.Add(row);
            }
        }'''

# Find the start and end of PopulateCovertOperationsTab
start_idx = code.find('private void PopulateCovertOperationsTab(CountryDataSO country)')
end_idx = code.find('private void PopulateDataAnalysisTab(CountryDataSO country)', start_idx)

code = code[:start_idx] + new_covert_ops_tab + "\n\n        " + code[end_idx:]

with open('C:/Users/Tobi/StateCraft/Assets/Scripts/UI/IntelligenceDashboardController.cs', 'w', encoding='utf-8') as f:
    f.write(code)

print("Updated PopulateCovertOperationsTab")
