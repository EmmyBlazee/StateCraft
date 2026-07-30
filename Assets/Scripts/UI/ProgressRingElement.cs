using UnityEngine;
using UnityEngine.UIElements;

namespace StateCraft.UI
{
    /// <summary>
    /// Custom UITK element that draws a progress ring chart using Painter2D.
    /// </summary>
    public class ProgressRingElement : VisualElement
    {
        // ── Constants ────────────────────────────────────────────────────────
        private const float RING_WIDTH = 9f;   // ring thickness in px

        private static readonly Color ColBackground = (Color)new Color32(69, 91, 111, 255); // #455B6F border color from Global.uss
        private static readonly Color ColFill = (Color)new Color32(25, 226, 255, 255);      // #19E2FF market color

        // ── State ────────────────────────────────────────────────────────────
        private float _valuePct;

        public ProgressRingElement()
        {
            generateVisualContent += OnGenerateVisualContent;
        }

        public void SetValue(float value)
        {
            _valuePct = Mathf.Clamp(value, 0f, 100f);
            MarkDirtyRepaint();
        }

        // ── Drawing ──────────────────────────────────────────────────────────

        private void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            float w = contentRect.width;
            float h = contentRect.height;
            if (w <= 1f || h <= 1f) return;

            float cx     = w * 0.5f;
            float cy     = h * 0.5f;
            float outerR = Mathf.Min(w, h) * 0.5f - 1f;
            float innerR = Mathf.Max(2f, outerR - RING_WIDTH);
            float midR   = (outerR + innerR) * 0.5f;
            float strokeW = outerR - innerR;

            var p = ctx.painter2D;
            p.lineCap   = LineCap.Butt;
            p.lineWidth = strokeW;

            // ── Grey background track (full 360°) ────────────────────────────
            StrokeArc(p, cx, cy, midR, 0f, 360f, ColBackground);

            if (_valuePct <= 0f) return;

            float fillDeg = (_valuePct / 100f) * 360f;

            // Clockwise from 12 o'clock (-90° = top in Painter2D convention)
            float startCursor = -90f;

            StrokeArc(p, cx, cy, midR,
                        startCursor,
                        startCursor + fillDeg,
                        ColFill);
        }

        private static void StrokeArc(Painter2D p, float cx, float cy, float radius,
                                       float startDeg, float endDeg, Color color)
        {
            if (endDeg <= startDeg) return;
            p.strokeColor = color;
            p.BeginPath();
            p.Arc(new Vector2(cx, cy), radius, startDeg, endDeg, ArcDirection.Clockwise);
            p.Stroke();
        }
    }
}
