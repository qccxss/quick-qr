using System;
using System.Windows;
using System.Windows.Controls;

namespace QuickQr
{
    public partial class SettingsWindow : Window
    {
        private readonly UserSettings settings;
        public UserSettings Result { get; private set; }

        public SettingsWindow(UserSettings settings)
        {
            InitializeComponent();
            this.settings = settings;
            ThemeCombo.SelectedIndex = (int)settings.Theme;
            PixelSizeCombo.SelectedIndex = PixelIndex(settings.PixelSize);
            HistoryLimitCombo.SelectedIndex = HistoryIndex(settings.MaxHistoryItems);
            QuietZoneCheck.IsChecked = settings.IncludeQuietZones;
            LivePreviewCheck.IsChecked = settings.LivePreview;
            HistoryCheck.IsChecked = settings.SaveHistory;
        }

        private int PixelIndex(int value)
        {
            if (value == 12) return 0;
            if (value == 18) return 1;
            if (value == 32) return 3;
            return 2;
        }

        private int HistoryIndex(int value)
        {
            if (value <= 6) return 0;
            if (value <= 12) return 1;
            if (value <= 24) return 2;
            return 3;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var theme = (ThemeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Light";
            settings.Theme = (AppTheme)Enum.Parse(typeof(AppTheme), theme);
            settings.PixelSize = int.Parse((PixelSizeCombo.SelectedItem as ComboBoxItem).Tag.ToString());
            settings.MaxHistoryItems = int.Parse((HistoryLimitCombo.SelectedItem as ComboBoxItem).Tag.ToString());
            settings.IncludeQuietZones = QuietZoneCheck.IsChecked == true;
            settings.LivePreview = LivePreviewCheck.IsChecked == true;
            settings.SaveHistory = HistoryCheck.IsChecked == true;
            settings.ApplyTheme(settings.Theme);
            settings.Save();
            Result = settings;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed) DragMove();
        }
    }
}
