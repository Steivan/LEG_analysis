#nullable enable
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using System.Collections.Generic;
using System.Windows.Forms;
using System;


namespace LEG.OxyPlotHelper
{
    public class OxyPlotHelper
    {
        private readonly PlotModel plotModel;
        private readonly LinearAxis xAxis;
        private readonly LinearAxis yAxis;

        public OxyPlotHelper(
            string title = "Azimuth/Elevation Plot",
            string xLabel = "Azimuth (°)",
            string yLabel = "Elevation (°)",
            double xMin = -150, double xMax = 150,
            double yMin = 0, double yMax = 30
            //, int width = 800, int height = 600
            )
        {
            plotModel = new PlotModel { Title = title };

            xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = xLabel,
                Minimum = xMin,
                Maximum = xMax,
                MajorStep = 10,
                MinorStep = 2,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = OxyColors.LightGray,
                MinorGridlineColor = OxyColors.LightGray
            };
            yAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = yLabel,
                Minimum = yMin,
                Maximum = yMax,
                MajorStep = 5,
                MinorStep = 1,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = OxyColors.LightGray,
                MinorGridlineColor = OxyColors.LightGray
            };

            plotModel.Axes.Add(xAxis);
            plotModel.Axes.Add(yAxis);

            // Add custom vertical lines for x-axis (azimuth)
            for (double x = xMin; x <= xMax; x += 10)
            {
                var line = new LineAnnotation
                {
                    Type = LineAnnotationType.Vertical,
                    X = x,
                    Color = OxyColors.Gray,
                    LineStyle = (x % 30 == 0) ? LineStyle.Solid : LineStyle.Dot,
                    StrokeThickness = (x % 30 == 0) ? 2 : 1,
                    Layer = AnnotationLayer.BelowAxes
                };
                plotModel.Annotations.Add(line);
            }

            // Add custom horizontal lines for y-axis (elevation)
            for (double y = yMin; y <= yMax; y += 5)
            {
                var line = new LineAnnotation
                {
                    Type = LineAnnotationType.Horizontal,
                    Y = y,
                    Color = OxyColors.Gray,
                    LineStyle = LineStyle.Dot,
                    StrokeThickness = 1,
                    Layer = AnnotationLayer.BelowAxes
                };
                plotModel.Annotations.Add(line);
            }
        }
        public void ShowLegend(LegendPosition position = LegendPosition.TopRight)
        {
            plotModel.IsLegendVisible = true;
            var legend = new Legend
            {
                LegendPosition = position
            };
            plotModel.Legends.Clear();
            plotModel.Legends.Add(legend);
        }
        public void AddCurve(
            double[] x, double[] y,
            OxyColor? lineColor = null,
            double lineWidth = 2,
            LineStyle lineStyle = LineStyle.Solid,
            string curveLabel = "")
        {
            var series = new LineSeries
            {
                StrokeThickness = lineWidth,
                LineStyle = lineStyle,
                Title = curveLabel
            };
            if (lineColor != null)
                series.Color = lineColor.Value;
            for (int i = 0; i < x.Length && i < y.Length; i++)
                series.Points.Add(new DataPoint(x[i], y[i]));
            plotModel.Series.Add(series);
        }

        public void AddMarkers(
            double[] x, double[] y,
            OxyColor markerColor,
            MarkerType markerType = MarkerType.Circle,
            double markerSize = 4,
            string markerLabel = "")
        {
            var series = new ScatterSeries
            {
                MarkerType = markerType,
                MarkerFill = markerColor,
                MarkerSize = markerSize,
                Title = markerLabel
            };
            for (int i = 0; i < x.Length && i < y.Length; i++)
                series.Points.Add(new ScatterPoint(x[i], y[i]));
            plotModel.Series.Add(series);
        }

        public void FillCurve(
            double[] x, double[] y,
            OxyColor color,
            byte alpha = 128, // 0=transparent, 255=opaque
            string fillLabel = "",
            bool addOutline = false,
            OxyColor? outlineColor = null,
            double outlineWidth = 1,
            LineStyle outlineLineStyle = LineStyle.Solid, // <-- Add this parameter
            bool addMarkers = false,
            OxyColor? markerColor = null,
            MarkerType markerType = MarkerType.Circle,
            double markerSize = 4,
            string markerLabel = "")
        {
            if (x.Length != y.Length || x.Length < 3)
                throw new ArgumentException("x and y must have the same length and at least 3 points.");

            // Ensure the curve is closed
            var points = new List<DataPoint>();
            for (int i = 0; i < x.Length; i++)
                points.Add(new DataPoint(x[i], y[i]));
            if (x[0] != x[^1] || y[0] != y[^1])
                points.Add(new DataPoint(x[0], y[0]));

            // Baseline (fill to y=0)
            var points2 = new List<DataPoint>();
            foreach (var pt in points)
                points2.Add(new DataPoint(pt.X, 0));

            var fillColor = OxyColor.FromAColor(alpha, color);

            var areaSeries = new AreaSeries
            {
                Color = outlineColor ?? color,
                StrokeThickness = addOutline ? outlineWidth : 0,
                LineStyle = addOutline ? outlineLineStyle : LineStyle.None, // <-- Set the outline style
                Fill = fillColor,
                Title = fillLabel
            };
            areaSeries.Points.AddRange(points);
            areaSeries.Points2.AddRange(points2);

            plotModel.Series.Add(areaSeries);

            // Optionally add markers
            if (addMarkers)
            {
                var scatter = new ScatterSeries
                {
                    MarkerType = markerType,
                    MarkerFill = markerColor ?? color,
                    MarkerSize = markerSize,
                    Title = markerLabel.Length > 0 ? markerLabel : null
                };
                foreach (var pt in points)
                    scatter.Points.Add(new ScatterPoint(pt.X, pt.Y));
                plotModel.Series.Add(scatter);
            }
        }
        public void AddTextBox(
            double x, double y,
            string text,
            OxyColor textColor,
            double fontSize = 12)
        {
            var annotation = new TextAnnotation
            {
                Text = text,
                TextPosition = new DataPoint(x, y),
                Stroke = OxyColors.Transparent,
                TextColor = textColor,
                FontSize = fontSize,
                Background = OxyColors.White,
                Padding = new OxyThickness(4)
            };
            plotModel.Annotations.Add(annotation);
        }

        public void ShowPlot(int width = 800, int height = 600)
        {
            var plotView = new PlotView
            {
                Model = plotModel,
                Dock = DockStyle.Fill
            };
            var form = new Form
            {
                Text = plotModel.Title,
                Width = width,
                Height = height
            };
            form.Controls.Add(plotView);
            form.ShowDialog();
        }

        public LineSeries? GetCurveByLabel(string label)
        {
            foreach (var s in plotModel.Series)
                if (s is LineSeries ls && ls.Title == label)
                    return ls;
            return null;
        }
        public void SavePlot(string filePath, int width = 800, int height = 600)
        {
            OxyPlot.WindowsForms.PngExporter.Export(plotModel, filePath, width, height, 96); // 96 is standard screen DPI
        }
    }
}