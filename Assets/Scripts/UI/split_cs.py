import os
import re

file_path = "MilitaryDashboardController.cs"

groups = {
    "MilitaryDashboardController_Forces.cs": [
        "PopulateGlobalPower", "PopulateGenerals", "BindCommanderData",
        "SetPosture", "UpdatePostureButtonStyles", "UpdateDoctrineText", "PopulateBranches"
    ],
    "MilitaryDashboardController_Recruit.cs": [
        "PopulateRecruit", "AddRecruitCard", "RecruitUnit", "DecommissionUnit", "RecruitGeneral", "PopulatePolice"
    ],
    "MilitaryDashboardController_Weapons.cs": [
        "PopulateWMD", "AddWeaponCard", "ShowPurchaseModal"
    ],
    "MilitaryDashboardController_Operations.cs": [
        "PopulateOperations", "PopulateTheatre", "PopulateBlockades", "ShowLaunchOpModal", 
        "ShowReinforceModal", "ShowInboxModal", "PopulateThreatList", "UpdateIntelAssessment",
        "PopulateActiveOpsList", "PopulateOperationsFeed", "PopulateOpTypesGrid", "DeployOperation"
    ],
    "MilitaryDashboardController_Logistics.cs": [
        "PopulateSupplyRoutes", "AddSupplyRoute", "BuyLogistics"
    ],
    "MilitaryDashboardController_Budget.cs": [
        "PopulateBudget", "AddBudgetRow"
    ],
    "MilitaryDashboardController_Projects.cs": [
        "PopulateProjects"
    ],
    "MilitaryDashboardController_UIHelpers.cs": [
        "AddStat", "SetBorderWidth", "SetBorderColor", "UpdateFactorRow", "AddImpactStat", "MakeEffectTag",
        "OpTypePrefix", "OpTypeAccentColour", "ProgressFillClass", "FeedDotClass"
    ]
}

with open(file_path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

def extract_methods(lines, method_names):
    extracted = []
    new_lines = []
    
    in_method = False
    brace_count = 0
    method_buffer = []
    
    i = 0
    while i < len(lines):
        line = lines[i]
        
        if not in_method:
            match = False
            for name in method_names:
                # Look for typical method definition signatures
                pattern = r'\b(void|int|float|bool|string|VisualElement|Color|Label|Button)\s+' + name + r'\s*\('
                if re.search(pattern, line) and not line.strip().startswith('//'):
                    match = True
                    break
                    
            if match:
                in_method = True
                brace_count = 0
                method_buffer = []
                
                j = i - 1
                prepend = []
                while j >= 0 and (lines[j].strip().startswith('//') or lines[j].strip().startswith('[')):
                    if new_lines:
                        prepend.insert(0, new_lines.pop())
                    j -= 1
                
                method_buffer.extend(prepend)
                method_buffer.append(line)
                brace_count += line.count('{') - line.count('}')
            else:
                new_lines.append(line)
        else:
            method_buffer.append(line)
            brace_count += line.count('{') - line.count('}')
            
            if brace_count <= 0: # method ended
                extracted.extend(method_buffer)
                extracted.append("\n")
                in_method = False
                brace_count = 0
                
        i += 1
        
    return new_lines, extracted

original_lines = lines[:]
for new_file, methods in groups.items():
    original_lines, extracted_content = extract_methods(original_lines, methods)
    
    if extracted_content:
        header = [
            "using UnityEngine;\n",
            "using UnityEngine.UIElements;\n",
            "using System.Collections.Generic;\n",
            "using System.Linq;\n",
            "using StateCraft.Core;\n",
            "using StateCraft.UI;\n",
            "\n",
            "namespace StateCraft.UI\n",
            "{\n",
            "    public partial class MilitaryDashboardController\n",
            "    {\n"
        ]
        
        footer = [
            "    }\n",
            "}\n"
        ]
        
        with open(new_file, 'w', encoding='utf-8') as f:
            f.writelines(header)
            f.writelines(extracted_content)
            f.writelines(footer)

for i, line in enumerate(original_lines):
    if "public class MilitaryDashboardController" in line:
        original_lines[i] = line.replace("public class MilitaryDashboardController", "public partial class MilitaryDashboardController")
        break

with open(file_path, 'w', encoding='utf-8') as f:
    f.writelines(original_lines)

print("Split completed successfully.")
