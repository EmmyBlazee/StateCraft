import re

# ── Fix 1: GameManager.cs stray brace ────────────────────────────
gm_path = r"c:\Users\Tobi\StateCraft\Assets\Scripts\Core\GameManager.cs"
with open(gm_path, "r", encoding="utf-8") as f:
    gm = f.read()

# The stray pattern is "        }\r\n        }\r\n    }\r\n}\r\n" right before namespace StateCraft.Data
# Lines 582-585 in the file:  "        }\n        }\n    }\n}\n"
# We need to remove the extra indented closing brace (the duplicate at line 583)
gm_fixed = gm.replace(
    "            }\r\n        }\r\n    }\r\n}\r\n\r\nnamespace StateCraft.Data",
    "            }\r\n        }\r\n    }\r\n}\r\n\r\nnamespace StateCraft.Data"
)
# More targeted: just remove the extra brace pair
gm_fixed = re.sub(
    r"(            \}\r?\n)        \}\r?\n(    \}\r?\n\}\r?\n)",
    r"\1\2",
    gm_fixed
)
with open(gm_path, "w", encoding="utf-8") as f:
    f.write(gm_fixed)
print("GameManager.cs brace fixed.")

# ── Fix 2: ScenarioBriefings.cs literal newlines in strings ──────
sb_path = r"c:\Users\Tobi\StateCraft\Assets\Scripts\Core\ScenarioBriefings.cs"
with open(sb_path, "r", encoding="utf-8") as f:
    sb = f.read()

# The generated entries for the 45 new countries used single-quoted strings
# with actual \n chars and bullet/dash unicode inside a single-quoted assignment.
# Pattern: priorities = "...ACTUAL NEWLINE...ACTUAL NEWLINE..."
# We need to find all auto-generated Briefing blocks (from Algeria onwards)
# and rewrite the priorities field to use proper C# escaped strings.

def fix_priorities_block(m):
    """Collapse multiline priorities value into a single properly escaped C# string."""
    prefix = m.group(1)   # 'priorities = "'
    content = m.group(2)  # everything until the closing '"'
    # content may contain actual newlines — replace them with \n (the escape)
    fixed = content.replace("\r\n", "\\n").replace("\n", "\\n")
    # The bullet char and em-dash are fine inside C# strings — keep them
    return prefix + fixed + '"'

# Match: priorities = "...multiline content..."  (greedy but stops at closing quote+comma/newline)
# We target ONLY the auto-generated section (after the Angola entry) to avoid touching the safe originals.
angola_end = sb.find('["Angola"] = new Briefing')
header = sb[:angola_end]
body = sb[angola_end:]

# Fix literal newlines inside string literals in the body
# Strategy: find every   priorities = "...CONTENT...",  where CONTENT spans multiple lines
body_fixed = re.sub(
    r'(                priorities = ")(.*?)("[\s,\n])',
    lambda m: '                priorities = "' + m.group(2).replace("\r\n", "\\n").replace("\n", "\\n") + m.group(3),
    body,
    flags=re.DOTALL
)

# Also fix any context fields that might have gotten literal newlines
body_fixed = re.sub(
    r'(                context = ")(.*?)("[\s,\n])',
    lambda m: '                context = "' + m.group(2).replace("\r\n", "\\n").replace("\n", "\\n") + m.group(3),
    body_fixed,
    flags=re.DOTALL
)

sb_fixed = header + body_fixed
with open(sb_path, "w", encoding="utf-8") as f:
    f.write(sb_fixed)
print("ScenarioBriefings.cs literal newlines fixed.")
