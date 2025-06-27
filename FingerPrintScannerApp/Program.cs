using DPUruNet;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;

Console.WriteLine("Digital Persona Fingerprint Scanner App");
try
{
    var readers = ReaderCollection.GetReaders();
    if (readers.Count == 0)
    {
        Console.WriteLine("No fingerprint readers detected.");
        return;
    }
    var reader = readers[0];
    Console.WriteLine($"Using reader: {reader.Description.Name}");
    var result = reader.Open(Constants.CapturePriority.DP_PRIORITY_COOPERATIVE);
    if (result != Constants.ResultCode.DP_SUCCESS)
    {
        Console.WriteLine($"Failed to open reader: {result}");
        return;
    }

    Console.WriteLine("Place your finger on the scanner...");
    int resolution = 500; // Define the resolution used for capture
    var capture = reader.Capture(Constants.Formats.Fid.ANSI, Constants.CaptureProcessing.DP_IMG_PROC_DEFAULT, 20000, resolution);
    Console.WriteLine($"Capture result: {capture.ResultCode}");
    var fid = capture.Data;
    if (fid == null)
    {
        Console.WriteLine("No fingerprint data captured (fid is null).");
    }
    else
    {
        Console.WriteLine($"fid.Format: {fid.Format}");
        Console.WriteLine($"fid.Views.Count: {fid.Views.Count}");
        Console.WriteLine($"Capture Resolution: {resolution} DPI");
        if (fid.Bytes != null)
        {
            Console.WriteLine($"fid.Bytes length: {fid.Bytes.Length}");
            var nonZeroBytes = fid.Bytes.Any(b => b != 0);
            Console.WriteLine($"fid.Bytes contains non-zero data: {nonZeroBytes}");
            // Log first 32 bytes for inspection
            var preview = string.Join(" ", fid.Bytes.Take(32).Select(b => b.ToString("X2")));
            Console.WriteLine($"fid.Bytes (first 32 bytes): {preview}");
        }
        if (fid.Views.Count > 0)
        {
            var view = fid.Views[0]; // Access the first Fingerprint Image View (Fiv)
            Console.WriteLine($"View Width: {view.Width}, Height: {view.Height}");
            string saveDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string imagePath = Path.Combine(saveDir, "fingerprint_test.png");
            try
            {
                if (view.RawImage == null || view.RawImage.Length != view.Width * view.Height)
                {
                    Console.WriteLine("Invalid fingerprint image data in view.");
                }
                else
                {
                    using (var image = Image.LoadPixelData<L8>(view.RawImage, view.Width, view.Height))
                    {
                        image.Save(imagePath, new PngEncoder());
                        Console.WriteLine($"Fingerprint image saved to: {imagePath}");
                    }
                }
            }
            catch (Exception imgEx)
            {
                Console.WriteLine($"Failed to save fingerprint image: {imgEx.Message}");
            }
        }
        else
        {
            Console.WriteLine("No views available in fid. Capture may have failed to detect a fingerprint.");
        }
    }
    reader.Dispose();
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}