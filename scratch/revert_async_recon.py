import re

with open(r'c:\Users\Tobi\StateCraft\Assets\Scripts\UI\IntelligenceDashboardController.cs', 'r', encoding='utf-8') as f:
    code = f.read()

new_method = """        private void LaunchReconMission()
        {
            var root = _uiDoc?.rootVisualElement;
            var drpPriority = root?.Q<UnityEngine.UIElements.DropdownField>("DrpReconPriority");
            string priority = drpPriority != null ? drpPriority.value : "Low";

            UnityEngine.Debug.Log($"Recon Mission Launch Button Clicked! Priority: {priority}");
            var gm = GameManager.Instance;
            if (gm != null)
            {
                if (gm.playerBudget < 0.05f)
                {
                    NotificationManager.Instance?.AddNotification("INSUFFICIENT FUNDS: Need $0.05B for Recon Mission.", UnityEngine.Color.red);
                    return;
                }
                gm.playerBudget -= 0.05f;
            }
            
            int delayDays = priority == "Critical" ? 1 : priority == "High" ? 3 : priority == "Medium" ? 7 : 14;
            NotificationManager.Instance?.AddNotification($"RECON MISSION LAUNCHED ({priority} Priority): Aerial assets deployed to scan area. ETA: {delayDays} Days", new UnityEngine.Color(0.2f, 0.8f, 0.2f));

            var country = GameManager.Instance?.playerCountry;
            if (country != null)
            {
                if (country.activeCovertOps == null) country.activeCovertOps = new System.Collections.Generic.List<StateCraft.Data.CovertOperation>();
                var op = new StateCraft.Data.CovertOperation
                {
                    opName = $"Recon Sweep {(char)UnityEngine.Random.Range(65, 91)}{UnityEngine.Random.Range(10, 99)}",
                    type = "Aerial Recon",
                    target = "Eastern Border",
                    operativeName = "Drone Sqn 1",
                    riskLevel = priority,
                    status = "Scanning",
                    detectionRisk = 5f,
                    currentPhase = 1,
                    totalPhases = 1,
                    monthsRemaining = 1,
                    daysRemaining = delayDays,
                    priority = priority
                };
                country.activeCovertOps.Add(op);
            }
            
            RefreshIntelligenceData();
        }"""

pattern = r'        private async void LaunchReconMission\(\).*?RefreshIntelligenceData\(\);\s*\}'
code = re.sub(pattern, new_method, code, flags=re.DOTALL)

with open(r'c:\Users\Tobi\StateCraft\Assets\Scripts\UI\IntelligenceDashboardController.cs', 'w', encoding='utf-8') as f:
    f.write(code)
print('Updated LaunchReconMission to use in-game days')
