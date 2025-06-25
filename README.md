# FingerPrintScannerApp

A .NET console application for capturing fingerprint images and templates using a DigitalPersona fingerprint scanner (via DPUruNet).

## Features
- Detects connected DigitalPersona fingerprint readers
- Captures fingerprint image and template (FMD)
- Saves fingerprint image as PNG (using ImageSharp)
- Saves fingerprint template as binary file
- Prompts user for save directory
- Clear error handling and console output

## Requirements
- **Windows OS** (DigitalPersona SDK and DPUruNet are Windows-only)
- .NET 9.0 SDK or compatible
- DigitalPersona fingerprint scanner (e.g., U.are.U 4500, 5300)
- DPUruNet library (NuGet)
- [SixLabors.ImageSharp](https://www.nuget.org/packages/SixLabors.ImageSharp) (NuGet)
- DigitalPersona drivers installed (ensure `dpfpdd.dll` is present)

> **Note:** This application will NOT work on macOS or Linux. Attempting to run on non-Windows platforms will result in missing DLL errors.

## Setup
1. Clone the repository or copy the project files.
2. Open the solution in Visual Studio or your preferred .NET IDE.
3. Restore NuGet packages:
   ```
   dotnet restore
   ```
4. Build the project:
   ```
   dotnet build
   ```
5. Ensure your DigitalPersona scanner is connected and drivers are installed.

## Usage
1. Run the application:
   ```
   dotnet run --project FingerPrintScannerApp
   ```
2. Enter the directory where you want to save fingerprint files (or press Enter for current directory).
3. Place your finger on the scanner when prompted.
4. The app will save:
   - `fingerprint.png` (image)
   - `fingerprint.fmd` (template)
   in the chosen directory.

## Troubleshooting
- **Missing `dpfpdd.dll` or similar errors:**
  - Make sure you are running on Windows.
  - Install the official DigitalPersona drivers.
  - Ensure the scanner is plugged in and recognized by Windows.
- **No readers detected:**
  - Check USB connection and drivers.
  - Try a different USB port.
- **Image/template not saved:**
  - Ensure you have write permissions to the chosen directory.

## License
MIT (or specify your license) 