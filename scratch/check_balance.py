with open(r'c:\Users\Tobi\StateCraft\Assets\UI\Layouts\IntelligenceDashboard.uxml', 'r', encoding='utf-8') as f:
    text = f.read()

idx_surv = text.find('name="PanelSurveillance"')
idx_covert = text.find('name="PanelCovertOps"')

subtext = text[idx_surv:idx_covert]
opens = subtext.count('<ui:VisualElement')
closes = subtext.count('</ui:VisualElement>')

print(f"Opens: {opens}, Closes: {closes}")
if opens > closes:
    print(f"PanelSurveillance is NOT closed! It's missing {opens - closes} closing tags.")
elif opens == closes:
    print("It IS closed exactly.")
else:
    print("Too many closes.")
