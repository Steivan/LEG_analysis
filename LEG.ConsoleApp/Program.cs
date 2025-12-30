////Login "Felix.senn@ggaweb.ch", "Verena1955"
////stationIds =  "481826002490", "702003001860" 

//using LEG.E3Dc.Client;

//namespace E3DC_DataDownloader
//{
//    internal class Program
//    {
//        // ==================  YOUR SETTINGS HERE  ==================
//        const string serialNumberN_S10E = "481826002490";
//        const string installation_S10E = "1000014694";
//        const string Key_S10E_Stefan_12_28_2025 = "FjDHIZV9CVoUn2tkIqEgU";

//        const string serialNumber_S10EPRO = "702003001860";
//        const string installation_S10EPRO = "1000036488";
//        const string Key_S10EPRO_Stefan_12_28_2025 = "YkJEYLMLcb27fWszySBS9";


//        const string serialNumber = "S10-481826002490";  // ← add "S10-" prefix (confirm exact from portal!)            string portalEmail = "Felix.senn@ggaweb.chh";       // ← portal login email
//        const string e3DcLoginEmail = "Felix.senn@ggaweb.ch"; // ← portal password (the one you use on s10.e3dc.com)
//        const string e3DcLoginPassWord = "Verena1955"; // ← portal password (the one you use on s10.e3dc.com)

//        const string targetFolder = @"C:\E3DC_Data";  // will be created if it doesn't exist

//        static async Task Main(string[] args)
//        {
//            Console.WriteLine("==============================================");
//            Console.WriteLine("E3DC Time Series Downloader - Test Application");
//            Console.WriteLine("==============================================\n");

//            // Configuration
//            //var portalUsername = GetInput("Enter portal username", "your.email@example.com");
//            //var portalPassword = GetSecureInput("Enter portal password");

//            var portalUsername = e3DcLoginEmail;
//            var portalPassword = e3DcLoginPassWord;

//            // System 1 - S10E
//            var system1 = new E3dcSystemConfig(
//                systemName: "S10E",
//                installationNumber: installation_S10E,
//                serialNumber: serialNumberN_S10E,
//                apiKey: Key_S10E_Stefan_12_28_2025
//            );

//            // System 2 - S10EPRO
//            var system2 = new E3dcSystemConfig(
//                systemName: "S10EPRO",
//                installationNumber: serialNumber_S10EPRO,
//                serialNumber: installation_S10EPRO,
//                apiKey: Key_S10EPRO_Stefan_12_28_2025
//            );

//            // Date range
//            var now = DateTime.Now;
//            var startDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0);
//            var endDate =  new DateTime(now.Year, now.Month, now.Day, 0, 0, 0).AddDays(1);
//            // Output folder
//            var outputFolder = targetFolder;

//            Console.WriteLine("\n==============================================");
//            Console.WriteLine("Starting download process...");
//            Console.WriteLine("==============================================\n");

//            try
//            {
//                using (var downloader = new E3dcSeleniumDownloader())
//                {
//                    // Login
//                    Console.WriteLine("Step 1: Logging in to E3DC portal...");
//                    var loginSuccess = await downloader.LoginAsync(portalUsername, portalPassword);

//                    if (!loginSuccess)
//                    {
//                        Console.WriteLine("\n❌ Login failed! Please check your credentials.");
//                        Console.WriteLine("\nPress any key to exit...");
//                        Console.ReadKey();
//                        return;
//                    }

//                    Console.WriteLine("✓ Login successful!\n");

//                    // Download System 1 data
//                    Console.WriteLine("Step 2: Downloading data for System 1 (S10E)...");
//                    try
//                    {
//                        var file1 = await downloader.DownloadTimeSeriesAsync(
//                            system1,
//                            outputFolder,
//                            startDate,
//                            endDate,
//                            timeResolution: 15
//                        );
//                        Console.WriteLine($"✓ System 1 data saved to: {file1}\n");
//                    }
//                    catch (Exception ex)
//                    {
//                        Console.WriteLine($"❌ Failed to download System 1 data: {ex.Message}\n");
//                    }

//                    // Download System 2 data
//                    Console.WriteLine("Step 3: Downloading data for System 2 (S10EPRO)...");
//                    try
//                    {
//                        var file2 = await downloader.DownloadTimeSeriesAsync(
//                            system2,
//                            outputFolder,
//                            startDate,
//                            endDate,
//                            timeResolution: 15
//                        );
//                        Console.WriteLine($"✓ System 2 data saved to: {file2}\n");
//                    }
//                    catch (Exception ex)
//                    {
//                        Console.WriteLine($"❌ Failed to download System 2 data: {ex.Message}\n");
//                    }

//                    // Logout
//                    Console.WriteLine("Step 4: Logging out...");
//                    await downloader.LogoutAsync();
//                    Console.WriteLine("✓ Logout complete\n");
//                }

//                Console.WriteLine("==============================================");
//                Console.WriteLine("✓ Download process completed successfully!");
//                Console.WriteLine("==============================================");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"\n❌ ERROR: {ex.Message}");
//                Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
//            }

//            Console.WriteLine("\nPress any key to exit...");
//            Console.ReadKey();
//        }

//        static string GetInput(string prompt, string defaultValue = "")
//        {
//            if (!string.IsNullOrEmpty(defaultValue))
//            {
//                Console.Write($"{prompt} [{defaultValue}]: ");
//            }
//            else
//            {
//                Console.Write($"{prompt}: ");
//            }

//            var input = Console.ReadLine();
//            return string.IsNullOrWhiteSpace(input) ? defaultValue : input;
//        }

//        static string GetSecureInput(string prompt)
//        {
//            Console.Write($"{prompt}: ");
//            var password = "";
//            ConsoleKeyInfo key;

//            do
//            {
//                key = Console.ReadKey(true);

//                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
//                {
//                    password += key.KeyChar;
//                    Console.Write("*");
//                }
//                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
//                {
//                    password = password.Substring(0, password.Length - 1);
//                    Console.Write("\b \b");
//                }
//            }
//            while (key.Key != ConsoleKey.Enter);

//            Console.WriteLine();
//            return password;
//        }

//        static DateTime GetDateInput(string prompt, DateTime defaultValue)
//        {
//            Console.Write($"{prompt} [{defaultValue:yyyy-MM-dd}]: ");
//            var input = Console.ReadLine();

//            if (string.IsNullOrWhiteSpace(input))
//                return defaultValue;

//            if (DateTime.TryParse(input, out DateTime result))
//                return result;

//            Console.WriteLine($"Invalid date format. Using default: {defaultValue:yyyy-MM-dd}");
//            return defaultValue;
//        }
//    }



//    //    static async Task Main(string[] args)
//    //    {
//    //        // ==================  YOUR SETTINGS HERE  ==================
//    //        string serialNumberN_S10E = "481826002490";
//    //        string installation_S10E = "1000014694";
//    //        string Key_S10E_Stefan_12_28_2025 = "FjDHIZV9CVoUn2tkIqEgU";

//    //        string serialNumber_S10EPRO = "702003001860";
//    //        string installation_S10EPRO = "1000036488";
//    //        string Key_S10EPRO_Stefan_12_28_2025 = "YkJEYLMLcb27fWszySBS9";


//    //        string serialNumber = "S10-481826002490";  // ← add "S10-" prefix (confirm exact from portal!)            string portalEmail = "Felix.senn@ggaweb.chh";       // ← portal login email
//    //        string portalEmail = "Felix.senn@ggaweb.ch"; // ← portal password (the one you use on s10.e3dc.com)
//    //        string portalPassword = "Verena1955"; // ← portal password (the one you use on s10.e3dc.com)

//    //        string startDate = "2025-11-01";   // YYYY-MM-DD
//    //        string endDate = "2025-11-16";   // YYYY-MM-DD (inclusive)

//    //        string targetFolder = @"C:\E3DC_Data";  // will be created if it doesn't exist
//    //        // =========================================================

//    //        //string pythonScript = "e3dc_download.py";
//    //        // This points exactly to your project root – works forever, no copy needed
//    //        string pythonScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\e3dc_download.py");
//    //        // Build the argument string (password stays in process, never logged)

//    //        string arguments = $"--serial {serialNumber} " +
//    //                           $"--user \"{portalEmail}\" " +
//    //                           $"--password \"{portalPassword}\"";

//    //        //string arguments = $"--serial {serialNumber} " +
//    //        //                   $"--start {startDate} " +
//    //        //                   $"--end {endDate} " +
//    //        //                   $"--target \"{targetFolder}\" " +
//    //        //                   $"--user \"{portalEmail}\" " +
//    //        //                   $"--password \"{portalPassword}\"";

//    //        Console.WriteLine($"Starting download {startDate} → {endDate} for {serialNumber} …");
//    //        Console.WriteLine($"Target folder: {targetFolder}");
//    //        Console.WriteLine();

//    //        var result = await RunPythonScript(pythonScript, arguments);

//    //        if (result.Success)
//    //        {
//    //            Console.WriteLine("SUCCESS!");
//    //            Console.WriteLine(result.Output);
//    //        }
//    //        else
//    //        {
//    //            Console.WriteLine("DOWNLOAD FAILED");
//    //            Console.WriteLine("Error output:");
//    //            Console.WriteLine(result.Error);
//    //        }

//    //        Console.WriteLine("\nPress any key to exit...");
//    //        Console.ReadKey();
//    //    }

//    //    private static async Task<(bool Success, string Output, string Error)> RunPythonScript(string script, string arguments)
//    //    {
//    //        // <<<=== THIS IS THE ONLY CHANGE – 3 LINES ADDED ===>>>
//    //        string scriptDirectory = Path.GetDirectoryName(Path.GetFullPath(script));
//    //        // <<<==============================================>>>

//    //        var startInfo = new ProcessStartInfo
//    //        {
//    //            FileName = "python",
//    //            Arguments = $"\"{script}\" {arguments}",
//    //            UseShellExecute = false,
//    //            RedirectStandardOutput = true,
//    //            RedirectStandardError = true,
//    //            CreateNoWindow = true,
//    //            StandardOutputEncoding = Encoding.UTF8,
//    //            StandardErrorEncoding = Encoding.UTF8,

//    //            // <<<=== THESE TWO LINES FIX THE MODULE SEARCH PATH ===>>>
//    //            WorkingDirectory = scriptDirectory,   // ← crucial!
//    //                                                  // Optional: also force PYTHONPATH (belt-and-suspenders)
//    //                                                  // Environment = { {50} { "PYTHONPATH", scriptDirectory } }
//    //                                                  // <<<====================================================>>>
//    //        };

//    //        using var process = new Process { StartInfo = startInfo };

//    //        var outputBuilder = new StringBuilder();
//    //        var errorBuilder = new StringBuilder();

//    //        process.OutputDataReceived += (sender, e) => { if (e.Data != null) { Console.WriteLine(e.Data); outputBuilder.AppendLine(e.Data); } };
//    //        process.ErrorDataReceived += (sender, e) => { if (e.Data != null) { Console.WriteLine("ERR> " + e.Data); errorBuilder.AppendLine(e.Data); } };

//    //        process.Start();
//    //        process.BeginOutputReadLine();
//    //        process.BeginErrorReadLine();

//    //        await process.WaitForExitAsync();

//    //        return (process.ExitCode == 0, outputBuilder.ToString(), errorBuilder.ToString());
//    //    }
//    //}
//}

////namespace E3DcConsoleApp
////{
////    public class Program
////    {
////        static void Main(string[] args)
////        {
////            // All MeteoSwiss-related logic has been migrated to the new MeteoConsoleApp.
////            // This application is now only responsible for E3DC aggregation.

////            // No command line arguments are provided, run the default E3DC aggregation.
////            if (args.Length == 0)
////            {
////                Console.WriteLine("No arguments provided, running default E3DC aggregation...");
////                E3DcAggregator.RunE3DcAggregation();
////                return;
////            }

////            // Handle any other command-line arguments if necessary for other tasks.
////            Console.WriteLine("Command line arguments are not used for E3DC aggregation.");
////        }
////    }
////}
