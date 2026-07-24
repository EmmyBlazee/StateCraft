import re
with open(r'c:\Users\Tobi\StateCraft\Assets\UI\Layouts\IntelligenceDashboard.uxml', 'r', encoding='utf-8') as f:
    text = f.read()

panels = ['PanelOverview', 'PanelCovertOps', 'PanelSurveillance', 'PanelCovertOperations', 'PanelDataAnalysis', 'PanelThreatBoard']

for p in panels:
    idx = text.find(f'name="{p}"')
    print(f'{p} starts at {idx}')

idx_surveillance = text.find('name="PanelSurveillance"')
idx_covert = text.find('name="PanelCovertOperations"')

if idx_surveillance != -1 and idx_covert != -1:
    print("\n--- Transition between Surveillance and CovertOperations ---")
    end_surveillance_tag = text.rfind('</ui:VisualElement>', idx_surveillance, idx_covert)
    print(f"Last closing tag before CovertOps is at: {end_surveillance_tag}")
    
    snippet = text[idx_covert-100:idx_covert+50]
    print(f"Snippet right before PanelCovertOperations: {snippet}")
