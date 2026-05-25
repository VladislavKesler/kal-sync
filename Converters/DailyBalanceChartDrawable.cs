using System.Globalization;
using kal_sync.Models;
using Microsoft.Maui.Graphics;

namespace kal_sync.Converters;

public class DailyBalanceChartDrawable : IDrawable
{
    private static readonly Color GreenColor = Color.FromArgb("#3F6E1F");
    private static readonly Color RedColor   = Color.FromArgb("#C0533A");
    private static readonly Color GridColor  = Color.FromArgb("#E2E5EA");
    private static readonly Color MutedColor = Color.FromArgb("#6B7280");

    public List<DailyBalance> Entries { get; set; } = [];
    public ChartMode Mode { get; set; } = ChartMode.SevenDays;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var slots = BuildSlots();

        if (slots.All(s => s.Value is null))
        {
            canvas.FontSize  = 12;
            canvas.FontColor = MutedColor;
            canvas.DrawString(
                "Noch keine Daten",
                dirtyRect.Center.X, dirtyRect.Center.Y,
                HorizontalAlignment.Center);
            return;
        }

        const float PadL = 44f;
        const float PadR = 8f;
        const float PadT = 16f;
        const float PadB = 28f;

        float w  = dirtyRect.Width  - PadL - PadR;
        float h  = dirtyRect.Height - PadT - PadB;
        float x0 = dirtyRect.X + PadL;
        float y0 = dirtyRect.Y + PadT;

        // ── Y scale ─────────────────────────────────────────────────────────
        double[] values = slots.Where(s => s.Value.HasValue).Select(s => s.Value!.Value).ToArray();
        double rawMin = values.Min();
        double rawMax = values.Max();
        double pad    = Math.Max(Math.Max(Math.Abs(rawMin), Math.Abs(rawMax)) * 0.15, 80.0);
        double minV   = Math.Min(0.0, rawMin - pad);
        double maxV   = Math.Max(0.0, rawMax + pad);
        double range  = maxV - minV;

        float ToY(double v) => y0 + h - (float)((v - minV) / range * h);
        float zeroY = ToY(0.0);

        // ── Grid lines ───────────────────────────────────────────────────────
        canvas.StrokeColor = GridColor;
        canvas.StrokeSize  = 0.5f;
        for (int i = 0; i <= 4; i++)
        {
            float gy = y0 + h / 4f * i;
            canvas.DrawLine(x0, gy, x0 + w, gy);
        }

        // ── Zero hairline ────────────────────────────────────────────────────
        canvas.StrokeColor = MutedColor;
        canvas.StrokeSize  = 1f;
        canvas.DrawLine(x0, zeroY, x0 + w, zeroY);

        // ── Y-axis labels ────────────────────────────────────────────────────
        canvas.FontSize  = 9;
        canvas.FontColor = MutedColor;
        for (int i = 0; i <= 4; i++)
        {
            double val = minV + range / 4.0 * (4 - i);
            float  gy  = y0 + h / 4f * i;
            canvas.DrawString($"{val:F0}", 0f, gy - 7f, PadL - 4f, 14f,
                HorizontalAlignment.Right, VerticalAlignment.Center);
        }

        // ── Bars ─────────────────────────────────────────────────────────────
        int   n       = slots.Length;
        float slotW   = w / n;
        float barPad  = slotW * 0.15f;
        float barW    = slotW - 2f * barPad;
        float labelY  = y0 + h + 4f;

        canvas.FontSize = 9;

        for (int i = 0; i < n; i++)
        {
            float barX    = x0 + i * slotW + barPad;
            float centerX = x0 + i * slotW + slotW / 2f;

            if (slots[i].Value.HasValue)
            {
                double val      = slots[i].Value!.Value;
                float  barTop    = Math.Min(ToY(val), zeroY);
                float  barBottom = Math.Max(ToY(val), zeroY);
                float  barHeight = Math.Max(barBottom - barTop, 1f);

                canvas.FillColor = val < 0 ? GreenColor : RedColor;
                canvas.FillRectangle(barX, barTop, barW, barHeight);
            }
            else
            {
                // Missing period — draw "–" at zero line
                canvas.FontColor = MutedColor;
                canvas.DrawString("–", centerX - 8f, zeroY - 8f, 16f, 16f,
                    HorizontalAlignment.Center, VerticalAlignment.Center);
            }

            // X-axis label
            canvas.FontColor = MutedColor;
            canvas.DrawString(slots[i].Label, centerX - 16f, labelY, 32f, 14f,
                HorizontalAlignment.Center, VerticalAlignment.Top);
        }
    }

    // ── Slot aggregation ─────────────────────────────────────────────────────

    private (double? Value, string Label)[] BuildSlots() => Mode switch
    {
        ChartMode.FourWeeks  => BuildFourWeeksSlots(),
        ChartMode.FourMonths => BuildFourMonthsSlots(),
        _                    => BuildSevenDaysSlots(),
    };

    private (double? Value, string Label)[] BuildSevenDaysSlots()
    {
        var map     = Entries.ToDictionary(e => e.Date.Date);
        var culture = CultureInfo.CurrentCulture;
        var slots   = new (double? Value, string Label)[7];

        for (int i = 0; i < 7; i++)
        {
            var    date  = DateTime.Today.AddDays(i - 6);
            string label = culture.DateTimeFormat.GetAbbreviatedDayName(date.DayOfWeek);
            if (label.Length > 2) label = label[..2].TrimEnd('.');

            slots[i] = map.TryGetValue(date, out var entry)
                ? (entry.BalanceKcal, label)
                : (null, label);
        }
        return slots;
    }

    private (double? Value, string Label)[] BuildFourWeeksSlots()
    {
        var map   = Entries.ToDictionary(e => e.Date.Date);
        var slots = new (double? Value, string Label)[4];

        for (int w = 0; w < 4; w++)
        {
            // w=0 oldest (today-27…today-21), w=3 newest (today-6…today)
            var startDate  = DateTime.Today.AddDays(-27 + w * 7);
            var weekValues = new List<double>();

            for (int d = 0; d < 7; d++)
            {
                var date = startDate.AddDays(d);
                if (map.TryGetValue(date, out var entry))
                    weekValues.Add(entry.BalanceKcal);
            }

            int    weekNum = ISOWeek.GetWeekOfYear(startDate);
            string label   = $"KW{weekNum}";

            slots[w] = weekValues.Count > 0
                ? (weekValues.Average(), label)
                : (null, label);
        }
        return slots;
    }

    private (double? Value, string Label)[] BuildFourMonthsSlots()
    {
        var byMonth = Entries
            .GroupBy(e => new { e.Date.Year, e.Date.Month })
            .ToDictionary(g => (g.Key.Year, g.Key.Month), g => g.ToList());

        var culture = CultureInfo.CurrentCulture;
        var slots   = new (double? Value, string Label)[4];

        for (int m = 0; m < 4; m++)
        {
            // m=0 oldest (3 months ago), m=3 current month
            var    refDate = DateTime.Today.AddMonths(-(3 - m));
            var    key     = (refDate.Year, refDate.Month);
            string label   = culture.DateTimeFormat.GetAbbreviatedMonthName(refDate.Month);
            if (label.Length > 3) label = label[..3].TrimEnd('.');

            slots[m] = byMonth.TryGetValue(key, out var list) && list.Count > 0
                ? (list.Average(e => e.BalanceKcal), label)
                : (null, label);
        }
        return slots;
    }
}
