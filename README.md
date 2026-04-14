# Custom Image Downloader

A robust, multi-threaded Windows Forms application designed to efficiently download bulk files from a list of URLs. It features advanced download management, memory optimization, and full internationalization.

## Supported File Types

While the application is primarily designed and optimized for downloading **images**, it also fully supports the batch downloading of other file formats, including:
- PDFs (`.pdf`)
- Text files (`.txt`)
- CSV files (`.csv`)

## Key Features (Functional Description)

- **Bulk Downloading**: Paste hundreds or thousands of URLs (one per line) to process them in a single batch.
- **Adjustable Concurrency**: Control the number of simultaneous downloads via the UI to avoid network bottlenecking and optimize bandwidth.
- **Pause & Resume**: Suspend the entire download queue at any moment and resume without losing progress.
- **Graceful Cancellation**: Instantly abort the process. The application automatically handles the cleanup of partially downloaded files to prevent corruption.
- **Multi-language Support (i18n)**: Fully localized UI and messages. Currently supports English (Default) and Spanish (`es`), automatically adapting to the user's OS language.
- **Dynamic Folder Management**: Select a destination and provide a subfolder name; the application handles the directory creation automatically.
- **Real-time Feedback**: Visual progress bar and status text keep the user informed of the exact progress (Successful / Failed downloads).
- **Daily Logging**: Errors and system events are silently recorded in a daily `log_YYYY-MM-DD.txt` file for easy debugging.

## Technical Overview & Mechanisms

The project is built emphasizing performance, responsive UI, and clean architecture:

- **Asynchronous Programming (TPL)**: Heavy utilization of `async/await` and `Task.Run` ensures the UI thread remains completely responsive, even when managing dozens of concurrent I/O operations.
- **Memory-Efficient Streaming**: Instead of loading whole files into RAM, the `HttpClient` uses `HttpCompletionOption.ResponseHeadersRead` combined with `CopyToAsync`. Data is streamed directly to the disk via an asynchronous `FileStream`, ensuring a flat memory footprint regardless of file size.
- **Concurrency Control**: A `SemaphoreSlim` mechanism restricts the number of active worker tasks based on the user's UI selection, efficiently throttling network and disk usage.
- **Efficient Pausing**: The Pause/Resume feature is implemented using `ManualResetEventSlim`. Unlike traditional `while` polling loops, this allows worker tasks to block at the OS level without consuming any CPU cycles while paused.
- **Robust Cancellation**: `CancellationTokenSource` is passed down the entire chain (tasks, network requests, stream writing, and pause events). This ensures immediate and safe termination of operations when requested.
- **Dependency Injection**: The architecture separates concerns by injecting `IDownloadManager` and `ILogger` into the presentation layer, keeping the UI logic clean and testable.
- **String-Only Localization**: The application uses resource files (`.resx`) accessed via a `ComponentResourceManager`. UI strings are dynamically applied at runtime, keeping the auto-generated Designer files clean and making it trivial to add new languages.

## Requirements

- .NET 8
- Windows with support for Windows Forms (run from Visual Studio or using `dotnet run`).

## Usage

1. Open the project in Visual Studio or run `dotnet run` from the project folder.
2. The application will launch in your OS language (English or Spanish).
3. Paste one URL per line in the URLs textbox.
4. Click `Browse...` to choose a parent destination folder.
5. Enter a subfolder name (e.g., `Vacation_Pics`).
6. Adjust the **Simultaneous downloads** counter according to your internet connection.
7. Click `Download All`.
8. Use `Pause`, `Resume`, or `Cancel` at any time.

## Notes

- The application extracts the original filename and extension directly from the URL.
- Fallback mechanism: If a URL does not contain a valid filename, the app generates a safe, timestamp-based filename (`download_ticks.bin`).
- Files are saved with an indexed suffix (e.g., `Name_001.jpg`, `Name_002.jpg`) to guarantee order and prevent accidental overwrites if multiple URLs share the same filename.