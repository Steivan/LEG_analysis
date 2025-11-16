using System;

namespace PV.Forecasting.App.Models
{
    public class DataPointViewModel
    {
        public DateTime Timestamp { get; set; }
        public double? Value { get; set; }
    }
}
