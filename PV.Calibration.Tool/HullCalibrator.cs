using LEG.PV.Core.Models;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using static LEG.PV.Data.Processor.DataRecords;

namespace PV.Calibration.Tool
{
    public class HullCalibrator
    {
        public static (double EthaSystem, double LDeg, double EthaSystemUncertainty, double LDegUncertainty) CalibrateTrend(
            List<PvRecord> pvRecords, 
            double installedPower,
            int periodsPerHour,
            PvModelParams pvModelParams)
        {
            const double daysPerYear = 365.2522;

            var firstRecordDate = pvRecords.First().Timestamp;
            var secondRecordDate = pvRecords[1].Timestamp;
            var lastRecordDate = pvRecords.Last().Timestamp;
            var nrYears = lastRecordDate.Year - firstRecordDate.Year + 1;

            var minutesPerPeriod = (secondRecordDate - firstRecordDate).Minutes;
            //var periodsPerHour = 60 / minutesPerPeriod;
            var recordsPerDay = 24 * periodsPerHour;

            var maxMeasuredPerPeriod = new double[nrYears, 12, recordsPerDay];
            var maxTheoreticalPerPeriod = new double[nrYears, 12, recordsPerDay];
            var maxTheoreticalPerMonth = new double[nrYears, 12];
            var monthHasData = new bool[nrYears, 12];
            var timeLagPerMonth = new double[nrYears, 12];
            var maxRatioPerMonth = new double[nrYears, 12];

            foreach (var record in pvRecords)
            {
                var yearIndex = record.Timestamp.Year - firstRecordDate.Year;
                var monthIndex = record.Timestamp.Month - 1;
                var dayIndex = record.Timestamp.Day;
                var timeIndex = record.Timestamp.Hour * periodsPerHour + (record.Timestamp.Minute / minutesPerPeriod);

                var theoreticalPower = PvJacobian.EffectiveCellPower(installedPower, periodsPerHour, record.DirectGeometryFactor, record.DiffuseGeometryFactor, record.CosSunElevation, 
                    record.GlobalHorizontalIrradiance, record.SunshineDuration, record.DiffuseHorizontalIrradiance, record.AmbientTemp, record.WindSpeed, record.SnowDepth, record.Age,
                    ethaSys: pvModelParams.Etha, gamma: pvModelParams.Gamma, u0: pvModelParams.U0, u1: pvModelParams.U1, lDegr: pvModelParams.LDegr);
                var measuredPower = record.MeasuredPower;

                if (theoreticalPower > 0.0)
                {
                    if (measuredPower > maxMeasuredPerPeriod[yearIndex, monthIndex, timeIndex])
                    {
                        maxMeasuredPerPeriod[yearIndex, monthIndex, timeIndex] = measuredPower;
                        maxTheoreticalPerPeriod[yearIndex, monthIndex, timeIndex] = theoreticalPower;
                    }

                    maxTheoreticalPerMonth[yearIndex, monthIndex] = Math.Max(maxTheoreticalPerMonth[yearIndex, monthIndex], theoreticalPower);
                    monthHasData[yearIndex, monthIndex] = true;
                }
            }

            var firstYear = firstRecordDate.Year;
            var lowTime = 11 * minutesPerPeriod;
            var highTime = 13 * minutesPerPeriod;
            for (var yearIndex = 0; yearIndex < nrYears; yearIndex++)
            {
                var year = firstYear + yearIndex;
                for (var monthIndex = 0; monthIndex < 12; monthIndex++)
                {
                    if (monthHasData[yearIndex, monthIndex])
                    {
                        timeLagPerMonth[yearIndex, monthIndex] = (new DateTime(year, 1 + monthIndex, 15, 0, 0, 0) - firstRecordDate).Days / daysPerYear;

                        var dayIntegralTheoretical = 0.0;
                        for (var timeIndex = 0; timeIndex < recordsPerDay; timeIndex++)
                        {
                            dayIntegralTheoretical += maxTheoreticalPerPeriod[yearIndex, monthIndex, timeIndex];
                        }

                        var sumTheoreticalLow = 0.0;
                        var sumTheoreticalMid = 0.0;
                        var sumTheoreticalHigh = 0.0;

                        var sumMeasuredlLow = 0.0;
                        var sumMeasuredMid = 0.0;
                        var sumMeasuredHigh = 0.0;

                        var sumTheoretical = 0.0;

                        for (var timeIndex = 0; timeIndex < recordsPerDay; timeIndex++)
                        {
                            var maxTheoretical = maxTheoreticalPerPeriod[yearIndex, monthIndex, timeIndex];
                            var maxMeasured = maxMeasuredPerPeriod[yearIndex, monthIndex, timeIndex];
                            sumTheoretical += maxTheoretical;
                            if (sumTheoretical < dayIntegralTheoretical / 3)
                            {
                                sumTheoreticalLow += maxTheoretical;
                                sumMeasuredlLow += maxMeasured;
                            }
                            else if (sumTheoretical < dayIntegralTheoretical * 2 / 3)
                            {
                                sumTheoreticalMid += maxTheoretical;
                                sumMeasuredMid += maxMeasured;
                            }
                            else
                            {
                                sumTheoreticalHigh += maxTheoretical;
                                sumMeasuredHigh += maxMeasured;
                            }
                        }
                        var ratioLow = sumTheoreticalLow > 0.0 ? sumMeasuredlLow / sumTheoreticalLow : 0.0;
                        var ratioMid = sumTheoreticalMid > 0.0 ? sumMeasuredMid / sumTheoreticalMid : 0.0;
                        var ratioHigh = sumTheoreticalHigh > 0.0 ? sumMeasuredHigh / sumTheoreticalHigh : 0.0;

                        maxRatioPerMonth[yearIndex, monthIndex] = Math.Max(ratioLow, Math.Max(ratioMid, ratioHigh));
                    }
                }
            }

            // --- Linear Regression ---
            var xData = new List<double>();
            var yData = new List<double>();

            for (int yearIndex = 0; yearIndex < nrYears; yearIndex++)
            {
                for (int monthIndex = 0; monthIndex < 12; monthIndex++)
                {
                    if (monthHasData[yearIndex, monthIndex])
                    {
                        xData.Add(timeLagPerMonth[yearIndex, monthIndex]);
                        yData.Add(maxRatioPerMonth[yearIndex, monthIndex]);
                    }
                }
            }

            if (xData.Count < 2)
            {
                // Not enough data for a regression, return default or throw exception
                return (1.0, 0.0, 0.0, 0.0);
            }

            // Perform the linear regression
            (double intercept, double slope) = Fit.Line(xData.ToArray(), yData.ToArray());

            // Manually calculate standard errors (uncertainty)
            int n = xData.Count;
            double sumOfSquaredResiduals = 0.0;
            for (int i = 0; i < n; i++)
            {
                double predictedY = slope * xData[i] + intercept;
                sumOfSquaredResiduals += Math.Pow(yData[i] - predictedY, 2);
            }

            double s = Math.Sqrt(sumOfSquaredResiduals / (n - 2));
            double xBar = xData.Average();
            double ssxx = xData.Sum(x => Math.Pow(x - xBar, 2));

            double interceptUncertainty = s * Math.Sqrt((1.0 / n) + (Math.Pow(xBar, 2) / ssxx));
            double slopeUncertainty = s / Math.Sqrt(ssxx);

            // The intercept is the initial system efficiency (EthaSystem)
            // The slope represents the annual degradation (LDeg), which is expected to be negative.
            return (intercept, slope, interceptUncertainty, slopeUncertainty);
        }
    }
}