using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using QRCoder;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace QuickQr
{
    public partial class MainWindow : Window
    {
        private byte[] currentPng;
        private readonly UserSettings settings;
        private readonly HistoryStore history;
        private readonly DispatcherTimer previewTimer;
        private readonly DispatcherTimer autoSaveTimer;
        public bool AllowClose { get; set; }

        public MainWindow()
        {
            settings = UserSettings.Load();
            history = new HistoryStore();
            history.Load();
            InitializeComponent();
            previewTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) };
            previewTimer.Tick += PreviewTimer_Tick;
            autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(650) };
            autoSaveTimer.Tick += AutoSaveTimer_Tick;
            RenderOptions.SetBitmapScalingMode(QrImage, BitmapScalingMode.NearestNeighbor);
            ApplyTheme();
            ApplySavedContentType();
            ContentBox.Focus();
        }

        private void ApplySavedContentType()
        {
            var index = GetTypeIndex(settings.LastContentType);
            TypeCombo.SelectedIndex = index;
        }

        private int GetTypeIndex(string type)
        {
            for (var index = 0; index < TypeCombo.Items.Count; index++)
            {
                if ((TypeCombo.Items[index] as System.Windows.Controls.ComboBoxItem)?.Tag?.ToString() == type)
                {
                    return index;
                }
            }
            return 0;
        }

        private void ContentBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            CountText.Text = ContentBox.Text.Length + " / 1200";
            if (settings == null || settings.LivePreview) SchedulePreview();
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
                : type == "bitcoin" ? "Use wallet|amount|label, or paste a bitcoin address."
                : type == "event" ? "Use title|YYYYMMDDTHHMM|location|description."
                : type == "location" ? "Use latitude|longitude|label for a map location."
                : type == "vcard" ? "Use name|phone|email separated by |."
                : "Add any text you want to share.";
            if (settings == null || settings.LivePreview) SchedulePreview();
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
                ImageInfoText.Text = "PNG · preview disabled";
                EmptyState.Visibility = Visibility.Visible;
                CopyButton.IsEnabled = false;
                SvgButton.IsEnabled = false;
                PdfButton.IsEnabled = false;
                SaveButton.IsEnabled = false;
                CopyPayloadButton.IsEnabled = false;
                CopyHtmlButton.IsEnabled = false;
                ShareButton.IsEnabled = false;
                StatusText.Text = "Ready";
                return;
            }

            try
            {
                var item = TypeCombo.SelectedItem as ComboBoxItem;
                var type = item == null ? "text" : item.Tag.ToString();
                var correctionItem = CorrectionCombo.SelectedItem as ComboBoxItem;
                var correctionTag = correctionItem == null ? "M" : correctionItem.Tag.ToString();
                currentPng = QrGeneratorHelper.CreatePngBytes(ContentBox.Text.Trim(), type, correctionTag, settings == null ? 24 : settings.PixelSize,
                    settings == null ? "#17212B" : settings.QrForegroundColor,
                    settings == null ? "#FFFFFF" : settings.QrBackgroundColor,
                    settings == null || settings.IncludeQuietZones);

                QrImage.Source = ToBitmapImage(currentPng);
                AnimateQrPreview();
                UpdateImageInfo();
                EmptyState.Visibility = Visibility.Collapsed;
                CopyButton.IsEnabled = true;
                SvgButton.IsEnabled = true;
                PdfButton.IsEnabled = true;
                SaveButton.IsEnabled = true;
                CopyPayloadButton.IsEnabled = true;
                CopyHtmlButton.IsEnabled = true;
                ShareButton.IsEnabled = true;
                if (settings.AutoCopy)
                {
                    try
                    {
                        Clipboard.SetImage(ToBitmapImage(currentPng));
                        StatusText.Text = "QR copied automatically";
                    }
                    catch
                    {
                        StatusText.Text = "Updated just now";
                    }
                }
                else
                {
                    StatusText.Text = "Updated just now";
                }
                if (settings.AutoSavePng) ScheduleAutoSave();
            }
            catch (Exception ex)
            {
                currentPng = null;
                QrImage.Source = null;
                EmptyState.Visibility = Visibility.Visible;
                CopyButton.IsEnabled = false;
                SvgButton.IsEnabled = false;
                PdfButton.IsEnabled = false;
                SaveButton.IsEnabled = false;
                StatusText.Text = ex.Message;
            }
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
            var effectiveTheme = settings.Theme;
            if (settings.FollowSystemTheme || effectiveTheme == AppTheme.System)
            {
                effectiveTheme = GetSystemThemePreference() ? AppTheme.Light : AppTheme.Dark;
            }

            SetBrushColor("InkBrush", settings.ForegroundColor);
            SetBrushColor("MutedBrush", IsDarkTheme(effectiveTheme) ? "#A8B5B8" : "#657581");
            SetBrushColor("AccentBrush", settings.AccentColor);
            SetBrushColor("CanvasBrush", settings.CanvasColor);
            SetBrushColor("SurfaceBrush", settings.BackgroundColor);
            SetBrushColor("PreviewBrush", IsDarkTheme(effectiveTheme) ? "#26383A" : settings.Theme == AppTheme.Sunrise ? "#FFF0DF" : "#E9F6F3");
            SetBrushColor("LineBrush", IsDarkTheme(effectiveTheme) ? "#344249" : "#DDE7E7");
            SetBrushColor("WindowBorderBrush", IsDarkTheme(effectiveTheme) ? "#4A5A60" : "#FFFFFF");
            SetBrushColor("GlassBrush", IsDarkTheme(effectiveTheme) ? "#351E2933" : "#D9FFFFFF");
            var gradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };
            if (effectiveTheme == AppTheme.Dark)
            {
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#182326"), 0));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#2A2429"), 0.52));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#172C2B"), 1));
            }
            else if (effectiveTheme == AppTheme.Sunrise)
            {
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#FFF2E7"), 0));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#F8E7DD"), 0.52));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#E7F0E8"), 1));
            }
            else if (effectiveTheme == AppTheme.Galaxy)
            {
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#101120"), 0));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#25203D"), 0.52));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#102B38"), 1));
            }
            else if (effectiveTheme == AppTheme.Forest)
            {
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#102019"), 0));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#18372C"), 0.52));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#102A2D"), 1));
            }
            else if (effectiveTheme == AppTheme.Ocean)
            {
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#0C1B25"), 0));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#123A4A"), 0.52));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#102332"), 1));
            }
            else if (effectiveTheme == AppTheme.Twilight)
            {
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#1F173B"), 0));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#2F2052"), 0.52));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#2B214F"), 1));
            }
            else if (effectiveTheme == AppTheme.Neon)
            {
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#0C0E17"), 0));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#1F1A3E"), 0.52));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#0F1329"), 1));
            }
            else if (effectiveTheme == AppTheme.Minimal)
            {
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#FFFFFF"), 0));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#F5F7FA"), 0.52));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#E8EEF4"), 1));
            }
            else if (effectiveTheme == AppTheme.Glass)
            {
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#1B2733"), 0));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#213144"), 0.52));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#172230"), 1));
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

        private bool IsDarkTheme(AppTheme theme)
        {
            return theme == AppTheme.Dark || theme == AppTheme.Galaxy || theme == AppTheme.Forest || theme == AppTheme.Ocean || theme == AppTheme.Twilight || theme == AppTheme.Neon || theme == AppTheme.Glass;
        }

        private bool GetSystemThemePreference()
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

        private void AnimateQrPreview()
        {
            if (QrImage == null) return;

            if (!(QrImage.RenderTransform is ScaleTransform))
            {
                QrImage.RenderTransform = new ScaleTransform(0.98, 0.98);
            }

            QrImage.Opacity = 0;
            var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var scale = new DoubleAnimation(0.98, 1, TimeSpan.FromMilliseconds(180))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            QrImage.BeginAnimation(UIElement.OpacityProperty, fade);
            ((ScaleTransform)QrImage.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, scale);
            ((ScaleTransform)QrImage.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, scale);
        }

        private void UpdateImageInfo()
        {
            if (QrImage.Source is BitmapSource bitmap)
            {
                ImageInfoText.Text = $"PNG · {bitmap.PixelWidth} × {bitmap.PixelHeight} px";
            }
            else
            {
                ImageInfoText.Text = "PNG · preview disabled";
            }
        }

        private void OpenGitHub_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://github.com/qccxss/quick-qr") { UseShellExecute = true });
            }
            catch (Exception)
            {
                MessageBox.Show("Could not open GitHub link.", "Quick QR", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        private void SchedulePreview()
        {
            previewTimer.Stop();
            previewTimer.Start();
        }

        private void PreviewTimer_Tick(object sender, EventArgs e)
        {
            previewTimer.Stop();
            GenerateQr();
        }

        private void ScheduleAutoSave()
        {
            autoSaveTimer.Stop();
            autoSaveTimer.Start();
        }

        private void AutoSaveTimer_Tick(object sender, EventArgs e)
        {
            autoSaveTimer.Stop();
            if (currentPng == null) return;
            try
            {
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QuickQr", "AutoSave");
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder, "latest.png"), currentPng);
            }
            catch
            {
            }
        }

        private void ApplyPreset_Click(object sender, RoutedEventArgs e)
        {
            var preset = (PresetCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (string.IsNullOrWhiteSpace(preset) || preset == "none")
            {
                StatusText.Text = "Choose a preset first";
                return;
            }

            var sample = preset == "url" ? "https://example.com"
                : preset == "vcard" ? "Full Name|+1 555 010 1234|name@example.com"
                : preset == "wifi" ? "NetworkName|NetworkPassword|WPA"
                : "wallet-address|0.01|Payment label";

            TypeCombo.SelectedIndex = GetTypeIndex(preset);
            ContentBox.Text = sample;
            ContentBox.Focus();
            ContentBox.SelectAll();
            StatusText.Text = "Preset applied";
        }

        private void BulkQr_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new BulkQrWindow { Owner = this };
                dialog.ShowDialog();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Bulk QR could not be opened", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private void CopyPayloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ContentBox.Text)) return;
            Clipboard.SetText(QrGeneratorHelper.BuildPayload(ContentBox.Text.Trim(), GetCurrentType()));
            StatusText.Text = "Payload copied";
        }

        private void CopyHtmlButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPng == null) return;
            var base64 = Convert.ToBase64String(currentPng);
            var html = $"<img src=\"data:image/png;base64,{base64}\" alt=\"Quick QR code\" />";
            Clipboard.SetText(html);
            StatusText.Text = "HTML snippet copied";
        }

        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ContentBox.Text)) return;
            try
            {
                var payload = QrGeneratorHelper.BuildPayload(ContentBox.Text.Trim(), GetCurrentType());
                Process.Start(new ProcessStartInfo($"mailto:?subject=Quick%20QR&body={Uri.EscapeDataString(payload)}") { UseShellExecute = true });
                StatusText.Text = "Sharing via mail client";
            }
            catch (Exception)
            {
                Clipboard.SetText(QrGeneratorHelper.BuildPayload(ContentBox.Text.Trim(), GetCurrentType()));
                StatusText.Text = "Share failed, payload copied";
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var window = new AboutWindow { Owner = this };
            window.ShowDialog();
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
            using (var data = generator.CreateQrCode(QrGeneratorHelper.BuildPayload(ContentBox.Text.Trim(), GetCurrentType()), correction))
            {
                var svg = new SvgQRCode(data).GetGraphic(10);
                File.WriteAllText(dialog.FileName, svg);
            }
            StatusText.Text = "SVG saved successfully";
        }

        private void PdfButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPng == null) return;
            var dialog = new SaveFileDialog
            {
                Filter = "PDF document|*.pdf",
                FileName = "quick-qr.pdf",
                AddExtension = true,
                OverwritePrompt = true
            };
            if (dialog.ShowDialog() != true) return;

            using (var document = new PdfDocument())
            using (var stream = new MemoryStream(currentPng))
            using (var image = XImage.FromStream(stream))
            {
                var page = document.AddPage();
                page.Size = PdfSharp.PageSize.A4;
                using (var graphics = XGraphics.FromPdfPage(page))
                {
                    const double imageSize = 420;
                    var x = (page.Width - imageSize) / 2;
                    var y = (page.Height - imageSize) / 2;
                    graphics.DrawImage(image, x, y, imageSize, imageSize);
                }
                document.Save(dialog.FileName);
            }
            StatusText.Text = "PDF saved successfully";
        }

        private string GetCurrentType()
        {
            var item = TypeCombo.SelectedItem as ComboBoxItem;
            return item == null ? "text" : item.Tag?.ToString() ?? "text";
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
            else if (e.Key == System.Windows.Input.Key.S &&
                (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
            {
                if ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) == System.Windows.Input.ModifierKeys.Shift)
                {
                    SvgButton_Click(sender, e);
                }
                else
                {
                    SaveButton_Click(sender, e);
                }
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.P &&
                (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
            {
                PdfButton_Click(sender, e);
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

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (ContentBox != null && ContentBox.IsKeyboardFocused)
            {
                ContentBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                Keyboard.ClearFocus();
            }
        }

        private void ContentBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (ContentBox != null && !ContentBox.IsKeyboardFocused)
            {
                Keyboard.ClearFocus();
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            previewTimer.Stop();
            autoSaveTimer.Stop();
            if (settings.MinimizeToTray && !AllowClose)
            {
                e.Cancel = true;
                Hide();
            }
        }
    }
}
