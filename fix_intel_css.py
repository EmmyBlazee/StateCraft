import codecs

path = r'C:\Users\Tobi\StateCraft\Assets\UI\Styles\Global.uss'
s = codecs.open(path, 'r', 'utf-8').read()

# 1. Fix intel-key: use exact width so it never pushes the value off-screen
s = s.replace(
    '.intel-key {\r\n    font-size: 10px;\r\n    color: #6b8099;\r\n    flex-shrink: 0;\r\n    min-width: 90px;\r\n}',
    '.intel-key {\r\n    font-size: 9px;\r\n    color: #6b8099;\r\n    flex-shrink: 0;\r\n    width: 70px;\r\n}'
)

# 2. Make intel-val properly right-aligned without wrapping
s = s.replace(
    '.intel-val {\r\n    font-size: 10px;\r\n    color: #e0e8f0;\r\n    -unity-font-style: bold;\r\n    -unity-text-align: middle-right;\r\n    flex-grow: 1;\r\n}',
    '.intel-val {\r\n    font-size: 9px;\r\n    color: #e0e8f0;\r\n    -unity-font-style: bold;\r\n    -unity-text-align: middle-right;\r\n    flex-grow: 1;\r\n    overflow: hidden;\r\n}'
)

# 3. Fix intel-row: add align-items center + tighter margin
old_row = '.intel-row {\r\n    flex-direction: row;\r\n    justify-content: space-between;\r\n    align-items: center;\r\n    min-height: 18px;\r\n    margin-bottom: 6px;\r\n}'
new_row = '.intel-row {\r\n    flex-direction: row;\r\n    justify-content: space-between;\r\n    align-items: center;\r\n    min-height: 16px;\r\n    margin-bottom: 5px;\r\n    flex-shrink: 0;\r\n}'
s = s.replace(old_row, new_row)

# 4. Style readiness icon as a letter badge
s = s.replace(
    '.readiness-row__icon {\r\n    font-size: 14px;\r\n    width: 20px;\r\n    color: #6b8099;\r\n    margin-right: 8px;\r\n    flex-shrink: 0;\r\n}',
    '.readiness-row__icon {\r\n    font-size: 9px;\r\n    width: 22px;\r\n    height: 22px;\r\n    border-radius: 11px;\r\n    background-color: rgba(0, 168, 255, 0.15);\r\n    border-width: 1px;\r\n    border-color: rgba(0, 168, 255, 0.4);\r\n    color: #00a8ff;\r\n    -unity-font-style: bold;\r\n    -unity-text-align: middle-center;\r\n    margin-right: 8px;\r\n    flex-shrink: 0;\r\n}'
)

# 5. Fix ops-panel__title - make it slightly smaller to save space
s = s.replace(
    '.ops-panel__title {\r\n    font-size: 11px;\r\n    -unity-font-style: bold;\r\n    color: #e0e8f0;\r\n    letter-spacing: 1px;\r\n    margin-bottom: 10px;\r\n}',
    '.ops-panel__title {\r\n    font-size: 10px;\r\n    -unity-font-style: bold;\r\n    color: #e0e8f0;\r\n    letter-spacing: 1px;\r\n    margin-bottom: 8px;\r\n    flex-shrink: 0;\r\n}'
)

codecs.open(path, 'w', 'utf-8').write(s)
print("Done")
