using System.Windows;

namespace FlaUI.Cli.TestApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
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
        StatusLabel.Text = "Ready";
    }

    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem menuItem)
        {
            StatusLabel.Text = $"Menu: {menuItem.Header}";
        }
    }

    private void FileExit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
