import codecs
import glob

# Files to aggressively shrink. We exclude MilitaryDashboard since it's already done, but adding it won't hurt.
pattern = r'C:/Users/Tobi/StateCraft/Assets/UI/Layouts/*Dashboard.uxml'
files = glob.glob(pattern)

def compress_uxml(filepath):
    print(f"Compressing {filepath}...")
    try:
        uxml = codecs.open(filepath, 'r', 'utf-8').read()
        
        # Aggressive inline squish: Reduce margins and padding globally
        # We try to shrink everything excessive safely
        
        # Margins
        uxml = uxml.replace("margin-bottom: 30px", "margin-bottom: 12px")
        uxml = uxml.replace("margin-bottom: 25px", "margin-bottom: 10px")
        uxml = uxml.replace("margin-bottom: 20px", "margin-bottom: 8px")
        uxml = uxml.replace("margin-bottom: 15px", "margin-bottom: 8px")
        uxml = uxml.replace("margin-bottom: 10px", "margin-bottom: 6px")
        
        uxml = uxml.replace("margin-top: 30px", "margin-top: 12px")
        uxml = uxml.replace("margin-top: 25px", "margin-top: 10px")
        uxml = uxml.replace("margin-top: 20px", "margin-top: 8px")
        uxml = uxml.replace("margin-top: 15px", "margin-top: 8px")
        uxml = uxml.replace("margin-top: 10px", "margin-top: 6px")
        
        # Paddings
        uxml = uxml.replace("padding: 30px", "padding: 12px")
        uxml = uxml.replace("padding: 25px", "padding: 10px")
        uxml = uxml.replace("padding: 20px", "padding: 8px")
        uxml = uxml.replace("padding: 15px", "padding: 8px")
        uxml = uxml.replace("padding: 12px", "padding: 6px")
        
        # Specific structural adjustments that are common
        uxml = uxml.replace("padding-bottom: 15px", "padding-bottom: 8px")
        uxml = uxml.replace("padding-bottom: 20px", "padding-bottom: 8px")
        uxml = uxml.replace("padding-top: 15px", "padding-top: 8px")
        uxml = uxml.replace("padding-top: 20px", "padding-top: 8px")
        
        codecs.open(filepath, 'w', 'utf-8').write(uxml)
    except Exception as e:
        print(f"Failed to process {filepath}: {e}")

for f in files:
    compress_uxml(f)

print("Global Dashboard compression complete!")
