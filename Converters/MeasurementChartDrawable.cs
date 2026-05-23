using System.Globalization;
using kal_sync.Models;
using Microsoft.Maui.Graphics;

namespace kal_sync.Converters;

public class MeasurementChartDrawable : IDrawable
{
    private static readonly Color WeightColor = Color.FromArgb("#1A1F2A"); // schwarz
    private static readonly Color BfColor     = Color.FromArgb("#1B5E20"); // dunkelgrün
    private static readonly Color GridColor   = Color.FromArgb("#E2E5EA");
    private static readonly Color MutedColor  = Color.FromArgb("#6B7280");

    public List<BodyMeasurement> Measurements { get; set; } = [];

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var pts = Measurements.OrderBy(m => m.Date).ToList();

        if (pts.Count < 2)
        {
            canvas.FontSize  = 12;
            canvas.FontColor = MutedColor;
            canvas.DrawString(
                "Mindestens 2 Messungen für den Verlauf",
                dirtyRect.Center.X, dirtyRect.Center.Y,
                HorizontalAlignment.Center);
            return;
        }

        const float PadL = 48f;
        const float PadR = 48f;
        const float PadT = 24f;
        const float PadB = 28f;

        float w  = dirtyRect.Width  - PadL - PadR;
        float h  = dirtyRect.Height - PadT - PadB;
        float x0 = dirtyRect.X + PadL;
        float y0 = dirtyRect.Y + PadT;

        // ── Value ranges ────────────────────────────────────────────────────
        double minW  = pts.Min(m => m.WeightKg);
        double maxW  = pts.Max(m => m.WeightKg);
        double rngW  = Math.Max(maxW - minW, 1.0);
        minW -= rngW * 0.1; maxW += rngW * 0.1; rngW = maxW - minW;

        double minBf = pts.Min(m => m.BodyFatPercent);
        double maxBf = pts.Max(m => m.BodyFatPercent);
        double rngBf = Math.Max(maxBf - minBf, 1.0);
        minBf -= rngBf * 0.1; maxBf += rngBf * 0.1; rngBf = maxBf - minBf;

        var minDate  = pts[0].Date;
        var maxDate  = pts[^1].Date;
        double days  = Math.Max((maxDate - minDate).TotalDays, 1.0);

        // ── Coordinate helpers ──────────────────────────────────────────────
        float ToX(DateTime d)  => x0 + (float)((d - minDate).TotalDays / days) * w;
        float ToYW(double kg)  => y0 + h - (float)((kg  - minW)  / rngW)  * h;
        float ToYBf(double pct)=> y0 + h - (float)((pct - minBf) / rngBf) * h;

        // ── Horizontal grid lines ────────────────────────────────────────────
        canvas.StrokeColor = GridColor;
        canvas.StrokeSize  = 0.5f;
        for (int i = 0; i <= 4; i++)
        {
            float gy = y0 + h / 4f * i;
            canvas.DrawLine(x0, gy, x0 + w, gy);
        }

        // ── Legend ──────────────────────────────────────────────────────────
        canvas.FontSize  = 9;
        float legY = dirtyRect.Y + 8f;

        canvas.FillColor = WeightColor;
        canvas.FillCircle(x0 + 6f, legY, 3.5f);
        canvas.FontColor = WeightColor;
        canvas.DrawString("Gewicht (kg)", x0 + 14f, legY - 5f, 90f, 14f,
            HorizontalAlignment.Left, VerticalAlignment.Center);

        canvas.FillColor = BfColor;
        canvas.FillCircle(x0 + 110f, legY, 3.5f);
        canvas.FontColor = BfColor;
        canvas.DrawString("KFA (%)", x0 + 118f, legY - 5f, 70f, 14f,
            HorizontalAlignment.Left, VerticalAlignment.Center);

        // ── Weight line (black) ──────────────────────────────────────────────
        canvas.StrokeColor = WeightColor;
        canvas.StrokeSize  = 2f;
        for (int i = 1; i < pts.Count; i++)
            canvas.DrawLine(ToX(pts[i-1].Date), ToYW(pts[i-1].WeightKg),
                            ToX(pts[i].Date),   ToYW(pts[i].WeightKg));
        canvas.FillColor = WeightColor;
        foreach (var m in pts)
            canvas.FillCircle(ToX(m.Date), ToYW(m.WeightKg), 3f);

        // ── KFA line (dark green) ────────────────────────────────────────────
        canvas.StrokeColor = BfColor;
        canvas.StrokeSize  = 2f;
        for (int i = 1; i < pts.Count; i++)
            canvas.DrawLine(ToX(pts[i-1].Date), ToYBf(pts[i-1].BodyFatPercent),
                            ToX(pts[i].Date),   ToYBf(pts[i].BodyFatPercent));
        canvas.FillColor = BfColor;
        foreach (var m in pts)
            canvas.FillCircle(ToX(m.Date), ToYBf(m.BodyFatPercent), 3f);

        // ── Left Y-axis labels (weight) ──────────────────────────────────────
        canvas.FontSize  = 9;
        canvas.FontColor = WeightColor;
        for (int i = 0; i <= 4; i++)
        {
            double val = minW + rngW / 4.0 * (4 - i);
            float  gy  = y0 + h / 4f * i;
            canvas.DrawString($"{val:F1}", 0f, gy - 7f, PadL - 6f, 14f,
                HorizontalAlignment.Right, VerticalAlignment.Center);
        }

        // ── Right Y-axis labels (body fat) ───────────────────────────────────
        canvas.FontColor = BfColor;
        for (int i = 0; i <= 4; i++)
        {
            double val = minBf + rngBf / 4.0 * (4 - i);
            float  gy  = y0 + h / 4f * i;
            canvas.DrawString($"{val:F1}%", x0 + w + 4f, gy - 7f, PadR - 4f, 14f,
                HorizontalAlignment.Left, VerticalAlignment.Center);
        }

        // ── X-axis date labels ───────────────────────────────────────────────
        canvas.FontColor = MutedColor;
        float labelY = y0 + h + 4f;

        canvas.DrawString(pts[0].Date.ToString("dd.MM", CultureInfo.CurrentCulture),
            ToX(pts[0].Date) - 16f, labelY, 32f, 14f,
            HorizontalAlignment.Center, VerticalAlignment.Top);

        canvas.DrawString(pts[^1].Date.ToString("dd.MM", CultureInfo.CurrentCulture),
            ToX(pts[^1].Date) - 16f, labelY, 32f, 14f,
            HorizontalAlignment.Center, VerticalAlignment.Top);

        if (pts.Count >= 3)
        {
            var mid = pts[pts.Count / 2];
            canvas.DrawString(mid.Date.ToString("dd.MM", CultureInfo.CurrentCulture),
                ToX(mid.Date) - 16f, labelY, 32f, 14f,
                HorizontalAlignment.Center, VerticalAlignment.Top);
        }
    }
}
