#nullable enable
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LEG.OxyPlotHelper
{
    public class MultiPanelPlotContext
    {
        public PlotModel PlotModel { get; }
        private LinearAxis XAxis { get; }
        private LinearAxis YAxis { get; }
        private (double x, double y) Origin { get; }
        public int NRows { get; }
        public int NCols { get; }
        public double XMin { get; }
        public double XMax { get; }
        public double[] YMins { get; }
        public double[] YMaxs { get; }
        public (double x0, double x1, double y0, double y1)[,] PanelFrames { get; }
        public double[,] XScales { get; }
        public double[,] YScales { get; }
        AxisStyleRecord? PanelXAxis { get; }
        AxisStyleRecord[]? PanelYAxis { get; }

        public MultiPanelPlotContext(
            int nRows, int nCols,
            double xMin, double xMax,
            double[] yMins, double[] yMaxs,
            string overallTitle,
            AxisStyleRecord? panelXAxis = null,
            AxisStyleRecord[]? panelYAxis = null,
            int legendPosition = 9
            )
        {
            NRows = nRows;
            NCols = nCols;
            XMin = xMin;
            XMax = xMax;
            YMins = yMins;
            YMaxs = yMaxs;
            PanelXAxis = panelXAxis;
            PanelYAxis = panelYAxis;
            Origin = (PanelYAxis == null ? 0 : 0.75, PanelXAxis == null ? 0 : 0.25);

            PlotModel = new PlotModel { Title = overallTitle };

            var legendPlacement = legendPosition < 0 ? LegendPlacement.Outside : LegendPlacement.Inside;
            var legendLocation = (1 + ((Math.Abs(legendPosition) - 1)) % 9) switch
            {
                1 => LegendPosition.BottomLeft,
                2 => LegendPosition.BottomCenter,
                3 => LegendPosition.BottomRight,
                4 => LegendPosition.LeftMiddle,
                6 => LegendPosition.RightMiddle,
                7 => LegendPosition.TopLeft,        //  same as LeftTop
                8 => LegendPosition.TopCenter,
                9 => LegendPosition.TopRight,
                _ => LegendPosition.TopRight
            };

            ShowLegend(
                position: legendLocation,
                title: "Legend",
                orientation: LegendOrientation.Vertical,
                placement: legendPlacement,
                background: null,
                border: null,
                borderThickness: 1,
                fontSize: 8,
                titleFontSize: 10
                );

            PanelFrames = new (double, double, double, double)[nRows, nCols];
            XScales = new double[nRows, nCols];
            YScales = new double[nRows, nCols];

            var panelWidth = 1.0;
            var panelHeight = 1.0;

            for (var row = 0; row < nRows; row++)
            {
                for (var col = 0; col < nCols; col++)
                {
                    var x0 = Origin.x + col * panelWidth;
                    var x1 = x0 + panelWidth;
                    var y0 = Origin.y + nRows - (row + 1) * panelHeight;
                    var y1 = y0 + panelHeight;
                    PanelFrames[row, col] = (x0, x1, y0, y1);
                    XScales[row, col] = (x1 - x0) / (XMax - XMin);
                    YScales[row, col] = (y1 - y0) / (YMaxs[row] - YMins[row]);

                    PlotModel.Annotations.Add(new RectangleAnnotation
                    {
                        MinimumX = x0,
                        MaximumX = x1,
                        MinimumY = y0,
                        MaximumY = y1,
                        Layer = AnnotationLayer.BelowSeries,
                        Fill = OxyColors.Undefined,
                        Stroke = OxyColors.Black,
                        StrokeThickness = 1
                    });
                }
            }

            // Set up invisible axes for the entire model
            XAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = 0,
                Maximum = Origin.x + nCols,
                MajorTickSize = 0,
                MinorTickSize = 0,
                MajorGridlineStyle = LineStyle.None,
                MinorGridlineStyle = LineStyle.None,
                AxislineStyle = LineStyle.None,
                AxislineColor = OxyColors.Transparent,
                TextColor = OxyColors.Transparent,
                TicklineColor = OxyColors.Transparent,
                Title = ""
            };
            YAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Minimum = 0,
                Maximum = Origin.y + nRows,
                MajorTickSize = 0,
                MinorTickSize = 0,
                MajorGridlineStyle = LineStyle.None,
                MinorGridlineStyle = LineStyle.None,
                AxislineStyle = LineStyle.None,
                AxislineColor = OxyColors.Transparent,
                TextColor = OxyColors.Transparent,
                TicklineColor = OxyColors.Transparent,
                Title = ""
            };

            PlotModel.Axes.Add(XAxis);
            PlotModel.Axes.Add(YAxis);

            for (var row = 0; row < nRows; row++)
            {
                for (var col = 0; col < nCols; col++)
                {
                    var bottomRow = row == NRows - 1;
                    var legendCol = col == 6;
                    var leftColumn = col == 0;
                    if (PanelXAxis != null)
                        AddAxisToPanel(row, col, PanelXAxis,
                            showMajorGrid: true, showMinorGrid: false,
                            showMajorTick: bottomRow, showMinorTick: bottomRow,
                            showText: bottomRow, showLegend: bottomRow && legendCol
                        );

                    if (PanelYAxis != null)
                        if (PanelYAxis[row] != null)
                            AddAxisToPanel(row, col, PanelYAxis[row],
                                showMajorGrid: true, showMinorGrid: true,
                                showMajorTick: leftColumn, showMinorTick: leftColumn,
                                showText: leftColumn, showLegend: leftColumn
                            );
                }
            }
        }

        public record AxisStyleRecord(
                bool IsXaxis,
                double MajorTickSize,
                double MinorTickSize,
                int NrDecimals,
                LineStyle MajorGridlineStyle,
                LineStyle MinorGridlineStyle,
                LineStyle AxislineStyle,
                OxyColor AxislineColor,
                OxyColor TextColor,
                OxyColor TicklineColor,
                int FontSize,
                bool PlotFirst,
                bool PlotLast,
                string Title
            );

        public static OxyColor ManualColor(int color)
        {
            var myColors = new[]
            {
                OxyColor.Parse("#1F77B4"), // Blue
                OxyColor.Parse("#FF7F0E"), // Orange
                OxyColor.Parse("#2CA02C"), // Green
                OxyColor.Parse("#D62728"), // Red
                OxyColor.Parse("#9467BD"), // Purple
                OxyColor.Parse("#8C564B"), // Brown
                OxyColor.Parse("#E377C2"), // Pink
                OxyColor.Parse("#7F7F7F"), // Gray
                OxyColor.Parse("#BCBD22"), // Olive
                OxyColor.Parse("#17BECF") // Cyan
            };

            return myColors[color % myColors.Length];
        }

        public void ShowLegend(
            LegendPosition position = LegendPosition.TopRight,
            string title = "Legend",
            LegendOrientation orientation = LegendOrientation.Vertical,
            LegendPlacement placement = LegendPlacement.Outside,
            OxyColor? background = null,
            OxyColor? border = null,
            int borderThickness = 1,
            int fontSize = 12,
            int titleFontSize = 14)
        {
            PlotModel.IsLegendVisible = true;
            var legend = new Legend
            {
                LegendPosition = position,
                LegendTitle = title,
                LegendOrientation = orientation,
                LegendPlacement = placement,
                LegendBackground = background ?? OxyColors.White,
                LegendBorder = border ?? OxyColors.Black,
                LegendBorderThickness = borderThickness,
                LegendFontSize = fontSize,
                LegendTitleFontSize = titleFontSize
            };
            PlotModel.Legends.Clear();
            PlotModel.Legends.Add(legend);
        }

        public static (double major, double minor, int nDecimals) GetAxisTickSizes(double scale)
        {
            var log10 = Math.Log10(scale);
            var floorLog10 = Math.Floor(log10);
            var baseSize = Math.Pow(10, floorLog10 - 1);
            double majorTickSize, minorTickSize;

            switch (log10 - floorLog10)
            {
                case < 0.15:
                    majorTickSize = 2 * baseSize;
                    minorTickSize = majorTickSize / 4;
                    break;
                case < 0.5:
                    majorTickSize = 5 * baseSize;
                    minorTickSize = majorTickSize / 5;
                    break;
                case < 0.85:
                    majorTickSize = 10 * baseSize;
                    minorTickSize = majorTickSize / 5;
                    break;
                default:
                    majorTickSize = 20 * baseSize;
                    minorTickSize = majorTickSize / 4;
                    break;
            }

            var nDecimals = majorTickSize < 1 ? (int)(-Math.Floor(Math.Log10(majorTickSize))) : 0;

            return (majorTickSize, minorTickSize, nDecimals);
        }

        private (double xNorm, double yNorm) DataToNorm(int row, int col, double xData, double yData)
        {
            var (x0, _, y0, _) = PanelFrames[row, col];
            return (x0 + XScales[row, col] * (xData - XMin), y0 + YScales[row, col] * (yData - YMins[row]));
        }

        private void AddAxisToPanel(int row, int col, AxisStyleRecord axis,
            bool showMajorGrid = true, bool showMinorGrid = false,
            bool showMajorTick = true, bool showMinorTick = true,
            bool showText = true, bool showLegend = false,
            int legendFontSize = 10)
        {
            var isXAxis = axis.IsXaxis;

            var axisMin = (isXAxis ? XMin : YMins[row]);
            var axisMax = (isXAxis ? XMax : YMaxs[row]);

            double[] valueSupport;
            double[] valueGrid;
            double[] valueTick;
            double[] xData;
            double[] yData;

            var deltaMajor = (isXAxis ? Origin.y / YScales[row, col] : Origin.x / XScales[row, col]) / 5;
            var deltaMinor = deltaMajor / 2;

            // Minor ticks and gridlines
            var indexLoMinorGrid = (int)Math.Ceiling(axisMin / axis.MinorTickSize) + 1;
            var indexHiMinorGrid = (int)Math.Floor(axisMax / axis.MinorTickSize) - 1; ;
            var indexLoMinorTick = (int)Math.Ceiling(axisMin / axis.MinorTickSize) + (axis.PlotFirst ? 0 : 1);
            var indexHiMinorTick = (int)Math.Floor(axisMax / axis.MinorTickSize) - (axis.PlotLast ? 0 : 1); ;
            for (var i = indexLoMinorTick; i <= indexHiMinorTick; i++)
            {
                var support = axis.MinorTickSize * i;
                valueSupport = [support, support];
                valueGrid = isXAxis ? [YMins[row], YMaxs[row]] : [XMin, XMax];
                valueTick = isXAxis ? [YMins[row], YMins[row] - deltaMinor] : [XMin, XMin - deltaMinor];

                // Minor gridlines
                if (showMinorGrid && i >= indexLoMinorGrid && i <= indexHiMinorGrid)
                {
                    xData = isXAxis ? valueSupport : valueGrid;
                    yData = isXAxis ? valueGrid : valueSupport;
                    AddCurveToPanel(row, col, xData, yData, axis.TicklineColor, lineWidth: 1, lineStyle: axis.MinorGridlineStyle);
                }

                // Minor ticks
                xData = isXAxis ? valueSupport : valueTick;
                yData = isXAxis ? valueTick : valueSupport;
                if (showMinorTick) AddCurveToPanel(row, col, xData, yData, axis.AxislineColor, lineWidth: 1, lineStyle: axis.AxislineStyle);
            }

            // Major ticks, gridlines and text
            var indexLoMajorGrid = (int)Math.Ceiling(axisMin / axis.MajorTickSize) + 1;
            var indexHiMajorGrid = (int)Math.Floor(axisMax / axis.MajorTickSize) - 1;
            var indexLoMajorTick = (int)Math.Ceiling(axisMin / axis.MajorTickSize) + (axis.PlotFirst ? 0 : 1);
            var indexHiMajorTick = (int)Math.Floor(axisMax / axis.MajorTickSize) - (axis.PlotLast ? 0 : 1);
            for (var i = indexLoMajorTick; i <= indexHiMajorTick; i++)
            {
                var support = axis.MajorTickSize * i;
                valueSupport = [support, support];
                valueGrid = isXAxis ? [YMins[row], YMaxs[row]] : [XMin, XMax];
                valueTick = isXAxis ? [YMins[row], YMins[row] - deltaMajor] : [XMin, XMin - deltaMajor];

                // Major gridlines
                if (showMajorGrid && i >= indexLoMajorGrid && i <= indexHiMajorGrid)
                {
                    xData = isXAxis ? valueSupport : valueGrid;
                    yData = isXAxis ? valueGrid : valueSupport;
                    AddCurveToPanel(row, col, xData, yData, axis.TicklineColor, lineWidth: 1, lineStyle: axis.MajorGridlineStyle);
                }

                // Major ticks
                xData = isXAxis ? valueSupport : valueTick;
                yData = isXAxis ? valueTick : valueSupport;
                if (showMajorTick) AddCurveToPanel(row, col, xData, yData, axis.AxislineColor, lineWidth: 1, lineStyle: axis.AxislineStyle);

                // BaseLine
                double[] lineRange = isXAxis ? [XMin, XMax] : [YMins[row], YMaxs[row]];
                double[] linePosition = isXAxis ? [YMins[row], YMins[row]] : [XMin, XMin];
                xData = isXAxis ? lineRange : linePosition;
                yData = isXAxis ? linePosition : lineRange;
                if (showMinorGrid) AddCurveToPanel(row, col, xData, yData, axis.AxislineColor, lineWidth: 1, lineStyle: axis.AxislineStyle);

                // Text
                var formatSpecifier = $"F{axis.NrDecimals}";
                var text = string.Format($"{{0:{formatSpecifier}}}", support);
                if (showText)
                    if (isXAxis)
                    {
                        var x = support;
                        var y = YMins[row] - deltaMajor * 1.2;
                        AddTextToPanel(row, col, x, y, text, axis.AxislineColor, textAlignment: 8, fontSize: axis.FontSize, drawBox: false);
                    }
                    else
                    {
                        var y = support;
                        var x = XMin - deltaMajor * 1.2;
                        AddTextToPanel(row, col, x, y, text, axis.AxislineColor, textAlignment: 6, fontSize: axis.FontSize, drawBox: false);
                    }
            }

            // Axis Title
            if (showLegend)
                if (isXAxis)
                {
                    var x = XMin;
                    var y = YMins[row] - deltaMajor * 2.5;
                    AddTextToPanel(row, col, x, y, axis.Title, axis.AxislineColor, textAlignment: 8, fontSize: legendFontSize, drawBox: false);
                }
                else
                {
                    var y = (YMins[row] + YMaxs[row]) / 2;
                    var x = XMin - deltaMajor * 2.5;
                    AddTextToPanel(row, col, x, y, axis.Title, axis.AxislineColor, textAlignment: 2, verticalOrientation: true, fontSize: legendFontSize, drawBox: false);
                }
        }

        // Add a curve to a specific panel
        public LineSeries AddCurveToPanel(
            int row, int col,
            double[] xData, double[] yData,
            OxyColor? color,
            int lineWidth = 1,
            LineStyle lineStyle = LineStyle.Solid,
            string label = "",
            bool filterZeros = false
            )
        {
            var lowestIndex = 0;
            var highestIndex = xData.Length - 1;
            if (filterZeros)
            {
                var indices = yData
                    .Select((value, index) => new { value, index })
                    .Where(x => x.value > 0)
                    .Select(x => x.index)
                    .ToList();
                lowestIndex = indices.Count > 0 ? indices.First() : -1;
                highestIndex = indices.Count > 0 ? indices.Last() : -1;
                if (lowestIndex > 0) lowestIndex -= 1;
                if (highestIndex < yData.Length - 1) highestIndex += 1;
            }

            var points = new List<DataPoint>();
            if (lowestIndex >= 0 && highestIndex >= lowestIndex)
            {
                for (var i = lowestIndex; i <= highestIndex; i++)
                {
                    var (xNorm, yNorm) = DataToNorm(row, col, xData[i], yData[i]);
                    points.Add(new DataPoint(xNorm, yNorm));
                }
            }

            var lineSeries = new LineSeries
            {
                Title = label,
                Color = color ?? OxyColors.Automatic, // Use OxyColors.Automatic or your default color
                LineStyle = lineStyle,
                StrokeThickness = lineWidth
            };

            lineSeries.Points.AddRange(points);
            PlotModel.Series.Add(lineSeries);

            return lineSeries;
        }

        public AreaSeries AddAreaToPanel(
            int row, int col,
            double[] xData, double[] y1Data, double[] y2Data,
            OxyColor fillColor,
            OxyColor? strokeColor = null,
            double strokeThickness = 1,
            string label = ""
        )
        {
            var points1 = new List<DataPoint>();
            var points2 = new List<DataPoint>();

            for (var i = 0; i < xData.Length; i++)
            {
                var (xNorm1, yNorm1) = DataToNorm(row, col, xData[i], y1Data[i]);
                points1.Add(new DataPoint(xNorm1, yNorm1));

                var (xNorm2, yNorm2) = DataToNorm(row, col, xData[i], y2Data[i]);
                points2.Add(new DataPoint(xNorm2, yNorm2));
            }

            var areaSeries = new AreaSeries
            {
                Title = label,
                Fill = fillColor,
                Color = strokeColor ?? OxyColors.Undefined,
                StrokeThickness = strokeThickness,
                LineStyle = strokeColor.HasValue ? LineStyle.Solid : LineStyle.None
            };

            areaSeries.Points.AddRange(points1);
            areaSeries.Points2.AddRange(points2);

            PlotModel.Series.Add(areaSeries);
            return areaSeries;
        }

        // Add a marker to a specific panel
        public void AddMarkerToPanel(int row, int col, double x, double y, OxyColor color, MarkerType markerType,
            double size)
        {
            var (xNorm, yNorm) = DataToNorm(row, col, x, y);
            var scatter = new ScatterSeries
            {
                MarkerType = markerType,
                MarkerFill = color,
                MarkerSize = size
            };
            scatter.Points.Add(new ScatterPoint(xNorm, yNorm));
            PlotModel.Series.Add(scatter);
        }

        // Add text to a specific panel
        public void AddTextToPanel(int row, int col, double x, double y, string text,
            OxyColor color,
            int textAlignment = 5,
            bool verticalOrientation = false,
            double fontSize = 8,
            bool drawBox = false)
        {
            var (xNorm, yNorm) = DataToNorm(row, col, x, y);
            textAlignment = (textAlignment - 1) % 9;
            var horizontalAlignment = textAlignment % 3;
            var verticalAlignment = textAlignment / 3;
            PlotModel.Annotations.Add(new TextAnnotation
            {
                Text = text,
                TextPosition = new DataPoint(xNorm, yNorm),
                TextColor = color,
                FontSize = fontSize,
                TextHorizontalAlignment = horizontalAlignment == 0 ? HorizontalAlignment.Left : horizontalAlignment == 1 ? HorizontalAlignment.Center : HorizontalAlignment.Right,
                TextVerticalAlignment = verticalAlignment == 0 ? VerticalAlignment.Bottom : verticalAlignment == 1 ? VerticalAlignment.Middle : VerticalAlignment.Top,
                TextRotation = verticalOrientation ? -90 : 0,
                Background = drawBox ? color : OxyColors.Undefined, // or OxyColors.Transparent
                Stroke = drawBox ? color : OxyColors.Undefined,
            });
        }


        public static void ShowPlot(PlotModel model, int width = 1200, int height = 600)
        {
            var plotView = new OxyPlot.WindowsForms.PlotView
            {
                Model = model,
                Dock = System.Windows.Forms.DockStyle.Fill
            };
            var form = new System.Windows.Forms.Form
            {
                Text = model.Title,
                Width = width,
                Height = height
            };
            form.Controls.Add(plotView);
            form.ShowDialog();
        }
        public void SavePlot(string filePath, int width = 800, int height = 600)
        {
            OxyPlot.WindowsForms.PngExporter.Export(PlotModel, filePath, width, height, 96); // 96 is standard screen DPI
        }
    }
}