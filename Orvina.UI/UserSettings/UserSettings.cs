using System.Text.Json;

namespace Orvina.UI.UserSettings
{
    internal class UserSettings
    {
        public UserSettings()
        {
        }

        private static UserSettings _instance;

        public static UserSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    UserSettings.Load();
                }

                return _instance;
            }
        }

        public List<string> Directories { get; set; } = new List<string>() { @"C:\users" };
        public List<string> SearchTexts { get; set; } = new List<string>() { @"orvina" };
        public List<string> FileTypes { get; set; } = new List<string>() { @".txt,.html" };

        public bool FoldersOnly { get; set; } = false;
        public bool HDDMode { get; set;} = false;

        public bool CaseSensitive { get; set; } = false;

        private static void Load()
        {
            string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), System.Reflection.Assembly.GetExecutingAssembly().GetName().Name, "settings.json");

            if (File.Exists(settingsPath))
            {
                string json = File.ReadAllText(settingsPath);
                _instance = JsonSerializer.Deserialize<UserSettings>(json);
            }
            else
            {
                _instance = new UserSettings();
            }
        }

        public void Save()
        {
            while (Directories.Count > 10)
            {
                Directories.RemoveAt(Directories.Count - 1);
            }
            while (SearchTexts.Count > 10)
            {
                SearchTexts.RemoveAt(SearchTexts.Count - 1);
            }
            while (FileTypes.Count > 10)
            {
                FileTypes.RemoveAt(FileTypes.Count - 1);
            }

            string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
            Directory.CreateDirectory(settingsPath);

            settingsPath = Path.Combine(settingsPath, "settings.json");

            string json = JsonSerializer.Serialize(this);
            File.WriteAllText(settingsPath, json);
        }
    }
}