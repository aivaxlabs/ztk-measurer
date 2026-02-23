using Microsoft.ML.Tokenizers;
using System.Threading;

namespace CountTokens.Internal
{
    internal static class SharedTokenizer
    {
        private static readonly Lazy<TiktokenTokenizer> Tokenizer = new(
            () => TiktokenTokenizer.CreateForEncoding("o200k_base"),
            LazyThreadSafetyMode.ExecutionAndPublication
        );

        public static int CountTokens(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            if (TryEstimateHighlyRepetitiveText(text, out int repetitiveEstimate))
                return repetitiveEstimate;

            return Tokenizer.Value.CountTokens(text);
        }

        public static int CountTokensWithFactor(string? text, double factor)
        {
            int baseCount = CountTokens(text);
            if (baseCount <= 0)
                return 0;

            if (factor <= 0)
                return baseCount;

            int adjusted = (int)Math.Ceiling(baseCount * factor);
            return Math.Max(1, adjusted);
        }

        private static bool TryEstimateHighlyRepetitiveText(string text, out int tokens)
        {
            tokens = 0;

            if (text.Length < 1024)
                return false;

            int maxRun = GetMaxCharRunLength(text);
            if (maxRun < 512)
                return false;

            int nonRunChars = text.Length - maxRun;
            int estimated = (int)Math.Ceiling(nonRunChars / 4.5d) + (int)Math.Ceiling(maxRun / 16d);
            tokens = Math.Max(1, estimated);
            return true;
        }

        private static int GetMaxCharRunLength(string text)
        {
            int max = 1;
            int current = 1;

            for (int i = 1; i < text.Length; i++)
            {
                if (text[i] == text[i - 1])
                {
                    current++;
                    if (current > max)
                        max = current;
                }
                else
                {
                    current = 1;
                }
            }

            return max;
        }
    }
}
