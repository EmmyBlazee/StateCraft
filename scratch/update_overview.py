import re

with open('C:/Users/Tobi/StateCraft/Assets/Scripts/UI/IntelligenceDashboardController.cs', 'r', encoding='utf-8') as f:
    code = f.read()

# 1. Replace PopulateIntelGrid
grid_pattern = r'("CYBER", )Mathf\.Clamp\(country\.intelligenceLevel \* 0\.8f \+ country\.covertBudget \* 5f, 0f, 100f\)(.*?\n.*?"SIGINT", )country\.globalIntelCoverage'
# Wait, SIGINT comes before CYBER in the array:
#                 ("SIGINT", country.globalIntelCoverage),
#                 ("HUMINT", country.intelligenceLevel),
#                 ("CYBER", Mathf.Clamp(country.intelligenceLevel * 0.8f + country.covertBudget * 5f, 0f, 100f)),
code = code.replace('("SIGINT", country.globalIntelCoverage)', '("SIGINT", country.signalInterceptCapability)')
code = code.replace('("CYBER", Mathf.Clamp(country.intelligenceLevel * 0.8f + country.covertBudget * 5f, 0f, 100f))', '("CYBER", country.cyberDefenseLevel)')

# 2. Replace active operations in PopulateOverviewTab
old_ops = '''            if (_overviewActiveOpsList != null)
            {
                _overviewActiveOpsList.Clear();
                // Add mock ops to match mockup
                AddOverviewActiveOp("Operation Eagle Eye", "Border Surveillance", "ACTIVE", new Color(0.14f, 0.93f, 0.34f), FaIcons.Bolt);
                AddOverviewActiveOp("Border Watch", "Northern Border", "ACTIVE", new Color(0.14f, 0.93f, 0.34f), FaIcons.Bolt);
                AddOverviewActiveOp("Gulf Shield", "Maritime Surveillance", "ACTIVE", new Color(0.14f, 0.93f, 0.34f), FaIcons.Shield);
                AddOverviewActiveOp("Cyber Shield", "National Cyber Defense", "ACTIVE", new Color(0.14f, 0.93f, 0.34f), FaIcons.Shield);
            }'''
new_ops = '''            if (_overviewActiveOpsList != null)
            {
                _overviewActiveOpsList.Clear();
                foreach (var op in country.activeCovertOps)
                {
                    Color statusColor = (op.status == "ACTIVE" || op.status == "In Progress") ? new Color(0.14f, 0.93f, 0.34f) : new Color(1f, 0.67f, 0.25f);
                    string icon = FaIcons.Shield;
                    if (op.type == "Assassination" || op.type == "Sabotage") icon = FaIcons.BullsEye;
                    else if (op.type == "Propaganda" || op.type == "Political Disruption") icon = FaIcons.UserSecret;
                    else if (op.type == "Cyber Attack") icon = FaIcons.Microchip;
                    
                    AddOverviewActiveOp(op.opName, op.target, op.status.ToUpper(), statusColor, icon);
                }
            }'''
code = code.replace(old_ops, new_ops)

# 3. Replace recent reports in PopulateOverviewTab
old_reports = '''            if (_overviewRecentReportsList != null)
            {
                _overviewRecentReportsList.Clear();
                // Add mock reports
                AddOverviewRecentReport("Border Incursion Detected", "Northern Nigeria", "Armed militants observed crossing from Niger near Katsina region.", "08 Jan 2026\\n07:45", "HIGH", new Color(1f, 0.2f, 0.2f), FaIcons.Shield, new Color(0.14f, 0.93f, 0.34f));
                AddOverviewRecentReport("Political Unrest", "Cameroon", "Growing public discontent due to economic hardship and political instability.", "08 Jan 2026\\n06:20", "MEDIUM", new Color(1f, 0.67f, 0.25f), FaIcons.UserSecret, new Color(1f, 0.67f, 0.25f));
                AddOverviewRecentReport("Pirate Activity Increase", "Gulf of Guinea", "Increase in pirate attacks on commercial vessels near Delta region.", "08 Jan 2026\\n05:10", "MEDIUM", new Color(1f, 0.67f, 0.25f), FaIcons.TriangleExclaim, new Color(0.24f, 0.53f, 0.94f));
                AddOverviewRecentReport("Military Build-up", "Niger", "Heavy military equipment movement observed near Diffa region.", "08 Jan 2026\\n04:30", "HIGH", new Color(1f, 0.2f, 0.2f), FaIcons.CircleXMark, new Color(1f, 0.2f, 0.2f));
            }'''
new_reports = '''            if (_overviewRecentReportsList != null)
            {
                _overviewRecentReportsList.Clear();
                var feedLines = DashboardLiveContent.BuildIntelFeed(country, GameManager.Instance);
                int count = 0;
                foreach (var feed in feedLines)
                {
                    if (count >= 4) break;
                    string severity = feed.Color.r > 0.8f && feed.Color.g < 0.4f ? "HIGH" : (feed.Color.r > 0.8f && feed.Color.g > 0.6f ? "MEDIUM" : "LOW");
                    AddOverviewRecentReport("Intelligence Intercept", feed.Tag, feed.Message, "Now", severity, feed.Color, FaIcons.Bolt, feed.Color);
                    count++;
                }
            }'''
code = code.replace(old_reports, new_reports)

with open('C:/Users/Tobi/StateCraft/Assets/Scripts/UI/IntelligenceDashboardController.cs', 'w', encoding='utf-8') as f:
    f.write(code)

print("Updated OverviewTab and IntelGrid")
