using ClinicManagementSystem.Data;
using ClinicManagementSystem.Services;

namespace ClinicManagementSystem;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var appDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ClinicManagementSystem");
        var jsonFile = Path.Combine(appDataDirectory, "clinic-data.json");
        var databaseFile = Path.Combine(appDataDirectory, "clinic-data.db");
        var legacyJsonStore = new JsonDataStore(jsonFile);
        var sqliteStore = new SqliteDataStore(databaseFile);

        AppData data;
        if (!File.Exists(databaseFile) && File.Exists(jsonFile))
        {
            data = legacyJsonStore.Load();
            sqliteStore.Save(data);
        }
        else
        {
            data = sqliteStore.Load();
        }

        var context = new ClinicContext(data, sqliteStore);
        var authenticationService = new AuthenticationService(data);

        while (true)
        {
            using var loginForm = new LoginForm(context, authenticationService);
            if (loginForm.ShowDialog() != DialogResult.OK || context.CurrentUser is null)
            {
                break;
            }

            using var mainForm = new Form1(context);
            mainForm.ShowDialog();

            if (!mainForm.LoggedOut)
            {
                break;
            }
        }
    }
}
