import codecs
import re

uxml_path = r'C:\Users\Tobi\StateCraft\Assets\UI\Layouts\MilitaryDashboard.uxml'
uxml = codecs.open(uxml_path, 'r', 'utf-8').read()

# Aggressive inline squish: Reduce margins and padding globally across all remaining tabs
uxml = uxml.replace("margin-bottom: 20px", "margin-bottom: 8px")
uxml = uxml.replace("margin-bottom: 15px", "margin-bottom: 8px")
uxml = uxml.replace("margin-bottom: 10px", "margin-bottom: 6px")

uxml = uxml.replace("margin-top: 20px", "margin-top: 8px")
uxml = uxml.replace("margin-top: 15px", "margin-top: 8px")
uxml = uxml.replace("margin-top: 10px", "margin-top: 6px")

uxml = uxml.replace("padding: 20px", "padding: 8px")
uxml = uxml.replace("padding: 15px", "padding: 8px")
uxml = uxml.replace("padding: 12px", "padding: 6px")

# Also shrink some font sizes that define large header areas 
# (the titles on top of boxes)
uxml = uxml.replace("font-size: 14px", "font-size: 12px")
uxml = uxml.replace("font-size: 12px", "font-size: 11px")

codecs.open(uxml_path, 'w', 'utf-8').write(uxml)

# And double check the style file for massive padding
uss_path = r'C:\Users\Tobi\StateCraft\Assets\UI\Styles\Global.uss'
uss = codecs.open(uss_path, 'r', 'utf-8').read()
uss = re.sub(r'(\.sub-panel\s*\{[^}]*margin-bottom:\s*)15px', r'\g<1>6px', uss)
uss = re.sub(r'(\.sub-panel\s*\{[^}]*padding:\s*)15px', r'\g<1>8px', uss)
# Catch any generic card styles
uss = re.sub(r'(margin-bottom:\s*)(15px|20px)', r'\g<1>8px', uss)
codecs.open(uss_path, 'w', 'utf-8').write(uss)

print("Squished all other tabs!")
