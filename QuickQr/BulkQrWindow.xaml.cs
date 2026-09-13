using System;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Forms;

namespace QuickQr
{
    public partial class BulkQrWindow : Window
    {
        public BulkQrWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new FolderBrowserDialog();
            folderDialog.Description = "Select a folder to save generated QR images";
            if (folderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            var items = GetItems();
            if (items == null) return;

            var folder = folderDialog.SelectedPath;
            var type = (TypeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "text";
            SaveItemsToFolder(items, type, folder);

            DialogResult = true;
            Close();
            System.Windows.MessageBox.Show($"Generated {items.Count} QR files in {folder}.", "Quick QR", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void GenerateZip_Click(object sender, RoutedEventArgs e)
        {
            var items = GetItems();
            if (items == null) return;

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "ZIP archive|*.zip",
                FileName = "quick-qr-batch.zip",
                AddExtension = true,
                OverwritePrompt = true
            };
            if (dialog.ShowDialog() != true) return;

            var type = (TypeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "text";
            using (var archive = ZipFile.Open(dialog.FileName, ZipArchiveMode.Create))
            {
                for (var index = 0; index < items.Count; index++)
                {
                    var bytes = CreateBytes(items[index], type);
                    var entry = archive.CreateEntry(QrGeneratorHelper.ToSafeFileName(items[index]) + "-" + (index + 1) + ".png", CompressionLevel.Fastest);
                    using (var stream = entry.Open())
                    {
                        stream.Write(bytes, 0, bytes.Length);
                    }
                }
            }

            DialogResult = true;
            Close();
            System.Windows.MessageBox.Show($"Generated {items.Count} QR files in {dialog.FileName}.", "Quick QR", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private System.Collections.Generic.List<string> GetItems()
        {
            var items = InputBox.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();
            if (items.Count > 0) return items;

            System.Windows.MessageBox.Show("Please enter at least one item.", "Quick QR", MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }

        private void SaveItemsToFolder(System.Collections.Generic.List<string> items, string type, string folder)
        {
            for (var index = 0; index < items.Count; index++)
            {
                var path = Path.Combine(folder, QrGeneratorHelper.ToSafeFileName(items[index]) + "-" + (index + 1) + ".png");
                File.WriteAllBytes(path, CreateBytes(items[index], type));
            }
        }

        private byte[] CreateBytes(string item, string type)
        {
            return QrGeneratorHelper.CreatePngBytes(item, type, "M", 24, "#17212B", "#FFFFFF", true);
        }
    }
}
