using System;

namespace Xiuxian.Scripts.Ui
{
    public static class ToastFoldRules
    {
        public const double FoldWindowSeconds = 3.0;

        public static string NormalizeFoldKey(string foldKey)
        {
            return foldKey?.Trim() ?? string.Empty;
        }

        public static bool CanFold(string existingFoldKey, string incomingFoldKey, double existingLastSeenAt, double now)
        {
            string normalizedExisting = NormalizeFoldKey(existingFoldKey);
            string normalizedIncoming = NormalizeFoldKey(incomingFoldKey);
            if (string.IsNullOrEmpty(normalizedExisting) || string.IsNullOrEmpty(normalizedIncoming))
            {
                return false;
            }

            if (!string.Equals(normalizedExisting, normalizedIncoming, StringComparison.Ordinal))
            {
                return false;
            }

            return now - existingLastSeenAt <= FoldWindowSeconds;
        }

        public static int GetNextCount(int currentCount)
        {
            return Math.Max(1, currentCount) + 1;
        }

        public static string BuildDisplayMessage(string baseMessage, int count)
        {
            if (count <= 1)
            {
                return baseMessage;
            }

            return $"{baseMessage} ×{count}";
        }
    }
}
