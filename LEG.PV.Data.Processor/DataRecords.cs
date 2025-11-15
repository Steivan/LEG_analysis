using LEG.PV.Core.Models;

namespace LEG.PV.Data.Processor
{
    public class DataRecords
    {
        public record PvRecord
        {
            public PvRecord(DateTime timestamp, int index, double geometryFactor, double irradiation, double ambientTemp, double windVelocity, double age, double measuredPower)
            {
                Timestamp = timestamp;
                Index = index;
                GeometryFactor = geometryFactor;
                Irradiation = irradiation;
                AmbientTemp = ambientTemp;
                WindVelocity = windVelocity;
                Age = age;
                MeasuredPower = measuredPower;
            }
            public DateTime Timestamp { get; init; }         // Timestamp [YYYY-MM-DD HH:MM:SS]
            public int Index { get; init; }                  // Index [unitless]
            public double GeometryFactor { get; init; }      // G_POA / G_ref [unitless]
            public double Irradiation { get; init; }         // G_POA [W/m²]
            public double AmbientTemp { get; init; }         // T_amb [°C]
            public double WindVelocity { get; init; }        // v_wind [m/s]
            public double Age { get; init; }                 // Age [years]
            public double MeasuredPower { get; init; }       // P_meas [W]
            public double ComputedPower (PvModelParams modelParams, double installedPower)       // P_meas [W]
            {
                return PvJacobian.EffectiveCellPower(installedPower, 
                    GeometryFactor, Irradiation, AmbientTemp, WindVelocity, Age,
                    ethaSys: modelParams.Etha, gamma: modelParams.Gamma, u0: modelParams.U0, u1: modelParams.U1, lDegr: modelParams.LDegr);
            }
        }

        public record PvRecordCalculated
        {
            public PvRecordCalculated(DateTime timestamp, int index, double geometryFactor, double irradiation, double ambientTemp, double windVelocity, double age, double measuredPower, double computedPower)
            {
                Timestamp = timestamp;
                Index = index;
                GeometryFactor = geometryFactor;
                Irradiation = irradiation;
                AmbientTemp = ambientTemp;
                WindVelocity = windVelocity;
                Age = age;
                MeasuredPower = measuredPower;
                ComputedPower = computedPower;
            }

            public DateTime Timestamp { get; init; }         // Timestamp [YYYY-MM-DD HH:MM:SS]
            public int Index { get; init; }                  // Index [unitless]
            public double GeometryFactor { get; init; }      // G_POA / G_ref [unitless]
            public double Irradiation { get; init; }         // G_POA [W/m²]
            public double AmbientTemp { get; init; }         // T_amb [°C]
            public double WindVelocity { get; init; }        // v_wind [m/s]
            public double Age { get; init; }                 // Age [years]
            public double MeasuredPower { get; init; }       // P_meas [W]
            public double ComputedPower { get; init; }       // P_comp [W]
        }

        public record PvModelParams
        {
            public PvModelParams(double etha, double gamma, double u0, double u1, double lDegr)
            {
                this.Etha = etha;
                this.Gamma = gamma;
                this.U0 = u0;
                this.U1 = u1;
                this.LDegr = lDegr;
            }

            public double Etha { get; init; }
            public double Gamma { get; init; }
            public double U0 { get; init; }
            public double U1 { get; init; }
            public double LDegr { get; init; }
        }

        public static PvModelParams GetDefaultPriorModelParams()
        {
            var (etha, gamma, u0, u1, lDegr) = PvPriorConfig.GetAllPriorsMeans();
            return new PvModelParams(etha, gamma, u0, u1, lDegr);
        }

    }
}
