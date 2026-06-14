import re

file_path = r"C:\Users\Tobi\StateCraft\Assets\Scripts\UI\MilitaryDashboardController.cs"
with open(file_path, "r", encoding="utf-8") as f:
    text = f.read()

# Replace MilitaryOperationsController calls with local calls, 
# because we'll just put the helpers in the same class.

text = text.replace("MilitaryOperationsController.", "")

# Add the helpers
helpers = """
        // ── Operations Tab Helpers & Enums ──
        public enum ThreatLevel  { High, Medium, Low }
        public enum OpType       { CounterTerror, Peacekeeping, BorderPatrol, AirRecon, Maritime, HvtCapture }
        public enum FeedColour   { Green, Yellow, Red }

        public class OpTypeEntry {
            public string Name, Description, RequiredForces, EstCost;
            public OpType Type;
            public OpTypeEntry(string n, string d, string rf, string ec, OpType t) { Name=n; Description=d; RequiredForces=rf; EstCost=ec; Type=t; }
        }

        static string ThreatIconClass(ThreatLevel l)
        {
            if (l == ThreatLevel.High) return "threat-row__icon--high";
            if (l == ThreatLevel.Medium) return "threat-row__icon--medium";
            return "threat-row__icon--low";
        }

        static string ThreatLevelClass(ThreatLevel l)
        {
            if (l == ThreatLevel.High) return "threat-row__level--high";
            if (l == ThreatLevel.Medium) return "threat-row__level--medium";
            return "threat-row__level--low";
        }

        static Color ThreatColour(ThreatLevel l)
        {
            if (l == ThreatLevel.High)   return new Color(0.96f, 0.26f, 0.21f);
            if (l == ThreatLevel.Medium) return new Color(1f,    0.92f, 0.23f);
            return new Color(0f,    0.90f, 0.46f);
        }

        static string ThreatGlyph(ThreatLevel l)
        {
            if (l == ThreatLevel.High)   return "!";
            if (l == ThreatLevel.Medium) return "~";
            return "✓";
        }

        static string OpTypeCardClass(OpType t)
        {
            if (t == OpType.CounterTerror) return "op-card--counter-terror";
            if (t == OpType.Peacekeeping)  return "op-card--peacekeeping";
            if (t == OpType.BorderPatrol)  return "op-card--border-patrol";
            if (t == OpType.AirRecon)      return "op-card--air-recon";
            if (t == OpType.Maritime)      return "op-card--maritime";
            if (t == OpType.HvtCapture)    return "op-card--hvt";
            return "";
        }

        static string OpTypeGlyph(OpType t)
        {
            if (t == OpType.CounterTerror) return "🎯";
            if (t == OpType.Peacekeeping)  return "🤝";
            if (t == OpType.BorderPatrol)  return "🛡";
            if (t == OpType.AirRecon)      return "✈";
            if (t == OpType.Maritime)      return "⚓";
            if (t == OpType.HvtCapture)    return "👁";
            return "★";
        }

        static Color OpTypeAccentColour(OpType t)
        {
            if (t == OpType.CounterTerror) return new Color(0.96f, 0.26f, 0.21f);
            if (t == OpType.Peacekeeping)  return new Color(0f,    0.66f, 1f);
            if (t == OpType.BorderPatrol)  return new Color(0f,    0.90f, 0.46f);
            if (t == OpType.AirRecon)      return new Color(0f,    0.66f, 1f);
            if (t == OpType.Maritime)      return new Color(0f,    0.74f, 0.83f);
            if (t == OpType.HvtCapture)    return new Color(0.67f, 0.28f, 0.74f);
            return Color.white;
        }

        static string ProgressFillClass(int progress)
        {
            if (progress >= 60) return "op-card__progress-fill--good";
            if (progress >= 30) return "op-card__progress-fill--medium";
            return "op-card__progress-fill--low";
        }

        static string FeedDotClass(FeedColour c)
        {
            if (c == FeedColour.Green)  return "feed-item__dot--green";
            if (c == FeedColour.Yellow) return "feed-item__dot--yellow";
            return "feed-item__dot--red";
        }

    }
}
"""

text = text.replace("    }\n}", helpers)

with open(file_path, "w", encoding="utf-8") as f:
    f.write(text)
