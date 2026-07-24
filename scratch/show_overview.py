with open('C:/Users/Tobi/StateCraft/Assets/Scripts/UI/IntelligenceDashboardController.cs', 'r', encoding='utf-8') as f:
    lines = f.readlines()
for i, line in enumerate(lines):
    if "private void PopulateOverviewTab" in line:
        for j in range(i, min(i+100, len(lines))):
            print(f"{j+1}: {lines[j].rstrip()}")
        break
