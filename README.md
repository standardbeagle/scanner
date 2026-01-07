# Scanner

A Windows desktop scanner application built with WinUI 3 and .NET 8.

## Features

- **Scanner Integration** -- WIA (Windows Image Acquisition) with support for Flatbed, ADF, Duplex, and Film sources
- **Image Processing** -- Auto-crop, perspective correction, auto-enhance via OpenCvSharp
- **OCR** -- Qwen2.5-VL via OpenRouter API (primary), Tesseract (offline fallback), multi-language support
- **Export** -- Searchable PDF (with OCR text layer), PNG, JPEG, TIFF
- **Cloud Storage** -- OneDrive and Google Drive integration (stubs)

## Architecture

```
Scanner.sln
  src/
    Scanner.App            WinUI 3 UI layer (views, converters, DI setup)
    Scanner.Core           Business logic (ViewModels, services, models)
    Scanner.Infrastructure WIA scanner hardware integration
  tests/
    Scanner.Core.Tests     Unit tests
```

- MVVM pattern with [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- Dependency injection via `Microsoft.Extensions.DependencyInjection`
- Clean separation: Core has no UI dependencies, Infrastructure handles hardware

## Prerequisites

- Windows 10 (1809+) or Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Windows App SDK 1.6+](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/)
- Visual Studio 2022 with the **Windows application development** workload (recommended)

## Build & Run

```bash
dotnet restore
dotnet build -c Debug -p:Platform=x64
dotnet run --project src/Scanner.App -p:Platform=x64
```

Or open `Scanner.sln` in Visual Studio and press F5.

## OCR Setup

**Qwen2.5-VL (recommended):** Set your OpenRouter API key in the app settings.

**Tesseract (offline fallback):** Download trained data files into a `tessdata/` directory at the repo root. See [tesseract-ocr/tessdata](https://github.com/tesseract-ocr/tessdata) for language packs.

## License

Private -- All rights reserved.
