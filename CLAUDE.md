# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Restore, build, and run (platform flag required for the App project)
dotnet restore
dotnet build -c Debug -p:Platform=x64
dotnet run --project src/Scanner.App -p:Platform=x64
```

Scanner.Core and Scanner.Infrastructure target `AnyCPU`; Scanner.App requires an explicit platform (`x86`, `x64`, or `ARM64`).

There is no test project yet. The solution references a `tests/Scanner.Core.Tests` folder in the README but it has not been created.

## Architecture

WinUI 3 desktop app (.NET 8, C# latest) with three projects:

- **Scanner.App** — WinUI 3 UI layer. XAML views live in `Views/`, data-binding converters in `Converters/`. DI is configured in `App.xaml.cs` (`ConfigureServices`). Uses `WindowsPackageType=None` (unpackaged).
- **Scanner.Core** — Platform-independent business logic. ViewModels use `CommunityToolkit.Mvvm` (`[ObservableProperty]`, `[RelayCommand]`). Services implement interfaces in `Services/Interfaces/`. No UI dependencies.
- **Scanner.Infrastructure** — WIA scanner hardware integration via COM interop (`WIA/` folder). Implements `IScannerService`.

Dependency flow: `App → Core + Infrastructure`. Core has no reference to App or Infrastructure.

## Key Patterns

- **MVVM**: ViewModels are in `Scanner.Core/ViewModels/`, registered as transient in DI (except `SettingsViewModel` which is singleton). Views resolve VMs via `App.GetService<T>()`.
- **Service interfaces**: All services have an interface in `Scanner.Core/Services/Interfaces/` — use these for new service registrations.
- **Settings**: Persisted as JSON to `%LOCALAPPDATA%\ScannerApp\settings.json` via `SettingsViewModel`.
- **OCR dual-path**: `QwenOcrService` (primary, cloud-based via OpenRouter API) and `OcrService` (Tesseract, offline fallback). `IOcrService` is the shared interface.

## Important Constraints

- The App project uses `EnableMsixTooling=true` but is unpackaged (`WindowsPackageType=None`). Do not add package identity or MSIX manifest references.
- OpenCV native binaries come from `OpenCvSharp4.runtime.win`. No manual native lib management needed.
- Tesseract requires trained data files in a `tessdata/` directory at repo root (gitignored).
- The cloud storage services (OneDrive, Google Drive) are stubs — they implement `ICloudStorageService` but have no real functionality yet.
