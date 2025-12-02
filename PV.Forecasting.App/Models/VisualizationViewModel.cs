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

        // --- Dynamic UI additions ---
        /// <summary>
        /// Maps each physical unit (e.g., "W/m²", "°C") to a list of parameter names (e.g., "GlobalRadiation", "Temperature").
        /// The "Power" group (for "MeasuredPower", "ComputedPower") should be included as a special case.
        /// </summary>
        public Dictionary<string, List<string>> ParameterGroupsByUnit { get; set; } = new();

        /// <summary>
        /// List of parameter names (strings) selected by the user for plotting.
        /// </summary>
        public List<string> SelectedParameters { get; set; } = new();

        // Maps group name (e.g. "Power", "Radiation") to whether the group is checked
        public Dictionary<string, bool> GroupChecked { get; set; } = new();

        // Maps group name to a list of available variable names (e.g. "Temperature", "DewPoint")
        public Dictionary<string, List<string>> GroupVariables { get; set; } = new();

        // Maps group name to a list of available locations (e.g. station names or "PV Site")
        public Dictionary<string, List<string>> GroupLocations { get; set; } = new();

        // Maps group name to a set of checked variable names (per group)
        public Dictionary<string, HashSet<string>> CheckedVariables { get; set; } = new();

        // Maps group name to a set of checked locations (per group)
        public Dictionary<string, HashSet<string>> CheckedLocations { get; set; } = new();
    }
}
