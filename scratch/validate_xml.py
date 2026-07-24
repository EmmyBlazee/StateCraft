import xml.etree.ElementTree as ET

try:
    tree = ET.parse(r'c:\Users\Tobi\StateCraft\Assets\UI\Layouts\IntelligenceDashboard.uxml')
    print("XML is valid!")
except ET.ParseError as e:
    print(f"XML parsing failed: {e}")
