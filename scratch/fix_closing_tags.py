import re

with open(r'c:\Users\Tobi\StateCraft\Assets\UI\Layouts\IntelligenceDashboard.uxml', 'r', encoding='utf-8') as f:
    text = f.read()

target = '<!-- 3. COVERT OPS TAB (renamed to INTELLIGENCE NETWORK in UI, kept PanelCovertOps ID to avoid breaking code) -->'
replacement = '''</ui:VisualElement>
            </ui:VisualElement>
            </ui:VisualElement>
            </ui:VisualElement>
            
            ''' + target

new_text = text.replace(target, replacement)

with open(r'c:\Users\Tobi\StateCraft\Assets\UI\Layouts\IntelligenceDashboard.uxml', 'w', encoding='utf-8') as f:
    f.write(new_text)

print("Added missing closing tags!")
