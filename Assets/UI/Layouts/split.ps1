$source = "MilitaryDashboard.uxml"
$lines = Get-Content $source

function Extract-Panel {
    param($start, $end, $outfile)
    $panelLines = $lines[($start-1)..($end-1)]
    $content = @(
        '<ui:UXML xmlns:ui="UnityEngine.UIElements" editor-extension-mode="False">',
        '    <Style src="project://database/Assets/UI/Styles/Global.uss" />'
    )
    $content += $panelLines
    $content += '</ui:UXML>'
    Set-Content -Path $outfile -Value $content -Encoding UTF8
}

Extract-Panel 88 543 "MilitaryDashboard_Forces.uxml"
Extract-Panel 546 1033 "MilitaryDashboard_Recruit.uxml"
Extract-Panel 1036 1043 "MilitaryDashboard_Weapons.uxml"
Extract-Panel 1046 1053 "MilitaryDashboard_Bases.uxml"
Extract-Panel 1056 1114 "MilitaryDashboard_Operations.uxml"
Extract-Panel 1117 1162 "MilitaryDashboard_Logistics.uxml"
Extract-Panel 1165 1171 "MilitaryDashboard_Budget.uxml"
Extract-Panel 1174 1185 "MilitaryDashboard_Projects.uxml"

Write-Host "Extraction complete!"
