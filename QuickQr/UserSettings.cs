using System;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Win32;

namespace QuickQr
{
    public enum AppTheme
    {
        Light,
        Dark,
        Sunrise,
        Galaxy,
        Forest,
        Ocean,
        Twilight,
        Neon,
        Minimal,
        Glass,
        Solarized,
        Monochrome,
        Pastel,
        System
    }

    public sealed class UserSettings
    {
        public AppTheme Theme { get; set; } = AppTheme.Light;
        public string ForegroundColor { get; set; } = "#17212B";
        public string BackgroundColor { get; set; } = "#FFFFFF";
        public string AccentColor { get; set; } = "#F36B4F";
        public string CanvasColor { get; set; } = "#F7F8F6";
        public int PixelSize { get; set; } = 24;
        public bool IncludeQuietZones { get; set; } = true;
        public bool LivePreview { get; set; } = true;
        public bool SaveHistory { get; set; } = true;
        public bool AutoCopy { get; set; } = false;
        public bool FollowSystemTheme { get; set; } = false;
        public int MaxHistoryItems { get; set; } = 12;
        public string LastContentType { get; set; } = "text";

        [XmlIgnore]
        public static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "QuickQr", "settings.xml");

        public static UserSettings Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return new UserSettings();
                using (var stream = File.OpenRead(FilePath))
                {
                    return (UserSettings)new XmlSerializer(typeof(UserSettings)).Deserialize(stream);
                }
            }
            catch
            {
                return new UserSettings();
            }
        }

        public void Save()
        {
            try
            {
                var folder = Path.GetDirectoryName(FilePath);
                Directory.CreateDirectory(folder);
                using (var stream = File.Create(FilePath))
                {
                    new XmlSerializer(typeof(UserSettings)).Serialize(stream, this);
                }
            }
            catch
            {
                // Settings should never prevent the generator from working.
            }
        }

        public void ApplyTheme(AppTheme theme)
        {
            Theme = theme;
            if (theme == AppTheme.Dark)
            {
                ForegroundColor = "#F7FAF9";
                BackgroundColor = "#1C252A";
                AccentColor = "#F17C61";
                CanvasColor = "#11181C";
            }
            else if (theme == AppTheme.Sunrise)
            {
                ForegroundColor = "#33251E";
                BackgroundColor = "#FFFDF8";
                AccentColor = "#D86B43";
                CanvasColor = "#FFF4E8";
            }
            else if (theme == AppTheme.Galaxy)
            {
                ForegroundColor = "#F2F5FF";
                BackgroundColor = "#1B1C32";
                AccentColor = "#A78BFA";
                CanvasColor = "#101120";
            }
            else if (theme == AppTheme.Forest)
            {
                ForegroundColor = "#EFFAF3";
                BackgroundColor = "#17261F";
                AccentColor = "#51C58B";
                CanvasColor = "#102019";
            }
            else if (theme == AppTheme.Ocean)
            {
                ForegroundColor = "#ECF8FF";
                BackgroundColor = "#102532";
                AccentColor = "#48B9E8";
                CanvasColor = "#0C1B25";
            }
            else if (theme == AppTheme.Twilight)
            {
                ForegroundColor = "#F5E6FF";
                BackgroundColor = "#1F173B";
                AccentColor = "#8D6EFD";
                CanvasColor = "#261C47";
            }
            else if (theme == AppTheme.Neon)
            {
                ForegroundColor = "#FFFFFF";
                BackgroundColor = "#0C0E17";
                AccentColor = "#FF5BEA";
                CanvasColor = "#11131F";
            }
            else if (theme == AppTheme.Minimal)
            {
                ForegroundColor = "#1F2937";
                BackgroundColor = "#FFFFFF";
                AccentColor = "#111827";
                CanvasColor = "#F8FAFC";
            }
            else if (theme == AppTheme.Glass)
            {
                ForegroundColor = "#FFFFFF";
                BackgroundColor = "#1B2733";
                AccentColor = "#76D7EA";
                CanvasColor = "#16202B";
            }
            else if (theme == AppTheme.Solarized)
            {
                ForegroundColor = "#657B83";
                BackgroundColor = "#FDF6E3";
                AccentColor = "#268BD2";
                CanvasColor = "#EEE8D5";
            }
            else if (theme == AppTheme.Monochrome)
            {
                ForegroundColor = "#1F2937";
                BackgroundColor = "#F8FAFC";
                AccentColor = "#111827";
                CanvasColor = "#FFFFFF";
            }
            else if (theme == AppTheme.Pastel)
            {
                ForegroundColor = "#3B4A55";
                BackgroundColor = "#FCF4FF";
                AccentColor = "#A78BFA";
                CanvasColor = "#F6F0FF";
            }
            else if (theme == AppTheme.System)
            {
                if (IsSystemLightTheme())
                {
                    ForegroundColor = "#17212B";
                    BackgroundColor = "#FFFFFF";
                    AccentColor = "#F36B4F";
                    CanvasColor = "#F7F8F6";
                }
                else
                {
                    ForegroundColor = "#F7FAF9";
                    BackgroundColor = "#1C252A";
                    AccentColor = "#F17C61";
                    CanvasColor = "#11181C";
                }
            }
            else
            {
                ForegroundColor = "#17212B";
                BackgroundColor = "#FFFFFF";
                AccentColor = "#F36B4F";
                CanvasColor = "#F7F8F6";
            }
        }

        private bool IsSystemLightTheme()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key?.GetValue("AppsUseLightTheme") is int value)
                    {
                        return value == 1;
                    }
                }
            }
            catch
            {
            }

            return true;
        }
    }

    public sealed class HistoryItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Type { get; set; }
        public string Content { get; set; }
        public string Preview { get; set; }
        public DateTime CreatedAt { get; set; }

        private bool isFavorite;
        public bool IsFavorite
        {
            get => isFavorite;
            set
            {
                if (isFavorite == value) return;
                isFavorite = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFavorite)));
            }
        }
    }

    public sealed class HistoryStore
    {
        private readonly string filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "QuickQr", "history.xml");

        public System.Collections.Generic.List<HistoryItem> Items { get; private set; } = new System.Collections.Generic.List<HistoryItem>();

        public void Load()
        {
            try
            {
                if (!File.Exists(filePath)) return;
                using (var stream = File.OpenRead(filePath))
                {
                    var loaded = (System.Collections.Generic.List<HistoryItem>)new XmlSerializer(typeof(System.Collections.Generic.List<HistoryItem>)).Deserialize(stream);
                    Items = loaded ?? new System.Collections.Generic.List<HistoryItem>();
                }
            }
            catch
            {
                Items = new System.Collections.Generic.List<HistoryItem>();
            }
        }

        public void Add(HistoryItem item, int maxItems)
        {
            Items.RemoveAll(existing => existing.Content == item.Content && existing.Type == item.Type);
            Items.Insert(0, item);
            while (Items.Count > Math.Max(1, maxItems)) Items.RemoveAt(Items.Count - 1);
            Save();
        }

        public void ReplaceAll(System.Collections.Generic.List<HistoryItem> items)
        {
            Items = items ?? new System.Collections.Generic.List<HistoryItem>();
            Save();
        }

        public void BackupTo(string filePath)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                using (var stream = File.Create(filePath))
                {
                    new XmlSerializer(typeof(System.Collections.Generic.List<HistoryItem>)).Serialize(stream, Items);
                }
            }
            catch
            {
                // Backup should not crash the app.
            }
        }

        public void RestoreFrom(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return;
                using (var stream = File.OpenRead(filePath))
                {
                    var loaded = (System.Collections.Generic.List<HistoryItem>)new XmlSerializer(typeof(System.Collections.Generic.List<HistoryItem>)).Deserialize(stream);
                    Items = loaded ?? new System.Collections.Generic.List<HistoryItem>();
                }
                Save();
            }
            catch
            {
                // Restore should not crash the app.
            }
        }

        public void Persist()
        {
            Save();
        }

        public void Clear()
        {
            Items.Clear();
            Save();
        }

        private void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                using (var stream = File.Create(filePath))
                {
                    new XmlSerializer(typeof(System.Collections.Generic.List<HistoryItem>)).Serialize(stream, Items);
                }
            }
            catch
            {
                // History is an optional convenience and must not block QR generation.
            }
        }
    }
}
