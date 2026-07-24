import re
with open(r'c:\Users\Tobi\StateCraft\Assets\UI\Layouts\IntelligenceDashboard.uxml', 'r', encoding='utf-8') as f:
    text = f.read()

idx = text.find('name="PanelCovertOps"')
print(text[idx-200:idx+50])
