using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using CloseDriver.Core.Protocol;

namespace CloseDriver.Wpf.Controls;

/// <summary>
/// Renders a 20-point power or regen curve from a <see cref="FardriverData"/> snapshot.
/// X-axis is RPM; Y-axis is percentage (current limit % or regen %).
/// </summary>
public partial class CurveChart : UserControl
{
    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(nameof(Data), typeof(FardriverData), typeof(CurveChart),
            new PropertyMetadata(default(FardriverData), OnDataChanged));

    public static readonly DependencyProperty IsRegenProperty =
        DependencyProperty.Register(nameof(IsRegen), typeof(bool), typeof(CurveChart),
            new PropertyMetadata(false, OnDataChanged));

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(CurveChart),
            new PropertyMetadata(string.Empty, OnTitleChanged));

    public FardriverData Data
    {
        get => (FardriverData)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public bool IsRegen
    {
        get => (bool)GetValue(IsRegenProperty);
        set => SetValue(IsRegenProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public CurveChart()
    {
        InitializeComponent();
        Loaded += (_, __) => Redraw();
        SizeChanged += (_, __) => Redraw();
    }

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CurveChart chart)
            chart.Redraw();
    }

    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CurveChart chart)
            chart.XAxisLabel.Text = chart.Title ?? string.Empty;
    }

    private void Redraw()
    {
        if (ChartCanvas.ActualWidth <= 0 || ChartCanvas.ActualHeight <= 0)
            return;

        CurveLine.Points = new PointCollection();
        AreaFill.Points = new PointCollection();

        if (Data.Buffer == null || Data.Buffer.Length < 312)
        {
            Baseline.X1 = 0; Baseline.X2 = 0; Baseline.Y1 = 0; Baseline.Y2 = 0;
            return;
        }

        float yMin = IsRegen ? -50f : 0f;
        float yMax = IsRegen ? 0f : 100f;

        // Find actual min/max so the chart fits tightly.
        for (int i = 0; i < 20; i++)
        {
            float v = IsRegen ? Data.GetRegenCurvePercent(i) : Data.GetPowerCurvePercent(i);
            if (v < yMin) yMin = v;
            if (v > yMax) yMax = v;
        }

        float yRange = yMax - yMin;
        if (yRange < 1f)
            yRange = 1f;

        double width = ChartCanvas.ActualWidth;
        double height = ChartCanvas.ActualHeight;
        double padding = 5;
        double plotWidth = Math.Max(0, width - 2 * padding);
        double plotHeight = Math.Max(0, height - 2 * padding);

        // Baseline at Y=0 if visible.
        if (yMin <= 0 && yMax >= 0)
        {
            double y = padding + plotHeight * (1 - (0 - yMin) / yRange);
            Baseline.X1 = padding; Baseline.X2 = width - padding;
            Baseline.Y1 = y; Baseline.Y2 = y;
        }
        else
        {
            Baseline.X1 = 0; Baseline.X2 = 0; Baseline.Y1 = 0; Baseline.Y2 = 0;
        }

        var curve = new PointCollection();
        var area = new PointCollection();

        for (int i = 0; i < 20; i++)
        {
            float v = IsRegen ? Data.GetRegenCurvePercent(i) : Data.GetPowerCurvePercent(i);
            double x = padding + (plotWidth * i) / 19.0;
            double y = padding + plotHeight * (1 - (v - yMin) / yRange);
            curve.Add(new Point(x, y));

            // For the area fill, trace down to baseline and close back.
            if (i == 0)
                area.Add(new Point(x, y));
            area.Add(new Point(x, y));
            if (i == 19)
            {
                area.Add(new Point(x, padding + plotHeight * (1 - (0 - yMin) / yRange)));
                area.Add(new Point(padding, padding + plotHeight * (1 - (0 - yMin) / yRange)));
                area.Add(new Point(padding, curve[0].Y));
            }
        }

        CurveLine.Points = curve;
        AreaFill.Points = area;
    }
}
