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
            int[] daysOfMonths = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

            // Prepare data
            var aggregate13 = new double[support13.Length];
            var aggregate365 = new double[support365.Length];
            for (var peakIdx = 0; peakIdx <= countPeaks; peakIdx++)
            {
                for (var i = 0; i < support13.Length; i++)
                {
                    aggregate13[i] += meanAmplitudes[peakIdx, i];
                }
                for (var i = 0; i < support365.Length; i++)
                {
                    aggregate365[i] += meanFourierAmplitudes365[peakIdx, i];
                }
            }
            var maxAmplitude = aggregate365.Max() * 1.1;

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

            var color = OxyColors.Black;
            var y_prior = 0.0;
            for (var peakIdx = 0; peakIdx <= countPeaks; peakIdx++)
            {
                color = OxyColor.FromHsv((double)peakIdx / (1 + countPeaks), 1.0, 1.0);

                var meanValues = Enumerable.Range(0, support13.Length).Select(i => meanAmplitudes[peakIdx, i]).ToArray();
                y_prior = meanValues[^1];
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

            color = OxyColors.Black;
            y_prior = aggregate13[^1];
            for (var i = 0; i < support13.Length; i++)
            {
                var label = i == 0 ?  "Mean aggregate" : "";
                var x1 = support13[i] - 14;
                var x2 = support13[i] + 14;
                var y = aggregate13[i];
                //plotHelper.AddCurve([x1, x1], [y_prior, y],
                //    OxyColors.Gray, lineWidth: 1, lineStyle: LineStyle.Dot, curveLabel: "");
                plotHelper.AddCurve([x1, x2], [y, y],
                    color, lineWidth: 2, lineStyle: LineStyle.Solid, curveLabel: label);
                y_prior = y;

            }

            plotHelper.AddMarkers(support13, aggregate13,
                color, markerType: MarkerType.Circle, markerSize: 4, markerLabel: "");

            plotHelper.AddCurve(support365, aggregate365,
                color, lineWidth: 1, lineStyle: LineStyle.Dash, curveLabel: "Fourier aggregate");

            var dayOfYear = 0;
            foreach (var daysOfMonth in daysOfMonths)
            {
                plotHelper.AddCurve([dayOfYear, dayOfYear], [0, maxAmplitude],
                    OxyColors.Black, lineWidth: 1, lineStyle: LineStyle.Solid, curveLabel: "");
                dayOfYear += daysOfMonth;
            }

            // Show the plot in a dialog window
            plotHelper.ShowPlot(width: 800, height: 600);
        }
    }
}
