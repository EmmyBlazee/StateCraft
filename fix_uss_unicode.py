import codecs
import re

uss_path = r"C:\Users\Tobi\StateCraft\Assets\UI\Styles\OperationsTab.uss"
with codecs.open(uss_path, "r", "utf-8") as f:
    uss = f.read()

# 1. Remove all the ═══ lines and block comments entirely to avoid Unity parser choking
uss = re.sub(r'/\* ═+.*?\*/', '', uss, flags=re.DOTALL)
uss = re.sub(r'═+', '', uss)
uss = re.sub(r'/\* ──.*?──────────────────────────────────────────── \*/', '', uss)

# 2. Remove all unsupported cursor styles
uss = uss.replace('cursor: link;', '')

with codecs.open(uss_path, "w", "utf-8") as f:
    f.write(uss)

# Let's also enforce UXML Style link back to just the file path, sometimes Unity prefers that with external changes
uxml_path = r"C:\Users\Tobi\StateCraft\Assets\UI\Layouts\MilitaryDashboard.uxml"
with codecs.open(uxml_path, "r", "utf-8") as f:
    uxml = f.read()

# Strip any query parameters just directly linked
uxml = re.sub(r'<Style src="project://database/Assets/UI/Styles/OperationsTab\.uss\?[^"]+" />', '<Style src="project://database/Assets/UI/Styles/OperationsTab.uss" />', uxml)

with codecs.open(uxml_path, "w", "utf-8") as f:
    f.write(uxml)
