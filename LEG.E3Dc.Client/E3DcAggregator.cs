using LEG.Common;
using LEG.CoreLib.Abstractions.SolarCalculations.Domain;
using LEG.E3Dc.Abstractions;
using System;
using static System.Net.Mime.MediaTypeNames;

namespace LEG.E3Dc.Client
{
    public static class E3DcAggregator
    {
        public static void RunE3DcAggregation()
        {
            for (var folderNumber = 1; folderNumber <= E3DcFileHelper.NrOfFolders; folderNumber++)
            {
                var periodAccumulation = new E3DcAggregationRecord()
                {
                    CountOfRecords = 0,
                    PeriodStart = E3DcFileHelper.MaxDate,
                    PeriodEnd = E3DcFileHelper.MinDate,
                    BatterySocStart = 0,
                    BatterySocEnd = 0,
                    BatterySocIntegral = 0,
                    BatteryCharging = 0,
                    BatteryDischarging = 0,
                    NetIn = 0,
                    NetOut = 0,
                    SolarProduction = 0,
                    HouseConsumption = 0,
                    WallBoxTotalChargingPower = 0
                };

                var (dataFolder, subFolder) = E3DcFileHelper.GetFolder(folderNumber);
                var folder = dataFolder + subFolder;
                Console.WriteLine(subFolder);

                var (firstYear, lastYear) = E3DcFileHelper.GetYears(folderNumber);
                for (var year = firstYear; year <= lastYear; year++)
                {
                    var annualAccumulation = new E3DcAggregationRecord()
                    {
                        CountOfRecords = 0,
                        PeriodStart = E3DcFileHelper.MaxDate,
                        PeriodEnd = E3DcFileHelper.MinDate,
                        BatterySocStart = 0,
                        BatterySocEnd = 0,
                        BatterySocIntegral = 0,
                        BatteryCharging = 0,
                        BatteryDischarging = 0,
                        NetIn = 0,
                        NetOut = 0,
                        SolarProduction = 0,
                        HouseConsumption = 0,
                        WallBoxTotalChargingPower = 0
                    };

                    var (firstMonth, lastMonth) = E3DcFileHelper.GetMonthsRange(folderNumber, year);
                    for (var month = firstMonth; month <= lastMonth; month++)
                    {
                        AggregateE3DcRecords(folder, year, month, annualAccumulation,
                            initializeStart: month == firstMonth);
                    }

                    PrintE3DcData($"{firstMonth:00}-{lastMonth:00} 20{year:00}", annualAccumulation);
                    if (year == firstYear)
                    {
                        periodAccumulation.PeriodStart = annualAccumulation.PeriodStart;
                        periodAccumulation.BatterySocStart = annualAccumulation.BatterySocStart;
                    }

                    periodAccumulation.PeriodEnd = annualAccumulation.PeriodEnd;
                    periodAccumulation.BatterySocEnd = annualAccumulation.BatterySocEnd;

                    periodAccumulation.CountOfRecords += annualAccumulation.CountOfRecords;
                    periodAccumulation.BatteryCharging += annualAccumulation.BatteryCharging;
                    periodAccumulation.BatteryDischarging += annualAccumulation.BatteryDischarging;
                    periodAccumulation.BatterySocIntegral += annualAccumulation.BatterySocIntegral;
                    periodAccumulation.NetIn += annualAccumulation.NetIn;
                    periodAccumulation.NetOut += annualAccumulation.NetOut;
                    periodAccumulation.SolarProduction += annualAccumulation.SolarProduction;
                    periodAccumulation.HouseConsumption += annualAccumulation.HouseConsumption;
                    periodAccumulation.WallBoxTotalChargingPower += annualAccumulation.WallBoxTotalChargingPower;
                }

                Console.WriteLine();
                PrintE3DcData($"20{firstYear:00}-20{lastYear:00}", periodAccumulation);
                Console.WriteLine();
            }
        }

        public static SolarProductionAggregateResults MapToSolarProductionAggregateResults(
            IE3DcAggregateArrayRecord e3DcRecord,
            string siteId = "",
            string town = "",
            int utcShift = -1,
            int nrOfRoofs = 1
            )
        {
            // Source data
            var dimensionRoofs = nrOfRoofs + 1;
            const int dimensionYear = 13;
            const int dimensionDay = 25;
            const double wToKw = 1.0 / 1000.0;
            var year = e3DcRecord.Year;
            var recordsPerHour = e3DcRecord.SubRecordsPerHour;
            var minutesPerInterval = e3DcRecord.GetMinutesPerRecord;
            var hoursPerInterval = minutesPerInterval / 60.0;
            var recordsPerYear = e3DcRecord.GetMaxRecordsPerYear;
            var startDateTime = e3DcRecord.RecordingStartTime;
            var endDateTime = e3DcRecord.RecordingEndTime;
            var solarProduction = e3DcRecord.SolarProduction?.Select(v => (double)v).ToArray();
            var recordIsValid = e3DcRecord.IsValid?.Select(v => v).ToArray();

            // Target data
            var maximumProduction = new double[dimensionRoofs, dimensionYear, dimensionDay];
            var averageProduction = new double[dimensionRoofs, dimensionYear, dimensionDay];
            var sumProductionHours = new double[dimensionRoofs, dimensionYear, dimensionDay];
            var peakPowerPerRoof = new double[dimensionRoofs];
            var countPerMonth = new int[dimensionYear];
            var theoreticalMonth = new List<double[]>();
            var effectiveMonth = new List<double[]>();
            var theoreticalYear = new List<double>();
            var effectiveYear = new List<double>();

            var startMinute = (int)(minutesPerInterval / 2);
            var dateTime0 = new DateTime(year, 1, 1, 0, startMinute, 0);

            // Aggregate over all records
            if (solarProduction != null)
            {
                for (var recordIndex = 0; recordIndex < recordsPerYear; recordIndex++)
                {
                    var isValid = recordIsValid != null && recordIsValid.Length > recordIndex
                        ? recordIsValid[recordIndex]
                        : true;

                    var dateTime = dateTime0.AddMinutes(recordIndex * minutesPerInterval);
                    if (isValid && dateTime.Year == year)
                    {
                        var month = dateTime.Month;
                        var day = dateTime.Day;
                        var hour = dateTime.Hour;
                        var minute = dateTime.Minute;
                        countPerMonth[month] = Math.Max(countPerMonth[month], day);
                        for (var roof = 1; roof < dimensionRoofs; roof++)
                        {
                            var production = roof switch
                            {
                                1 => solarProduction[recordIndex] * wToKw,
                                _ => 0.0
                            };

                            if (production > 0)
                            {
                                var productionPerHour = production / hoursPerInterval;
                                peakPowerPerRoof[roof] = Math.Max(productionPerHour, peakPowerPerRoof[roof]);
                                peakPowerPerRoof[0] = Math.Max(productionPerHour, peakPowerPerRoof[roof]);

                                maximumProduction[roof, month, hour] = Math.Max(productionPerHour, maximumProduction[roof, month, hour]);
                                maximumProduction[roof, 0, hour] = Math.Max(productionPerHour, maximumProduction[roof, 0, hour]);
                                maximumProduction[0, month, hour] = Math.Max(productionPerHour, maximumProduction[0, month, hour]);
                                maximumProduction[0, 0, hour] = Math.Max(productionPerHour, maximumProduction[0, 0, hour]);

                                averageProduction[roof, month, hour] += production;
                                averageProduction[roof, 0, hour] += production;
                                averageProduction[0, month, hour] += production;
                                averageProduction[0, 0, hour] += production;

                                sumProductionHours[roof, month, hour] += hoursPerInterval;
                                sumProductionHours[roof, 0, hour] += hoursPerInterval;
                                sumProductionHours[0, month, hour] += hoursPerInterval;
                                sumProductionHours[0, 0, hour] += hoursPerInterval;
                            }
                        }
                    }
                }
                // Compute averages
                for (var roof = 0; roof < dimensionRoofs; roof++)
                {
                    if (peakPowerPerRoof[roof] > 0)
                    {
                        for (var month = 0; month < dimensionYear; month++)
                        {
                            for (var index = 0; index < dimensionDay; index++)
                            {
                                var count = sumProductionHours[roof, month, index];
                                if (count > 0)
                                {
                                    averageProduction[roof, month, index] /= count;
                                }
                            }
                        }
                    }
                }
                // Extract aggregates per Month
                for (var roof = 0; roof < dimensionRoofs; roof++)
                {
                    var sumMaxPerMonth = new double[dimensionYear];
                    var sumAvgPerMonth = new double[dimensionYear];
                    var sumMaxPerYear = 0.0;
                    var sumAvgPerYear = 0.0;
                    for (var month = 1; month < dimensionYear ; month++)
                    {
                        for (var intraDayIndex = 0; intraDayIndex < dimensionDay; intraDayIndex++)
                        {
                            sumMaxPerMonth[month] += maximumProduction[0, month, intraDayIndex];
                            sumAvgPerMonth[month] += averageProduction[0, month, intraDayIndex];
                        }
                        sumMaxPerMonth[month] *= countPerMonth[month];
                        sumAvgPerMonth[month] *= countPerMonth[month];
                        sumMaxPerYear += sumMaxPerMonth[month];
                        sumAvgPerYear += sumAvgPerMonth[month];
                    }
                    theoreticalMonth.Add(sumMaxPerMonth);
                    effectiveMonth.Add(sumAvgPerMonth);
                    theoreticalYear.Add(sumMaxPerYear);
                    effectiveYear.Add(sumAvgPerYear);
                }

                // Normalize
                peakPowerPerRoof[0] = peakPowerPerRoof.Max();
                for (var roof = 0; roof < dimensionRoofs; roof++)
                {
                    var peakPower = peakPowerPerRoof[roof];
                    if (peakPower > 0)
                    {
                        for (var month = 0; month < dimensionYear; month++)
                        {
                            for (var intraDayIndex = 0; intraDayIndex < dimensionDay; intraDayIndex++)
                            {
                                maximumProduction[roof, month, intraDayIndex] /= peakPower;
                                averageProduction[roof, month, intraDayIndex] /= peakPower;
                            }
                        }
                    }
                }

                return new SolarProductionAggregateResults(
                    SiteId: siteId,
                    Town: town,
                    EvaluationYear: year,
                    UtcShift: utcShift,
                    DimensionRoofs: nrOfRoofs,
                    PeakPowerPerRoof: peakPowerPerRoof[1..],
                    TheoreticalAggregation: maximumProduction,
                    EffectiveAggregation: averageProduction,
                    CountPerMonth: countPerMonth,
                    TheoreticalMonth: theoreticalMonth,
                    EffectiveMonth: effectiveMonth,
                    TheoreticalYear: theoreticalYear,
                    EffectiveYear: effectiveYear
                );
            }
            else
            {
                throw new InvalidOperationException("Solar production data is null.");
            }
        }

        public static void AggregateE3DcRecords(string folderName, int year, int month,
            E3DcAggregationRecord flowAccumulation,
            bool initializeStart = false)
        {
            var fileName = E3DcFileHelper.FileName(year, month);
            var records = ImportCsv.ImportFromFile<E3DcRecord>(folderName + fileName, ";");
            var countOfRecords = records.Count;

            double batteryChargingSum = 0, batteryDischargingSum = 0, batterySocSum = 0;
            double houseConsumptionSum = 0, netInSum = 0, netOutSum = 0;
            double solarProductionSum = 0;
            double wallBoxTotalChargingPowerSum = 0;

            foreach (var r in records)
            {
                batteryChargingSum += r.BatteryCharging;
                batteryDischargingSum += r.BatteryDischarging;
                batterySocSum += r.BatterySoc;
                houseConsumptionSum += r.HouseConsumption;
                netInSum += r.NetIn;
                netOutSum += r.NetOut;
                solarProductionSum += r.SolarProduction;
                wallBoxTotalChargingPowerSum += r.WallBoxTotalChargingPower;
            }

            const double whTokWh = 1.0 / 1000;
            if (initializeStart)
            {
                flowAccumulation.PeriodStart = E3DcFileHelper.ParseTimestamp(records[0].Timestamp);
                flowAccumulation.BatterySocStart = records[0].BatterySoc;
            }

            flowAccumulation.PeriodEnd = E3DcFileHelper.ParseTimestamp(records[countOfRecords - 1].Timestamp);
            flowAccumulation.BatterySocEnd = records[countOfRecords - 1].BatterySoc;

            flowAccumulation.CountOfRecords += countOfRecords;
            flowAccumulation.BatterySocIntegral += batterySocSum / 4;
            flowAccumulation.BatteryCharging += batteryChargingSum * whTokWh;
            flowAccumulation.BatteryDischarging += batteryDischargingSum * whTokWh;
            flowAccumulation.NetIn += netInSum * whTokWh;
            flowAccumulation.NetOut += netOutSum * whTokWh;
            flowAccumulation.SolarProduction += solarProductionSum * whTokWh;
            flowAccumulation.HouseConsumption += houseConsumptionSum * whTokWh;
            flowAccumulation.WallBoxTotalChargingPower += wallBoxTotalChargingPowerSum * whTokWh;
        }

        public static void PrintE3DcData(string label, E3DcAggregationRecord accumulation)
        {
            var residual = accumulation.SolarProduction + accumulation.BatteryDischarging + accumulation.NetOut
                           - accumulation.HouseConsumption - accumulation.WallBoxTotalChargingPower -
                           accumulation.BatteryCharging - accumulation.NetIn;

            Console.WriteLine($"Accumulation for {label}:");
            Console.WriteLine(
                $"  Period:              {accumulation.PeriodStart:dd.MM.yy HH:mm} - {accumulation.PeriodEnd:dd.MM.yy HH:mm}");
            Console.WriteLine($"  Duration:            {(double)accumulation.CountOfRecords / 96,10:N1} [days]");
            Console.WriteLine(
                $"  Battery SOC from to: {accumulation.BatterySocStart,10:N0} -> {accumulation.BatterySocEnd:N0} [kWh]");
            Console.WriteLine(
                $"  Battery SOC integral:{accumulation.BatterySocIntegral / 24,10:N1} [kWh days] (mean = {accumulation.BatterySocIntegral * 4 / accumulation.CountOfRecords:N1} [kWh])");
            Console.WriteLine($"  Solar Production:    {accumulation.SolarProduction,10:N1} [kWh]");
            Console.WriteLine($"  Battery Discharging: {accumulation.BatteryDischarging,10:N1} [kWh]");
            Console.WriteLine($"  Net Out:             {accumulation.NetOut,10:N1} [kWh]");
            Console.WriteLine($"  House Consumption:   {-accumulation.HouseConsumption,10:N1} [kWh]");
            Console.WriteLine($"  Wall-box charging:   {-accumulation.WallBoxTotalChargingPower,10:N1} [kWh]");
            Console.WriteLine($"  Battery Charging:    {-accumulation.BatteryCharging,10:N1} [kWh]");
            Console.WriteLine($"  Net In:              {-accumulation.NetIn,10:N1} [kWh]");
            Console.WriteLine($"  Residual:            {-residual,10:N1} [kWh]");
        }
    }
}