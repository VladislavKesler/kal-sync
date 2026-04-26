namespace kal_sync.Converters;

/// <summary>
/// Custom IDrawable for the home-screen hero ring.
/// Renders a 14 px wide donut arc: dark portion = TDEE share, accent/danger = surplus/deficit arc.
/// Set Surplus from code-behind and call GraphicsView.Invalidate() to refresh.
/// </summary>
public class TargetRingDrawable : BindableObject, IDrawable
{
    public static readonly BindableProperty SurplusProperty =
        BindableProperty.Create(
            nameof(Surplus),
            typeof(double),
            typeof(TargetRingDrawable),
            defaultValue: 5.0);

    /// <summary>Caloric surplus/deficit in percent (e.g. 5 = +5 %, -5 = −5 %).</summary>
    public double Surplus
    {
        get => (double)GetValue(SurplusProperty);
        set => SetValue(SurplusProperty, value);
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

        // 2. TDEE arc (dark ink — main calorie need)
        var surplusFrac = (float)Math.Clamp(Surplus / 100.0, -0.4, 0.4);
        var baseFrac    = 1f - MathF.Abs(surplusFrac);

        canvas.StrokeColor = Color.FromArgb("#1A1F2A");
        canvas.DrawArc(
            cx - r, cy - r, r * 2, r * 2,
            startAngle: 90f,
            endAngle:   90f - 360f * baseFrac,
            clockwise:  true,
            closed:     false);

        // 3. Surplus / deficit arc (accent green or danger red)
        canvas.StrokeColor = surplusFrac >= 0
            ? Color.FromArgb("#A8D86A")
            : Color.FromArgb("#C0533A");
        canvas.DrawArc(
            cx - r, cy - r, r * 2, r * 2,
            startAngle: 90f - 360f * baseFrac,
            endAngle:   90f - 360f * (baseFrac + MathF.Abs(surplusFrac)),
            clockwise:  true,
            closed:     false);
    }
}
