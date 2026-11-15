import re

def add_placeholders():
    path = r"c:\Users\Tobi\StateCraft\Assets\Scripts\UI\IntelligenceDashboardController.cs"
    with open(path, "r", encoding="utf-8") as f:
        content = f.read()

    # 1. Add AddPlaceholder method
    if "private void AddPlaceholder" not in content:
        placeholder_method = """
        private void AddPlaceholder(VisualElement container, string text = "Data not available")
        {
            if (container == null) return;
            container.Clear();
            var label = new Label(text) { style = { color = Color.gray, unityTextAlign = TextAnchor.MiddleCenter, flexGrow = 1, marginTop = 20, fontSize = 12 } };
            container.Add(label);
        }
"""
        content = content.replace("private void PopulateIntelGrid(CountryDataSO country, GameManager gm)", placeholder_method + "\n        private void PopulateIntelGrid(CountryDataSO country, GameManager gm)")

    # 2. Add Surveillance Placeholders
    surveillance_ph = """
                AddPlaceholder(panel.Q<VisualElement>("RS_AssetsList"), "No surveillance assets deployed.");
                AddPlaceholder(panel.Q<VisualElement>("RS_MissionsList"), "No active recon missions.");
                AddPlaceholder(panel.Q<VisualElement>("RS_SensorCoverageList"), "Sensor network offline.");
                AddPlaceholder(panel.Q<VisualElement>("RS_DonutBreakdownList"), "No data.");
                AddPlaceholder(panel.Q<VisualElement>("RS_AnalyticsList"), "Insufficient data for analytics.");
                AddPlaceholder(panel.Q<VisualElement>("RS_RecentActivityContainer"), "No recent activity.");
                
                var feedList = panel.Q<ScrollView>("RS_LiveFeedList");
                if (feedList != null) {
                    feedList.Clear();
                    var feedLines = DashboardLiveContent.BuildIntelFeed(country, gm);
                    foreach (var feed in feedLines) {
                        var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 4 } };
                        row.Add(new Label($"[{feed.Timestamp.ToString("HH:mm")}]") { style = { color = Color.gray, fontSize = 10, width = 45 } });
                        row.Add(new Label(feed.Message) { style = { color = feed.TextColor, fontSize = 10, flexGrow = 1, whiteSpace = WhiteSpace.Normal } });
                        feedList.Add(row);
                    }
                }
"""
    if "RS_AssetsList" not in content[content.find("PopulateSurveillance"):content.find("PopulateIntelGrid")]:
        content = re.sub(r'(float fusionAccuracy = Mathf\.Clamp\(70f \+ country\.intelligenceLevel \* 0\.2f, 0, 100\);\s*if \(panel\.Q<Label>\("RS_FusionAccuracy"\) != null\) panel\.Q<Label>\("RS_FusionAccuracy"\)\.text = \$"\{fusionAccuracy:F0\}%";\s*if \(panel\.Q<Label>\("RS_FusionStatus"\) != null\) panel\.Q<Label>\("RS_FusionStatus"\)\.text = fusionAccuracy > 80 \? "High" : "Average";)', r'\1' + "\n" + surveillance_ph, content)

    # 3. Add Threat Assessment Placeholders
    threat_ph = """
              AddPlaceholder(panel.Q<VisualElement>("TA_TrendChart"), "Trend visualization offline.");
              AddPlaceholder(panel.Q<VisualElement>("TA_CountriesList"), "No countries of concern monitored.");
              AddPlaceholder(panel.Q<VisualElement>("TA_CriticalCards"), "No critical threats detected.");
              AddPlaceholder(panel.Q<VisualElement>("TA_MitigationList"), "No active mitigation progress.");
              AddPlaceholder(panel.Q<VisualElement>("TA_RecommendationsList"), "No recommendations available.");
              AddPlaceholder(panel.Q<VisualElement>("TA_CategoryList"), "No category breakdown.");
"""
    if "TA_TrendChart" not in content[content.find("PopulateThreatAssessmentTab"):content.find("PopulateDataAnalysisTab")]:
        content = re.sub(r'(rList\.Add\(row\);\s*\}\s*\})', r'\1' + "\n" + threat_ph, content, count=1)

    # 4. Add Data Analysis Placeholders
    data_ph = """
                        AddPlaceholder(panel.Q<VisualElement>("DA_TrendChart"), "Trend visualization offline.");
                        AddPlaceholder(panel.Q<VisualElement>("DA_GeographicList"), "Geographic focus unavailable.");
                        AddPlaceholder(panel.Q<VisualElement>("DA_PatternList"), "No patterns detected.");
                        AddPlaceholder(panel.Q<VisualElement>("DA_ProcessingList"), "No data processing active.");
                        AddPlaceholder(panel.Q<VisualElement>("DA_QualityList"), "Quality breakdown unavailable.");
                        AddPlaceholder(panel.Q<VisualElement>("DA_ReportsList"), "No recent analytical reports.");
"""
    if "DA_GeographicList" not in content[content.find("PopulateDataAnalysisTab"):]:
        content = re.sub(r'(sourcesList\.Add\(row\);\s*\})', r'\1' + "\n" + data_ph, content, count=1)

    with open(path, "w", encoding="utf-8") as f:
        f.write(content)
    print("UI placeholders added successfully.")

add_placeholders()
