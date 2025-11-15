using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace PV.Forecasting.App.Models
{
    public class VisualizationViewModel
    {
        public Dictionary<string, string> PlotHtmls { get; set; } = [];
        public List<SelectListItem> TimeSeriesOptions { get; set; } = [];
        public List<SelectListItem> ViewOptions { get; set; } = [];
        public List<SelectListItem> YearOptions { get; set; } = [];
        public List<string> SelectedTimeSeries { get; set; } = ["MeasuredPower"];
        public string SelectedView { get; set; } = "Daily";
        public int SelectedYear { get; set; }
        public int MinYear { get; set; }
        public int MaxYear { get; set; }
    }
}
