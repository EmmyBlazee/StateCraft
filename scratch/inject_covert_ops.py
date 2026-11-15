import re

with open(r'c:\Users\Tobi\StateCraft\Assets\Scripts\Core\GameManager.cs', 'r', encoding='utf-8') as f:
    lines = f.readlines()

new_lines = []
for i, line in enumerate(lines):
    new_lines.append(line)
    if 'warSys.ProcessWMDDevelopment(country);' in line:
        new_lines.append('''                }

                // Process Covert/Recon Operations
                if (country.activeCovertOps != null && country.activeCovertOps.Count > 0)
                {
                    for (int i_op = country.activeCovertOps.Count - 1; i_op >= 0; i_op--)
                    {
                        var op = country.activeCovertOps[i_op];
                        if (op.daysRemaining > 0)
                        {
                            op.daysRemaining--;
                            if (op.daysRemaining <= 0)
                            {
                                // Operation completes!
                                string report = $"RECON REPORT ({op.priority}): Enemy movement detected in Sector 4. 2x Mechanized Brigades spotted.";
                                if (country == playerCountry)
                                {
                                    NotificationManager.Instance?.AddNotification(report, new UnityEngine.Color(1.0f, 0.64f, 0.0f));
                                }
                                
                                if (country.recentReconReports == null) country.recentReconReports = new System.Collections.Generic.List<string>();
                                country.recentReconReports.Add($"[{TurnSystem?.CurrentDate ?? "Unknown Date"}] {op.opName} - {report}");
                                
                                country.activeCovertOps.RemoveAt(i_op);
                                
                                // Ideally we refresh the UI if it's open, but the player might not be on the tab
                                // The feed will pick it up next time it is drawn.
                            }
                        }
                    }''')

with open(r'c:\Users\Tobi\StateCraft\Assets\Scripts\Core\GameManager.cs', 'w', encoding='utf-8') as f:
    f.writelines(new_lines)
print('Injected CovertOps processing into GameManager.ProcessTick')
