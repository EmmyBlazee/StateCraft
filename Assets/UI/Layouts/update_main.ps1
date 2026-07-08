$source = "MilitaryDashboard.uxml"
$lines = Get-Content $source

$templates = @(
    '    <ui:Template name="Template_Forces" src="project://database/Assets/UI/Layouts/MilitaryDashboard_Forces.uxml" />',
    '    <ui:Template name="Template_Recruit" src="project://database/Assets/UI/Layouts/MilitaryDashboard_Recruit.uxml" />',
    '    <ui:Template name="Template_Weapons" src="project://database/Assets/UI/Layouts/MilitaryDashboard_Weapons.uxml" />',
    '    <ui:Template name="Template_Bases" src="project://database/Assets/UI/Layouts/MilitaryDashboard_Bases.uxml" />',
    '    <ui:Template name="Template_Operations" src="project://database/Assets/UI/Layouts/MilitaryDashboard_Operations.uxml" />',
    '    <ui:Template name="Template_Logistics" src="project://database/Assets/UI/Layouts/MilitaryDashboard_Logistics.uxml" />',
    '    <ui:Template name="Template_Budget" src="project://database/Assets/UI/Layouts/MilitaryDashboard_Budget.uxml" />',
    '    <ui:Template name="Template_Projects" src="project://database/Assets/UI/Layouts/MilitaryDashboard_Projects.uxml" />'
)

$instances = @(
    '            <!-- 1. FORCES TAB  (visible by default) -->',
    '            <ui:Instance template="Template_Forces" name="PanelForcesInstance" />',
    '',
    '            <!-- 2. RECRUITMENT TAB -->',
    '            <ui:Instance template="Template_Recruit" name="PanelRecruitInstance" />',
    '',
    '            <!-- 3. WEAPONS TAB -->',
    '            <ui:Instance template="Template_Weapons" name="PanelWeaponsInstance" />',
    '',
    '            <!-- 4. BASES TAB -->',
    '            <ui:Instance template="Template_Bases" name="PanelBasesInstance" />',
    '',
    '            <!-- 5. OPERATIONS TAB -->',
    '            <ui:Instance template="Template_Operations" name="PanelOperationsInstance" />',
    '',
    '            <!-- 6. LOGISTICS TAB -->',
    '            <ui:Instance template="Template_Logistics" name="PanelLogisticsInstance" />',
    '',
    '            <!-- 7. BUDGET TAB -->',
    '            <ui:Instance template="Template_Budget" name="PanelBudgetInstance" />',
    '',
    '            <!-- 8. PROJECTS TAB -->',
    '            <ui:Instance template="Template_Projects" name="PanelProjectsInstance" />'
)

$newLines = @()
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($i -eq 2) {
        $newLines += $templates
    }
    
    if ($i -ge 80 -and $i -le 1184) {
        if ($i -eq 80) {
            $newLines += $instances
        }
    } else {
        $newLines += $lines[$i]
    }
}

Set-Content -Path $source -Value $newLines -Encoding UTF8
Write-Host "Replacement complete!"
