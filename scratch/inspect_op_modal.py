import codecs
import re

s = codecs.open(r'C:\Users\Tobi\StateCraft\Assets\Scripts\UI\MilitaryDashboardController.cs', 'r', 'utf-8').read()
match = re.search(r'(private void ShowLaunchOpModal.*?\n {4}\})', s, re.DOTALL)
if match:
    print(match.group(1))
else:
    print("Method not found")
