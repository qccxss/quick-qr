using System.Windows;
using System.Windows.Input;

namespace QuickQr
{
    public partial class HistoryEditWindow : Window
    {
        public string EditedContent { get; private set; }
        public string EditedTag { get; private set; }

        public HistoryEditWindow(HistoryItem item)
        {
            InitializeComponent();
            ContentBox.Text = item.Content ?? string.Empty;
            TagBox.Text = item.Tag ?? string.Empty;
            ContentBox.Focus();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ContentBox.Text))
            {
                MessageBox.Show("Content cannot be empty.", "Quick QR", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            EditedContent = ContentBox.Text.Trim();
            EditedTag = TagBox.Text.Trim();
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }
    }
}
