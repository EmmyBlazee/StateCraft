import codecs
import re

# 1. Update OperationsTab.uss
uss_path = r"C:\Users\Tobi\StateCraft\Assets\UI\Styles\OperationsTab.uss"
with codecs.open(uss_path, "r", "utf-8") as f:
    uss = f.read()

# remove row-gap and column-gap
uss = uss.replace("column-gap: 10px;", "")
uss = uss.replace("row-gap: 10px;", "")

# Add margins to columns to replace column-gap
uss = uss.replace(".ops-col--left   { width: 260px; flex-shrink: 0; }", ".ops-col--left   { width: 260px; flex-shrink: 0; margin-right: 10px; }")
uss = uss.replace(".ops-col--center { flex-grow: 1; }", ".ops-col--center { flex-grow: 1; margin-right: 10px; }")

# Add margins to panels to replace row-gap
panel_def = """.ops-panel {
    background-color: #0d1117;
    border-width: 1px;
    border-color: #1e2d3d;
    border-radius: 4px;
    padding: 12px;
    flex-direction: column;
}"""
new_panel_def = """.ops-panel {
    background-color: #0d1117;
    border-width: 1px;
    border-color: #1e2d3d;
    border-radius: 4px;
    padding: 12px;
    flex-direction: column;
    margin-bottom: 10px;
}"""
uss = uss.replace(panel_def, new_panel_def)

# Also fix the op-types-grid flex wrap gaps (which were probably meant to have margins)
# The user's code had margin-right: 8px; margin-bottom: 8px; on .op-type-card which handles limits there.

with codecs.open(uss_path, "w", "utf-8") as f:
    f.write(uss)


# 2. Update MilitaryDashboard.uxml
uxml_path = r"C:\Users\Tobi\StateCraft\Assets\UI\Layouts\MilitaryDashboard.uxml"
with codecs.open(uxml_path, "r", "utf-8") as f:
    uxml = f.read()

# Force the style GUID so Unity doesn't ignore it
old_style = '<Style src="project://database/Assets/UI/Styles/OperationsTab.uss" />'
new_style = '<Style src="project://database/Assets/UI/Styles/OperationsTab.uss?fileID=7433441132597879392&amp;guid=e64d0f666206bb0499657bbf1b3aade5&amp;type=3" />'
uxml = uxml.replace(old_style, new_style)

with codecs.open(uxml_path, "w", "utf-8") as f:
    f.write(uxml)


# 3. Update C# file newline fix
cs_path = r"C:\Users\Tobi\StateCraft\Assets\Scripts\UI\MilitaryDashboardController.cs"
with codecs.open(cs_path, "r", "utf-8") as f:
    cs = f.read()

# Replace \\n with \n in literal
cs = cs.replace('var successLbl = new Label("SUCCESS\\\\nCHANCE");', 'var successLbl = new Label("SUCCESS\\nCHANCE");')
with codecs.open(cs_path, "w", "utf-8") as f:
    f.write(cs)

print("Applied fixes to USS, UXML, and CS")
