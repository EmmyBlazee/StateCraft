using UnityEngine;
using UnityEngine.UIElements;

namespace StateCraft.UI
{
    public class ComplianceDonutElement : VisualElement
    {
        private const float RING_WIDTH = 12f;
        private const float GAP_DEG    = 2.5f;

        private static readonly Color ColCompliant    = (Color)new Color32(20,  220, 115, 255);
        private static readonly Color ColPartial      = (Color)new Color32(255, 165,   0, 255);
        private static readonly Color ColNonCompliant = (Color)new Color32(255,  70,  70, 255);
        private static readonly Color ColBackground   = (Color)new Color32( 38,  46,  56, 255);

        private float _compliantPct;
        private float _partialPct;
        private float _nonCompliantPct;

        public ComplianceDonutElement()
        {
            style.position = Position.Absolute;
            style.left   = 0;
            style.right  = 0;
            style.top    = 0;
            style.bottom = 0;

            pickingMode = PickingMode.Ignore;

            generateVisualContent += OnGenerateVisualContent;
        }

        public void SetValues(float compliant, float partial, float nonCompliant)
        {
            _compliantPct    = Mathf.Max(0f, compliant);
            _partialPct      = Mathf.Max(0f, partial);
            _nonCompliantPct = Mathf.Max(0f, nonCompliant);
            MarkDirtyRepaint();
        }

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

            float total = _compliantPct + _partialPct + _nonCompliantPct;

            var p = ctx.painter2D;
            p.lineCap   = LineCap.Butt;
            p.lineWidth = strokeW;

            StrokeArc(p, cx, cy, midR, 0f, 360f, ColBackground);

            if (total <= 0f) return;

            float compDeg    = (_compliantPct    / total) * 360f;
            float partDeg    = (_partialPct      / total) * 360f;
            float nonCompDeg = (_nonCompliantPct / total) * 360f;

            float gapC = _compliantPct    > 0f ? GAP_DEG : 0f;
            float gapP = _partialPct      > 0f ? GAP_DEG : 0f;
            float gapN = _nonCompliantPct > 0f ? GAP_DEG : 0f;

            float cursor = -90f;

            if (compDeg - gapC > 0f)
                StrokeArc(p, cx, cy, midR,
                          cursor + gapC * 0.5f,
                          cursor + compDeg - gapC * 0.5f,
                          ColCompliant);
            cursor += compDeg;

            if (partDeg - gapP > 0f)
                StrokeArc(p, cx, cy, midR,
                          cursor + gapP * 0.5f,
                          cursor + partDeg - gapP * 0.5f,
                          ColPartial);
            cursor += partDeg;

            if (nonCompDeg - gapN > 0f)
                StrokeArc(p, cx, cy, midR,
                          cursor + gapN * 0.5f,
                          cursor + nonCompDeg - gapN * 0.5f,
                          ColNonCompliant);
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