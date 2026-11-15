import os, re

for root, _, files in os.walk('Assets/Scripts'):
    for file in files:
        if file.endswith('.cs'):
            path = os.path.join(root, file)
            enc = 'utf-8'
            try:
                with open(path, 'r', encoding='utf-8') as f:
                    content = f.read()
            except UnicodeDecodeError:
                enc = 'utf-16'
                with open(path, 'r', encoding='utf-16') as f:
                    content = f.read()
            
            new_content = re.sub(r'&#x([0-9a-fA-F]{4});', r'\\u\1', content, flags=re.IGNORECASE)
            
            if new_content != content:
                with open(path, 'w', encoding=enc) as f:
                    f.write(new_content)
                print(f"Fixed {path}")
