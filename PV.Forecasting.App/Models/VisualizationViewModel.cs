using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace PV.Forecasting.App.Models
{
    public class VisualizationViewModel
    {
        public string? PlotHtml { get; set; }
        public List<SelectListItem> TimeSeriesOptions { get; set; } = [];
        public List<SelectListItem> ViewOptions { get; set; } = [];
        public string SelectedTimeSeries { get; set; } = "MeasuredPower";
        public string SelectedView { get; set; } = "Daily";
    }
}
