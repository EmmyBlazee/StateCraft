import re
with open(r'c:\Users\Tobi\StateCraft\Assets\UI\Layouts\IntelligenceDashboard.uxml', 'r', encoding='utf-8') as f:
    text = f.read()
print('PanelSurveillance:', len(re.findall(r'name="PanelSurveillance"', text)))
print('PanelCovertOps:', len(re.findall(r'name="PanelCovertOps"', text)))
print('PanelCovertOperations:', len(re.findall(r'name="PanelCovertOperations"', text)))
print('PanelDataAnalysis:', len(re.findall(r'name="PanelDataAnalysis"', text)))
print('PanelThreatBoard:', len(re.findall(r'name="PanelThreatBoard"', text)))
print('PanelOverview:', len(re.findall(r'name="PanelOverview"', text)))
