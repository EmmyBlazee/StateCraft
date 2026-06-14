import codecs
import re

uxml_path = r'C:\Users\Tobi\StateCraft\Assets\UI\Layouts\MilitaryDashboard.uxml'
uxml = codecs.open(uxml_path, 'r', 'utf-8').read()

# Shrink Header
uxml = uxml.replace(
    'margin-bottom: 20px; border-bottom-width: 1px; border-bottom-color: rgba(255, 82, 82, 0.3); padding-bottom: 15px;',
    'margin-bottom: 10px; border-bottom-width: 1px; border-bottom-color: rgba(255, 82, 82, 0.3); padding-bottom: 8px;'
)

# Shrink Sub-Tab Navigation
uxml = uxml.replace(
    'style="flex-direction: row; margin-bottom: 20px; background-color: rgba(0,0,0,0.4); border-radius: 8px; padding: 5px;"',
    'style="flex-direction: row; margin-bottom: 10px; background-color: rgba(0,0,0,0.4); border-radius: 6px; padding: 2px;"'
)

# Shrink gaps between panels in the layout (inline styles)
uxml = uxml.replace('margin-bottom: 10px;"', 'margin-bottom: 6px;"')
uxml = uxml.replace('margin-bottom: 8px;"', 'margin-bottom: 6px;"')

codecs.open(uxml_path, 'w', 'utf-8').write(uxml)

uss_path = r'C:\Users\Tobi\StateCraft\Assets\UI\Styles\Global.uss'
uss = codecs.open(uss_path, 'r', 'utf-8').read()

# Shrink Tab Buttons
uss = re.sub(r'(\.nav-button\s*\{[^}]*padding:\s*)10px\s*20px', r'\g<1>6px 12px', uss)
uss = re.sub(r'(\.nav-button\s*\{[^}]*font-size:\s*)12px', r'\g<1>10px', uss)

# Shrink Ops Panels
uss = uss.replace(
    '.ops-panel {\r\n    background-color: #0d1117;\r\n    border-width: 1px;\r\n    border-color: #1e2d3d;\r\n    border-radius: 4px;\r\n    padding: 12px;',
    '.ops-panel {\r\n    background-color: #0d1117;\r\n    border-width: 1px;\r\n    border-color: #1e2d3d;\r\n    border-radius: 4px;\r\n    padding: 8px;'
)
uss = uss.replace(
    '.ops-panel__title {\r\n    font-size: 10px;\r\n    -unity-font-style: bold;\r\n    color: #e0e8f0;\r\n    letter-spacing: 1px;\r\n    margin-bottom: 8px;\r\n    flex-shrink: 0;\r\n}',
    '.ops-panel__title {\r\n    font-size: 10px;\r\n    -unity-font-style: bold;\r\n    color: #e0e8f0;\r\n    letter-spacing: 0.5px;\r\n    margin-bottom: 4px;\r\n    flex-shrink: 0;\r\n}'
)

# Shrink Op Type Cards
uss = re.sub(r'(\.op-type-card\s*\{[^}]*padding:\s*)10px', r'\g<1>6px', uss)
uss = re.sub(r'(\.op-type-card\s*\{[^}]*margin-bottom:\s*)8px', r'\g<1>4px', uss)
uss = re.sub(r'(\.op-type-card__header\s*\{[^}]*margin-bottom:\s*)6px', r'\g<1>4px', uss)
uss = re.sub(r'(\.op-type-card__desc\s*\{[^}]*margin-bottom:\s*)6px', r'\g<1>4px', uss)

codecs.open(uss_path, 'w', 'utf-8').write(uss)

print("UI space significantly reduced!")
