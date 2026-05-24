namespace kal_sync.Converters;

/// <summary>
/// Custom IDrawable for the home-screen hero ring.
/// Renders a 14 px wide donut arc: dark = TDEE share, green = surplus arc, red = deficit arc.
/// Set AdjustmentPercent from code-behind and call GraphicsView.Invalidate() to refresh.
/// </summary>
public class TargetRingDrawable : BindableObject, IDrawable
{
    public static readonly BindableProperty AdjustmentPercentProperty =
        BindableProperty.Create(
            nameof(AdjustmentPercent),
            typeof(double),
            typeof(TargetRingDrawable),
            defaultValue: 0.0);

    /// <summary>Signed % of TDEE: positive = surplus (green arc), negative = deficit (red arc).</summary>
    public double AdjustmentPercent
    {
        get => (double)GetValue(AdjustmentPercentProperty);
        set => SetValue(AdjustmentPercentProperty, value);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        const float Stroke = 14f;
        var size = MathF.Min(dirtyRect.Width, dirtyRect.Height);
        var r    = (size - Stroke) / 2f;
        var cx   = dirtyRect.Center.X;
        var cy   = dirtyRect.Center.Y;

        canvas.StrokeSize    = Stroke;
        canvas.StrokeLineCap = LineCap.Butt;

        // 1. Hairline base ring
        canvas.StrokeColor = Color.FromArgb("#E2E5EA");
        canvas.DrawCircle(cx, cy, r);

        var adjustFrac = (float)Math.Clamp(Math.Abs(AdjustmentPercent) / 100.0, 0.0, 0.99);
        var baseFrac   = 1f - adjustFrac;

        // 2. TDEE arc (dark ink)
        canvas.StrokeColor = Color.FromArgb("#1A1F2A");
        canvas.DrawArc(
            cx - r, cy - r, r * 2, r * 2,
            startAngle: 90f,
            endAngle:   90f - 360f * baseFrac,
            clockwise:  true,
            closed:     false);

        // 3. Adjustment arc (green = surplus, red = deficit)
        if (adjustFrac > 0f)
        {
            canvas.StrokeColor = AdjustmentPercent >= 0
                ? Color.FromArgb("#A8D86A")
                : Color.FromArgb("#C0533A");
            canvas.DrawArc(
                cx - r, cy - r, r * 2, r * 2,
                startAngle: 90f - 360f * baseFrac,
                endAngle:   90f - 360f * (baseFrac + adjustFrac),
                clockwise:  true,
                closed:     false);
        }
    }
}
