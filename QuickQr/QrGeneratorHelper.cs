using QRCoder;
using System;
using System.IO;
using System.Linq;

namespace QuickQr
{
    public static class QrGeneratorHelper
    {
        public static string BuildPayload(string value, string type)
        {
            if (type == "email") return "mailto:" + value;
            if (type == "phone") return "tel:" + value;
            if (type == "sms")
            {
                var smsParts = value.Split(new[] { '|' }, 2);
                return smsParts.Length == 2 ? "SMSTO:" + smsParts[0] + ":" + smsParts[1] : "SMSTO:" + value;
            }
            if (type == "bitcoin")
            {
                var bitcoinParts = value.Split(new[] { '|' }, 3);
                var address = bitcoinParts.Length > 0 ? bitcoinParts[0] : value;
                var amount = bitcoinParts.Length > 1 ? bitcoinParts[1] : string.Empty;
                var label = bitcoinParts.Length > 2 ? bitcoinParts[2] : string.Empty;
                var uri = "bitcoin:" + address;
                var query = string.Empty;
                if (!string.IsNullOrWhiteSpace(amount)) query += "amount=" + Uri.EscapeDataString(amount);
                if (!string.IsNullOrWhiteSpace(label)) query += (query.Length > 0 ? "&" : string.Empty) + "label=" + Uri.EscapeDataString(label);
                return query.Length > 0 ? uri + "?" + query : uri;
            }
            if (type == "event")
            {
                var eventParts = value.Split(new[] { '|' }, 4);
                var title = eventParts.Length > 0 ? eventParts[0] : "Event";
                var start = eventParts.Length > 1 ? eventParts[1] : string.Empty;
                var location = eventParts.Length > 2 ? eventParts[2] : string.Empty;
                var description = eventParts.Length > 3 ? eventParts[3] : string.Empty;
                return "BEGIN:VEVENT\nSUMMARY:" + Uri.EscapeDataString(title) + "\nDTSTART:" + start + "\nLOCATION:" + Uri.EscapeDataString(location) + "\nDESCRIPTION:" + Uri.EscapeDataString(description) + "\nEND:VEVENT";
            }
            if (type == "location")
            {
                var locationParts = value.Split(new[] { '|' }, 3);
                var latitude = locationParts.Length > 0 ? locationParts[0] : string.Empty;
                var longitude = locationParts.Length > 1 ? locationParts[1] : string.Empty;
                var label = locationParts.Length > 2 ? locationParts[2] : string.Empty;
                var uri = "geo:" + latitude + "," + longitude;
                return string.IsNullOrWhiteSpace(label) ? uri : uri + "?q=" + Uri.EscapeDataString(label);
            }
            if (type == "vcard")
            {
                var cardParts = value.Split('|');
                var name = cardParts.Length > 0 ? cardParts[0] : value;
                var phone = cardParts.Length > 1 ? cardParts[1] : string.Empty;
                var email = cardParts.Length > 2 ? cardParts[2] : string.Empty;
                return "BEGIN:VCARD\nVERSION:3.0\nFN:" + name + "\nTEL:" + phone + "\nEMAIL:" + email + "\nEND:VCARD";
            }
            if (type == "wifi")
            {
                var parts = value.Split('|');
                if (parts.Length >= 2)
                {
                    var security = parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2] : "WPA";
                    return "WIFI:T:" + security + ";S:" + EscapeWifi(parts[0]) + ";P:" + EscapeWifi(parts[1]) + ";;";
                }
            }
            return value;
        }

        public static byte[] CreatePngBytes(string value, string type, string correctionTag, int pixelSize, string foregroundHex, string backgroundHex, bool includeQuietZones)
        {
            var payload = BuildPayload(value, type);
            var correction = GetCorrection(correctionTag);
            using (var generator = new QRCodeGenerator())
            using (var data = generator.CreateQrCode(payload, correction))
            {
                var qr = new PngByteQRCode(data);
                return qr.GetGraphic(pixelSize, HexToRgb(foregroundHex), HexToRgb(backgroundHex), includeQuietZones);
            }
        }

        public static string ToSafeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "qr-item";
            var safe = new string(value.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '-' : ch).ToArray()).Trim();
            safe = safe.Replace(" ", "-").Replace("_", "-");
            safe = string.Join("-", safe.Split(new[] { '-', ' ' }, StringSplitOptions.RemoveEmptyEntries));
            if (string.IsNullOrWhiteSpace(safe)) safe = "qr-item";
            if (safe.Length > 60) safe = safe.Substring(0, 60).TrimEnd('-', ' ');
            return safe;
        }

        public static QRCodeGenerator.ECCLevel GetCorrection(string tag)
        {
            if (tag == "L") return QRCodeGenerator.ECCLevel.L;
            if (tag == "Q") return QRCodeGenerator.ECCLevel.Q;
            if (tag == "H") return QRCodeGenerator.ECCLevel.H;
            return QRCodeGenerator.ECCLevel.M;
        }

        private static string EscapeWifi(string value)
        {
            return value.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace(":", "\\:");
        }

        private static byte[] HexToRgb(string hex)
        {
            try
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
                return new[] { color.R, color.G, color.B };
            }
            catch
            {
                return new byte[] { 23, 33, 43 };
            }
        }
    }
}
