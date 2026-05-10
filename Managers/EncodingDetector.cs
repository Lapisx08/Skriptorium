using System;
using System.IO;
using System.Text;
using UtfUnknown;

namespace Skriptorium.Managers
{
    public static class EncodingDetector
    {
        private static readonly Encoding Cp1252 = Encoding.GetEncoding(1252);
        private static readonly Encoding Cp1251 = Encoding.GetEncoding(1251);
        private static readonly Encoding Cp1250 = Encoding.GetEncoding(1250);

        private const double ConfidenceThreshold = 0.90;
        private const double ByteRatioThreshold = 0.015;

        public static Encoding Detect(string filePath)
        {
            Encoding? utfUnknownResult = TryDetectWithUtfUnknown(filePath);

            byte[] rawBytes = File.ReadAllBytes(filePath);

            if (utfUnknownResult?.CodePage == 1251 && HasCyrillicSignature(rawBytes))
                return Cp1251;

            if (utfUnknownResult?.CodePage == 1250 && HasPolishSignature(rawBytes))
                return Cp1250;

            return Cp1252;
        }

        private static Encoding? TryDetectWithUtfUnknown(string filePath)
        {
            try
            {
                var result = CharsetDetector.DetectFromFile(filePath);
                if (result.Detected == null || result.Detected.Confidence < ConfidenceThreshold)
                    return null;

                int codePage = result.Detected.Encoding.CodePage;
                return codePage is 1250 or 1251 or 1252
                    ? result.Detected.Encoding
                    : null;
            }
            catch { return null; }
        }

        private static bool HasCyrillicSignature(byte[] bytes)
        {
            if (bytes.Length == 0) return false;

            int cyrillicCount = 0;
            foreach (byte b in bytes)
            {
                if (b >= 0xC0)
                    cyrillicCount++;
            }

            return (double)cyrillicCount / bytes.Length >= ByteRatioThreshold;
        }

        private static bool HasPolishSignature(byte[] bytes)
        {
            if (bytes.Length == 0) return false;

            ReadOnlySpan<byte> polishMarkers = stackalloc byte[]
            {
                0x82, // ł (CP1250) vs. ‚ (CP1252)
                0x8C, // Ś (CP1250) vs. Œ (CP1252)
                0x8F, // Ź (CP1250) vs. undefined (CP1252)
                0x9C, // ś (CP1250) vs. œ (CP1252)
                0x9F, // ź (CP1250) vs. Ÿ (CP1252)
                0xA5, // Ą (CP1250) vs. ¥ (CP1252)
                0xB9, // ą (CP1250) vs. ¹ (CP1252)
                0xBC, // Ł (CP1250) vs. ¼ (CP1252)
                0xBE, // ż (CP1250) vs. ¾ (CP1252)
                0xBF, // Ż (CP1250) vs. ¿ (CP1252)
                0xCA, // ę (CP1250) vs. Ê (CP1252)
                0xEA, // ę klein (CP1250) vs. ê (CP1252)
                0xF1, // ń (CP1250) vs. ñ (CP1252)
            };

            int polishCount = 0;
            foreach (byte b in bytes)
            {
                foreach (byte marker in polishMarkers)
                {
                    if (b == marker)
                    {
                        polishCount++;
                        break;
                    }
                }
            }

            return (double)polishCount / bytes.Length >= ByteRatioThreshold;
        }
    }
}