using Neurotec.Biometrics;
using System;
using System.IO;
using System.Threading.Tasks;
using Neurotec.Biometrics.Client;

class FingerprintApp
{
    private NBiometricClient _biometricClient;
    private NSubject _subject;
    private NFinger _subjectFinger;
    private string saveDir;

    public async Task RunAsync()
    {
        try
        {
            // Prompt for save directory
            Console.Write("Enter directory to save fingerprint files (leave blank for Desktop): ");
            string inputDir = Console.ReadLine();
            string defaultDesktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            saveDir = string.IsNullOrWhiteSpace(inputDir) ? defaultDesktop : inputDir.Trim();

            if (!Directory.Exists(saveDir))
            {
                try
                {
                    Directory.CreateDirectory(saveDir);
                    Console.WriteLine($"Created directory: {saveDir}");
                }
                catch (Exception dirEx)
                {
                    Console.WriteLine($"Failed to create directory '{saveDir}': {dirEx.Message}");
                    return;
                }
            }

            // Initialize biometric client
            _biometricClient = new NBiometricClient();

            // Detect and select Eikon Solo scanner
            _biometricClient.FingerScanner = _biometricClient.FingerScanners.FirstOrDefault(s => s.Name.Contains("Eikon Solo"));
            if (_biometricClient.FingerScanner == null)
            {
                Console.WriteLine("No Eikon Solo scanner detected. Please connect the scanner and try again.");
                return;
            }

            Console.WriteLine($"Using scanner: {_biometricClient.FingerScanner.Name}");

            int maxRetries = 3;
            bool captured = false;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                Console.WriteLine($"Attempt {attempt} of {maxRetries}: Place your finger on the scanner...");

                // Create a finger and set manual capture mode
                _subjectFinger = new NFinger();
                _subjectFinger.CaptureOptions = NBiometricCaptureOptions.Manual;

                // Add finger to the subject
                _subject = new NSubject();
                _subject.Fingers.Add(_subjectFinger);

                // Begin capturing
                _biometricClient.FingersReturnBinarizedImage = true;
                NBiometricTask task = _biometricClient.CreateTask(NBiometricOperations.Capture | NBiometricOperations.CreateTemplate, _subject);
                var performedTask = await _biometricClient.PerformTaskAsync(task);

                if (performedTask.Status == NBiometricStatus.Success)
                {
                    captured = true;

                    // Save image
                    var image = _subjectFinger.OriginalImage;
                    if (image != null)
                    {
                        string imagePath = Path.Combine(saveDir, "fingerprint.png");
                        try
                        {
                            image.Save(imagePath);
                            Console.WriteLine($"Fingerprint image saved to: {imagePath}");
                        }
                        catch (Exception imgEx)
                        {
                            Console.WriteLine($"Failed to save fingerprint image: {imgEx.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No fingerprint image captured.");
                    }

                    // Save template
                    var template = _subjectFinger.Template;
                    if (template != null)
                    {
                        string templatePath = Path.Combine(saveDir, "fingerprint.fmd");
                        try
                        {
                            File.WriteAllBytes(templatePath, template.Bytes);
                            Console.WriteLine($"Fingerprint template saved to: {templatePath}");
                        }
                        catch (Exception fmdEx)
                        {
                            Console.WriteLine($"Failed to save fingerprint template: {fmdEx.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No fingerprint template extracted.");
                    }

                    break; // Exit loop on successful capture
                }
                else
                {
                    Console.WriteLine($"Capture failed: {performedTask.Status}");
                    if (attempt < maxRetries)
                    {
                        Console.WriteLine("Retrying...");
                    }
                }
            }

            if (!captured)
            {
                Console.WriteLine($"Failed to capture fingerprint after {maxRetries} attempts.");
            }

            // Clean up
            _biometricClient.Dispose();
            Console.WriteLine("Done.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static async Task Main(string[] args)
    {
        var app = new FingerprintApp();
        await app.RunAsync();
    }
}