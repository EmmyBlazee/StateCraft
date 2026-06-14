import codecs

path = r'C:\Users\Tobi\StateCraft\Assets\UI\Layouts\MilitaryDashboard.uxml'
s = codecs.open(path, 'r', 'utf-8').read()
s = s.replace('<ui:VisualElement name="panel-intel" class="ops-panel">', '<ui:VisualElement name="panel-intel" class="ops-panel" style="flex-shrink: 0;">')
codecs.open(path, 'w', 'utf-8').write(s)

path2 = r'C:\Users\Tobi\StateCraft\Assets\Scripts\UI\MilitaryDashboardController.cs'
s2 = codecs.open(path2, 'r', 'utf-8').read()
s2 = s2.replace('"[CT]"', '"[CT] "')
s2 = s2.replace('"[PK]"', '"[PK] "')
s2 = s2.replace('"[BP]"', '"[BP] "')
s2 = s2.replace('"[AR]"', '"[AR] "')
s2 = s2.replace('"[MR]"', '"[MR] "')
s2 = s2.replace('"[HV]"', '"[HV] "')
s2 = s2.replace('"[?]"', '"[?] "')
codecs.open(path2, 'w', 'utf-8').write(s2)

print("Done")
