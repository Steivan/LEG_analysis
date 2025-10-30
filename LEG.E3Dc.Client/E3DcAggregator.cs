using System;
using LEG.Common;
using LEG.E3Dc.Abstractions;

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