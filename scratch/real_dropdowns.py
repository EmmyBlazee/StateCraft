import re

with open(r'c:\Users\Tobi\StateCraft\Assets\UI\Layouts\IntelligenceDashboard.uxml', 'r', encoding='utf-8') as f:
    text = f.read()

# Replace Mission Type
target1 = '''<ui:VisualElement style="min-height: 24px; align-items: center; background-color: rgba(1,1,1,0.2); border-width: 1px; border-color: rgba(255,255,255,0.1); padding: 4px 8px; margin-bottom: 12px; border-radius: 2px; flex-direction: row; justify-content: space-between;">
                                <ui:Label text="Select Mission Type" style="font-size: 12px; color: grey;" />
                                <ui:Label text="▼" style="font-size: 11px; color: grey;" />
                            </ui:VisualElement>'''
rep1 = '''<ui:DropdownField choices="Aerial Recon,Satellite Scan,SIGINT Intercept,HUMINT Network,Cyber Probe" value="Aerial Recon" style="min-height: 24px; background-color: rgba(1,1,1,0.2); border-width: 1px; border-color: rgba(255,255,255,0.1); margin-bottom: 12px; border-radius: 2px; font-size: 12px; color: grey;" class="custom-dropdown" />'''
text = text.replace(target1, rep1)

# Replace Area of Interest
target2 = '''<ui:VisualElement style="min-height: 24px; align-items: center; background-color: rgba(1,1,1,0.2); border-width: 1px; border-color: rgba(255,255,255,0.1); padding: 4px 8px; margin-bottom: 12px; border-radius: 2px; flex-direction: row; justify-content: space-between;">
                                <ui:Label text="Select Area" style="font-size: 12px; color: grey;" />
                                <ui:Label text="▼" style="font-size: 11px; color: grey;" />
                            </ui:VisualElement>'''
rep2 = '''<ui:DropdownField choices="Eastern Border,Capital City,Naval Base Alpha,Industrial Sector,Global Network" value="Eastern Border" style="min-height: 24px; background-color: rgba(1,1,1,0.2); border-width: 1px; border-color: rgba(255,255,255,0.1); margin-bottom: 12px; border-radius: 2px; font-size: 12px; color: grey;" class="custom-dropdown" />'''
text = text.replace(target2, rep2)

# Replace Priority and Asset
target3 = '''<ui:VisualElement style="flex-direction: row; justify-content: space-between; margin-bottom: 8px;">
                                <ui:VisualElement style="width: 48%;">
                                    <ui:Label text="Priority" style="font-size: 11px; color: grey; margin-bottom: 2px;" />
                                    <ui:Label text="Medium ▼" style="font-size: 12px; color: rgb(237, 180, 35);" />
                                </ui:VisualElement>
                                <ui:VisualElement style="width: 48%;">
                                    <ui:Label text="Assigned Asset" style="font-size: 11px; color: grey; margin-bottom: 2px;" />
                                    <ui:Label text="Auto Assign ▼" style="font-size: 12px; color: grey;" />
                                </ui:VisualElement>
                            </ui:VisualElement>'''

rep3 = '''<ui:VisualElement style="flex-direction: row; justify-content: space-between; margin-bottom: 8px;">
                                <ui:VisualElement style="width: 48%;">
                                    <ui:Label text="Priority" style="font-size: 11px; color: grey; margin-bottom: 2px;" />
                                    <ui:DropdownField choices="Low,Medium,High,Critical" value="Medium" style="min-height: 24px; background-color: rgba(1,1,1,0.2); border-width: 1px; border-color: rgba(255,255,255,0.1); border-radius: 2px; font-size: 12px; color: rgb(237, 180, 35);" class="custom-dropdown" />
                                </ui:VisualElement>
                                <ui:VisualElement style="width: 48%;">
                                    <ui:Label text="Assigned Asset" style="font-size: 11px; color: grey; margin-bottom: 2px;" />
                                    <ui:DropdownField choices="Auto Assign,Drone Sqn 1,Agent 'Viper',Satellite K-9" value="Auto Assign" style="min-height: 24px; background-color: rgba(1,1,1,0.2); border-width: 1px; border-color: rgba(255,255,255,0.1); border-radius: 2px; font-size: 12px; color: grey;" class="custom-dropdown" />
                                </ui:VisualElement>
                            </ui:VisualElement>'''

text = text.replace(target3, rep3)

with open(r'c:\Users\Tobi\StateCraft\Assets\UI\Layouts\IntelligenceDashboard.uxml', 'w', encoding='utf-8') as f:
    f.write(text)

print("Dropdowns replaced!")
