import codecs

file_path = r"C:\Users\Tobi\StateCraft\Assets\Scripts\UI\MilitaryDashboardController.cs"
with codecs.open(file_path, "r", "utf-8") as f:
    text = f.read()

# 1. infrastructure -> stability
text = text.replace("r.infrastructure:F0", "r.stability:F0")

# 2. progress calculation
old_progress = """                int progress = 100;
                if (op.turnsToDeploy > 0) progress = 10;
                else if (op.durationMonths > 0) progress = (int)Mathf.Clamp01((float)op.monthsActive / op.durationMonths) * 100;
                else progress = 50;"""
new_progress = """                int progress = 100;
                if (op.turnsToDeploy > 0) progress = 10;
                else progress = 50;"""
text = text.replace(old_progress, new_progress)

# 3. f.Contains
old_f = """                if (f.Contains("Critical") || f.Contains("WAR") || f.Contains("KIA") || f.Contains("SHORTAGE")) col = FeedColour.Red;
                else if (f.Contains("Alert") || f.Contains("Warning")) col = FeedColour.Yellow;"""
new_f = """                if (f.Message.Contains("Critical") || f.Message.Contains("WAR") || f.Message.Contains("KIA") || f.Message.Contains("SHORTAGE")) col = FeedColour.Red;
                else if (f.Message.Contains("Alert") || f.Message.Contains("Warning")) col = FeedColour.Yellow;"""
text = text.replace(old_f, new_f)

# 4. TurnSystem & Text label
old_date = """                var date = new Label($"{gm.TurnSystem.CurrentYear}/{gm.TurnSystem.CurrentMonth}");
                date.AddToClassList("feed-item__date");
                dateCol.Add(day); dateCol.Add(date);
                item.Add(dateCol);

                var text = new Label(f);"""
        
new_date = """                var date = new Label($"{gm.TurnSystem.CurrentDate}");
                date.AddToClassList("feed-item__date");
                dateCol.Add(day); dateCol.Add(date);
                item.Add(dateCol);

                var text = new Label($"[{f.Tag}] {f.Message}");"""
text = text.replace(old_date, new_date)

with codecs.open(file_path, "w", "utf-8") as f:
    f.write(text)
