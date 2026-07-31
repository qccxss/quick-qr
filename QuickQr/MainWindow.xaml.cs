using Microsoft.Win32;
using QRCoder;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QuickQr
{
    public partial class MainWindow : Window
    {
        private byte[] currentPng;
        private readonly UserSettings settings;
        private readonly HistoryStore history;

        public MainWindow()
        {
            settings = UserSettings.Load();
            history = new HistoryStore();
            history.Load();
            ApplyTheme();
            InitializeComponent();
            ApplyTheme();
            ContentBox.Focus();
        }

        private void ContentBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            CountText.Text = ContentBox.Text.Length + " / 1200";
            if (settings == null || settings.LivePreview) GenerateQr();
        }

        private void TypeCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (HintText == null) return;
            var item = TypeCombo.SelectedItem as System.Windows.Controls.ComboBoxItem;
            var type = item == null ? "text" : item.Tag.ToString();
            HintText.Text = type == "url" ? "Paste a full link, including https://."
                : type == "email" ? "Enter the e-mail address to share."
                : type == "wifi" ? "Use SSID|password|WPA for a Wi-Fi network."
                : type == "phone" ? "Enter a phone number, including country code."
                : type == "sms" ? "Use phone number and message separated by |."
                : type == "vcard" ? "Use name|phone|email separated by |."
                : "Add any text you want to share.";
            if (settings == null || settings.LivePreview) GenerateQr();
        }

        private void CorrectionCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            GenerateQr();
        }

        private void GenerateQr()
        {
            if (ContentBox == null || QrImage == null || EmptyState == null || CopyButton == null || SvgButton == null || SaveButton == null || StatusText == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(ContentBox.Text))
            {
                currentPng = null;
                QrImage.Source = null;
                EmptyState.Visibility = Visibility.Visible;
                CopyButton.IsEnabled = false;
                SvgButton.IsEnabled = false;
                SaveButton.IsEnabled = false;
                StatusText.Text = "Ready";
                return;
            }

            try
            {
                var payload = BuildPayload(ContentBox.Text.Trim());
                var correctionItem = CorrectionCombo.SelectedItem as System.Windows.Controls.ComboBoxItem;
                var correction = correctionItem == null ? QRCodeGenerator.ECCLevel.M : GetCorrection(correctionItem.Tag.ToString());

                using (var generator = new QRCodeGenerator())
                using (var data = generator.CreateQrCode(payload, correction))
                {
                    var qr = new PngByteQRCode(data);
                    currentPng = qr.GetGraphic(settings == null ? 24 : settings.PixelSize,
                        HexToRgb(settings == null ? "#17212B" : settings.ForegroundColor),
                        HexToRgb(settings == null ? "#FFFFFF" : settings.BackgroundColor),
                        settings == null || settings.IncludeQuietZones);
                }

                QrImage.Source = ToBitmapImage(currentPng);
                EmptyState.Visibility = Visibility.Collapsed;
                CopyButton.IsEnabled = true;
                SvgButton.IsEnabled = true;
                SaveButton.IsEnabled = true;
                StatusText.Text = "Updated just now";
            }
            catch (Exception ex)
            {
                currentPng = null;
                QrImage.Source = null;
                EmptyState.Visibility = Visibility.Visible;
                CopyButton.IsEnabled = false;
                SvgButton.IsEnabled = false;
                SaveButton.IsEnabled = false;
                StatusText.Text = ex.Message;
            }
        }

        private string BuildPayload(string value)
        {
            var item = TypeCombo.SelectedItem as System.Windows.Controls.ComboBoxItem;
            var type = item == null ? "text" : item.Tag.ToString();
            if (type == "email") return "mailto:" + value;
            if (type == "phone") return "tel:" + value;
            if (type == "sms")
            {
                var smsParts = value.Split(new[] { '|' }, 2);
                return smsParts.Length == 2 ? "SMSTO:" + smsParts[0] + ":" + smsParts[1] : "SMSTO:" + value;
            }
            if (type == "vcard")
            {
                var cardParts = value.Split('|');
                var name = cardParts.Length > 0 ? cardParts[0] : value;
                var phone = cardParts.Length > 1 ? cardParts[1] : string.Empty;
                var email = cardParts.Length > 2 ? cardParts[2] : string.Empty;
                return "BEGIN:VCARD\nVERSION:3.0\nFN:" + name + "\nTEL:" + phone + "\nEMAIL:" + email + "\nEND:VCARD";
            }
            if (type == "wifi")
            {
                var parts = value.Split('|');
                if (parts.Length >= 2)
                {
                    var security = parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2] : "WPA";
                    return "WIFI:T:" + security + ";S:" + EscapeWifi(parts[0]) + ";P:" + EscapeWifi(parts[1]) + ";;";
                }
            }
            return value;
        }

        private string EscapeWifi(string value)
        {
            return value.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace(":", "\\:");
        }

        private QRCodeGenerator.ECCLevel GetCorrection(string tag)
        {
            if (tag == "L") return QRCodeGenerator.ECCLevel.L;
            if (tag == "Q") return QRCodeGenerator.ECCLevel.Q;
            if (tag == "H") return QRCodeGenerator.ECCLevel.H;
            return QRCodeGenerator.ECCLevel.M;
        }

        private byte[] HexToRgb(string hex)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                return new[] { color.R, color.G, color.B };
            }
            catch
            {
                return new byte[] { 23, 33, 43 };
            }
        }

        private void ApplyTheme()
        {
            SetBrushColor("InkBrush", settings.ForegroundColor);
            SetBrushColor("MutedBrush", IsDarkTheme() ? "#A8B5B8" : "#657581");
            SetBrushColor("AccentBrush", settings.AccentColor);
            SetBrushColor("CanvasBrush", settings.CanvasColor);
            SetBrushColor("SurfaceBrush", settings.BackgroundColor);
            SetBrushColor("PreviewBrush", IsDarkTheme() ? "#26383A" : settings.Theme == AppTheme.Sunrise ? "#FFF0DF" : "#E9F6F3");
            SetBrushColor("LineBrush", IsDarkTheme() ? "#344249" : "#DDE7E7");
            SetBrushColor("WindowBorderBrush", IsDarkTheme() ? "#4A5A60" : "#FFFFFF");
            SetBrushColor("GlassBrush", IsDarkTheme() ? "#351E2933" : "#D9FFFFFF");
            var gradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };
            if (settings.Theme == AppTheme.Dark)
            {
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#182326"), 0));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#2A2429"), 0.52));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#172C2B"), 1));
            }
            else if (settings.Theme == AppTheme.Sunrise)
            {
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#FFF2E7"), 0));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#F8E7DD"), 0.52));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#E7F0E8"), 1));
            }
            else if (settings.Theme == AppTheme.Galaxy)
            {
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#101120"), 0));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#25203D"), 0.52));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#102B38"), 1));
            }
            else if (settings.Theme == AppTheme.Forest)
            {
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#102019"), 0));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#18372C"), 0.52));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#102A2D"), 1));
            }
            else if (settings.Theme == AppTheme.Ocean)
            {
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#0C1B25"), 0));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#123A4A"), 0.52));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#102332"), 1));
            }
            else
            {
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#E9F2F3"), 0));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#F6F1EE"), 0.52));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#E5F1EF"), 1));
            }
            Application.Current.Resources["CanvasGradient"] = gradient;
            Background = Brushes.Transparent;
        }

        private bool IsDarkTheme()
        {
            return settings.Theme == AppTheme.Dark || settings.Theme == AppTheme.Galaxy || settings.Theme == AppTheme.Forest || settings.Theme == AppTheme.Ocean;
        }

        private void SetBrushColor(string key, string value)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(value);
                Application.Current.Resources[key] = new SolidColorBrush(color);
            }
            catch
            {
                // An invalid theme value should not prevent the window from opening.
            }
        }

        private BitmapImage ToBitmapImage(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
                return image;
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            GenerateQr();
            if (currentPng == null)
            {
                StatusText.Text = "Add content first";
                return;
            }
            AddToHistory();
            StatusText.Text = "QR generated";
        }

        private void AddToHistory()
        {
            if (!settings.SaveHistory || string.IsNullOrWhiteSpace(ContentBox.Text)) return;
            var item = TypeCombo.SelectedItem as System.Windows.Controls.ComboBoxItem;
            history.Add(new HistoryItem
            {
                Type = item == null ? "Plain text" : item.Content.ToString(),
                Content = ContentBox.Text,
                Preview = ContentBox.Text.Replace("\r", " ").Replace("\n", " "),
                CreatedAt = DateTime.Now
            }, settings.MaxHistoryItems);
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SettingsWindow(settings) { Owner = this };
                if (dialog.ShowDialog() == true)
                {
                    ApplyTheme();
                    GenerateQr();
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Settings could not be opened", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void History_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new HistoryWindow(history) { Owner = this };
                if (dialog.ShowDialog() == true && dialog.SelectedItem != null)
                {
                    ContentBox.Text = dialog.SelectedItem.Content;
                    for (var index = 0; index < TypeCombo.Items.Count; index++)
                    {
                        var item = TypeCombo.Items[index] as System.Windows.Controls.ComboBoxItem;
                        if (item != null && item.Content.ToString() == dialog.SelectedItem.Type)
                        {
                            TypeCombo.SelectedIndex = index;
                            break;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "History could not be opened", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ContentBox.Clear();
            ContentBox.Focus();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPng == null) return;
            Clipboard.SetImage(ToBitmapImage(currentPng));
            StatusText.Text = "Copied to clipboard";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPng == null) return;
            var dialog = new SaveFileDialog
            {
                Filter = "PNG image|*.png",
                FileName = "quick-qr.png",
                AddExtension = true,
                OverwritePrompt = true
            };
            if (dialog.ShowDialog() == true)
            {
                File.WriteAllBytes(dialog.FileName, currentPng);
                StatusText.Text = "Saved successfully";
            }
        }

        private void SvgButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPng == null || string.IsNullOrWhiteSpace(ContentBox.Text)) return;
            var dialog = new SaveFileDialog
            {
                Filter = "SVG image|*.svg",
                FileName = "quick-qr.svg",
                AddExtension = true,
                OverwritePrompt = true
            };
            if (dialog.ShowDialog() != true) return;

            var correctionItem = CorrectionCombo.SelectedItem as System.Windows.Controls.ComboBoxItem;
            var correction = correctionItem == null ? QRCodeGenerator.ECCLevel.M : GetCorrection(correctionItem.Tag.ToString());
            using (var generator = new QRCodeGenerator())
            using (var data = generator.CreateQrCode(BuildPayload(ContentBox.Text.Trim()), correction))
            {
                var svg = new SvgQRCode(data).GetGraphic(10);
                File.WriteAllText(dialog.FileName, svg);
            }
            StatusText.Text = "SVG saved successfully";
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter &&
                (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
            {
                CreateButton_Click(sender, e);
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.L &&
                (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
            {
                ContentBox.Focus();
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                ContentBox.Clear();
                e.Handled = true;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}
