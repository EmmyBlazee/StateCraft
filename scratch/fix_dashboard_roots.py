import os
import re

def fix_controller(filepath):
    if not os.path.exists(filepath):
        print(f"File not found: {filepath}")
        return

    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    original_content = content
    
    # 1. Add _dashboardRoot field if not exists
    if "_dashboardRoot" not in content:
        content = re.sub(r'(\s*private\s+VisualElement\s+_root;)', r'\1\n        private VisualElement _dashboardRoot;', content)
        
    # 2. Store dashboard into _dashboardRoot in Awake()
    # Looking for var dashboard = _root?.Q<VisualElement>("EconomyDashboard");
    # or similar and then if (dashboard != null) { ... InitializeElements
    pattern_awake = r'(\s*var\s+dashboard\s*=\s*_root\??\.Q<VisualElement>\([^)]+\);[\s\n]*if\s*\(dashboard\s*!=\s*null\)[\s\n]*\{)(?![\s\n]*_dashboardRoot)'
    content = re.sub(pattern_awake, r'\1\n                _dashboardRoot = dashboard;', content)
    
    # 3. In Show(), replace _root.Q with _dashboardRoot
    # var panel = _root.Q<VisualElement>("...");
    # replace with var panel = _dashboardRoot;
    pattern_show = r'(\s*public\s+void\s+Show\s*\(\)\s*\{[\s\n]*)var\s+panel\s*=\s*_root\.Q<VisualElement>\([^)]+\);'
    content = re.sub(pattern_show, r'\1var panel = _dashboardRoot;', content)
    
    # 4. In Refresh methods, replace _root.Q with _dashboardRoot?.Q
    # only inside methods. We can just replace all _root.Q( with _dashboardRoot?.Q( EXCEPT in Awake/OnEnable
    # Actually, a simple targeted replacement for the specific ones in Economy:
    if "Economy" in filepath:
        content = content.replace('_root.Q<Label>("DateMain")', '_dashboardRoot?.Q<Label>("DateMain")')
        content = content.replace('_root.Q<Label>("DateSub")', '_dashboardRoot?.Q<Label>("DateSub")')
        content = content.replace('_root.Q<Label>("ValInflationDelta")', '_dashboardRoot?.Q<Label>("ValInflationDelta")')
        content = content.replace('_root.Q<Label>("ValDebtRatio")', '_dashboardRoot?.Q<Label>("ValDebtRatio")')
        content = content.replace('_root.Q<Label>("ValApprovalDelta")', '_dashboardRoot?.Q<Label>("ValApprovalDelta")')
    elif "Military" in filepath:
        content = content.replace('_root.Q<VisualElement>("MilitaryDashboard")', '_dashboardRoot')
    elif "Diplomacy" in filepath:
        content = content.replace('_root.Q<VisualElement>("DiplomacyDashboard")', '_dashboardRoot')
    elif "Event" in filepath:
        content = content.replace('_root.Q<VisualElement>("EventDashboard")', '_dashboardRoot')
    elif "Global" in filepath:
        pass # GlobalDashboard uses MainUI, not injected maybe? Actually GlobalDashboard uses GlobalDashboard

    if content != original_content:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Fixed {os.path.basename(filepath)}")
    else:
        print(f"No changes needed for {os.path.basename(filepath)}")

base_dir = r"c:\Users\Tobi\StateCraft\Assets\Scripts\UI"
fix_controller(os.path.join(base_dir, "EconomyDashboardController.cs"))
fix_controller(os.path.join(base_dir, "MilitaryDashboardController.cs"))
fix_controller(os.path.join(base_dir, "DiplomacyDashboardController.cs"))
fix_controller(os.path.join(base_dir, "EventDashboardController.cs"))
fix_controller(os.path.join(base_dir, "GlobalDashboardController.cs"))
