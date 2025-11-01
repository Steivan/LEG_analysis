using OxyPlot;
using LEG.CoreLib.Abstractions.SolarCalculations.Domain;
using LEG.OxyPlotHelper;
using static LEG.OxyPlotHelper.MultiPanelPlotContext;

namespace CalibrationApp
{
    public class PlotE3DcProfiles
    {
        internal static void ProductionProfilePlot(SolarProductionAggregateResults productionResults, int countYears = 1)
        {
            // Prepare data
            var theoreticalRelativeProductionPerMonthAndRoofList = new List<List<double[]>>();
            var effectiveRelativeProductionPerMonthAndRoofList = new List<List<double[]>>();

            var theoreticalProductionPerMonthList = new List<double[]>();
            var effectiveProductionPerMonthList = new List<double[]>();

            var maxPower = 0.0;
            for (var month = 1; month <= 12; month++)
            {
                var theoreticalRelativeProductionPerRoofList = new List<double[]>();
                var effectiveRelativeProductionPerRoofList = new List<double[]>();

                var theoreticalProductionPerMonth = new double[24];
                var effectiveProductionPerMonth = new double[24];
                for (var roof = 1; roof <= productionResults.DimensionRoofs; roof++)
                {
                    var theoreticalRelativeProductionPerMonthAndRoof = new double[24];
                    var effectiveRelativeProductionPerMonthAndRoof = new double[24];
                    for (var hour = 0; hour < 24; hour++)
                    {
                        theoreticalRelativeProductionPerMonthAndRoof[hour] = productionResults.TheoreticalAggregation[roof, month, hour];
                        effectiveRelativeProductionPerMonthAndRoof[hour] = productionResults.EffectiveAggregation[roof, month, hour];

                        theoreticalProductionPerMonth[hour] += theoreticalRelativeProductionPerMonthAndRoof[hour] *
                                                         productionResults.PeakPowerPerRoof[roof - 1] / countYears; // kW
                        effectiveProductionPerMonth[hour] +=
                            effectiveRelativeProductionPerMonthAndRoof[hour] * productionResults.PeakPowerPerRoof[roof - 1] / countYears; // kW
                    }
                    theoreticalRelativeProductionPerRoofList.Add(theoreticalRelativeProductionPerMonthAndRoof);
                    effectiveRelativeProductionPerRoofList.Add(effectiveRelativeProductionPerMonthAndRoof);
                }

                theoreticalRelativeProductionPerMonthAndRoofList.Add(theoreticalRelativeProductionPerRoofList);
                effectiveRelativeProductionPerMonthAndRoofList.Add(effectiveRelativeProductionPerRoofList);

                theoreticalProductionPerMonthList.Add(theoreticalProductionPerMonth);
                effectiveProductionPerMonthList.Add(effectiveProductionPerMonth);

                maxPower = Math.Max(maxPower, theoreticalProductionPerMonth.Max());
            }

            // Define plot axes styles
            var peakPowerBound = productionResults.PeakPowerPerRoof.Sum() / countYears;
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
                "Average Diurnal Profiles per Month" // Title
            );

            var panelYAxis0 = new AxisStyleRecord(
                false,
                0.2,                                // MajorTickSize
                0.05,                               // MinorTickSize
                1,
                panelXAxis.MajorGridlineStyle,                  // MajorGridlineStyle
                panelXAxis.MinorGridlineStyle,                  // MinorGridlineStyle
                panelXAxis.AxislineStyle,                       // AxislineStyle
                panelXAxis.AxislineColor,                       // AxislineColor
                panelXAxis.TextColor,                           // TextColor
                panelXAxis.TicklineColor,                       // TicklineColor
                8,
                true,
                true,
                "Relative Output"                // Title
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
                nRows: 2,
                nCols: 12,
                xMin: 0, xMax: 24,
                yMins: [ 0, 0 ],
                yMaxs: [ 1.1, powerMaxScale ],
                overallTitle: $"Computed Profiles for: {productionResults.SiteId}, max {maxPower:N1}kW/{peakPowerBound:N1}kWp, {productionResults.EffectiveYear[0]:N0}kWh (effective {productionResults.EvaluationYear})",
                panelXAxis: panelXAxis,
                panelYAxis: [panelYAxis0, panelYAxis1],
                legendPosition : -6         // "-" => outside, "6" => middle right 
                );

            // Plot profiles
            var monthLabels = new List<string>() {""," Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul,", "Aug", "Sep", "Oct", "Nov", "Dec"};
            var hourSupport = Enumerable.Range(0, 24).Select(h => (h + 0.5)).ToArray();
            for (var month = 1; month <= 12; month++)
            {
                var theoreticalRelativeProductionPerRoofList = theoreticalRelativeProductionPerMonthAndRoofList[month-1];
                var effectiveRelativeProductionPerRoofList = effectiveRelativeProductionPerMonthAndRoofList[month - 1];

                var theoreticalProductionPerMonth = theoreticalProductionPerMonthList[month - 1];
                var effectiveProductionPerMonth = effectiveProductionPerMonthList[month - 1];

                var theoreticalLabel = month == 1 ? "Theoretical" : "";
                var effectiveLabel = month == 1 ? "Effective" : "";
                // Plot of relative outputs per roof in upper panel
                for (var roof = 1; roof <= productionResults.DimensionRoofs; roof++)
                {
                    var roofColor = ManualColor(roof-1);
                    var roofLabel = month == 1 ? $"{roof} : {productionResults.PeakPowerPerRoof[roof-1]:F0} kWp" : "";
                    context.AddCurveToPanel(0, month - 1, hourSupport, effectiveRelativeProductionPerRoofList[roof -1],
                        roofColor, lineWidth: 2, lineStyle: LineStyle.Solid, label: roofLabel, filterZeros: true);
                    context.AddCurveToPanel(0, month - 1, hourSupport, theoreticalRelativeProductionPerRoofList[roof - 1], 
                        roofColor, lineWidth: 1, lineStyle: LineStyle.Dash, label: "", filterZeros: true);
                }

                // Plot of aggregate absolute productions in lower panel
                context.AddCurveToPanel(1, month - 1, hourSupport, theoreticalProductionPerMonth,
                    OxyColors.Blue, lineWidth: 1, lineStyle: LineStyle.Dash, label: theoreticalLabel, filterZeros: true);
                context.AddCurveToPanel(1, month - 1, hourSupport, effectiveProductionPerMonth,
                    OxyColors.DarkBlue, lineWidth: 2, lineStyle: LineStyle.Solid, label: effectiveLabel, filterZeros: true);

                // Solid Line on top of upper panel (100% level)
                context.AddCurveToPanel(0, month - 1, [0, 24], [1.0, 1.0],
                    OxyColors.Black, lineWidth: 1, lineStyle: LineStyle.Solid, label: "", filterZeros: false);

                // Add month labels and total production texts
                context.AddTextToPanel(0, month - 1, 12, 1.08, $"{monthLabels[month]}", OxyColors.Black,
                    textAlignment: 8, fontSize: 10, drawBox: false);
                context.AddTextToPanel(1, month - 1, 23, powerMaxScale * 0.98, $"{productionResults.EffectiveMonth[0][month]:N0} kWh", OxyColors.Black,
                    textAlignment: 9, fontSize: 10, drawBox: false);
            }


            //context.AddMarkerToPanel(1, 5, 12, 50, OxyColors.Green, MarkerType.Circle, 4);
            //context.AddTextToPanel(0, 0, 12, 80, "Peak", OxyColors.Black, textAlignment: 5, fontSize: 10, drawBox: false);

            // Show the plot in a window
            MultiPanelPlotContext.ShowPlot(context.PlotModel, 1200, 600); // or context.PlotModel.ShowPlot(...)

            // Optionally, save the plot as a PNG
            context.SavePlot("plot.png", width: 800, height: 600);
        }
    }

}
