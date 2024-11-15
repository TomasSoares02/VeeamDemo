public class TaskFile
{
    private static Timer? updateTimer;
    private static string? basePathForFolders;
    private static string? sourcePath;
    private static string? replicaPath;
    private static string? logPath;

    public static void Main()
    {
        bool exitProgram = false;

        while (!exitProgram)
        {
            Console.Write("Please select the number of the action you want to perform:\n1: Create new folders and log file\n2: Define update time\n3: Exit\n");
            string? action = Console.ReadLine();

            switch (action)
            {
                //create
                case "1":

                    Console.Write("Enter the directory where you want to create the Source and Replica folders: ");
                    basePathForFolders = Console.ReadLine();

                    while (string.IsNullOrWhiteSpace(basePathForFolders) || !Directory.Exists(basePathForFolders))
                    {
                        Console.WriteLine("The path you entered is invalid. Please enter a valid base directory path for the folders.");
                        basePathForFolders = Console.ReadLine();
                    }

                    sourcePath = Path.Combine(basePathForFolders, "Source");
                    replicaPath = Path.Combine(basePathForFolders, "Replica");

                    if (!Directory.Exists(sourcePath) || !Directory.Exists(replicaPath))
                    {
                        DirectoryInfo dirSource = CreateDirectory(basePathForFolders, "Source");
                        DirectoryInfo dirReplica = CreateDirectory(basePathForFolders, "Replica");

                        Console.WriteLine($"Folders created at: {dirSource.FullName} and {dirReplica.FullName}");
                    }
                    else
                    {
                        Console.WriteLine("Both 'Source' and 'Replica' folders already exist at the specified location.");
                    }

                    Console.Write("Enter the directory where you want to create the updates.log file: ");
                    
                    string? logFilePath = Console.ReadLine();

                    while (string.IsNullOrWhiteSpace(logFilePath) || !Directory.Exists(logFilePath))
                    {
                        Console.WriteLine("The path you entered is invalid. Please enter a valid directory path for the log file.");
                        logFilePath = Console.ReadLine();
                    }

                    logPath = Path.Combine(logFilePath, "updates.log");

                    if (!File.Exists(logPath))
                    {
                        try
                        {
                            File.Create(logPath).Close();
                            CreateLogFile(logFilePath, "Log file 'updates.log' created.");
                            Console.WriteLine($"Log file 'updates.log' created at: {logPath}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"An error occurred while creating the log file: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("The 'updates.log' file already exists.");
                    }

                    break;

                //update timer
                case "2":

                    if (basePathForFolders == null)
                    {
                        Console.WriteLine("Please create the folders first (Action 1) before setting an update time.");
                        break;
                    }

                    Console.Write("Enter the update interval in seconds: ");

                    if (int.TryParse(Console.ReadLine(), out int intervalInSeconds) && intervalInSeconds > 0)
                    {
                        Console.WriteLine($"Update interval set to {intervalInSeconds} seconds. Starting synchronization...");

                        updateTimer = new Timer(
                            callback: _ => SyncFolders(sourcePath!, replicaPath!),
                            state: null,
                            dueTime: 0,
                            period: intervalInSeconds * 1000
                        );
                    }
                    else
                    {
                        Console.WriteLine("Invalid input. Please enter a valid number of seconds.");
                    }
                    break;

                //exit
                case "3":

                    Console.WriteLine("Exiting the program...");
                    exitProgram = true;
                    break;

                default:
                    Console.WriteLine("Invalid option selected.");
                    break;
            }

            if (!exitProgram)
            {
                Console.WriteLine("\nPress Enter to return to the main menu...");
                Console.ReadLine();
            }
        }
    }

    public static DirectoryInfo CreateDirectory(string basePath, string folderName)
    {
        string fullPath = Path.Combine(basePath, folderName);
        return Directory.CreateDirectory(fullPath);
    }

    public static void SyncFolders(string sourcePath, string replicaPath)
    {
        try
        {
            bool isUpdated = false;

            CopyFilesAndDirectories(sourcePath, replicaPath, ref isUpdated);
            DeleteMissingFilesAndDirectories(sourcePath, replicaPath, ref isUpdated);

            if (isUpdated)
            {
                CreateLogFile(Path.GetDirectoryName(logPath)!, "Replica folder updated to match Source folder.");
                Console.WriteLine($"{DateTime.Now}: Replica folder updated to match Source folder.");
            }
            else
            {
                CreateLogFile(Path.GetDirectoryName(logPath)!, "Synchronization completed. No updates were necessary.");
                Console.WriteLine($"{DateTime.Now}: No updates were necessary.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during synchronization: {ex.Message}");
        }
    }

    public static void CopyFilesAndDirectories(string sourcePath, string replicaPath, ref bool isUpdated)
    {
        string fileName, replicaFilePath, dirName, replicaDirPath;

        foreach (string sourceFilePath in Directory.GetFiles(sourcePath))
        {
            fileName = Path.GetFileName(sourceFilePath);
            replicaFilePath = Path.Combine(replicaPath, fileName);

            if (!File.Exists(replicaFilePath))
            {
                //log file creation only once
                File.Copy(sourceFilePath, replicaFilePath, true);
                Console.WriteLine($"File created: {fileName}");
                CreateLogFile(Path.GetDirectoryName(logPath)!, $"File created: {fileName}");
                isUpdated = true;
            }
            else if (File.GetLastWriteTime(sourceFilePath) > File.GetLastWriteTime(replicaFilePath))
            {
                //log file update when the source file is modified
                File.Copy(sourceFilePath, replicaFilePath, true);
                Console.WriteLine($"File updated: {fileName}");
                CreateLogFile(Path.GetDirectoryName(logPath)!, $"File updated: {fileName}");
                isUpdated = true;
            }
        }

        foreach (string sourceDirPath in Directory.GetDirectories(sourcePath))
        {
            dirName = Path.GetFileName(sourceDirPath);
            replicaDirPath = Path.Combine(replicaPath, dirName);

            if (!Directory.Exists(replicaDirPath))
            {
                Directory.CreateDirectory(replicaDirPath);
                Console.WriteLine($"Directory created: {dirName}");
                CreateLogFile(Path.GetDirectoryName(logPath)!, $"Directory created: {dirName}");
                isUpdated = true;
            }

            CopyFilesAndDirectories(sourceDirPath, replicaDirPath, ref isUpdated);
        }
    }

    public static void DeleteMissingFilesAndDirectories(string sourcePath, string replicaPath, ref bool isUpdated)
    {
        string fileName, sourceFilePath, dirName, sourceDirPath;
        
        foreach (string replicaFilePath in Directory.GetFiles(replicaPath))
        {
            fileName = Path.GetFileName(replicaFilePath);
            sourceFilePath = Path.Combine(sourcePath, fileName);

            if (!File.Exists(sourceFilePath))
            {
                File.Delete(replicaFilePath);
                Console.WriteLine($"File deleted from Replica: {fileName}");
                CreateLogFile(Path.GetDirectoryName(logPath)!, $"File deleted: {fileName}");
                isUpdated = true;
            }
        }

        foreach (string replicaDirPath in Directory.GetDirectories(replicaPath))
        {
            dirName = Path.GetFileName(replicaDirPath);
            sourceDirPath = Path.Combine(sourcePath, dirName);

            if (!Directory.Exists(sourceDirPath))
            {
                Directory.Delete(replicaDirPath, true);
                Console.WriteLine($"Directory deleted from Replica: {dirName}");
                CreateLogFile(Path.GetDirectoryName(logPath)!, $"Directory deleted: {dirName}");
                isUpdated = true;
            }
        }
    }

    public static void CreateLogFile(string directory, string message)
    {
        string logFilePath = Path.Combine(directory, "updates.log");

        string logContent = $"{DateTime.Now}: {message}\n";
        File.AppendAllText(logFilePath, logContent);
    }
}