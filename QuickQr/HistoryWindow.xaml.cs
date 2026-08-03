using Microsoft.Win32;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace QuickQr
{
    public partial class HistoryWindow : Window
    {
        private readonly HistoryStore store;
        private ICollectionView historyView;
        private bool showFavoritesOnly;
        private bool isInitialized;
        public HistoryItem SelectedItem { get; private set; }

        public HistoryWindow(HistoryStore store)
        {
            isInitialized = false;
            InitializeComponent();
            this.store = store;
            historyView = CollectionViewSource.GetDefaultView(store.Items);
            historyView.Filter = FilterHistory;
            historyView.SortDescriptions.Add(new SortDescription(nameof(HistoryItem.CreatedAt), ListSortDirection.Descending));
            HistoryList.ItemsSource = historyView;
            UpdateFilterButtons();
            UpdateSummary();
            UpdateEmptyState();
            isInitialized = true;
        }

        private void HistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedItem = HistoryList.SelectedItem as HistoryItem;
            if (SelectedItem != null) DialogResult = true;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isInitialized || historyView == null) return;
            historyView.Refresh();
            UpdateEmptyState();
            UpdateSummary();
            SearchHint.Visibility = string.IsNullOrWhiteSpace(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AllFilter_Click(object sender, RoutedEventArgs e)
        {
            if (historyView == null) return;
            showFavoritesOnly = false;
            historyView.Refresh();
            UpdateFilterButtons();
            UpdateSummary();
        }

        private void FavoritesFilter_Click(object sender, RoutedEventArgs e)
        {
            showFavoritesOnly = true;
            historyView.Refresh();
            UpdateFilterButtons();
            UpdateSummary();
        }

        private void Favorite_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is HistoryItem item)
            {
                item.IsFavorite = !item.IsFavorite;
                store.Persist();
                historyView.Refresh();
                UpdateSummary();
            }
        }

        private void CopyHistory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is HistoryItem item)
            {
                Clipboard.SetText(item.Content ?? string.Empty);
                MessageBox.Show("History item copied to clipboard.", "Quick QR", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            if (!isInitialized || historyView == null) return;
            SearchBox.Clear();
            historyView.Refresh();
            UpdateSummary();
        }

        private void SortCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isInitialized || historyView == null || SortCombo.SelectedItem is not ComboBoxItem item) return;
            var tag = item.Tag?.ToString();
            historyView.SortDescriptions.Clear();
            if (tag == "Newest")
            {
                historyView.SortDescriptions.Add(new SortDescription(nameof(HistoryItem.CreatedAt), ListSortDirection.Descending));
            }
            else if (tag == "Oldest")
            {
                historyView.SortDescriptions.Add(new SortDescription(nameof(HistoryItem.CreatedAt), ListSortDirection.Ascending));
            }
            else if (tag == "FavoritesFirst")
            {
                historyView.SortDescriptions.Add(new SortDescription(nameof(HistoryItem.IsFavorite), ListSortDirection.Descending));
                historyView.SortDescriptions.Add(new SortDescription(nameof(HistoryItem.CreatedAt), ListSortDirection.Descending));
            }
            UpdateSummary();
        }

        private void Backup_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "XML backup|*.xml",
                FileName = "quick-qr-history-backup.xml",
                AddExtension = true,
                OverwritePrompt = true
            };
            if (dialog.ShowDialog() == true)
            {
                store.BackupTo(dialog.FileName);
            }
        }

        private void Restore_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "XML backup|*.xml",
                Multiselect = false
            };
            if (dialog.ShowDialog() == true)
            {
                store.RestoreFrom(dialog.FileName);
                historyView = CollectionViewSource.GetDefaultView(store.Items);
                historyView.Filter = FilterHistory;
                HistoryList.ItemsSource = historyView;
                UpdateEmptyState();
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Clear all saved QR history?", "Quick QR", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            store.Clear();
            historyView = CollectionViewSource.GetDefaultView(store.Items);
            historyView.Filter = FilterHistory;
            HistoryList.ItemsSource = historyView;
            UpdateEmptyState();
        }

        private bool FilterHistory(object obj)
        {
            if (obj is not HistoryItem item) return false;
            if (showFavoritesOnly && !item.IsFavorite) return false;
            var search = SearchBox.Text?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(search)) return true;
            return item.Preview.ToLowerInvariant().Contains(search) || item.Type.ToLowerInvariant().Contains(search) || item.Content.ToLowerInvariant().Contains(search);
        }

        private void UpdateFilterButtons()
        {
            AllFilterButton.IsEnabled = showFavoritesOnly;
            FavoritesFilterButton.IsEnabled = !showFavoritesOnly;
        }

        private void UpdateSummary()
        {
            var total = store.Items.Count;
            var visible = historyView.Cast<object>().Count();
            var favorites = store.Items.Count(item => item.IsFavorite);
            HistorySummaryText.Text = $"{visible} of {total} items shown · {favorites} favorites";
        }

        private void UpdateEmptyState()
        {
            EmptyText.Visibility = historyView.IsEmpty ? Visibility.Visible : Visibility.Collapsed;
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
