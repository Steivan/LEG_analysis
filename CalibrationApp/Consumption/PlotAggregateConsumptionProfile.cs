using LEG.OxyPlotHelper;
using OxyPlot;

namespace CalibrationApp.Consumption
{
    internal class PlotAggregateConsumptionProfile
    {
        internal static void PlotDiurnalProfiles(string siteId,
            double[] p90,
            double[] p75,
            double[] p50,
            double[] p25,
            double[] mean,
            double[] std,
            double[] fourierMean
            )
        {
            const int hoursPerDay = 24;
            const int quartersPerHour = 4;
            const int quartersPerDay = hoursPerDay * quartersPerHour;

            // Prepare data
            var hourSupport = new double[quartersPerDay];
            var conversionFactor = quartersPerHour / 1000.0; // Wh to kW
            for (var i=0; i< quartersPerDay; i++)
            {
                hourSupport[i] = (double)i / quartersPerHour;
                p90[i] *= conversionFactor;
                p75[i] *= conversionFactor;
                p50[i] *= conversionFactor;
                p25[i] *= conversionFactor;
                mean[i] *= conversionFactor;
                std[i] *= conversionFactor;
                fourierMean[i] *= conversionFactor;
            }

            // Define plot axes styles
            var maxPower = p90.Max();
            var powerMaxScale = maxPower * 1.1;

            // Create the plot helper
            var plotHelper = new OxyPlotHelper(
                title: $"Aggregate Diurnal Profiles for {siteId}",
                xLabel: "Hour of Day",
                yLabel: "Power [kW]",
                xMin: 0, xMax: 24,
                yMin: 0, yMax: powerMaxScale
            );

            // After creating plotHelper
            plotHelper.ShowLegend(OxyPlot.Legends.LegendPosition.TopLeft);

            // Plot InterQuartile range
            plotHelper.FillTwoCurves(hourSupport, p75, p25,
                OxyColors.LightSteelBlue, alpha: 128, fillLabel: "P25-P75");

            // Plot 90% percentile, median and mean 
            plotHelper.AddCurve(hourSupport, p90, 
                OxyColors.Red, lineWidth: 1, lineStyle: LineStyle.Dot, curveLabel: "P90");

            plotHelper.AddCurve(hourSupport, p50,
                OxyColors.Blue, lineWidth: 2, lineStyle: LineStyle.Solid, curveLabel: "Median");

            plotHelper.AddCurve(hourSupport, mean,
                OxyColors.Green, lineWidth: 2, lineStyle: LineStyle.Solid, curveLabel: "Mean");

            plotHelper.AddCurve(hourSupport, fourierMean,
                OxyColors.Black, lineWidth: 1, lineStyle: LineStyle.Dash, curveLabel: "Fourier Mean");

            for (var i=0; i< quartersPerDay; i++)
                plotHelper.AddCurve([hourSupport[i], hourSupport[i]], [mean[i]-std[i], mean[i]+std[i]],
                    OxyColors.Green, lineWidth: 1, lineStyle: LineStyle.Solid, curveLabel: i==0 ? "StdDev" : "");

            // Show the plot in a dialog window
            plotHelper.ShowPlot(width: 800, height: 600);

        }

        internal static void Plot13x4Amplitudes(string siteId, int countPeaks, 
            double[] support13, double[,] meanAmplitudes,
            double[] support365, double[,] meanFourierAmplitudes365
            )
        {
            // Prepare data
            var maxAmplitude = meanAmplitudes.Max2D() * 1.1;

            // Create the plot helper
            var plotHelper = new OxyPlotHelper(
                title: $"Aggregate Diurnal Profiles for {siteId}",
                xLabel: "Days",
                yLabel: "Power [W]",
                xMin: 0, xMax: 366,
                yMin: 0, yMax: maxAmplitude
            );

            // After creating plotHelper
            plotHelper.ShowLegend(OxyPlot.Legends.LegendPosition.TopLeft);

            for (var peakIdx = 0; peakIdx <= countPeaks; peakIdx++)
            {
                var color = OxyColor.FromHsv((double)peakIdx / (1 + countPeaks), 1.0, 1.0);


                var meanValues = Enumerable.Range(0, support13.Length).Select(i => meanAmplitudes[peakIdx, i]).ToArray();
                var y_prior = meanValues[^1];
                for (var i = 0; i < support13.Length; i++)
                {
                    var label = i == 0 ? (peakIdx == 0 ? "Mean baseline" : $"Mean Peak {peakIdx}") : "";
                    var x1 = support13[i] - 14;
                    var x2 = support13[i] + 14;
                    var y = meanValues[i];
                    //plotHelper.AddCurve([x1, x1], [y_prior, y],
                    //    OxyColors.Gray, lineWidth: 1, lineStyle: LineStyle.Dot, curveLabel: "");
                    plotHelper.AddCurve([x1, x2], [y, y],
                        color, lineWidth: 2, lineStyle: LineStyle.Solid, curveLabel: label);
                    y_prior = y;

                }

                plotHelper.AddMarkers(support13, meanValues,
                    color, markerType: MarkerType.Circle, markerSize: 4, markerLabel: "");

                plotHelper.AddCurve(support365,
                    Enumerable.Range(0, support365.Length).Select(i => meanFourierAmplitudes365[peakIdx, i]).ToArray(),
                    color, lineWidth: 1, lineStyle: LineStyle.Dash, curveLabel: peakIdx == 0 ? "Fourier baseline" : $"Fourier Peak {peakIdx}");
            }

            // Show the plot in a dialog window
            plotHelper.ShowPlot(width: 800, height: 600);

        }
    }
}
