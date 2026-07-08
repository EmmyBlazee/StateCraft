import sys
import re

filepath = r"c:\Users\Tobi\StateCraft\Assets\Scripts\UI\MilitaryDashboardController_Weapons.cs"
with open(filepath, "r", encoding="utf-8") as f:
    content = f.read()

# We need to increase font sizes in several specific methods for the Production tab.

# 1. AddOverviewCard (Top 4 cards in Production & other places maybe? Yes, Procurement uses it too, but we can bump it everywhere since it's probably small everywhere)
content = content.replace('titleLbl.style.fontSize = 8;', 'titleLbl.style.fontSize = 11;')
content = content.replace('subLbl.style.fontSize   = 6;', 'subLbl.style.fontSize   = 9;')
content = content.replace('valLbl.style.fontSize   = 14;', 'valLbl.style.fontSize   = 18;')

# 2. AddProductionFacilityCard
content = content.replace('titleLbl.style.fontSize = 9;', 'titleLbl.style.fontSize = 12;')
content = content.replace('unitTypeLbl.style.fontSize = 8;', 'unitTypeLbl.style.fontSize = 11;')
content = content.replace('qtyHeader.style.fontSize = 7;', 'qtyHeader.style.fontSize = 10;')
content = content.replace('qtyLbl.style.fontSize = 11;', 'qtyLbl.style.fontSize = 15;')
content = content.replace('progHeader.style.fontSize = 7;', 'progHeader.style.fontSize = 10;')
content = content.replace('pctLbl.style.fontSize = 10;', 'pctLbl.style.fontSize = 14;')
content = content.replace('etaHeader.style.fontSize = 7;', 'etaHeader.style.fontSize = 10;')
content = content.replace('etaLbl.style.fontSize = 8;', 'etaLbl.style.fontSize = 11;')
content = content.replace('outHeader.style.fontSize = 7;', 'outHeader.style.fontSize = 10;')
content = content.replace('outLbl.style.fontSize = 8;', 'outLbl.style.fontSize = 11;')
content = content.replace('btn.style.fontSize = 7;', 'btn.style.fontSize = 10;')

# 3. AddNewOrderForm (Right sidebar)
content = content.replace('catLbl.style.fontSize = 6;', 'catLbl.style.fontSize = 9;')
content = content.replace('itemLbl.style.fontSize = 7;', 'itemLbl.style.fontSize = 10;')
content = content.replace('qtyLbl.style.fontSize = 6;', 'qtyLbl.style.fontSize = 9;')
# qtyField font size is implicitly set by Unity or we can add it? Let's leave textfield default for now, or bump it if we see it.
content = content.replace('orderBtn.style.fontSize = 7;', 'orderBtn.style.fontSize = 10;')

# 4. View Queue button
content = content.replace('viewQueueBtn.style.fontSize        = 9;', 'viewQueueBtn.style.fontSize        = 12;')

# 5. Section Labels (like "ACTIVE PRODUCTION", "PRODUCTION OVERVIEW", "NEW PRODUCTION ORDER")
content = content.replace('lbl.style.fontSize = 9;', 'lbl.style.fontSize = 12;')

# 6. RenderCompletedPage (Recently Completed)
# The completed names use 'nameLbl.style.fontSize = 9;' (already replaced above)
content = content.replace('qtyLbl.style.fontSize = 8;', 'qtyLbl.style.fontSize = 11;')
content = content.replace('statusLbl.style.fontSize = 8;', 'statusLbl.style.fontSize = 11;')
# prev/next arrow buttons
content = content.replace('prevArrow.style.fontSize          = 14;', 'prevArrow.style.fontSize          = 18;')
content = content.replace('nextArrow.style.fontSize        = 14;', 'nextArrow.style.fontSize        = 18;')

# "No active production orders" label
content = content.replace('emptyLbl.style.fontSize  = 9;', 'emptyLbl.style.fontSize  = 12;')

with open(filepath, "w", encoding="utf-8") as f:
    f.write(content)

print("Font sizes increased.")
