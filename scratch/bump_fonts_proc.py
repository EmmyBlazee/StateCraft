import sys
import re

filepath = r"c:\Users\Tobi\StateCraft\Assets\Scripts\UI\MilitaryDashboardController_Weapons.cs"
with open(filepath, "r", encoding="utf-8") as f:
    content = f.read()

# 1. AddOffer (Procurement)
content = content.replace('titleLbl.style.fontSize = 10;', 'titleLbl.style.fontSize = 13;')
content = content.replace('subLblText.style.fontSize = 7;', 'subLblText.style.fontSize = 10;')
content = content.replace('tl.style.fontSize = 7;', 'tl.style.fontSize = 10;') # tags
content = content.replace('supL1.style.fontSize = 7;', 'supL1.style.fontSize = 10;')
content = content.replace('supL2.style.fontSize = 7;', 'supL2.style.fontSize = 10;')
content = content.replace('ctryL1.style.fontSize = 7;', 'ctryL1.style.fontSize = 10;')
content = content.replace('ctryL2.style.fontSize = 7;', 'ctryL2.style.fontSize = 10;')
content = content.replace('sc1l1.style.fontSize = 6;', 'sc1l1.style.fontSize = 9;')
content = content.replace('sc1l2.style.fontSize = 10;', 'sc1l2.style.fontSize = 13;')
content = content.replace('sc2l1.style.fontSize = 6;', 'sc2l1.style.fontSize = 9;')
content = content.replace('sc2l2.style.fontSize = 10;', 'sc2l2.style.fontSize = 13;')
content = content.replace('sc3l1.style.fontSize = 6;', 'sc3l1.style.fontSize = 9;')
content = content.replace('sc3l2.style.fontSize = 12;', 'sc3l2.style.fontSize = 15;')
content = content.replace('sc4l1.style.fontSize = 6;', 'sc4l1.style.fontSize = 9;')
content = content.replace('sc4l2.style.fontSize = 9;', 'sc4l2.style.fontSize = 12;')
content = content.replace('negBtn.style.fontSize = 8;', 'negBtn.style.fontSize = 11;')

# 2. AddNegItem (Active Negotiations)
content = content.replace('flagLbl2.style.fontSize = 14;', 'flagLbl2.style.fontSize = 16;')
content = content.replace('nTit.style.fontSize = 8;', 'nTit.style.fontSize = 11;')
content = content.replace('nSub.style.fontSize = 6;', 'nSub.style.fontSize = 9;')
content = content.replace('nVal.style.fontSize = 9;', 'nVal.style.fontSize = 12;')
content = content.replace('nStat.style.fontSize = 6;', 'nStat.style.fontSize = 9;')

# 3. AddFilterDropdown and AddFilterSection
content = content.replace('fTit.style.fontSize = 7;', 'fTit.style.fontSize = 10;')
content = content.replace('fVal.style.fontSize = 8;', 'fVal.style.fontSize = 11;')
content = content.replace('fArr.style.fontSize = 8;', 'fArr.style.fontSize = 11;')
content = content.replace('ft.style.fontSize = 7;', 'ft.style.fontSize = 10;')

# 4. Headers in Procurement
content = content.replace('anLbl.style.fontSize = 9;', 'anLbl.style.fontSize = 12;') # ACTIVE NEGOTIATIONS
content = content.replace('vaLbl.style.fontSize = 8;', 'vaLbl.style.fontSize = 11;') # View All
content = content.replace('filtersHeader.style.fontSize = 9;', 'filtersHeader.style.fontSize = 12;') # FILTER BY
content = content.replace('clearFilters.style.fontSize = 8;', 'clearFilters.style.fontSize = 11;') # Clear All
content = content.replace('offersHeader.style.fontSize = 9;', 'offersHeader.style.fontSize = 12;') # AVAILABLE OFFERS

with open(filepath, "w", encoding="utf-8") as f:
    f.write(content)

print("Procurement tab font sizes increased.")
