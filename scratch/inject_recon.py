import re

with open(r'c:\Users\Tobi\StateCraft\Assets\Scripts\UI\IntelligenceDashboardController.cs', 'r', encoding='utf-8') as f:
    lines = f.readlines()

new_lines = []
for i, line in enumerate(lines):
    new_lines.append(line)
    if 'private void InitializeElements(VisualElement root)' in line:
        new_lines.append('            root.Q<Button>("BtnLaunchReconMission")?.RegisterCallback<ClickEvent>(_ => LaunchReconMission());\n')
    if 'private void LaunchSampleCovertOp()' in line:
        new_lines.insert(-1, '''
        private void LaunchReconMission()
        {
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
            NotificationManager.Instance?.AddNotification("RECON MISSION LAUNCHED: Aerial assets deployed to scan area.", new UnityEngine.Color(0.2f, 0.8f, 0.2f));
            RefreshIntelligenceData();
        }
''')

with open(r'c:\Users\Tobi\StateCraft\Assets\Scripts\UI\IntelligenceDashboardController.cs', 'w', encoding='utf-8') as f:
    f.writelines(new_lines)
print('Added LaunchReconMission logic.')
