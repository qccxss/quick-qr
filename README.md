<div align="center">

# Quick QR

**A polished desktop QR code generator for Windows**

![Platform](https://img.shields.io/badge/platform-Windows-lightgrey?style=flat-square)
![Framework](https://img.shields.io/badge/.NET%20Framework-4.8-512BD4?style=flat-square)
![Language](https://img.shields.io/badge/language-C%23-239120?style=flat-square)
![UI](https://img.shields.io/badge/UI-WPF-5C2D91?style=flat-square)
![License](https://img.shields.io/badge/license-MIT-white?style=flat-square)
![Version](https://img.shields.io/badge/version-0.0.2-black?style=flat-square)

</div>

---

## Overview

**Quick QR** is a modern Windows desktop application for creating, previewing, copying, and exporting QR codes. It is built with WPF and .NET Framework 4.8, with a glass-inspired interface, live generation, configurable output settings, local history, and multiple visual themes.

The application is designed to keep the QR creation workflow fast and private. Content is processed locally on the computer and is not uploaded to a remote service.

---

## What's new in 0.0.2

- Expanded theming options with `Twilight`, `Neon`, `Minimal`, and `Glass`.
- Added new QR content types: Bitcoin payment, Event invitation, and Location.
- Added quick workflow actions: Copy payload, Copy HTML snippet, and Share (mailto integration).
- Improved export workflows, status messaging, and several UI polish fixes.

---

## Features

- Live QR preview while typing
- Plain text QR codes
- Website and URL QR codes
- Email address QR codes
- Wi-Fi network QR codes
- Phone number QR codes
- SMS message QR codes
 - vCard contact QR codes
 - Bitcoin payment QR codes (URI support)
 - Event invitation QR codes (VEVENT/ICS snippets)
 - Location QR codes (geo: URI)
 - Error correction levels:
   - Auto
   - Low: 7%
   - Medium: 15%
   - Quartile: 25%
   - High: 30%
- PNG export
- SVG export for scalable output
- Copy generated QR codes to the clipboard
- Configurable QR pixel size
- Optional quiet zone control
- Local QR history
- Restore previously generated QR content
- Clear history support
- Live preview can be enabled or disabled
- Modern glass-inspired WPF interface
- Light, Dark, Sunrise, Galaxy, Forest, and Ocean themes
- Custom Windows application icon
- Borderless window with minimize, maximize, close, and drag support
- Per-monitor DPI awareness
- Standard-user execution through the application manifest
- No external web service required during normal operation
 - PNG export
 - SVG export for scalable output
 - Copy generated QR codes to the clipboard
 - Copy payload (text/URI) and Copy HTML (data URI) actions
 - Share action to open the default mail client with the payload
 - Configurable QR pixel size
 - Optional quiet zone control
 - Local QR history
 - Restore previously generated QR content
 - Clear history support
 - Live preview can be enabled or disabled
 - Modern glass-inspired WPF interface
 - Light, Dark, Sunrise, Galaxy, Forest, Ocean, Twilight, Neon, Minimal, and Glass themes
 - Custom Windows application icon
 - Borderless window with minimize, maximize, close, and drag support
 - Per-monitor DPI awareness
 - Standard-user execution through the application manifest
 - No external web service required during normal operation

---

## Requirements

| Requirement | Detail |
|---|---|
| Operating system | Windows 7, Windows 10, or Windows 11 |
| Runtime | .NET Framework 4.8 |
| Development IDE | Visual Studio 2022 recommended |
| Build system | MSBuild or the .NET SDK |
| Architecture | Any CPU |
| Network access | Not required for normal QR generation |

The target computer must have the .NET Framework 4.8 Runtime installed.

---

## Build

### Visual Studio

1. Open `quick-qr.sln` in Visual Studio 2022.
2. Restore the NuGet package dependencies.
3. Select the `Release` configuration.
4. Build the solution with `Ctrl+Shift+B`.
5. Run the application from the generated Release output folder.

### .NET CLI

Restore dependencies:

```powershell
dotnet restore .\quick-qr.sln
```

Build the Release configuration:

```powershell
dotnet build .\quick-qr.sln --configuration Release
```

Build the Debug configuration:

```powershell
dotnet build .\quick-qr.sln --configuration Debug
```

The application output is generated under the project `bin` folder. Build output is intentionally not part of the source tree and can be regenerated at any time.

---

## Usage

### Basic Workflow

1. Launch `QuickQr.exe`.
2. Select a content type from the content type selector.
3. Enter the content to encode.
4. Review the live QR preview.
5. Select an error correction level if needed.
6. Copy the QR code or save it as PNG or SVG.

### Plain Text

Select **Plain text** and enter any text that should be encoded in the QR code.

### Website or URL

Select **Web link** and enter a complete URL, preferably including the protocol:

```text
https://example.com
```

### Email Address

Select **E-mail address** and enter the destination address. The generated payload uses a `mailto:` URI.

### Wi-Fi Network

Select **Wi-Fi network** and enter the values using this format:

```text
NetworkName|NetworkPassword|WPA
```

The security value can be omitted when the default `WPA` value is acceptable:

```text
NetworkName|NetworkPassword
```

### Phone Number

Select **Phone number** and enter a phone number, including the country code when appropriate:

```text
+1 555 010 1234
```

### SMS Message

Select **SMS message** and separate the number and message with a pipe character:

```text
+1 555 010 1234|Hello from Quick QR
```

### Contact Card

Select **Contact card** and provide the values in this order:

```text
Full Name|Phone Number|Email Address
```

The application converts the values into a vCard payload.

---

## Export Options

### PNG

PNG export is suitable for documents, presentations, websites, messaging apps, and general image workflows. The output uses the configured pixel size and quiet zone settings.

### SVG

SVG export creates a scalable vector representation that can be resized without losing sharpness. It is useful for print layouts, design tools, and high-resolution documents.

### Clipboard

The **Copy** action places the current QR image on the Windows clipboard so it can be pasted into compatible applications.

---

## Settings

The Settings window provides controls for the generation workflow and appearance.

### Appearance

Available themes:

- **Light**: bright neutral glass surfaces
- **Dark**: high-contrast dark interface
- **Sunrise**: warm light palette
- **Galaxy**: dark blue and violet palette
- **Forest**: dark green palette
- **Ocean**: dark cyan and blue palette
 - **Twilight**: deep purple twilight accents
 - **Neon**: dark background with neon accent tones
 - **Minimal**: muted neutral surfaces for distraction-free use
 - **Glass**: subtle glass and aqua accents
 - **Light**: bright neutral glass surfaces
 - **Dark**: high-contrast dark interface
 - **Sunrise**: warm light palette
 - **Galaxy**: dark blue and violet palette
 - **Forest**: dark green palette
 - **Ocean**: dark cyan and blue palette

### QR Output

- Pixel size controls the rendered QR module size.
- Quiet zone adds or removes the outer QR margin.
- Error correction controls how much damage or obstruction the QR code can tolerate.

### Workflow

- Live preview can generate the QR code while typing.
- History storage can be enabled or disabled.
- The maximum number of saved history items can be selected.

---

## Keyboard Shortcuts

| Shortcut | Action |
|---|---|
| `Ctrl+Enter` | Generate the QR code |
| `Ctrl+L` | Focus the content field |
| `Escape` | Clear the content field |

---

## Local Data

Quick QR stores settings and history locally under:

```text
%LOCALAPPDATA%\QuickQr
```

The application does not upload QR content during normal operation. Local history can be disabled from Settings or cleared from the History window.

---

## Project Structure

```text
quick-qr/
├── quick-qr.sln                 Visual Studio solution
├── README.md                    Project documentation
├── LICENSE                      MIT license
└── QuickQr/
    ├── QuickQr.csproj           .NET Framework 4.8 WPF project
    ├── app.manifest             Windows execution and DPI metadata
    ├── App.xaml                 Application resources and shared styles
    ├── App.xaml.cs              Application startup
    ├── MainWindow.xaml          Main generator interface
    ├── MainWindow.xaml.cs       QR generation and main window behavior
    ├── SettingsWindow.xaml      Settings interface
    ├── SettingsWindow.xaml.cs  Settings behavior
    ├── HistoryWindow.xaml       History interface
    ├── HistoryWindow.xaml.cs   History behavior
    ├── UserSettings.cs          Persistent settings and history models
    ├── Assets/
    │   ├── quick-qr.ico         Multi-resolution Windows application icon
    │   └── quick-qr.png         512x512 source icon asset
    └── Properties/
        └── AssemblyInfo.cs      Assembly metadata
```

---

## Dependencies

Quick QR uses the following NuGet package:

| Package | Version | Purpose |
|---|---:|---|
| QRCoder | 1.6.0 | QR payload generation and PNG/SVG rendering |

The package is restored automatically during development builds.

---

## Privacy

Quick QR is designed for local use:

- QR content is processed locally.
- No QR payload is sent to a web API.
- Settings and history remain on the local computer.
- Clipboard access occurs only when the user selects Copy.
- File access occurs only when the user selects an export location.

---

## Troubleshooting

### The project does not restore packages

Run the following commands from the repository root:

```powershell
dotnet restore .\quick-qr.sln
dotnet build .\quick-qr.sln --configuration Release
```

If Visual Studio shows an old NuGet diagnostic after restore succeeds, reload the solution or use **Developer: Reload Window** in VS Code.

### The application does not start

Verify that the .NET Framework 4.8 Runtime is installed on the target computer. Then rebuild the Release configuration and launch the generated executable again.

### The icon does not update in Windows Explorer

Windows may cache application icons. Rebuild the application and refresh Explorer. Creating a new shortcut can also force Windows to display the current icon.

### QR output is difficult to scan

Try the following:

- Use a higher error correction level.
- Keep the quiet zone enabled.
- Increase the pixel size.
- Avoid placing the QR code on a busy background.
- Make sure the exported image is not excessively compressed or blurred.

---

<div align="center">
  <sub>Quick QR v0.0.2</sub>
</div>
