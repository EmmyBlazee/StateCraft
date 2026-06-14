import codecs

path = r'C:\Users\Tobi\StateCraft\Assets\UI\Styles\Global.uss'
s = codecs.open(path, 'r', 'utf-8').read()

# 1. Make op-type cards full-width (single column list, not 2-across grid)
s = s.replace(
    '.op-type-card {\r\n    width: 130px;',
    '.op-type-card {\r\n    width: 100%;'
)

# 2. Fix intel-row spacing  
s = s.replace(
    '.intel-row {\r\n    flex-direction: row;\r\n    justify-content: space-between;\r\n    margin-bottom: 5px;\r\n}',
    '.intel-row {\r\n    flex-direction: row;\r\n    justify-content: space-between;\r\n    align-items: center;\r\n    min-height: 18px;\r\n    margin-bottom: 6px;\r\n}'
)

# 3. Fix intel-key so it wraps cleanly
s = s.replace(
    '.intel-key {\r\n    font-size: 10px;\r\n    color: #6b8099;\r\n    flex-grow: 1;\r\n}',
    '.intel-key {\r\n    font-size: 10px;\r\n    color: #6b8099;\r\n    flex-shrink: 0;\r\n    min-width: 90px;\r\n}'
)

# 4. Intel val right-aligned
s = s.replace(
    '.intel-val {\r\n    font-size: 10px;\r\n    color: #e0e8f0;\r\n    -unity-font-style: bold;\r\n}',
    '.intel-val {\r\n    font-size: 10px;\r\n    color: #e0e8f0;\r\n    -unity-font-style: bold;\r\n    -unity-text-align: middle-right;\r\n    flex-grow: 1;\r\n}'
)

# 5. Also fix op-types-grid - no longer wrapping since cards are full width
s = s.replace(
    '.op-types-grid {\r\n    flex-direction: row;\r\n    flex-wrap: wrap;\r\n    align-content: flex-start;\r\n}',
    '.op-types-grid {\r\n    flex-direction: column;\r\n}'
)

codecs.open(path, 'w', 'utf-8').write(s)
print("Done")
