import re

with open(r'c:\Users\Tobi\StateCraft\Assets\UI\Layouts\IntelligenceDashboard.uxml', 'r', encoding='utf-8') as f:
    text = f.read()

# Let's write a simple parser to track the stack of tags.
stack = []
line_num = 1
for line in text.split('\n'):
    # find all tags on the line
    tags = re.findall(r'</?[\w:]+[^>]*>', line)
    for tag in tags:
        if tag.startswith('</'):
            tag_name = re.match(r'</([\w:]+)>', tag).group(1)
            if not stack:
                print(f"Error at line {line_num}: extra closing tag {tag}")
                exit(1)
            last_open = stack.pop()
            if last_open != tag_name:
                print(f"Error at line {line_num}: expected </{last_open}> but found {tag}")
                exit(1)
        elif tag.endswith('/>') or tag.startswith('<?'):
            pass # self-closing or xml declaration
        else:
            tag_name = re.match(r'<([\w:]+)', tag).group(1)
            stack.append(tag_name)
    line_num += 1

print(f"Success! Stack left: {stack}")
