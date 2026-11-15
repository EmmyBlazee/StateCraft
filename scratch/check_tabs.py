import re
with open(r'c:\Users\Tobi\StateCraft\Assets\UI\Layouts\IntelligenceDashboard.uxml', 'r', encoding='utf-8') as f:
    text = f.read()

print('PanelSurveillance:', len(re.findall(r'name="PanelSurveillance"', text)))
print('PanelCovertOps:', len(re.findall(r'name="PanelCovertOps"', text)))
print('PanelDataAnalysis:', len(re.findall(r'name="PanelDataAnalysis"', text)))
print('PanelThreatAssessment:', len(re.findall(r'name="PanelThreatAssessment"', text)))
print('TabRecon:', len(re.findall(r'name="TabRecon"', text)))
print('TabCovertOps:', len(re.findall(r'name="TabCovertOps"', text)))
print('TabOverview:', len(re.findall(r'name="TabOverview"', text)))

start = text.find('name="PanelSurveillance"')
end = text.find('name="PanelCovertOps"')
print(start, end)
