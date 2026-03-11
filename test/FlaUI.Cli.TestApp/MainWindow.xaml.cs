using System.Windows;

namespace FlaUI.Cli.TestApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        PopulateVirtualizedCombo();
        PopulateTestGrid();
    }

    private void PopulateTestGrid()
    {
        TestGrid.ItemsSource = new[]
        {
            new ContactRow("Alice", 30, "Helsinki"),
            new ContactRow("Bob", 25, "Stockholm"),
            new ContactRow("Carol", 35, "Oslo")
        };
    }

    private void PopulateVirtualizedCombo()
    {
        for (var i = 1; i <= 200; i++)
            VirtualizedCombo.Items.Add($"VItem {i}");
    }

    private void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        var firstName = FirstNameInput.Text.Trim();
        var lastName = LastNameInput.Text.Trim();

        if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
        {
            StatusLabel.Text = "Error: Required fields missing";
            return;
        }

        StatusLabel.Text = $"Submitted: {firstName} {lastName}";
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        FirstNameInput.Text = string.Empty;
        LastNameInput.Text = string.Empty;
        EmailInput.Text = string.Empty;
        CountryCombo.SelectedIndex = -1;
        AgreeCheckbox.IsChecked = false;
        TestExpander.IsExpanded = false;
        VirtualizedCombo.SelectedIndex = -1;
        TestSlider.Value = 50;
        StatusLabel.Text = "Ready";
    }

    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem menuItem)
        {
            StatusLabel.Text = $"Menu: {menuItem.Header}";
        }
    }

    private void HelpAbout_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AboutDialog { Owner = this };
        dialog.Show();
    }

    private void FileExit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

public record ContactRow(string Name, int Age, string City);
