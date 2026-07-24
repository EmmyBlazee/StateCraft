import re

with open(r'c:\Users\Tobi\StateCraft\Assets\Scripts\UI\IntelligenceDashboardController.cs', 'r', encoding='utf-8') as f:
    lines = f.readlines()

new_lines = []
for i, line in enumerate(lines):
    if 'RefreshIntelligenceData();' in line and 'LaunchReconMission()' in ''.join(lines[max(0, i-15):i]):
        new_lines.append('''            var country = GameManager.Instance?.playerCountry;
            if (country != null)
            {
                if (country.activeCovertOps == null) country.activeCovertOps = new System.Collections.Generic.List<StateCraft.Data.CovertOperation>();
                var op = new StateCraft.Data.CovertOperation
                {
                    opName = $"Recon Sweep {(char)UnityEngine.Random.Range(65, 91)}{UnityEngine.Random.Range(10, 99)}",
                    type = "Aerial Recon",
                    target = "Eastern Border",
                    operativeName = "Drone Sqn 1",
                    riskLevel = "Low",
                    status = "Scanning",
                    detectionRisk = 5f,
                    currentPhase = 1,
                    totalPhases = 1,
                    monthsRemaining = 1
                };
                country.activeCovertOps.Add(op);
            }
''')
    new_lines.append(line)

with open(r'c:\Users\Tobi\StateCraft\Assets\Scripts\UI\IntelligenceDashboardController.cs', 'w', encoding='utf-8') as f:
    f.writelines(new_lines)
print('Updated LaunchReconMission to add to activeCovertOps')
