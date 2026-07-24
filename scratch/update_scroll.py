import re

with open(r'c:\Users\Tobi\StateCraft\Assets\UI\Layouts\IntelligenceDashboard.uxml', 'r', encoding='utf-8', errors='ignore') as f:
    text = f.read()

# Make all ScrollViews scrollable by replacing vertical-scroller-visibility="Hidden" with "Auto"
new_text = re.sub(r'vertical-scroller-visibility="Hidden"', 'vertical-scroller-visibility="Auto"', text)

with open(r'c:\Users\Tobi\StateCraft\Assets\UI\Layouts\IntelligenceDashboard.uxml', 'w', encoding='utf-8') as f:
    f.write(new_text)

print("Updated ScrollViews!")
