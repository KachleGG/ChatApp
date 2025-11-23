namespace Chatter;

public static class AppConstants
{
    public static string AppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Chatter");

    public static void EnsureFolderStructure() {
        if (!Directory.Exists(AppData)) {
            Directory.CreateDirectory(AppData);
        }

        if (!Directory.Exists(Path.Combine(AppData, "users"))) {
            Directory.CreateDirectory(Path.Combine(AppData, "users"));
        }

        if (!Directory.Exists(Path.Combine(AppData, "messages"))) {
            Directory.CreateDirectory(Path.Combine(AppData, "messages"));
        }
    }
}
