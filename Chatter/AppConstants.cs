namespace Chatter;

public static class AppConstants
{
    // Use environment variable for Docker compatibility, fallback to AppData for local development
    public static string AppData
    {
        get
        {
            var dataPath = Environment.GetEnvironmentVariable("DATA_PATH");
            if (!string.IsNullOrEmpty(dataPath))
            {
                return dataPath;
            }

            // For Docker, use /app/data
            if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
            {
                return "/app/data";
            }

            // For local development, use AppData
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Chatter");
        }
    }

    public static void EnsureFolderStructure()
    {
        try
        {
            if (!Directory.Exists(AppData))
            {
                Directory.CreateDirectory(AppData);
            }

            var usersPath = Path.Combine(AppData, "users");
            if (!Directory.Exists(usersPath))
            {
                Directory.CreateDirectory(usersPath);
            }

            var messagesPath = Path.Combine(AppData, "messages");
            if (!Directory.Exists(messagesPath))
            {
                Directory.CreateDirectory(messagesPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not create folder structure: {ex.Message}");
        }
    }
}
