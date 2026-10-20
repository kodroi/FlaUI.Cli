using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FlaUI.Cli.TestApp;

public partial class DpiTestWindow : Window
{
    private int _mouseCount;
    public DpiTestWindow()
    {
        InitializeComponent();
        Rows.ItemsSource = new[] { new BatchRow("Batch-A"), new BatchRow("Batch-B"), new BatchRow("Batch-C") };
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e) => MouseStatus.Text = $"mouse={++_mouseCount}";
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e) =>
        SelectionStatus.Text = $"selected={(Rows.SelectedItem as BatchRow)?.Key ?? "none"}";
    public record BatchRow(string Key);
}
