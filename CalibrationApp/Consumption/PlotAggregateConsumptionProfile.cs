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
            double[] mean
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

            // Show the plot in a dialog window
            plotHelper.ShowPlot(width: 800, height: 600);

        }
    }
}
