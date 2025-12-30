using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace LEG.E3Dc.Client
{
    /// <summary>
    /// VISUAL DEBUG VERSION - Chrome window will be visible to see what's happening
    /// </summary>
    public class E3dcVisualDebugDownloader : IE3dcDownloader
    {
        private readonly string _portalUrl = "https://my.e3dc.com";
        private IWebDriver? _driver;
        private WebDriverWait? _wait;
        private bool _isLoggedIn = false;
        private readonly int _defaultTimeoutSeconds = 30;

        public E3dcVisualDebugDownloader()
        {
            InitializeDriver();
        }

        private void InitializeDriver()
        {
            var options = new ChromeOptions();
            // VISUAL MODE - Browser window will be visible!
            // options.AddArgument("--headless");  // COMMENTED OUT FOR DEBUGGING
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--start-maximized");
            
            var downloadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "temp_downloads");
            Directory.CreateDirectory(downloadDirectory);
            
            options.AddUserProfilePreference("download.default_directory", downloadDirectory);
            options.AddUserProfilePreference("download.prompt_for_download", false);
            options.AddUserProfilePreference("disable-popup-blocking", "true");

            _driver = new ChromeDriver(options);
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(_defaultTimeoutSeconds));
            
            Console.WriteLine("✓ Chrome browser started in VISUAL MODE");
            Console.WriteLine("  You can watch what's happening!");
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            if (_driver == null || _wait == null)
                throw new InvalidOperationException("Driver not initialized");

            try
            {
                Console.WriteLine($"Navigating to E3DC portal: {_portalUrl}");
                _driver.Navigate().GoToUrl(_portalUrl);
                await Task.Delay(3000);

                TakeScreenshot("01_initial_page");

                // Fill username
                var usernameField = FindElement(
                    By.Id("username"),
                    By.Name("username")
                );

                if (usernameField == null)
                {
                    Console.WriteLine("ERROR: Could not find username field");
                    TakeScreenshot("error_no_username_field");
                    return false;
                }

                usernameField.Clear();
                usernameField.SendKeys(username);
                Console.WriteLine("Username entered");

                // Fill password
                var passwordField = FindElement(
                    By.Id("password"),
                    By.Name("password")
                );

                if (passwordField == null)
                {
                    Console.WriteLine("ERROR: Could not find password field");
                    TakeScreenshot("error_no_password_field");
                    return false;
                }

                passwordField.Clear();
                passwordField.SendKeys(password);
                Console.WriteLine("Password entered");

                TakeScreenshot("02_credentials_entered");

                // Click login
                var loginButton = FindElement(
                    By.CssSelector("input[type='submit']"),
                    By.CssSelector("button[type='submit']"),
                    By.Id("kc-login")
                );

                if (loginButton == null)
                {
                    Console.WriteLine("ERROR: Could not find login button");
                    TakeScreenshot("error_no_login_button");
                    return false;
                }

                loginButton.Click();
                Console.WriteLine("Login button clicked");

                // Wait for SAML authentication
                Console.WriteLine("Waiting for SAML authentication...");
                await WaitForLoginRedirect();

                TakeScreenshot("03_after_saml");

                // Handle cookie consent popup
                Console.WriteLine("Checking for cookie popup...");
                await HandleCookiePopup();

                TakeScreenshot("04_after_cookie_popup");

                // Verify we're on overview page
                await Task.Delay(2000);
                _isLoggedIn = _driver.Url.Contains("my.e3dc.com/overview");
                
                if (_isLoggedIn)
                {
                    Console.WriteLine("✓ Login successful!");
                    Console.WriteLine($"  Current URL: {_driver.Url}");
                    TakeScreenshot("05_overview_page");
                }
                else
                {
                    Console.WriteLine("⚠ Login verification unclear");
                    Console.WriteLine($"  Current URL: {_driver.Url}");
                    TakeScreenshot("warn_unclear_login");
                }

                return _isLoggedIn;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Login error: {ex.Message}");
                TakeScreenshot("error_login_exception");
                return false;
            }
        }

        public async Task<string> DownloadTimeSeriesAsync(
            E3dcSystemConfig systemConfig,
            string folderName,
            DateTime startDate,
            DateTime? endDate = null,
            int timeResolution = 15)
        {
            if (_driver == null || _wait == null)
                throw new InvalidOperationException("Driver not initialized");

            if (!_isLoggedIn)
                throw new InvalidOperationException("Not logged in. Call LoginAsync first.");

            endDate ??= DateTime.Now;

            try
            {
                Console.WriteLine($"\n=== Downloading data for {systemConfig.SystemName} ===");
                Console.WriteLine($"  Serial: {systemConfig.SerialNumber}");
                Console.WriteLine($"  Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
                Console.WriteLine($"  Resolution: {timeResolution} minutes");

                // Step 1: Click on the system panel
                await NavigateToSystemDashboard(systemConfig);

                // Step 2: Select appropriate period based on date range
                var daysDifference = (endDate.Value - startDate).Days;
                await SelectPeriod(daysDifference);

                // Step 3: Click export icon
                await ClickExportIcon();

                // Step 4: Select resolution
                await SelectResolution(timeResolution);

                // Step 5: Wait for download and save file
                var csvFilePath = await WaitAndSaveDownload(folderName, systemConfig.SystemName, startDate, endDate.Value);

                // Step 6: Navigate back to overview for next system
                await NavigateBackToOverview();

                Console.WriteLine($"✓ Download completed: {csvFilePath}");
                return csvFilePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Download error: {ex.Message}");
                TakeScreenshot($"error_download_{systemConfig.SystemName}");
                throw;
            }
        }

        private async Task WaitForLoginRedirect()
        {
            if (_driver == null) return;

            var maxWaitSeconds = 15;
            var startTime = DateTime.Now;
            var initialUrl = _driver.Url;

            while ((DateTime.Now - startTime).TotalSeconds < maxWaitSeconds)
            {
                await Task.Delay(500);
                var currentUrl = _driver.Url;

                if (currentUrl != initialUrl && currentUrl.Contains("my.e3dc.com"))
                {
                    Console.WriteLine("  Redirected to E3DC portal");
                    await Task.Delay(2000);
                    return;
                }
            }
        }

        private async Task HandleCookiePopup()
        {
            if (_driver == null) return;

            await Task.Delay(1500);

            var modal = FindElement(
                By.CssSelector(".modal.show"),
                By.CssSelector("[role='dialog']"),
                By.ClassName("modal"),
                By.CssSelector(".MuiDialog-root")
            );

            if (modal != null)
            {
                Console.WriteLine("  Found cookie popup");
                TakeScreenshot("cookie_popup_found");

                var acceptButton = FindElement(
                    By.XPath("//button[contains(text(), 'Accept selection')]"),
                    By.XPath("//button[contains(text(), 'Accept all')]"),
                    By.XPath("//button[contains(text(), 'Akzeptieren')]"),
                    By.CssSelector(".modal button.btn-primary"),
                    By.CssSelector(".modal button"),
                    By.CssSelector(".MuiDialog-root button")
                );

                if (acceptButton != null)
                {
                    Console.WriteLine($"  Clicking cookie button: '{acceptButton.Text}'");
                    
                    ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", acceptButton);
                    await Task.Delay(2000);
                    
                    Console.WriteLine("  ✓ Cookie popup dismissed");
                }
            }
            else
            {
                Console.WriteLine("  No cookie popup found");
            }

            // CRITICAL: Handle "Important messages" dialog that appears AFTER cookie popup
            await HandleImportantMessagesDialog();
        }

        private async Task HandleImportantMessagesDialog()
        {
            if (_driver == null) return;

            Console.WriteLine("  Checking for 'Important messages' dialog...");
            await Task.Delay(2000);

            // Look for the important messages dialog
            var messagesDialog = FindElement(
                By.XPath("//div[contains(text(), 'Important messages')]"),
                By.XPath("//div[contains(text(), 'Wichtige Nachrichten')]"),
                By.XPath("//*[contains(text(), 'Important messages')]/ancestor::div[@role='dialog']"),
                By.XPath("//*[contains(text(), 'Wichtige Nachrichten')]/ancestor::div[@role='dialog']")
            );

            if (messagesDialog != null)
            {
                Console.WriteLine("  ✓ Found 'Important messages' dialog!");
                TakeScreenshot("important_messages_found");

                // Look for Close button
                var closeButton = FindElement(
                    By.XPath("//button[contains(text(), 'Close')]"),
                    By.XPath("//button[contains(text(), 'Schließen')]"),
                    By.XPath("//button[contains(text(), 'Schliessen')]"),
                    By.CssSelector("button[aria-label*='close']"),
                    By.CssSelector("button.close"),
                    By.XPath("//button[contains(@class, 'close')]")
                );

                if (closeButton != null)
                {
                    Console.WriteLine($"  Clicking close button: '{closeButton.Text}'");
                    ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", closeButton);
                    await Task.Delay(1500);
                    Console.WriteLine("  ✓ Important messages dialog closed");
                }
                else
                {
                    // Try to find X button in top-right
                    var xButton = FindElement(
                        By.CssSelector("button[aria-label='close']"),
                        By.CssSelector(".MuiIconButton-root"),
                        By.XPath("//button[.//*[name()='svg']]")
                    );

                    if (xButton != null)
                    {
                        Console.WriteLine("  Clicking X button to close dialog");
                        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", xButton);
                        await Task.Delay(1500);
                        Console.WriteLine("  ✓ Important messages dialog closed (X button)");
                    }
                    else
                    {
                        Console.WriteLine("  ⚠ Could not find close button!");
                        TakeScreenshot("cannot_close_messages_dialog");
                    }
                }

                TakeScreenshot("after_messages_dialog_closed");
            }
            else
            {
                Console.WriteLine("  No important messages dialog found");
            }

            // Final overlay check
            await WaitForOverlaysToDisappear();
        }

        private async Task NavigateToSystemDashboard(E3dcSystemConfig systemConfig)
        {
            if (_driver == null) return;

            Console.WriteLine($"\n[1/6] Looking for system panel: {systemConfig.SerialNumber}");

            TakeScreenshot("before_system_search");

            // Wait for any overlays to disappear
            await WaitForOverlaysToDisappear();

            var systemPanel = FindElement(
                By.XPath($"//div[contains(text(), '{systemConfig.SerialNumber}')]"),
                By.XPath($"//*[contains(text(), '{systemConfig.SerialNumber}')]"),
                By.XPath($"//div[contains(text(), '{systemConfig.InstallationNumber}')]")
            );

            if (systemPanel != null)
            {
                Console.WriteLine("  ✓ Found system panel!");
                TakeScreenshot("system_panel_found");
                
                var clicked = false;
                
                // Strategy 1: JavaScript click (most reliable)
                try
                {
                    Console.WriteLine("  Attempting JavaScript click...");
                    ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", systemPanel);
                    await Task.Delay(500);
                    ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", systemPanel);
                    clicked = true;
                    Console.WriteLine("  ✓ Clicked! (JavaScript)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ JavaScript click failed: {ex.Message}");
                }

                // Strategy 2: Try parent container
                if (!clicked)
                {
                    Console.WriteLine("  Trying parent container...");
                    try
                    {
                        var parents = systemPanel.FindElements(By.XPath("./ancestor::*"));
                        // Find clickable parent (like a card)
                        foreach (var parent in parents)
                        {
                            var classAttr = parent.GetAttribute("class");
                            if (classAttr != null && (classAttr.Contains("card") || classAttr.Contains("container")))
                            {
                                Console.WriteLine($"  Found parent: {classAttr}");
                                ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", parent);
                                clicked = true;
                                Console.WriteLine("  ✓ Clicked! (parent container)");
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  ✗ Parent click failed: {ex.Message}");
                    }
                }

                // Strategy 3: Close dialogs and retry
                if (!clicked)
                {
                    Console.WriteLine("  Closing any blocking dialogs...");
                    await CloseAllDialogs();
                    await Task.Delay(1000);
                    
                    try
                    {
                        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", systemPanel);
                        clicked = true;
                        Console.WriteLine("  ✓ Clicked! (after closing dialogs)");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  ✗ Final attempt failed: {ex.Message}");
                    }
                }

                if (!clicked)
                {
                    TakeScreenshot("click_failed");
                    Console.WriteLine("\n⚠ MANUAL INTERVENTION NEEDED:");
                    Console.WriteLine("  The browser is open - please click the system panel manually!");
                    Console.WriteLine("  Press ENTER after you've clicked it...");
                    Console.ReadLine();
                }

                await Task.Delay(3000);

                // CRITICAL FIX: Handle window switching and focus
                await BringWindowToForeground();

                TakeScreenshot("dashboard_loaded");
                Console.WriteLine($"  ✓ Dashboard URL: {_driver.Url}");
            }
            else
            {
                TakeScreenshot("system_panel_not_found");
                throw new Exception($"Could not find system panel for {systemConfig.SerialNumber}");
            }
        }

        private async Task CloseAllDialogs()
        {
            if (_driver == null) return;

            try
            {
                var dialogs = _driver.FindElements(By.CssSelector(".MuiDialog-root, .MuiModal-root, .modal"));
                
                foreach (var dialog in dialogs)
                {
                    try
                    {
                        if (dialog.Displayed)
                        {
                            var closeButton = dialog.FindElements(By.CssSelector("button[aria-label*='close'], .close, button.close"))
                                .FirstOrDefault(b => b.Displayed);
                            
                            if (closeButton != null)
                            {
                                Console.WriteLine("  Found close button, clicking...");
                                ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", closeButton);
                                await Task.Delay(500);
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private async Task WaitForOverlaysToDisappear()
        {
            if (_driver == null) return;

            Console.WriteLine("  Checking for remaining overlays...");
            
            for (int i = 0; i < 15; i++) // Increased from 10 to 15 attempts
            {
                try
                {
                    var dialogs = _driver.FindElements(By.CssSelector(".MuiDialog-root, .MuiModal-root, .modal, [role='dialog']"));
                    var visibleDialogs = dialogs.Where(d => {
                        try {
                            return d.Displayed;
                        } catch {
                            return false;
                        }
                    }).ToList();
                    
                    if (visibleDialogs.Count == 0)
                    {
                        Console.WriteLine("  ✓ All overlays cleared");
                        await Task.Delay(500);
                        return;
                    }

                    // Try to identify what dialog is still visible
                    foreach (var dialog in visibleDialogs)
                    {
                        try
                        {
                            var text = dialog.Text;
                            if (text.Contains("Important") || text.Contains("Wichtige"))
                            {
                                Console.WriteLine($"  ⏳ 'Important messages' dialog still visible... ({i + 1}/15)");
                            }
                            else if (text.Contains("Cookie") || text.Contains("cookie"))
                            {
                                Console.WriteLine($"  ⏳ Cookie dialog still visible... ({i + 1}/15)");
                            }
                            else
                            {
                                Console.WriteLine($"  ⏳ Waiting for {visibleDialogs.Count} overlay(s)... ({i + 1}/15)");
                            }
                        }
                        catch
                        {
                            Console.WriteLine($"  ⏳ Waiting for {visibleDialogs.Count} overlay(s)... ({i + 1}/15)");
                        }
                    }
                    
                    await Task.Delay(1000);
                }
                catch
                {
                    Console.WriteLine("  ✓ No overlays detected");
                    return;
                }
            }

            Console.WriteLine("  ⚠ Overlays may still be present after 15 attempts");
            TakeScreenshot("overlays_timeout");
        }

        private async Task SelectPeriod(int daysDifference)
        {
            if (_driver == null) return;

            await EnsureWindowFocused();

            string period;
            if (daysDifference <= 1)
                period = "Day";
            else if (daysDifference <= 7)
                period = "Week";
            else if (daysDifference <= 31)
                period = "Month";
            else
                period = "Year";

            Console.WriteLine($"\n[2/6] Selecting period: {period} (for {daysDifference} days)");
            TakeScreenshot("before_period_selection");

            // Scroll to top first - period buttons are at the top
            ((IJavaScriptExecutor)_driver).ExecuteScript("window.scrollTo(0, 0);");
            await Task.Delay(1000);

            IWebElement? periodButton = null;

            // Strategy 1: Find span with text, then get parent button
            Console.WriteLine($"  Looking for '{period}' button...");
            try
            {
                var spans = _driver.FindElements(By.XPath($"//span[normalize-space(text())='{period}']"));
                foreach (var span in spans)
                {
                    try
                    {
                        if (span.Displayed)
                        {
                            // Get the parent button element
                            var parent = span.FindElement(By.XPath("./parent::*"));
                            if (parent != null && parent.Displayed && parent.Enabled)
                            {
                                var tagName = parent.TagName;
                                Console.WriteLine($"  Found: <{tagName}> containing '{period}'");
                                periodButton = parent;
                                break;
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            // Strategy 2: Direct button search
            if (periodButton == null)
            {
                Console.WriteLine($"  Trying direct button search...");
                periodButton = FindElement(
                    By.XPath($"//button[.//text()='{period}']"),
                    By.XPath($"//div[@role='button' and .//text()='{period}']"),
                    By.XPath($"//*[contains(@class, 'Mui') and .//text()='{period}']")
                );
            }

            if (periodButton != null)
            {
                Console.WriteLine($"  ✓ Found '{period}' button!");
                
                // Highlight
                try
                {
                    ((IJavaScriptExecutor)_driver).ExecuteScript(
                        "arguments[0].style.border='3px solid red';", periodButton);
                    await Task.Delay(300);
                    TakeScreenshot("period_button_highlighted");
                }
                catch { }
                
                // Click
                Console.WriteLine($"  → Clicking '{period}'...");
                ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", periodButton);
                await Task.Delay(2000);
                
                TakeScreenshot($"period_{period}_selected");
                Console.WriteLine($"  ✓ '{period}' selected!");
            }
            else
            {
                TakeScreenshot("period_button_not_found");
                Console.WriteLine($"  ⚠ Could not find '{period}' button");
                Console.WriteLine($"  Please click '{period}' manually, then press ENTER...");
                Console.ReadLine();
            }
        }

        private async Task ClickExportIcon()
        {
            if (_driver == null) return;

            await EnsureWindowFocused();

            Console.WriteLine($"\n[3/6] Looking for export/download icon...");
            TakeScreenshot("before_export_search");

            IWebElement? exportIcon = null;

            // Strategy 1: Look for download icon by SVG content in bottom-right
            Console.WriteLine("  Strategy 1: Looking for download SVG icon...");
            var allButtons = _driver.FindElements(By.TagName("button"));
            foreach (var button in allButtons)
            {
                try
                {
                    if (!button.Displayed || !button.Enabled) continue;

                    var svgs = button.FindElements(By.TagName("svg"));
                    if (svgs.Count > 0)
                    {
                        var svgHtml = button.GetAttribute("innerHTML");
                        if (svgHtml != null && (
                            svgHtml.Contains("download") ||
                            svgHtml.Contains("arrow-down") ||
                            svgHtml.Contains("M12") ||
                            svgHtml.Contains("export")))
                        {
                            var location = button.Location;
                            var size = _driver.Manage().Window.Size;
                            
                            if (location.X > size.Width / 2 && location.Y > size.Height / 2)
                            {
                                exportIcon = button;
                                Console.WriteLine($"  → Found download icon in bottom-right!");
                                break;
                            }
                        }
                    }
                }
                catch { }
            }

            // Strategy 2: Try the 3rd icon button (typical position)
            if (exportIcon == null)
            {
                Console.WriteLine("  Strategy 2: Looking for icons near Production chart...");
                
                var visibleButtons = allButtons.Where(b => {
                    try { return b.Displayed && b.Enabled && b.FindElements(By.TagName("svg")).Count > 0; }
                    catch { return false; }
                }).ToList();

                Console.WriteLine($"  Found {visibleButtons.Count} icon buttons");

                if (visibleButtons.Count >= 3)
                {
                    exportIcon = visibleButtons[2]; // 3rd button (index 2)
                    Console.WriteLine($"  → Trying 3rd icon button as download button");
                }
            }

            if (exportIcon != null)
            {
                Console.WriteLine("  ✓ Found export/download icon!");
                TakeScreenshot("export_icon_found");
                
                // Scroll into view
                ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", exportIcon);
                await Task.Delay(500);
                
                // Highlight
                try
                {
                    ((IJavaScriptExecutor)_driver).ExecuteScript(
                        "arguments[0].style.border='3px solid red';", exportIcon);
                    await Task.Delay(500);
                    TakeScreenshot("export_icon_highlighted");
                }
                catch { }
                
                // Click
                Console.WriteLine("  → Clicking export icon...");
                ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", exportIcon);
                await Task.Delay(2000);
                
                TakeScreenshot("after_export_click");
                Console.WriteLine("  ✓ Export icon clicked! Resolution dropdown should appear...");
            }
            else
            {
                TakeScreenshot("export_icon_not_found_final");
                
                Console.WriteLine("\n  ⚠ EXPORT ICON NOT FOUND - MANUAL MODE");
                Console.WriteLine("  ════════════════════════════════════════");
                Console.WriteLine("  Please MANUALLY click the download icon (↓),");
                Console.WriteLine("  then press ENTER to continue...");
                Console.WriteLine("  ════════════════════════════════════════\n");
                Console.ReadLine();
                
                await Task.Delay(1000);
                TakeScreenshot("after_manual_export_click");
            }
        }

        private async Task SelectResolution(int timeResolution)
        {
            if (_driver == null) return;

            string resolutionText;
            if (timeResolution == 15)
                resolutionText = "15 minutes";
            else if (timeResolution == 60)
                resolutionText = "Hours";
            else if (timeResolution == 1440)
                resolutionText = "Days";
            else
                resolutionText = "15 minutes";

            Console.WriteLine($"\n[4/6] Selecting resolution: {resolutionText}...");
            
            // Wait longer for resolution dropdown to appear after clicking export icon
            await Task.Delay(3000);
            TakeScreenshot("before_resolution_selection");

            // Scroll down to see the resolution options (they appear below the chart)
            ((IJavaScriptExecutor)_driver).ExecuteScript("window.scrollBy(0, 300);");
            await Task.Delay(1000);
            TakeScreenshot("after_scroll_for_resolution");

            var resolutionOption = FindElement(
                By.XPath($"//*[normalize-space(text())='{resolutionText}' and not(contains(@class, 'hidden'))]"),
                By.XPath($"//div[normalize-space(text())='{resolutionText}']"),
                By.XPath($"//span[normalize-space(text())='{resolutionText}']"),
                By.XPath($"//li[normalize-space(text())='{resolutionText}']"),
                By.XPath($"//*[contains(text(), '{resolutionText}')]")
            );

            if (resolutionOption != null)
            {
                Console.WriteLine($"  ✓ Found resolution: '{resolutionText}'");
                TakeScreenshot("resolution_found");
                
                // Highlight
                try
                {
                    ((IJavaScriptExecutor)_driver).ExecuteScript(
                        "arguments[0].style.border='3px solid red';", resolutionOption);
                    await Task.Delay(500);
                    TakeScreenshot("resolution_highlighted");
                }
                catch { }
                
                // Scroll into view and click
                ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", resolutionOption);
                await Task.Delay(500);
                
                Console.WriteLine($"  → Clicking '{resolutionText}' (this triggers download)...");
                ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", resolutionOption);
                
                await Task.Delay(2000);
                TakeScreenshot("after_resolution_clicked");
                Console.WriteLine($"  ✓ Clicked '{resolutionText}' - download should start!");
            }
            else
            {
                TakeScreenshot("resolution_not_found");
                Console.WriteLine($"  ⚠ Resolution '{resolutionText}' not found");
                
                // Debug: List what's actually visible
                Console.WriteLine($"  Listing visible text elements...");
                try
                {
                    var allText = _driver.FindElements(By.XPath("//*[string-length(normalize-space(text())) > 0]"));
                    var visibleText = allText
                        .Where(e => {
                            try { return e.Displayed && !string.IsNullOrWhiteSpace(e.Text); }
                            catch { return false; }
                        })
                        .Select(e => e.Text.Trim())
                        .Distinct()
                        .Where(t => t.Length < 50)
                        .Take(20)
                        .ToList();
                    
                    Console.WriteLine($"  Visible text on page:");
                    foreach (var text in visibleText)
                    {
                        Console.WriteLine($"    - '{text}'");
                    }
                }
                catch { }
                
                Console.WriteLine($"\n  Please select '{resolutionText}' manually,");
                Console.WriteLine($"  then press ENTER...");
                Console.ReadLine();
            }
        }

        private async Task<string> WaitAndSaveDownload(string folderName, string systemName, DateTime startDate, DateTime endDate)
        {
            Console.WriteLine($"\n[5/6] Waiting for download...");

            Directory.CreateDirectory(folderName);
            
            var tempDownloadDir = Path.Combine(Directory.GetCurrentDirectory(), "temp_downloads");
            
            foreach (var file in Directory.GetFiles(tempDownloadDir))
            {
                File.Delete(file);
            }

            await WaitForDownloadAsync(tempDownloadDir);

            var downloadedFiles = Directory.GetFiles(tempDownloadDir);
            if (downloadedFiles.Length == 0)
            {
                throw new Exception("No file was downloaded");
            }

            var sourceFile = downloadedFiles[0];
            var fileName = $"{systemName}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv";
            var targetFile = Path.Combine(folderName, fileName);

            File.Move(sourceFile, targetFile, overwrite: true);
            Console.WriteLine($"  ✓ File saved: {targetFile}");

            return targetFile;
        }

        private async Task WaitForDownloadAsync(string downloadDirectory, int maxWaitSeconds = 60)
        {
            var startTime = DateTime.Now;
            
            while ((DateTime.Now - startTime).TotalSeconds < maxWaitSeconds)
            {
                var files = Directory.GetFiles(downloadDirectory);
                var completeFiles = files.Where(f => 
                    !f.EndsWith(".crdownload") && 
                    !f.EndsWith(".tmp") &&
                    new FileInfo(f).Length > 0
                ).ToArray();

                if (completeFiles.Length > 0)
                {
                    await Task.Delay(1000);
                    Console.WriteLine("  ✓ Download complete");
                    return;
                }

                await Task.Delay(500);
            }

            throw new TimeoutException("Download did not complete within the expected time");
        }

        private async Task NavigateBackToOverview()
        {
            if (_driver == null) return;

            Console.WriteLine($"\n[6/6] Navigating back to overview...");
            
            var overviewLink = FindElement(
                By.XPath("//a[contains(text(), 'Overview')]"),
                By.XPath("//span[contains(text(), 'Overview')]"),
                By.CssSelector("a[href*='overview']")
            );

            if (overviewLink != null)
            {
                overviewLink.Click();
                await Task.Delay(2000);
            }
            else
            {
                _driver.Navigate().GoToUrl($"{_portalUrl}/overview");
                await Task.Delay(2000);
            }
            
            TakeScreenshot("back_to_overview");
            Console.WriteLine("  ✓ Back at overview");
        }

        private IWebElement? FindElement(params By[] selectors)
        {
            if (_driver == null) return null;

            foreach (var selector in selectors)
            {
                try
                {
                    var element = _driver.FindElement(selector);
                    if (element != null && element.Displayed && element.Enabled)
                        return element;
                }
                catch (NoSuchElementException) { }
                catch (StaleElementReferenceException) { }
            }

            return null;
        }

        private void TakeScreenshot(string name)
        {
            if (_driver == null) return;

            try
            {
                var screenshotDir = "screenshots";
                Directory.CreateDirectory(screenshotDir);
                
                var screenshot = ((ITakesScreenshot)_driver).GetScreenshot();
                var filename = Path.Combine(screenshotDir, $"{DateTime.Now:yyyyMMdd_HHmmss}_{name}.png");
                screenshot.SaveAsFile(filename);
                Console.WriteLine($"    📸 Screenshot: {filename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    ⚠ Screenshot failed: {ex.Message}");
            }
        }

        public async Task LogoutAsync()
        {
            if (_driver == null || !_isLoggedIn)
                return;

            try
            {
                Console.WriteLine("\nLogging out...");
                
                var logoutButton = FindElement(
                    By.XPath("//a[contains(@href, 'logout')]"),
                    By.XPath("//button[contains(text(), 'Logout')]")
                );

                if (logoutButton != null)
                {
                    logoutButton.Click();
                    await Task.Delay(1000);
                    Console.WriteLine("  ✓ Logged out");
                }

                _isLoggedIn = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ⚠ Logout error: {ex.Message}");
            }
        }

        private async Task EnsureWindowFocused()
        {
            if (_driver == null) return;

            try
            {
                // Bring window to foreground and focus
                ((IJavaScriptExecutor)_driver).ExecuteScript("window.focus();");
                _driver.Manage().Window.Maximize();
                await Task.Delay(300);
            }
            catch { }
        }

        private async Task BringWindowToForeground()
        {
            if (_driver == null) return;

            try
            {
                Console.WriteLine("  🔄 Checking for new windows/tabs...");

                // Get all window handles
                var handles = _driver.WindowHandles;
                Console.WriteLine($"  Found {handles.Count} window(s)/tab(s)");

                // If there's more than one window, switch to the most recent one
                if (handles.Count > 1)
                {
                    var currentHandle = _driver.CurrentWindowHandle;
                    var newHandle = handles.Last();

                    if (newHandle != currentHandle)
                    {
                        Console.WriteLine("  ↪ Switching to new window/tab...");
                        _driver.SwitchTo().Window(newHandle);
                        await Task.Delay(1000);
                    }
                }

                // Maximize the window
                Console.WriteLine("  ⛶ Maximizing window...");
                _driver.Manage().Window.Maximize();
                await Task.Delay(500);

                // Use JavaScript to bring window to focus
                Console.WriteLine("  ✨ Focusing window...");
                ((IJavaScriptExecutor)_driver).ExecuteScript("window.focus();");
                await Task.Delay(500);

                Console.WriteLine("  ✓ Window is now in foreground");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ⚠ Window management warning: {ex.Message}");
            }
        }

        public void Dispose()
        {
            try
            {
                Console.WriteLine("\nClosing browser...");
                _driver?.Quit();
                _driver?.Dispose();
                Console.WriteLine("  ✓ Browser closed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ⚠ Dispose error: {ex.Message}");
            }
        }
    }
}
