with open(r'c:\Users\Tobi\StateCraft\Assets\UI\Layouts\IntelligenceDashboard.uxml', 'r', encoding='utf-8', errors='ignore') as f:
    text = f.read()

start = text.find('<ui:VisualElement name="PanelSurveillance"')
if start == -1: exit(1)
end = text.find('<ui:VisualElement name="PanelCovertOps"', start)
if end == -1: exit(1)

with open(r'c:\Users\Tobi\StateCraft\scratch\recon_tab_content.xml', 'w', encoding='utf-8') as f:
    f.write(text[start:end])
