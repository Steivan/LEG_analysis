using LEG.OxyPlotHelper;
using OxyPlot;
using CalibrationApp.Helpers;
using static CalibrationApp.Consumption.DiurnalSeasonalAnalysis;
using static LEG.OxyPlotHelper.MultiPanelPlotContext;

namespace CalibrationApp.Consumption
{
    public class PlotDiurnalConsumptionProfiles
    {
        internal static void Plot13x4DiurnalProfiles(string siteId, List<TimeSlotStats> timeSlotStats)
        {
            const int periods13x4 = 13;
            const int hoursPerDay = 24;
            const int quartersPerHour = 4;
            const int minutesPerQuarter = 60 / quartersPerHour;
            const int quartersPerDay = hoursPerDay * quartersPerHour;

            // Prepare data
            var hourSupport = new double[quartersPerDay];
            var count = new int[periods13x4, quartersPerDay];
            var mean = new double[periods13x4, quartersPerDay];
            var max = new double[periods13x4, quartersPerDay];
            var p25 = new double[periods13x4,quartersPerDay];
            var p50 = new double[periods13x4, quartersPerDay];
            var p75 = new double[periods13x4, quartersPerDay];
            var p90 = new double[periods13x4, quartersPerDay];

            var conversionFactor = quartersPerHour / 1000.0; // Wh to kW
            foreach (var timeSlot in timeSlotStats)
            {
                var p13 = timeSlot.Period13x4 - 1;
                var time = timeSlot.TimeOfDay;
                var i = time.Hours * quartersPerHour + time.Minutes / minutesPerQuarter;
                if (p13 == 0)
                {
                    hourSupport[i] = (double)i / quartersPerHour;
                }
                count[p13, i] = timeSlot.Count;
                mean[p13, i] = timeSlot.Mean * conversionFactor;
                max[p13, i] = timeSlot.Max * conversionFactor;
                p25[p13, i] = timeSlot.P25 * conversionFactor;
                p50[p13, i] = timeSlot.P50 * conversionFactor;
                p75[p13, i] = timeSlot.P75 * conversionFactor;
                p90[p13, i] = timeSlot.P90 * conversionFactor;
            }

            // Define plot axes styles
            var maxPower = p90.Max2D();
            var powerMaxScale = maxPower * 1.1;
            var (majorTickSizer, minorTickSize, nDecimals) = GetAxisTickSizes(powerMaxScale);

            var panelXAxis = new AxisStyleRecord(
                true,
                3.0,                                    // MajorTickSize
                1.0,                                    // MinorTickSize
                0,
                LineStyle.Dash,                                    // MajorGridlineStyle
                LineStyle.Dot,                                     // MinorGridlineStyle
                LineStyle.Solid,                                    // AxislineStyle
                OxyColors.Black,                        // AxislineColor
                OxyColors.LightGray,                       // TextColor
                OxyColors.LightGray,                    // TicklineColor
                7,
                false,
                false,
                "Average Diurnal Profiles per 4 Weeks Period" // Title
            );

            var panelYAxis1 = new AxisStyleRecord(
                false,
                majorTickSizer,                                 // MajorTickSize
                minorTickSize,                                  // MinorTickSize
                nDecimals,
                panelXAxis.MajorGridlineStyle,                  // MajorGridlineStyle
                panelXAxis.MinorGridlineStyle,                  // MinorGridlineStyle
                panelXAxis.AxislineStyle,                       // AxislineStyle
                panelXAxis.AxislineColor,                       // AxislineColor
                panelXAxis.TextColor,                           // TextColor
                panelXAxis.TicklineColor,                       // TicklineColor
                10,
                true,
                true,
                "Power [kW]"                               // Title
            );

            // Initialize plot context
            var context = new MultiPanelPlotContext(
                nRows: 1,
                nCols: periods13x4,
                xMin: 0, xMax: hoursPerDay,
                yMins: [0],
                yMaxs: [powerMaxScale],
                overallTitle: $"Consumption Profiles for: {siteId}, Max P90 Power = {maxPower:N1}kW",
                panelXAxis: panelXAxis,
                panelYAxis: [panelYAxis1],
                legendPosition: -6         // "-" => outside, "6" => middle right 
                );

            // Plot profiles
            var period13x4Labels = new List<string>() { "P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8", "P9", "P10", "P11", "P12", "P13" };
            for (var period = 0; period < periods13x4; period++)
            {
                // Plot InterQuartile range
                context.AddAreaToPanel(0, period,
                    hourSupport, Convert2DArray.GetRow(p75, period, quartersPerDay), Convert2DArray.GetRow(p25, period, quartersPerDay),
                    OxyColors.LightSteelBlue, strokeColor: null, strokeThickness: 1, label: period == 0 ? "P25-P75" : "");

                // Plot 90% percentile, median and mean 
                context.AddCurveToPanel(0, period, hourSupport, Convert2DArray.GetRow(p90, period, quartersPerDay),
                    OxyColors.Red, lineWidth: 1, lineStyle: LineStyle.Dot, label: period==0 ? "P90" : "", filterZeros: false);

                //context.AddCurveToPanel(0, period, hourSupport, GetRow(p75, period, quartersPerDay),
                //    OxyColors.Gray, lineWidth: 1, lineStyle: LineStyle.Dash, label: period == 0 ? "P75" : "", filterZeros: false);

                context.AddCurveToPanel(0, period, hourSupport, Convert2DArray.GetRow(p50, period, quartersPerDay),
                    OxyColors.Blue, lineWidth: 2, lineStyle: LineStyle.Solid, label: period == 0 ? "Median" : "", filterZeros: false);

                context.AddCurveToPanel(0, period, hourSupport, Convert2DArray.GetRow(mean, period, quartersPerDay),
                    OxyColors.Green, lineWidth: 2, lineStyle: LineStyle.Solid, label: period == 0 ? "Mean" : "", filterZeros: false);

                // Add period labels
                context.AddTextToPanel(0, period, hoursPerDay / 2, maxPower, $"{period13x4Labels[period]}", OxyColors.Black,
                    textAlignment: 2, fontSize: 10, drawBox: false);
            }

            // Show the plot in a window
            MultiPanelPlotContext.ShowPlot(context.PlotModel, 1200, 600); // or context.PlotModel.ShowPlot(...)


            // Optionally, save the plot as a PNG
            context.SavePlot("plot.png", width: 800, height: 600);
        }
    }

}
