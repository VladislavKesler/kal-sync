namespace kal_sync.Converters;

/// <summary>
/// Custom IDrawable for the home-screen hero ring.
/// Renders a 14 px wide donut arc: dark portion = target share of TDEE, red = deficit arc.
/// Set DeficitPercent from code-behind and call GraphicsView.Invalidate() to refresh.
/// </summary>
public class TargetRingDrawable : BindableObject, IDrawable
{
    public static readonly BindableProperty DeficitPercentProperty =
        BindableProperty.Create(
            nameof(DeficitPercent),
            typeof(double),
            typeof(TargetRingDrawable),
            defaultValue: 0.0);

    /// <summary>Deficit as a percentage of TDEE (0–100). Always ≥ 0.</summary>
    public double DeficitPercent
    {
        get => (double)GetValue(DeficitPercentProperty);
        set => SetValue(DeficitPercentProperty, value);
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

        var deficitFrac = (float)Math.Clamp(DeficitPercent / 100.0, 0.0, 0.99);
        var baseFrac    = 1f - deficitFrac;

        // 2. Target arc (dark ink — calories the user should eat)
        canvas.StrokeColor = Color.FromArgb("#1A1F2A");
        canvas.DrawArc(
            cx - r, cy - r, r * 2, r * 2,
            startAngle: 90f,
            endAngle:   90f - 360f * baseFrac,
            clockwise:  true,
            closed:     false);

        // 3. Deficit arc (danger red — calories not added back)
        if (deficitFrac > 0f)
        {
            canvas.StrokeColor = Color.FromArgb("#C0533A");
            canvas.DrawArc(
                cx - r, cy - r, r * 2, r * 2,
                startAngle: 90f - 360f * baseFrac,
                endAngle:   90f - 360f * (baseFrac + deficitFrac),
                clockwise:  true,
                closed:     false);
        }
    }
}
