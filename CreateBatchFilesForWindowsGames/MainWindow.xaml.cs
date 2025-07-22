using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace CreateBatchFilesForWindowsGames;

public partial class MainWindow
{
    // The local BugReportService instance and constants have been removed.
    // The service is now accessed via App.BugReportService.

    public MainWindow()
    {
        InitializeComponent();

        LogMessage("Welcome to the Batch File Creator for Microsoft Windows Games.");
        LogMessage("");
        LogMessage("This program creates batch files to launch your Microsoft Windows games.");
        LogMessage("Please follow these steps:");
        LogMessage("1. Select the game executable file (.exe)");
        LogMessage("2. Choose where to save the batch file");
        LogMessage("3. Click 'Create Batch File' to generate the batch file");
        LogMessage("");

        // Initially disable the Create Batch File button until paths are selected
        CreateBatchFileButton.IsEnabled = false;
        UpdateStatusBarMessage("Ready");
    }

    private void UpdateStatusBarMessage(string message)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            StatusBarMessage.Text = message;
        });
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        // This is the standard, graceful way to shut down a WPF application.
        // The redundant and forceful Environment.Exit(0) has been removed.
        Application.Current.Shutdown();
    }

    private void LogMessage(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            LogTextBox.AppendText(message + Environment.NewLine);
            LogTextBox.ScrollToEnd();
        });
    }

    private async void BrowseGameExeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var gameExePath = SelectGameExecutable();
            if (string.IsNullOrEmpty(gameExePath)) return;

            GameExePathTextBox.Text = gameExePath;
            LogMessage($"Game executable selected: {gameExePath}");
            UpdateStatusBarMessage("Game executable selected.");

            // Verify the file exists and is an executable
            if (!File.Exists(gameExePath))
            {
                LogMessage("Warning: The selected file does not exist.");
                await ReportBugAsync("User selected a game executable that doesn't exist: " + gameExePath);
            }
            else if (!gameExePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                LogMessage("Warning: The selected file does not appear to be an executable (.exe) file.");
                await ReportBugAsync("User selected a file that doesn't appear to be an executable: " + gameExePath);
            }

            UpdateCreateButtonStatus();
        }
        catch (Exception ex)
        {
            await ReportBugAsync("Error in BrowseGameExeButton_Click", ex);
        }
    }

    private async void SaveBatchFileButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var gameExePath = GameExePathTextBox.Text;
            if (string.IsNullOrEmpty(gameExePath))
            {
                ShowError("Please select a game executable first.");
                return;
            }

            var gameFolderPath = Path.GetDirectoryName(gameExePath) ?? "";
            var folderName = Path.GetFileName(gameFolderPath.TrimEnd(Path.DirectorySeparatorChar));

            var batchFilePath = SaveBatchFile(folderName);
            if (string.IsNullOrEmpty(batchFilePath)) return;

            BatchFilePathTextBox.Text = batchFilePath;
            LogMessage($"Batch file location selected: {batchFilePath}");
            UpdateStatusBarMessage("Batch file location selected.");

            // Check if the directory exists and is writable
            var batchFileDirectory = Path.GetDirectoryName(batchFilePath);
            if (string.IsNullOrEmpty(batchFileDirectory) || !Directory.Exists(batchFileDirectory))
            {
                LogMessage($"Warning: The selected directory does not exist: {batchFileDirectory}");
                await ReportBugAsync($"User selected a non-existent batch file directory: {batchFileDirectory}");
            }
            else
            {
                try
                {
                    // Test if we can write to the directory
                    var testFilePath = Path.Combine(batchFileDirectory, ".write_test_" + Guid.NewGuid());
                    await File.WriteAllTextAsync(testFilePath, "test");
                    File.Delete(testFilePath);
                }
                catch (Exception ex)
                {
                    LogMessage($"Warning: The selected directory may not be writable: {ex.Message}");
                    await ReportBugAsync($"Selected batch file directory may not be writable: {batchFileDirectory}", ex);
                }
            }

            UpdateCreateButtonStatus();
        }
        catch (Exception ex)
        {
            LogMessage($"Error selecting batch file location: {ex.Message}");
            ShowError($"Error selecting batch file location: {ex.Message}");
            await ReportBugAsync("Error selecting batch file location", ex);
        }
    }

    private async void CreateBatchFileButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var gameExePath = GameExePathTextBox.Text;
            var batchFilePath = BatchFilePathTextBox.Text;

            if (string.IsNullOrEmpty(gameExePath))
            {
                LogMessage("Error: No game executable selected.");
                ShowError("Please select a game executable file.");
                UpdateStatusBarMessage("Error: Game executable not selected.");
                return;
            }

            if (!File.Exists(gameExePath))
            {
                LogMessage($"Error: Game executable not found at path: {gameExePath}");
                ShowError("The selected game executable file does not exist.");
                await ReportBugAsync("Game executable not found", new FileNotFoundException("The game executable was not found", gameExePath));
                UpdateStatusBarMessage("Error: Game executable not found.");
                return;
            }

            if (string.IsNullOrEmpty(batchFilePath))
            {
                LogMessage("Error: No batch file location selected.");
                ShowError("Please select where to save the batch file.");
                UpdateStatusBarMessage("Error: Batch file location not selected.");
                return;
            }

            // Create the batch file
            try
            {
                UpdateStatusBarMessage("Creating batch file...");
                var gameFolderPath = Path.GetDirectoryName(gameExePath) ?? "";
                var gameFileName = Path.GetFileName(gameExePath);

                await using (StreamWriter sw = new(batchFilePath))
                {
                    await sw.WriteLineAsync("@echo off");
                    await sw.WriteLineAsync($"cd /d \"{gameFolderPath}\"");
                    await sw.WriteLineAsync($"start \"\" \"{gameFileName}\""); // Added an empty title to "start" for robustness
                }

                LogMessage("");
                LogMessage($"Batch file '{Path.GetFileName(batchFilePath)}' has been successfully created.");
                UpdateStatusBarMessage("Batch file created successfully.");

                ShowMessageBox($"Batch file '{Path.GetFileName(batchFilePath)}' has been successfully created.",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Show the "Create Another" button and hide the "Create" button
                CreateBatchFileButton.Visibility = Visibility.Collapsed;
                CreateAnotherButton.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                LogMessage($"Error creating batch file: {ex.Message}");
                ShowError($"Error creating batch file: {ex.Message}");
                await ReportBugAsync("Error creating batch file", ex);
                UpdateStatusBarMessage("Error creating batch file.");
            }
        }
        catch (Exception ex)
        {
            await ReportBugAsync("Error in CreateBatchFileButton_Click", ex);
            UpdateStatusBarMessage("An unexpected error occurred.");
        }
    }

    private void CreateAnotherButton_Click(object sender, RoutedEventArgs e)
    {
        // Reset the UI for creating another batch file
        GameExePathTextBox.Text = "";
        BatchFilePathTextBox.Text = "";

        // Hide the "Create Another" button and show the "Create" button
        CreateAnotherButton.Visibility = Visibility.Collapsed;
        CreateBatchFileButton.Visibility = Visibility.Visible;
        CreateBatchFileButton.IsEnabled = false;

        LogMessage("");
        LogMessage("Ready to create another batch file.");
        UpdateStatusBarMessage("Ready");
    }

    private void UpdateCreateButtonStatus()
    {
        // Enable the Create button only if both paths are selected
        CreateBatchFileButton.IsEnabled = !string.IsNullOrEmpty(GameExePathTextBox.Text) &&
                                          !string.IsNullOrEmpty(BatchFilePathTextBox.Text);
    }

    private string? SelectGameExecutable()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Please select the game executable file (e.g., game.exe)",
            Filter = "exe files (*.exe)|*.exe|All files (*.*)|*.*",
            RestoreDirectory = true
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private string? SaveBatchFile(string suggestedName)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save Batch File",
            Filter = "Batch files (*.bat)|*.bat",
            DefaultExt = "bat",
            AddExtension = true,
            FileName = $"{suggestedName}.bat"
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private void ShowMessageBox(string message, string title, MessageBoxButton buttons, MessageBoxImage icon)
    {
        Dispatcher.Invoke(() =>
            MessageBox.Show(this, message, title, buttons, icon));
    }

    private void ShowError(string message)
    {
        ShowMessageBox(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private async Task ReportBugAsync(string message, Exception? exception = null)
    {
        try
        {
            var fullReport = new StringBuilder();
            var assemblyName = GetType().Assembly.GetName();

            // Add system information
            fullReport.AppendLine("=== Bug Report ===");
            fullReport.AppendLine(CultureInfo.InvariantCulture, $"Application: {assemblyName.Name}");
            fullReport.AppendLine(CultureInfo.InvariantCulture, $"Version: {assemblyName.Version}");
            fullReport.AppendLine(CultureInfo.InvariantCulture, $"OS: {Environment.OSVersion}");
            fullReport.AppendLine(CultureInfo.InvariantCulture, $".NET Version: {Environment.Version}");
            fullReport.AppendLine(CultureInfo.InvariantCulture, $"Date/Time: {DateTime.Now}");
            fullReport.AppendLine();

            // Add a message
            fullReport.AppendLine("=== Error Message ===");
            fullReport.AppendLine(message);
            fullReport.AppendLine();

            // Add exception details if available
            if (exception != null)
            {
                fullReport.AppendLine("=== Exception Details ===");
                fullReport.AppendLine(CultureInfo.InvariantCulture, $"Type: {exception.GetType().FullName}");
                fullReport.AppendLine(CultureInfo.InvariantCulture, $"Message: {exception.Message}");
                fullReport.AppendLine(CultureInfo.InvariantCulture, $"Source: {exception.Source}");
                fullReport.AppendLine("Stack Trace:");
                fullReport.AppendLine(exception.StackTrace);

                // Add inner exception if available
                if (exception.InnerException != null)
                {
                    fullReport.AppendLine("Inner Exception:");
                    fullReport.AppendLine(CultureInfo.InvariantCulture, $"Type: {exception.InnerException.GetType().FullName}");
                    fullReport.AppendLine(CultureInfo.InvariantCulture, $"Message: {exception.InnerException.Message}");
                    fullReport.AppendLine("Stack Trace:");
                    fullReport.AppendLine(exception.InnerException.StackTrace);
                }
            }

            // Add log contents if available
            if (LogTextBox != null)
            {
                var logContent = await Dispatcher.InvokeAsync(() => LogTextBox.Text);
                if (!string.IsNullOrEmpty(logContent))
                {
                    fullReport.AppendLine().AppendLine("=== Application Log ===").Append(logContent);
                }
            }

            // Add paths' information if available
            if (GameExePathTextBox != null && BatchFilePathTextBox != null)
            {
                var (gameExePath, batchFilePath) = await Dispatcher.InvokeAsync(() => (GameExePathTextBox.Text, BatchFilePathTextBox.Text));
                fullReport.AppendLine().AppendLine("=== Paths ===").AppendLine(CultureInfo.InvariantCulture, $"Game Executable Path: {gameExePath}").AppendLine(CultureInfo.InvariantCulture, $"Batch File Path: {batchFilePath}");
            }

            // Silently send the report using the shared service from the App class
            if (App.BugReportService != null)
            {
                await App.BugReportService.SendBugReportAsync(fullReport.ToString());
            }
        }
        catch
        {
            // Silently fail if error reporting itself fails
        }
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            LogMessage($"Error opening About window: {ex.Message}");
            _ = ReportBugAsync("Error opening About window", ex);
        }
    }
}
