import re

with open(r'c:\Users\Tobi\StateCraft\Assets\Scripts\UI\IntelligenceDashboardController.cs', 'r', encoding='utf-8') as f:
    code = f.read()

new_method = """        private async void LaunchReconMission()
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
            
            NotificationManager.Instance?.AddNotification($"RECON MISSION LAUNCHED ({priority} Priority): Aerial assets deployed to scan area.", new UnityEngine.Color(0.2f, 0.8f, 0.2f));

            var country = GameManager.Instance?.playerCountry;
            StateCraft.Data.CovertOperation op = null;
            if (country != null)
            {
                if (country.activeCovertOps == null) country.activeCovertOps = new System.Collections.Generic.List<StateCraft.Data.CovertOperation>();
                op = new StateCraft.Data.CovertOperation
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
                    monthsRemaining = 1
                };
                country.activeCovertOps.Add(op);
            }
            
            // Show it in the Active list immediately
            RefreshIntelligenceData();

            // Determine delay based on priority
            int delayMs = priority == "Critical" ? 1000 : priority == "High" ? 2500 : priority == "Medium" ? 5000 : 8000;
            
            await System.Threading.Tasks.Task.Delay(delayMs);

            // Mission completes
            NotificationManager.Instance?.AddNotification($"RECON REPORT ({priority}): Enemy movement detected in Sector 4. 2x Mechanized Brigades spotted.", new UnityEngine.Color(1.0f, 0.64f, 0.0f));

            if (country != null && op != null)
            {
                country.activeCovertOps.Remove(op);
            }
            
            // Remove it from the Active list
            RefreshIntelligenceData();
        }"""

pattern = r'        private void LaunchReconMission\(\).*?RefreshIntelligenceData\(\);\s*\}'
code = re.sub(pattern, new_method, code, flags=re.DOTALL)

with open(r'c:\Users\Tobi\StateCraft\Assets\Scripts\UI\IntelligenceDashboardController.cs', 'w', encoding='utf-8') as f:
    f.write(code)
print('Updated LaunchReconMission to be async and delay based on urgency')
