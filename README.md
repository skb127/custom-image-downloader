# Custom Image Downloader

A robust, multi-threaded Windows Forms application designed to efficiently download bulk files from a list of URLs. It features advanced download management, file-type filtering, URL validation, memory optimization, and full internationalization.

## Supported File Types

While the application is primarily designed and optimized for downloading **images**, it also fully supports the batch downloading of other file formats. The application restricts downloads to a configurable set of allowed types. By default it supports:

**Images:** `.jpg` / `.jpeg`, `.png`, `.gif`, `.bmp`, `.webp`, `.svg`, `.tiff` / `.tif`, `.ico`, `.avif`

**Documents:** `.pdf`, `.txt`, `.csv`

## Key Features

- **Bulk Downloading**: Paste hundreds or thousands of URLs — one URL per line, separated by a line break — to process them in a single batch.
- **Clear URL List**: Quickly clear all entered URLs from the text box with a single button click.
- **URL Format Validation**: Before starting, the application validates each line. Lines that are not valid `http`/`https` URLs (e.g. contain spaces, have an invalid scheme, or include two URLs on the same line) are flagged as invalid. A dialog informs the user of how many lines are invalid and how many are valid, giving the option to proceed or cancel.
- **Two-Tier File Type Filtering**:
  - *Tier 1 – Extension check (no network)*: If the URL has a visible extension, it is validated locally against `AllowedExtensions`. Disallowed extensions are skipped immediately.
  - *Tier 2 – Content-Type check (headers only)*: If the URL has no extension (e.g. `https://api.example.com/avatar/55`), the application fetches only the response headers and inspects the `Content-Type`. If the MIME type is not in `AllowedMimeTypes`, the file is skipped without downloading the body.
- **Original Filename Preservation**: The application uses the original filename from the URL whenever possible (e.g. `img_5terre.jpg`). A custom indexed name (`subfolder_001.ext`) is only used when the URL has no filename. Naming conflicts are resolved automatically by appending a numeric suffix (`_2`, `_3`, …).
- **Download Summary**: At the end of the process, a summary shows:
  - ✅ Successful downloads
  - ❌ Failed downloads (network/server errors)
  - ⏭️ Skipped downloads (file type not allowed)
- **Adjustable Concurrency**: Control the number of simultaneous downloads via the UI to avoid network bottlenecking and optimize bandwidth.
- **Pause & Resume**: Suspend the entire download queue at any moment and resume without losing progress.
- **Graceful Cancellation**: Instantly abort the process. Only the files downloaded in the current session are removed; any pre-existing content in the destination folder is preserved.
- **Multi-language Support (i18n)**: Fully localized UI and messages. Currently supports English (default) and Spanish (`es`), automatically adapting to the user's OS language.
- **Dynamic Folder Management**: Select a destination and provide a subfolder name (defaults to `Downloads` if left blank). The application handles directory creation automatically and safely blocks or sanitizes any invalid characters for file paths.
- **Real-time Feedback**: Visual progress bar and status label keep the user informed of the exact progress. The status resets to *Ready* after the process completes.
- **Daily Logging**: Errors and system events are silently recorded in a daily `log_YYYY-MM-DD.txt` file for easy debugging.

## Technical Overview & Mechanisms

The project is built emphasising performance, responsive UI, and clean architecture:

- **Asynchronous Programming (TPL)**: Heavy utilisation of `async/await` and `Task.Run` ensures the UI thread remains completely responsive, even when managing dozens of concurrent I/O operations.
- **Memory-Efficient Streaming**: Instead of loading whole files into RAM, the `HttpClient` uses `HttpCompletionOption.ResponseHeadersRead` combined with `CopyToAsync`. Data is streamed directly to disk via an asynchronous `FileStream`, keeping a flat memory footprint regardless of file size.
- **Concurrency Control**: A `SemaphoreSlim` restricts the number of active worker tasks based on the user's selection, efficiently throttling network and disk usage.
- **Efficient Pausing**: Implemented with `ManualResetEventSlim`. Worker tasks block at OS level without consuming CPU cycles while paused.
- **Robust Cancellation**: `CancellationTokenSource` is passed down the entire chain (tasks, network requests, stream writing, and pause events), ensuring immediate and safe termination.
- **Options Pattern**: File type configuration is managed via `IOptions<DownloadSettings>`, binding `appsettings.json` at startup. Allowed extensions and MIME types can be changed without recompiling.
- **Dependency Injection**: `IDownloadManager`, `IUrlValidator`, and `ILogger` are registered in a `ServiceCollection` in `Program.cs` and injected into the form, keeping the UI logic clean and testable.
- **String-Only Localization**: UI strings are applied at runtime from `.resx` resource files, keeping Designer files clean and making it trivial to add new languages.

## Requirements

- .NET 8
- Windows 10 or later
- Windows Forms support (run from Visual Studio or `dotnet run`).

## Usage

1. Extract the published `.zip` file and run `custom image downloader.exe`. Alternatively, open the project in Visual Studio or run `dotnet run` from the project folder.
2. The application launches in your OS language (English or Spanish).
3. Paste one URL per line (separated by line break) in the URLs text box.
4. Click `Browse...` to select a parent destination folder.
5. Enter a subfolder name (e.g. `Vacation_Pics`). If left empty, files will be saved in a `Downloads` folder.
6. Adjust the **Simultaneous downloads** counter to suit your connection.
7. Click `Download`.
8. Use `Pause`, `Resume`, or `Cancel` at any time during the process.

## Notes

- URLs containing spaces or any other whitespace are treated as invalid format (RFC 3986 forbids literal spaces in URLs; use `%20` instead).
- Cancelling a download only deletes the files written during that session; pre-existing files in the destination folder are never touched.
- To customise the allowed file types, edit `appsettings.json` in the application output directory — no recompilation needed.