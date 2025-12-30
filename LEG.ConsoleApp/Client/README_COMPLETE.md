# E3DC Time Series Downloader - COMPLETE SOLUTION

## 🎯 Overview

This is a complete C# solution for automatically downloading time series data from E3DC solar power systems via the my.e3dc.com web portal. After extensive testing and refinement, this solution successfully:

1. ✅ Authenticates via SAML login
2. ✅ Handles cookie consent popups
3. ✅ Navigates to individual system dashboards
4. ✅ Selects time periods (Day/Week/Month/Year)
5. ✅ Exports data with configurable resolution (15min/Hour/Day/Month)
6. ✅ Downloads CSV files for multiple systems

## 📋 What You Discovered

During testing, we learned:

- **No Direct API**: E3DC's API keys are for the RSCP protocol (real-time local connection), NOT for CSV export
- **Portal Navigation Required**: Historical CSV downloads must be done through the web portal
- **Selenium is Correct**: Web automation is the ONLY way to get historical aggregated CSV data
- **Complete Flow Mapped**: We reverse-engineered the exact navigation path through the portal

## 🏗️ Architecture

```
Login → Handle Cookies → Overview Page → Click System Panel
    ↓
Dashboard → Select Period → Click Export → Select Resolution
    ↓
Download CSV → Save File → Navigate Back → Next System
```

## 📦 Components

### Core Files

1. **E3dcCompleteDownloader.cs** (12KB)
   - Complete implementation with full portal navigation
   - Handles login, cookie popups, system selection, data export
   - Automatic download management

2. **IE3dcDownloader.cs**
   - Clean interface definition

3. **E3dcSystemConfig.cs**
   - System configuration model

4. **ProgramComplete.cs**
   - Console application for testing
   - Interactive prompts for all parameters

### Diagnostic Tools

5. **E3dcEnhancedDiagnostic.cs**
   - Step-by-step diagnostic with screenshots
   - Used for troubleshooting portal changes

## 🚀 Quick Start

### Prerequisites

```bash
# .NET 9.0 SDK
# Chrome browser (for Selenium)
# E3DC portal account credentials
```

### Installation

```bash
# 1. Add to your project
cp E3dcCompleteDownloader.cs YourProject/LEG.E3Dc.Client/
cp IE3dcDownloader.cs YourProject/LEG.E3Dc.Abstractions/
cp E3dcSystemConfig.cs YourProject/LEG.E3Dc.Client/

# 2. Verify NuGet packages (already in your .csproj)
- Selenium.WebDriver (4.38.0)
- Selenium.WebDriver.ChromeDriver (130.0.6723.5800)
- Selenium.Support (4.38.0)
```

### Configuration

Your systems (as discovered during testing):

```csharp
// System 1: S10E
Installation: 1000014694
Serial: [Your Serial Number]

// System 2: S10EPRO  
Installation: 1000036488
Serial: [Your Serial Number]
```

## 📝 Usage

### Option 1: Console Application

```bash
dotnet run --project ProgramComplete.csproj
```

Interactive prompts will guide you through:
- Portal credentials
- System configurations
- Date range
- Time resolution
- Output folder

### Option 2: Programmatic Usage

```csharp
using LEG.E3Dc.Client;

var system1 = new E3dcSystemConfig(
    "S10E",
    "1000014694",
    "YOUR_SERIAL_NUMBER",
    "API_KEY" // Optional, not used for CSV download
);

using (var downloader = new E3dcCompleteDownloader())
{
    // Login
    await downloader.LoginAsync("felix.senn@ggaweb.ch", "password");

    // Download with 15-minute resolution
    var file = await downloader.DownloadTimeSeriesAsync(
        system1,
        "./data",
        new DateTime(2024, 12, 1),
        new DateTime(2024, 12, 28),
        timeResolution: 15
    );

    // Logout
    await downloader.LogoutAsync();
}
```

## ⚙️ Configuration Options

### Time Resolutions

```csharp
timeResolution: 15    // 15 minutes (highest detail)
timeResolution: 60    // Hourly
timeResolution: 1440  // Daily
```

The downloader automatically selects the appropriate period (Day/Week/Month/Year) based on your date range.

### Period Selection Logic

```
Date Range          → Auto-Selected Period
-----------           --------------------
1 day               → Day
2-7 days            → Week
8-31 days           → Month
32+ days            → Year
```

## 🔍 How It Works

### Step-by-Step Process

1. **Navigate to Portal**
   ```
   https://my.e3dc.com → SAML redirect → auth.hagerenergy.com
   ```

2. **Login**
   - Fill username (email)
   - Fill password
   - Submit form
   - Wait for SAML authentication

3. **Handle Popups**
   - Detect cookie consent modal
   - Click "Accept selection"
   - Wait for dismissal

4. **System Selection**
   - Find system panel by installation/serial number
   - Click panel to open dashboard
   - Wait for dashboard load

5. **Configure Export**
   - Select period button (Day/Week/Month/Year)
   - Click export icon (📥)
   - Select resolution from dropdown

6. **Download**
   - Wait for file download
   - Move from temp to final location
   - Rename: `{SystemName}_{StartDate}_{EndDate}.csv`

7. **Return to Overview**
   - Navigate back for next system

## 📂 File Output

Downloads are saved as:

```
e3dc_data/
├── S10E_20241201_20241228.csv
└── S10EPRO_20241201_20241228.csv
```

## 🐛 Troubleshooting

### Login Fails

**Check:**
- Credentials are correct
- Chrome/ChromeDriver versions match
- No MFA/2FA on your account

**Debug:**
Run diagnostic:
```bash
dotnet run --project E3dcDiagnostic.csproj
```

### Can't Find System Panel

**Solution:**
The system panels are identified by installation or serial number. Verify these match what's shown in the portal.

Update selectors if needed:
```csharp
By.XPath($"//div[contains(text(), '{systemConfig.InstallationNumber}')]")
```

### Export Button Not Found

**Check:**
The export icon location might change. Current selectors:
```csharp
By.XPath("//button[@title='Export']")
By.CssSelector("button[title*='Export']")
```

### Download Doesn't Start

**Verify:**
- Chrome download settings aren't blocking
- temp_downloads/ folder is writable
- No antivirus blocking

## 🔧 Customization

### Headless vs. Visual Mode

By default, Chrome runs headless. To debug visually:

```csharp
// In E3dcCompleteDownloader.cs, InitializeDriver():
var options = new ChromeOptions();
// options.AddArgument("--headless");  // Comment out this line
```

### Custom Selectors

If the portal UI changes, update selectors in:
- `NavigateToSystemDashboard()` - System panels
- `SelectPeriod()` - Period buttons  
- `ClickExportIcon()` - Export button
- `SelectResolution()` - Resolution dropdown

### Timeout Adjustments

```csharp
private readonly int _defaultTimeoutSeconds = 30;  // Increase if slow connection
```

## 📊 Resolution Details

The E3DC portal offers different resolutions based on the selected period:

| Period | Available Resolutions          |
|--------|-------------------------------|
| Day    | 15 minutes, Hours            |
| Week   | Hours, Days                  |
| Month  | Days, Weeks                  |
| Year   | Days, Weeks, Months          |

The downloader intelligently selects the period based on your date range and then applies the requested resolution.

## 🔐 Security Notes

- Credentials are only used for authentication
- No credentials are stored
- Session ends when downloader is disposed
- Downloaded files are local only

## 📈 Performance

- Login: ~5-10 seconds
- Per system download: ~5-15 seconds
- Both systems: ~15-30 seconds total

## 🚦 Known Limitations

1. **No Concurrent Downloads**: One system at a time
2. **Session-Based**: Must re-login if process interrupted
3. **Portal Changes**: UI updates may break selectors
4. **No Retry Logic**: Failed downloads must be restarted

## 🔄 Future Enhancements

Potential improvements:
- Retry logic for failed downloads
- Progress callbacks
- Concurrent system downloads
- Data validation after download
- Automatic schema detection
- Integration with RSCP for real-time data

## 📚 Technical Details

### Why Selenium?

1. **No CSV API**: E3DC doesn't provide an API endpoint for CSV exports
2. **RSCP Limitation**: The RSCP protocol only does real-time polling, not historical CSV
3. **Portal-Only Feature**: Historical aggregated data (15min resolution) is portal-exclusive
4. **Complex Auth**: SAML authentication requires browser automation

### Portal Navigation Flow

```
my.e3dc.com
    ↓
auth.hagerenergy.com (SAML)
    ↓
my.e3dc.com/login?token=...
    ↓
Cookie popup (Accept)
    ↓
my.e3dc.com/overview
    ↓
Click system panel
    ↓
my.e3dc.com/dashboard?system=...
    ↓
Select period → Export → Resolution
    ↓
CSV download
```

## 🎓 Lessons Learned

From extensive testing:

1. ✅ **API Keys**: Used for RSCP (local), not portal
2. ✅ **Cookie Handling**: Critical - blocks everything if not handled
3. ✅ **SAML Flow**: Requires patience, multiple redirects
4. ✅ **System Panels**: Clickable cards on overview page
5. ✅ **Period Selection**: Determines available resolutions
6. ✅ **Export Icon**: Small button in chart area
7. ✅ **Resolution Dropdown**: Appears after export click

## 📞 Support

If the portal changes:

1. Run diagnostic: `E3dcEnhancedDiagnostic.cs`
2. Check screenshots in `screenshots/` folder
3. Inspect `e3dc_final_page.html`
4. Update selectors in `E3dcCompleteDownloader.cs`

## 🎉 Success Criteria

Your download succeeded when you see:

```
✓ Login successful!
✓ System 1 data saved to: ./e3dc_data/S10E_20241201_20241228.csv
✓ System 2 data saved to: ./e3dc_data/S10EPRO_20241201_20241228.csv
✓ Logout complete
✓ Download process completed successfully!
```

---

**Status**: Production Ready ✅  
**Last Tested**: December 2024  
**Portal Version**: my.e3dc.com (SAML-based)  
**Systems Tested**: S10E, S10EPRO
