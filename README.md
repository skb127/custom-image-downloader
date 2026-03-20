# Custom Image Downloader V 1.0.0

A simple Windows Forms application that downloads files (typically images) from a list of URLs and saves them into a user-specified subfolder.

## Key features
- Paste multiple URLs (one per line) into the URL textbox.
- Choose a parent folder and provide a subfolder name; the subfolder will be created automatically.
- Downloads each file and overwrites existing files with the same name.
- Progress bar and status messages display current progress.
- Cancellation support: stop the process while the current file finishes.
- Simple logging: a daily log file `log_YYYY-MM-DD.txt` is created in the application's directory.

## Requirements
- .NET 8
- Windows with support for Windows Forms (run from Visual Studio or using `dotnet run`).

## Usage
1. Open the project in Visual Studio or run `dotnet run` from the project folder.
2. Paste one URL per line in the URLs textbox.
3. Click the folder selector to choose a parent folder.
4. Enter a subfolder name and click `Download`.
5. Use `Cancel` to stop the process (the file currently downloading will finish first).

## Notes
- The app uses `HttpClient` to download files and saves them using their original filename extracted from the URL.
- If a URL does not contain a filename, the app generates a timestamp-based filename.
- Errors and events are appended to the daily log file in the application folder.