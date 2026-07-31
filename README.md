# Quick QR

A modern, lightweight WPF QR code generator built for .NET Framework 4.8.

## Features

- QR generation for text, URLs, email addresses, and Wi-Fi networks
- Phone, SMS, and vCard content types
- Live preview
- Error correction level selection
- PNG and scalable SVG export
- Clipboard copy
- Light, Dark, Sunrise, Galaxy, Forest, and Ocean themes
- Pixel size, quiet zone, and live preview settings
- Local QR history with restore support
- `Ctrl+Enter` to generate, `Ctrl+L` to focus the content field, `Escape` to clear
- Multi-resolution QR-based Windows application icon
- Windows manifest with standard-user execution, DPI awareness, and OS compatibility

Settings and history are stored locally under `%LOCALAPPDATA%\QuickQr`. QR content is never uploaded.

## Development

1. Open `quick-qr.sln` with Visual Studio 2022.
2. Restore NuGet packages.
3. Build the `Release` configuration.
4. Copy the generated Release output folder to the target machine.

The target machine must have the .NET Framework 4.8 Runtime installed.
