using LEG.OxyPlotHelper;
using LEG.CoreLib.SolarCalculations.Calculations;
using OxyPlot;
using LEG.Common.Utils;
using LEG.HorizonProfiles.Client;
using LEG.CoreLib.Abstractions.SolarCalculations.Domain;
using LEG.CoreLib.SampleData.SampleData;
using LEG.CoreLib.SampleData;

var siteName = "Dummy";
const double siteLatitude = 46.5;
const double siteLongitude = 10.5;
const double siteElevation = 800;

// Use the getter from the SampleData project
var siteOptions = PvSiteModelGetters.GetSitesList();
siteOptions.Add("Manual");

siteName = "Liuns";
const double nearbySitesLookupDistance = 250; // meters

// Instantiate the providers from the SampleData project
ISiteCoordinateProvider coordinateProvider = new SampleSiteCoordinateProvider();
ISiteHorizonControlProvider horizonControlProvider = new SampleSiteHorizonControlProvider();

var (lat, lon, siteElev, azimuths, fetchElev) = (0.0, 0.0, 0.0, new List<double>(), false);
if (siteOptions.Contains(siteName))
{
    (lat, lon, siteElev, azimuths, fetchElev) = GetSiteHorizonParam.GetSiteParams(siteName,
        coordinateProvider, horizonControlProvider,
        lat: siteLatitude, lon: siteLongitude, elev: siteElevation, nearbySitesLookupDistance: nearbySitesLookupDistance);
}
else
{
    (lat, lon, siteElev, azimuths, fetchElev) = GetSiteHorizonParam.GetSiteParams(siteName,
        coordinateProvider, horizonControlProvider,
        lat: siteLatitude, lon: siteLongitude, elev: siteElevation);
}
Console.WriteLine($"PvSite: {siteName}, Latitude {lat:F1}°, Longitude: {lon:F2}°, Elevation {siteElev}");

var angles = azimuths.Select(_ => 0.0).ToList(); // Initialize with zeros
if (fetchElev)
{
    // Instantiate the client with your API key : See C:\LEG_documentation\GOOGLE_ELEVATION_API_KEY.txt
    var apiKey = Environment.GetEnvironmentVariable("GOOGLE_ELEVATION_API_KEY");
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        Console.WriteLine("Google Elevation API key is not set. Please set the 'GOOGLE_ELEVATION_API_KEY' environment variable.");
        return;
    }
    var horizonClient = new HorizonProfileClient(apiKey);
    angles = await horizonClient.GetHorizonAnglesAsync(
        lat: lat,
        lon: lon,
        siteElev: null, // Auto-query surface elevation
        roofHeight: 10.0, // Default 10m
        azimuths: azimuths
    );
}

for (var i = 0; i < azimuths.Count; i++)
    Console.WriteLine($"Azimuth (South=0°): {azimuths[i]:F1}°, Horizon Angle: {angles[i]:F2}°");

const int evaluationYear = 2022;
var evaluationDays = new List<int>() { 1, 8, 15, 23 };
const int utcShift = -1;
var (sunRise, sunSet) = SunRiseSetFromProfile.GetSunRiseAndSetArrays(evaluationYear, evaluationDays,
    utcShift, lon, lat, [..azimuths], [..angles]);

var daysPerMonth = evaluationDays.Count;
for (var month = 1; month <= 12; month++)
{
    var stringSunRise = $"{month,3} sunrise: ";
    var stringSunSet = "    sunset : ";

    for (var dayIndex = 0; dayIndex < daysPerMonth; dayIndex++)
    {
        var day = evaluationDays[dayIndex];
        var arrayIndex = (month - 1) * daysPerMonth + dayIndex;
        stringSunRise += $"{day,3}: {HourStr(sunRise[arrayIndex])} ";
        stringSunSet += $"{day,3}: {HourStr(sunSet[arrayIndex])} ";
    }

    Console.WriteLine(stringSunRise);
    Console.WriteLine(stringSunSet);
}

// Create the plot helper
var plotHelper = new OxyPlotHelper(
    title: $"Azimuth/Elevation Plot for: '{siteName}', {GeoUtils.DegToString(lat)}N / {GeoUtils.DegToString(lon)}E",
    xLabel: "Azimuth [°]",
    yLabel: "Elevation [°]",
    xMin: azimuths.Min(), xMax: azimuths.Max(),
    yMin: 0, yMax: 45
);

// After creating plotHelper
plotHelper.ShowLegend(OxyPlot.Legends.LegendPosition.TopLeft);

const int evaluationDay = 15;
const int hourStart = 2;
const int hourEnd = 22;
const int startMinute = 5;
const int minutesPerPeriod = 10;
const double wLo = (double)startMinute / minutesPerPeriod;
const double wHi = 1.0 - wLo;

var sunRiseAzi = new double[12];
var sunRiseElev = new double[12];
var sunSetAzi = new double[12];
var sunSetElev = new double[12];

var fullHoursIndex = new List<int>();
var fullHourAzi = new List<double>();
var fullHourElev = new List<double>();
var hour06Azi = new List<double>();
var hour06Elev = new List<double>();
var hour09Azi = new List<double>();
var hour09Elev = new List<double>();
var hour12Azi = new List<double>();
var hour12Elev = new List<double>();
var hour15Azi = new List<double>();
var hour15Elev = new List<double>();
var hour18Azi = new List<double>();
var hour18Elev = new List<double>();

for (var month = 1; month <= 12; month++)
{
    var (sunRisePt, sunSetPt, time, azimuthSun, elevationSun) = SunRiseSetFromProfile.GetSunRiseAndSet(
        [..azimuths], [..angles],
        evaluationYear, month, evaluationDay, utcShift, lon, lat,
        hourStart: hourStart, hourEnd: hourEnd, startMinute: startMinute, minutesPerPeriod: minutesPerPeriod);

    sunRiseAzi[month - 1] = sunRisePt.a;
    sunRiseElev[month - 1] = sunRisePt.e;
    sunSetAzi[month - 1] = sunSetPt.a;
    sunSetElev[month - 1] = sunSetPt.e;

    var curveLabel = $"{evaluationDay:D2}.{month:D2}";
    plotHelper.AddCurve(
        x: azimuthSun,
        y: elevationSun,
        lineWidth: month % 3 == 0 ? 2 : 1,
        lineStyle: month <= 6 ? LineStyle.Solid : LineStyle.Dash,
        curveLabel: curveLabel
    );

    // Find indices for full hours + startMinute
    for (var i = 0; i < time.Length; i++)
    {
        var hour = (int)time[i];
        if (Math.Abs((time[i] - hour) * 60 - startMinute) < 1 && i > 0)
        {
            fullHoursIndex.Add(i);
        }
    }

    foreach (var i in fullHoursIndex)
    {
        var hour = (int)time[i];
        var hourAzi = azimuthSun[i - 1] * wLo + azimuthSun[i] * wHi;
        var hourElev = elevationSun[i - 1] * wLo + elevationSun[i] * wHi;

        if (hour % 3 > 0)
        {
            fullHourAzi.Add(hourAzi);
            fullHourElev.Add(hourElev);
        }
        if (hour == 6)
        {
            hour06Azi.Add(hourAzi);
            hour06Elev.Add(hourElev);
        }
        if (hour == 9)
        {
            hour09Azi.Add(hourAzi);
            hour09Elev.Add(hourElev);
        }
        if (hour == 12)
        {
            hour12Azi.Add(hourAzi);
            hour12Elev.Add(hourElev);
        }
        if (hour == 15)
        {
            hour15Azi.Add(hourAzi);
            hour15Elev.Add(hourElev);
        }
        if (hour == 18)
        {
            hour18Azi.Add(hourAzi);
            hour18Elev.Add(hourElev);
        }
    }
}

// Connect the hour dots
for (var hour = 4; hour <= 20; hour++)
{
    var hourAzi = new List<double>();
    var hourElev = new List<double>();
    for (var year = evaluationYear; year <= evaluationYear + 3; year++) // 4 years for leap year consideration
    {
        for (var month = 1; month <= 12; month++)
        {
            var daysInMonth = DateTime.DaysInMonth(year, month);
            for (var day = 1; day <= daysInMonth; day++)
            {
                var (sunAzi, sunElev) = AstroGeometry.GetSolarAziElev(year,
                    month, day, hour, 0, 0, utcShift, lon, lat);
                hourAzi.Add(sunAzi);
                hourElev.Add(sunElev);
            }
        }
    }
    // close the curve
    hourAzi.Add(hourAzi[0]);
    hourElev.Add(hourElev[0]);

    plotHelper.AddCurve(
        x: [..hourAzi],
        y: [..hourElev],
        lineColor: OxyColors.Gray,
        lineWidth: hour % 3 == 0 ? 2 : 1,
        lineStyle: LineStyle.Solid
    );
}

// Full hour markers
plotHelper.AddMarkers(
    x: [..fullHourAzi],
    y: [..fullHourElev],
    markerColor: OxyColors.Gray,
    markerType: MarkerType.Circle,
    markerSize: 2
);
plotHelper.AddMarkers(
    x: [..hour06Azi],
    y: [..hour06Elev],
    markerColor: OxyColors.Gray,
    markerType: MarkerType.Circle,
    markerSize: 3
);
plotHelper.AddMarkers(
    x: [..hour09Azi],
    y: [..hour09Elev],
    markerColor: OxyColors.Gray,
    markerType: MarkerType.Circle,
    markerSize: 3
);
plotHelper.AddMarkers(
    x: [..hour12Azi],
    y: [..hour12Elev],
    markerColor: OxyColors.Black,
    markerType: MarkerType.Circle,
    markerSize: 3
);
plotHelper.AddMarkers(
    x: [..hour15Azi],
    y: [..hour15Elev],
    markerColor: OxyColors.Gray,
    markerType: MarkerType.Circle,
    markerSize: 3
);
plotHelper.AddMarkers(
    x: [..hour18Azi],
    y: [..hour18Elev],
    markerColor: OxyColors.Gray,
    markerType: MarkerType.Circle,
    markerSize: 3
);

// Horizon: Add a filled curve with markers
var closedAzimuths = new List<double>(azimuths) { azimuths[^1], azimuths[0], azimuths[0] };
var closedAngles = new List<double>(angles) { 0, 0, angles[0] };
plotHelper.FillCurve(
    x: [..closedAzimuths],
    y: [..closedAngles],
    color: OxyColors.LightGreen,
    alpha: 175,
    fillLabel: "Horizon",
    addOutline: true,
    outlineColor: OxyColors.Green,
    outlineWidth: 2,
    outlineLineStyle: LineStyle.Solid,
    addMarkers: true,
    markerColor: OxyColors.Green,
    markerType: MarkerType.Triangle,
    markerSize: 3,
    markerLabel: ""
);

// Sunrise and Sunset markers
plotHelper.AddMarkers(
    x: sunRiseAzi,
    y: sunRiseElev,
    markerColor: OxyColors.Orange,
    markerType: MarkerType.Circle,
    markerSize: 5,
    markerLabel: "SunRise"
);
plotHelper.AddMarkers(
    x: sunSetAzi,
    y: sunSetElev,
    markerColor: OxyColors.Black,
    markerType: MarkerType.Circle,
    markerSize: 5,
    markerLabel: "Sunset"
);

// Add a text box annotation
plotHelper.AddTextBox(
    x: 0,
    y: 1,
    text: $"By St. Bernegger, {DateTime.Today.ToShortDateString()}",
    textColor: OxyColors.Black,
    fontSize: 7
);

// Show the plot in a dialog window
plotHelper.ShowPlot(width: 800, height: 600);

// Optionally, save the plot as a PNG
// plotHelper.SavePlot("plot.png", width: 800, height: 600);


static string HourStr(double time)
{
    var hour = (int)time;
    var minute = (int)((time - hour) * 60);

    return $"{hour:D2}:{minute:D2}";
}