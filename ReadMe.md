# Batch File Creator for Windows Games

## Overview

This application simplifies the process of creating batch files for launching Windows games. It provides a user-friendly interface to select the game executable and choose the output location for the batch file. The application also includes automated bug reporting to help improve its reliability.

## Features

*   **Easy Batch File Creation:** Generates batch files with just a few clicks.
*   **User-Friendly Interface:** A simple GUI with a top menu and status bar for easy interaction.
*   **Game Executable Selection:** Allows users to browse and select the game's `.exe` file.
*   **Batch File Output Location:** Lets users choose where to save the generated `.bat` file.
*   **Logging:** Provides a log window to display messages and potential errors.
*   **Status Bar:** Displays real-time status messages about the application's state.
*   **Error Handling:** Includes error handling and warnings for common issues.
*   **Automated Bug Reporting:** Silently reports any unhandled exceptions or application errors to a remote API to help with debugging and improvements.

## How to Use

1.  **Select Game Executable:** Click the "Browse" button next to "Game Executable" and select the `.exe` file of the game you want to launch.
2.  **Choose Batch File Output:** Click the "Save As..." button next to "Batch File Output" and choose a location to save the generated `.bat` file.
3.  **Create Batch File:** Click the "Create Batch File" button.
4.  **Confirmation:** A message box will confirm the successful creation of the batch file.
5.  **Create Another:** After creating a batch file, a "Create Another Batch File" button will appear, allowing you to easily create another batch file for a different game. Clicking this button resets the UI.

## System Requirements

-   Windows Operating System
-   .NET 9 Desktop Runtime

## Technical Details

*   **Language:** C#
*   **Framework:** .NET 9 (WPF)
*   **UI:** XAML
*   **Dependencies:**
    *   System.Net.Http
    *   System.Net.Http.Json
    *   Microsoft.Win32

## File Structure

*   **App.xaml/App.xaml.cs:** Application entry point, global exception handling, and central management of the `BugReportService`.
*   **MainWindow.xaml/MainWindow.xaml.cs:** Main window of the application, handles UI interactions and batch file creation logic.
*   **AboutWindow.xaml/AboutWindow.xaml.cs:** A separate window that displays application information, version, and credits.
*   **BugReportService.cs:** A service that handles sending bug reports to a remote API using a shared, static `HttpClient`.