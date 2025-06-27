using System.Data.SQLite;
using DPUruNet;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;

Console.WriteLine("Digital Persona Fingerprint Scanner App");
try
{
    // Initialize SQLite database
    string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "fingerprints.db");
    using var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
    connection.Open();
    string createTableQuery = "CREATE TABLE IF NOT EXISTS Users (UserID INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, FingerprintTemplate TEXT)";
    using (var command = new SQLiteCommand(createTableQuery, connection))
    {
        command.ExecuteNonQuery();
    }

    // Initialize fingerprint reader
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

    // Menu loop
    bool exit = false;
    int resolution = 500;
    while (!exit)
    {
        Console.WriteLine("\nFingerprint App Menu:");
        Console.WriteLine("1. Enroll a new user");
        Console.WriteLine("2. Verify a user");
        Console.WriteLine("3. Exit");
        Console.Write("Enter choice (1-3): ");
        string choice = Console.ReadLine();

        switch (choice)
        {
            case "1":
            {
                // Enrollment process: capture 4 fingerprints
                List<Fmd> preenrollmentFmds = [];
                int captureCount = 0;
                int maxCaptures = 4;
                Fid lastFid = null;

                while (captureCount < maxCaptures)
                {
                    Console.WriteLine($"Place your finger on the scanner (Capture {captureCount + 1}/{maxCaptures})...");
                    var capture = reader.Capture(Constants.Formats.Fid.ANSI, Constants.CaptureProcessing.DP_IMG_PROC_DEFAULT, 20000, resolution);
                    Console.WriteLine($"Capture result: {capture.ResultCode}");

                    if (capture.ResultCode == Constants.ResultCode.DP_SUCCESS && capture.Data != null)
                    {
                        var fmdResult = FeatureExtraction.CreateFmdFromFid(capture.Data, Constants.Formats.Fmd.ANSI);
                        if (fmdResult.ResultCode == Constants.ResultCode.DP_SUCCESS)
                        {
                            preenrollmentFmds.Add(fmdResult.Data);
                            captureCount++;
                            lastFid = capture.Data;
                            Console.WriteLine($"Fingerprint {captureCount} captured successfully.");
                        }
                        else
                        {
                            Console.WriteLine($"Failed to create FMD: {fmdResult.ResultCode}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Capture failed or no data. Please try again.");
                    }
                }

                // Create enrollment FMD
                var enrollmentResult = Enrollment.CreateEnrollmentFmd(Constants.Formats.Fmd.ANSI, preenrollmentFmds);
                if (enrollmentResult.ResultCode == Constants.ResultCode.DP_SUCCESS)
                {
                    Console.WriteLine("Enrollment FMD created successfully.");

                    // Serialize FMD to XML
                    string enrollmentFmdXml = Fmd.SerializeXml(enrollmentResult.Data);

                    // Get user information
                    Console.Write("Enter user name: ");
                    string userName = Console.ReadLine();

                    // Store in database
                    string insertQuery = "INSERT INTO Users (Name, FingerprintTemplate) VALUES (@Name, @FingerprintTemplate)";
                    using var command = new SQLiteCommand(insertQuery, connection);
                    command.Parameters.AddWithValue("@Name", userName);
                    command.Parameters.AddWithValue("@FingerprintTemplate", enrollmentFmdXml);
                    command.ExecuteNonQuery();
                    Console.WriteLine($"Fingerprint template for {userName} saved to database.");
                }
                else
                {
                    Console.WriteLine($"Enrollment failed: {enrollmentResult.ResultCode}");
                }

                // Save the last captured image
                if (captureCount > 0 && preenrollmentFmds.Count > 0 && lastFid != null)
                {
                    if (lastFid.Views.Count > 0)
                    {
                        var view = lastFid.Views[0];
                        string saveDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        string imagePath = Path.Combine(saveDir, "fingerprint_test.png");
                        try
                        {
                            if (view.RawImage != null && view.RawImage.Length == view.Width * view.Height)
                            {
                                using (var image = Image.LoadPixelData<L8>(view.RawImage, view.Width, view.Height))
                                {
                                    image.Save(imagePath, new PngEncoder());
                                    Console.WriteLine($"Fingerprint image saved to: {imagePath}");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Invalid fingerprint image data in view.");
                            }
                        }
                        catch (Exception imgEx)
                        {
                            Console.WriteLine($"Failed to save fingerprint image: {imgEx.Message}");
                        }
                    }
                }

                break;
            }
            case "2":
            {
                // Verification process: capture one fingerprint
                Console.WriteLine("Place your finger on the scanner for verification...");
                var capture = reader.Capture(Constants.Formats.Fid.ANSI, Constants.CaptureProcessing.DP_IMG_PROC_DEFAULT, 20000, resolution);
                Console.WriteLine($"Capture result: {capture.ResultCode}");

                if (capture.ResultCode == Constants.ResultCode.DP_SUCCESS && capture.Data != null)
                {
                    var fmdResult = FeatureExtraction.CreateFmdFromFid(capture.Data, Constants.Formats.Fmd.ANSI);
                    if (fmdResult.ResultCode == Constants.ResultCode.DP_SUCCESS)
                    {
                        Fmd newFmd = fmdResult.Data;

                        // Retrieve stored FMDs from database
                        List<Fmd> storedFmds = [];
                        List<string> userNames = [];
                        string selectQuery = "SELECT Name, FingerprintTemplate FROM Users";
                        using (var command = new SQLiteCommand(selectQuery, connection))
                        {
                            using var dataReader = command.ExecuteReader();
                            while (dataReader.Read())
                            {
                                string userName = dataReader["Name"].ToString();
                                string templateXml = dataReader["FingerprintTemplate"].ToString();
                                try
                                {
                                    Fmd storedFmd = Fmd.DeserializeXml(templateXml);
                                    storedFmds.Add(storedFmd);
                                    userNames.Add(userName);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Failed to deserialize FMD for {userName}: {ex.Message}");
                                }
                            }
                        }

                        if (storedFmds.Count == 0)
                        {
                            Console.WriteLine("No users enrolled in the database.");
                        }
                        else
                        {
                            // Perform identification
                            int thresholdScore = 10000; // 0.00001 false positive rate
                            var identifyResult = Comparison.Identify(newFmd, 0, storedFmds, thresholdScore, storedFmds.Count);
                            if (identifyResult.ResultCode == Constants.ResultCode.DP_SUCCESS &&
                                identifyResult.Indexes != null &&
                                identifyResult.Indexes.Length > 0 &&
                                identifyResult.Indexes[0].Length > 0)
                            {
                                int bestMatchIndex = identifyResult.Indexes[0][0];
                                if (bestMatchIndex >= 0 && bestMatchIndex < userNames.Count)
                                {
                                    string matchedUser = userNames[bestMatchIndex];
                                    Console.WriteLine($"Match found: User = {matchedUser}");
                                }
                                else
                                {
                                    Console.WriteLine("Invalid match index.");
                                }
                            }
                            else
                            {
                                Console.WriteLine("No match found.");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed to create FMD: {fmdResult.ResultCode}");
                    }
                }
                else
                {
                    Console.WriteLine("Capture failed or no data.");
                }

                break;
            }
            case "3":
                exit = true;
                break;
            default:
                Console.WriteLine("Invalid choice. Please enter 1, 2, or 3.");
                break;
        }
    }

    reader.Dispose();
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
