using System.Windows;
using System.Windows.Controls;

namespace QuickQr
{
    public partial class HistoryWindow : Window
    {
        private readonly HistoryStore store;
        public HistoryItem SelectedItem { get; private set; }

        public HistoryWindow(HistoryStore store)
        {
            InitializeComponent();
            this.store = store;
            HistoryList.ItemsSource = store.Items;
            EmptyText.Visibility = store.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void HistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedItem = HistoryList.SelectedItem as HistoryItem;
            if (SelectedItem != null) DialogResult = true;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Clear all saved QR history?", "Quick QR", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            store.Clear();
            HistoryList.ItemsSource = null;
            HistoryList.ItemsSource = store.Items;
            EmptyText.Visibility = Visibility.Visible;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed) DragMove();
        }
    }
}
