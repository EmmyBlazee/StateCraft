import re

with open(r'c:\Users\Tobi\StateCraft\scratch\recon_tab_content.xml', 'r', encoding='utf-8') as f:
    text = f.read()

# Split into grids
middle_match = re.search(r'<!-- MIDDLE GRID \(3 COLUMNS\) -->\s*<ui:VisualElement style="flex-direction: row;.*?>(.*?)</ui:VisualElement>\s*<!-- BOTTOM GRID -->', text, re.DOTALL)
bottom_match = re.search(r'<!-- BOTTOM GRID -->\s*<ui:VisualElement style="flex-direction: row;.*?>(.*?)</ui:VisualElement>\s*<!-- BOTTOM STRIP:', text, re.DOTALL)
recent_match = re.search(r'<!-- BOTTOM STRIP: RECENT RECON ACTIVITY -->(.*?)</ui:VisualElement>\s*</ui:VisualElement>', text, re.DOTALL)

if not middle_match or not bottom_match or not recent_match:
    print("Failed to find grids!")
    exit(1)

middle_text = middle_match.group(1)
bottom_text = bottom_match.group(1)

assets_match = re.search(r'<!-- COL 1: SURVEILLANCE ASSETS -->(.*?)</ui:VisualElement>\s*<!-- COL 2', middle_text, re.DOTALL)
missions_match = re.search(r'<!-- COL 2: ACTIVE RECON MISSIONS -->(.*?)</ui:VisualElement>\s*<!-- COL 3', middle_text, re.DOTALL)
feed_match = re.search(r'<!-- COL 3: LIVE INTELLIGENCE FEED -->(.*?)$', middle_text, re.DOTALL)

sensor_match = re.search(r'<!-- SENSOR COVERAGE -->(.*?)</ui:VisualElement>\s*<!-- INTEL', bottom_text, re.DOTALL)
breakdown_match = re.search(r'<!-- INTEL COLLECTION BREAKDOWN -->(.*?)</ui:VisualElement>\s*<!-- TASK', bottom_text, re.DOTALL)
new_mission_match = re.search(r'<!-- TASK NEW RECON MISSION -->(.*?)</ui:VisualElement>\s*<!-- RECON', bottom_text, re.DOTALL)
analytics_match = re.search(r'<!-- RECON ANALYTICS -->(.*?)$', bottom_text, re.DOTALL)

assets = assets_match.group(1) + '</ui:VisualElement>'
assets = re.sub(r'width: 28%;', 'width: 48%;', assets)

missions = missions_match.group(1) + '</ui:VisualElement>'
missions = re.sub(r'width: 46%;', 'width: 48%; margin-right: 0;', missions)

feed = feed_match.group(1).rsplit('</ui:VisualElement>', 1)[0] + '</ui:VisualElement>'
feed = re.sub(r'flex-grow: 1;', 'width: 100%; margin-bottom: 12px;', feed, count=1)

sensor = sensor_match.group(1) + '</ui:VisualElement>'
sensor = re.sub(r'width: 22%;', 'width: 100%; margin-right: 0;', sensor)

breakdown = breakdown_match.group(1) + '</ui:VisualElement>'
breakdown = re.sub(r'width: 32%;', 'width: 58%; margin-right: 0;', breakdown)

new_mission = new_mission_match.group(1) + '</ui:VisualElement>'
new_mission = re.sub(r'width: 22%;', 'width: 38%;', new_mission)

analytics = analytics_match.group(1).rsplit('</ui:VisualElement>', 1)[0] + '</ui:VisualElement>'
analytics = re.sub(r'flex-grow: 1;', 'width: 100%; margin-bottom: 12px;', analytics, count=1)
analytics = re.sub(r'width: 22%;', 'width: 100%; margin-bottom: 12px;', analytics, count=1)

recent_text = '<!-- RECENT RECON ACTIVITY -->' + recent_match.group(1).rsplit('</ui:VisualElement>', 1)[0] + '</ui:VisualElement>'

new_layout = f'''<!-- 2-COLUMN LAYOUT -->
                <ui:VisualElement style="flex-direction: row; flex-grow: 1;">
                    
                    <!-- LEFT COLUMN (Main Content - 75%) -->
                    <ui:VisualElement style="width: 75%; padding-right: 12px; flex-direction: column;">
                        
                        <!-- ROW 1 -->
                        <ui:VisualElement style="flex-direction: row; min-height: 200px; margin-bottom: 12px; justify-content: space-between;">
                            <!-- COL 1: SURVEILLANCE ASSETS -->
{assets}
                            <!-- COL 2: ACTIVE RECON MISSIONS -->
{missions}
                        </ui:VisualElement>

                        <!-- ROW 2 -->
                        <ui:VisualElement style="flex-direction: row; min-height: 250px; margin-bottom: 12px; justify-content: space-between;">
                            <!-- TASK NEW RECON MISSION -->
{new_mission}
                            <!-- INTEL COLLECTION BREAKDOWN -->
{breakdown}
                        </ui:VisualElement>

                        <!-- ROW 3 -->
                        <ui:VisualElement class="sub-panel" style="padding: 12px; background-color: rgba(10, 14, 20, 0.95); border-radius: 4px; border-width: 1px; border-color: rgba(255,255,255,0.05);">
{recent_text}
                        </ui:VisualElement>
                    </ui:VisualElement>

                    <!-- RIGHT COLUMN (Sidebar - 25%) -->
                    <ui:VisualElement style="width: 25%; flex-direction: column;">
                        <!-- LIVE INTELLIGENCE FEED -->
{feed}
                        <!-- RECON ANALYTICS -->
{analytics}
                        <!-- SENSOR COVERAGE -->
{sensor}
                    </ui:VisualElement>

                </ui:VisualElement>'''

# Read original
with open(r'c:\Users\Tobi\StateCraft\Assets\UI\Layouts\IntelligenceDashboard.uxml', 'r', encoding='utf-8') as f:
    full_text = f.read()

new_content = re.sub(r'<!-- MIDDLE GRID \(3 COLUMNS\) -->.*?<!-- BOTTOM STRIP: RECENT RECON ACTIVITY -->.*?</ui:VisualElement>\s*</ui:VisualElement>', new_layout + r'\n            </ui:VisualElement>', full_text, flags=re.DOTALL)

with open(r'c:\Users\Tobi\StateCraft\Assets\UI\Layouts\IntelligenceDashboard.uxml', 'w', encoding='utf-8') as f:
    f.write(new_content)

print("Layout replaced completely!")
