using DPUruNet;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;

namespace FingerPrintScannerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Digital Persona Fingerprint Scanner App");
            try
            {
                // Prompt for save directory
                Console.Write("Enter directory to save fingerprint files (leave blank for current directory): ");
                string inputDir = Console.ReadLine();
                string saveDir = string.IsNullOrWhiteSpace(inputDir) ? Directory.GetCurrentDirectory() : inputDir.Trim();
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

                // Detect connected readers
                var readers = ReaderCollection.GetReaders();
                if (readers.Count == 0)
                {
                    Console.WriteLine("No fingerprint readers detected. Connect a Digital Persona scanner and try again.");
                    return;
                }

                Console.WriteLine($"Found {readers.Count} reader(s):");
                for (int i = 0; i < readers.Count; i++)
                {
                    Console.WriteLine($"[{i}] {readers[i].Description.Name}");
                }

                // Use the first reader
                var reader = readers[0];
                Console.WriteLine($"Using reader: {reader.Description.Name}");

                var result = reader.Open(Constants.CapturePriority.DP_PRIORITY_COOPERATIVE);
                if (result != Constants.ResultCode.DP_SUCCESS)
                {
                    Console.WriteLine($"Failed to open reader: {result}");
                    return;
                }

                Console.WriteLine("Place your finger on the scanner...");
                var captureResult = reader.GetStatus();
                if (captureResult != Constants.ResultCode.DP_SUCCESS)
                {
                    Console.WriteLine($"Reader status error: {captureResult}");
                    reader.Dispose();
                    return;
                }

                // Capture fingerprint
                var capture = reader.Capture(Constants.Formats.Fid.ANSI, Constants.CaptureProcessing.DP_IMG_PROC_DEFAULT, 5000, reader.Capabilities.Resolutions[0]);
                if (capture.ResultCode != Constants.ResultCode.DP_SUCCESS)
                {
                    Console.WriteLine($"Capture failed: {capture.ResultCode}");
                    reader.Dispose();
                    return;
                }

                // Save image using ImageSharp
                var fid = capture.Data;
                if (fid == null || fid.Views.Count == 0)
                {
                    Console.WriteLine("No fingerprint data captured.");
                    reader.Dispose();
                    return;
                }

                var view = fid.Views[0];
                string imagePath = Path.Combine(saveDir, "fingerprint.png");
                try
                {
                    // DPUruNet provides grayscale image data as a byte array
                    int width = view.Width;
                    int height = view.Height;
                    byte[] rawImage = view.RawImage;
                    if (rawImage == null || rawImage.Length != width * height)
                    {
                        Console.WriteLine("Invalid fingerprint image data.");
                    }
                    else
                    {
                        using (var image = Image.LoadPixelData<L8>(rawImage, width, height))
                        {
                            image.Save(imagePath, new PngEncoder());
                        }
                        Console.WriteLine($"Fingerprint image saved to: {imagePath}");
                    }
                }
                catch (Exception imgEx)
                {
                    Console.WriteLine($"Failed to save fingerprint image: {imgEx.Message}");
                }

                // Save template (FMD)
                var fmdResult = FeatureExtraction.CreateFmdFromFid(fid, Constants.Formats.Fmd.ANSI);
                if (fmdResult.ResultCode == Constants.ResultCode.DP_SUCCESS)
                {
                    string fmdPath = Path.Combine(saveDir, "fingerprint.fmd");
                    File.WriteAllBytes(fmdPath, fmdResult.Data.Bytes);
                    Console.WriteLine($"Fingerprint template saved to: {fmdPath}");
                }
                else
                {
                    Console.WriteLine($"Failed to extract template: {fmdResult.ResultCode}");
                }

                reader.Dispose();
                Console.WriteLine("Done.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}