import re
with open(r'c:\Users\Tobi\StateCraft\Assets\UI\Layouts\IntelligenceDashboard.uxml', 'r', encoding='utf-8') as f:
    text = f.read()
matches = re.findall(r'name=\"(Panel[A-Za-z0-9]+)\"', text)
print(matches)
