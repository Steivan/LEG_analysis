using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System;

namespace PV.Forecasting.App.Models
{
    public class VisualizationViewModel
    {
        public Dictionary<string, (string HtmlWithLegend, string HtmlWithoutLegend)> PlotHtmls { get; set; } = new();
        public Dictionary<string, List<string>> TimeSeriesLabelsByGroup { get; set; } = new();
        public List<string> SelectedTimeSeries { get; set; } = new();
        public string SelectedView { get; set; } = "15-min";
        public List<SelectListItem> ViewOptions { get; set; } = new();
        public List<SelectListItem> YearOptions { get; set; } = new();
        public int SelectedYear { get; set; }
        public int MinYear { get; set; }
        public int MaxYear { get; set; }

        // New properties for flexible period selection
        public string SelectedPeriod { get; set; } = "Year";
        public List<SelectListItem> PeriodOptions { get; set; } = new();
        public DateTime SelectedDate { get; set; }
    }
}
