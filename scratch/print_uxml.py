with open(r'c:\Users\Tobi\StateCraft\Assets\UI\Layouts\IntelligenceDashboard.uxml', 'r', encoding='utf-8', errors='ignore') as f:
    text = f.read()

start = text.find('<ui:VisualElement name="PanelSurveillance"')
print(text[start:start+1500])
