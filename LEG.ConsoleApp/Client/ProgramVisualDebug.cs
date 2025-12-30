using LEG.E3Dc.Client;

namespace E3dcDownloadTest
{
    class ProgramVisualDebug
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("╔════════════════════════════════════════════════╗");
            Console.WriteLine("║  E3DC VISUAL DEBUG DOWNLOADER                  ║");
            Console.WriteLine("║  Chrome window will be visible!                ║");
            Console.WriteLine("╚════════════════════════════════════════════════╝\n");

            const string serialNumberN_S10E = "481826002490";
            const string installation_S10E = "1000014694";
            const string Key_S10E_Stefan_12_28_2025 = "FjDHIZV9CVoUn2tkIqEgU";

            const string serialNumber_S10EPRO = "702003001860";
            const string installation_S10EPRO = "1000036488";
            const string Key_S10EPRO_Stefan_12_28_2025 = "YkJEYLMLcb27fWszySBS9";

            const string e3DcLoginEmail = "Felix.senn@ggaweb.ch"; // ← portal password (the one you use on s10.e3dc.com)
            const string e3DcLoginPassWord = "Verena1955"; // ← portal password (the one you use on s10.e3dc.com)

            //const string targetFolder = @"C:\E3DC_Data";  // will be created if it doesn't exist

            // Configuration
            //var portalUsername = GetInput("Enter portal username", "your.email@example.com");
            //var portalPassword = GetSecureInput("Enter portal password");

            var portalUsername = e3DcLoginEmail;
            var portalPassword = e3DcLoginPassWord;

            // System 1 - S10E
            var system1 = new E3dcSystemConfig(
                systemName: "S10E",
                installationNumber: installation_S10E,
                serialNumber: serialNumberN_S10E,
                apiKey: Key_S10E_Stefan_12_28_2025
            );

            // System 2 - S10EPRO
            var system2 = new E3dcSystemConfig(
                systemName: "S10EPRO",
                installationNumber: serialNumber_S10EPRO,
                serialNumber: installation_S10EPRO,
                apiKey: Key_S10EPRO_Stefan_12_28_2025
            );

            //// TODO: Replace with your actual credentials
            //var portalUsername = "felix.senn@ggaweb.ch";
            //var portalPassword = "Verena1955";

            //// System 1 - S10E
            //var system1 = new E3dcSystemConfig(
            //    systemName: "S10E",
            //    installationNumber: "1000014694",
            //    serialNumber: "481826002490",  // Your actual serial
            //    apiKey: "YOUR_API_KEY"
            //);

            //// System 2 - S10EPRO
            //var system2 = new E3dcSystemConfig(
            //    systemName: "S10EPRO",
            //    installationNumber: "1000036488",
            //    serialNumber: "1000036488",  // Your actual serial
            //    apiKey: "YOUR_API_KEY"
            //);

            var startDate = new DateTime(2025, 12, 1);
            var endDate = new DateTime(2025, 12, 29);
            var outputFolder = "./e3dc_data";
            var timeResolution = 15; // 15 minutes

            Console.WriteLine("Configuration:");
            Console.WriteLine($"  Portal: {portalUsername}");
            Console.WriteLine($"  Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
            Console.WriteLine($"  Resolution: {timeResolution} minutes");
            Console.WriteLine($"  Output: {outputFolder}");
            Console.WriteLine("\nStarting in 3 seconds...");
            await Task.Delay(3000);

            try
            {
                using (var downloader = new E3dcVisualDebugDownloader())
                {
                    // Login
                    Console.WriteLine("\n╔════════════════════════════════════════════════╗");
                    Console.WriteLine("║  STEP 1: LOGIN                                 ║");
                    Console.WriteLine("╚════════════════════════════════════════════════╝");
                    
                    var loginSuccess = await downloader.LoginAsync(portalUsername, portalPassword);

                    if (!loginSuccess)
                    {
                        Console.WriteLine("\n❌ Login failed!");
                        Console.WriteLine("   Check your credentials and try again.");
                        Console.WriteLine("\nPress any key to exit...");
                        Console.ReadKey();
                        return;
                    }

                    Console.WriteLine("\n✓ LOGIN SUCCESSFUL!");

                    // Download System 1
                    Console.WriteLine("\n╔════════════════════════════════════════════════╗");
                    Console.WriteLine("║  STEP 2: DOWNLOAD SYSTEM 1 (S10E)             ║");
                    Console.WriteLine("╚════════════════════════════════════════════════╝");
                    
                    try
                    {
                        var file1 = await downloader.DownloadTimeSeriesAsync(
                            system1,
                            outputFolder,
                            startDate,
                            endDate,
                            timeResolution: timeResolution
                        );
                        Console.WriteLine($"\n✓ SYSTEM 1 COMPLETE: {file1}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"\n❌ SYSTEM 1 FAILED: {ex.Message}");
                    }

                    // Download System 2
                    Console.WriteLine("\n╔════════════════════════════════════════════════╗");
                    Console.WriteLine("║  STEP 3: DOWNLOAD SYSTEM 2 (S10EPRO)          ║");
                    Console.WriteLine("╚════════════════════════════════════════════════╝");
                    
                    try
                    {
                        var file2 = await downloader.DownloadTimeSeriesAsync(
                            system2,
                            outputFolder,
                            startDate,
                            endDate,
                            timeResolution: timeResolution
                        );
                        Console.WriteLine($"\n✓ SYSTEM 2 COMPLETE: {file2}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"\n❌ SYSTEM 2 FAILED: {ex.Message}");
                    }

                    // Logout
                    await downloader.LogoutAsync();
                }

                Console.WriteLine("\n╔════════════════════════════════════════════════╗");
                Console.WriteLine("║  PROCESS COMPLETE                              ║");
                Console.WriteLine("╚════════════════════════════════════════════════╝");
                Console.WriteLine("\nCheck the screenshots/ folder for debugging info!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ FATAL ERROR: {ex.Message}");
                Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
