import re

# Clean up IntelligenceDashboardController.cs
intel_path = r'c:\Users\Tobi\StateCraft\Assets\Scripts\UI\IntelligenceDashboardController.cs'
with open(intel_path, 'r', encoding='utf-8') as f:
    content = f.read()

lines = content.split('\n')
clean_lines = []
for line in lines:
    stripped = line.strip()
    skip = False
    if '[IntelDashboard]' in stripped and ('Debug.Log(' in stripped or 'Debug.LogError(' in stripped or 'Debug.LogWarning(' in stripped):
        skip = True
    if not skip:
        clean_lines.append(line)

content = '\n'.join(clean_lines)

# Restore OnEnable to actually call refresh when _dashboardRoot is ready
content = content.replace(
    "            // Don't call Refresh here — _dashboardRoot isn't set yet on first enable (before Start)",
    "            if (_dashboardRoot != null) RefreshIntelligenceData();"
)

with open(intel_path, 'w', encoding='utf-8') as f:
    f.write(content)
print('Cleaned IntelligenceDashboardController.cs')

# Clean up MainDashboardToolkitController.cs
main_path = r'c:\Users\Tobi\StateCraft\Assets\Scripts\UI\MainDashboardToolkitController.cs'
with open(main_path, 'r', encoding='utf-8') as f:
    content = f.read()

lines = content.split('\n')
clean_lines = []
for line in lines:
    stripped = line.strip()
    skip = False
    if '[Dashboard]' in stripped and ('Debug.Log(' in stripped or 'Debug.LogError(' in stripped):
        skip = True
    if not skip:
        clean_lines.append(line)

content = '\n'.join(clean_lines)

with open(main_path, 'w', encoding='utf-8') as f:
    f.write(content)
print('Cleaned MainDashboardToolkitController.cs')
