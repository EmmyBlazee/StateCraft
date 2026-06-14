import codecs
import re

path = r'C:\Users\Tobi\StateCraft\Assets\Scripts\UI\MilitaryDashboardController.cs'
s = codecs.open(path, 'r', 'utf-8').read()

func_str = '''
        private Label AddImpactStat(VisualElement parent, string titleText, string initialVal)
        {
            var box = new VisualElement();
            box.style.flexDirection = FlexDirection.Column;
            box.style.width = Length.Percent(33);
            
            var title = new Label(titleText);
            title.style.color = Color.grey;
            title.style.fontSize = 8;
            box.Add(title);

            var val = new Label(initialVal);
            val.style.color = Color.white;
            val.style.unityFontStyleAndWeight = FontStyle.Bold;
            val.style.fontSize = 10;
            val.style.marginTop = 2;
            box.Add(val);
            
            parent.Add(box);
            return val;
        }

        private VisualElement MakeEffectTag(string text, Color color)
        {
            var el = new VisualElement();
            el.style.backgroundColor = new StyleColor(new Color(color.r, color.g, color.b, 0.2f));
            SetBorderColor(el.style, color);
            el.style.borderTopWidth = el.style.borderBottomWidth = el.style.borderLeftWidth = el.style.borderRightWidth = 1;
            el.style.borderTopLeftRadius = el.style.borderTopRightRadius = el.style.borderBottomLeftRadius = el.style.borderBottomRightRadius = 3;
            el.style.paddingLeft = el.style.paddingRight = 4;
            el.style.paddingTop = el.style.paddingBottom = 2;
            el.style.marginRight = 4;
            el.style.marginBottom = 4;

            var lbl = new Label(text);
            lbl.style.color = color;
            lbl.style.fontSize = 8;
            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            el.Add(lbl);
            return el;
        }
'''

# Find the end of the class and insert the methods
idx = s.rfind('    }\r\n}')
if idx == -1:
    idx = s.rfind('    }\n}')

if idx != -1:
    new_s = s[:idx] + func_str + s[idx:]
    codecs.open(path, 'w', 'utf-8').write(new_s)
    print('SUCCESS')
else:
    print('FAILED to find class end')
