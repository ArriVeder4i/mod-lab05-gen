using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TextGenerator
{
    public class WordFrequencyTextGenerator
    {
        private DiscreteDistribution<string> _wordDistribution;
        private Random _random;

        public WordFrequencyTextGenerator(string filePath, Random random = null)
        {
            _random = random ?? new Random();
            LoadWords(filePath);
        }

        private void LoadWords(string filePath)
        {
            var items = new List<(string Item, int Weight)>();

            foreach (var line in File.ReadLines(filePath))
            {
                var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5)
                    continue;

                string word = parts[1];

                double sumFreq = 0;
                for (int i = 2; i < parts.Length; i++)
                {
                    if (double.TryParse(parts[i].Replace(',', '.'),
                                        System.Globalization.NumberStyles.Any,
                                        System.Globalization.CultureInfo.InvariantCulture,
                                        out double val))
                    {
                        sumFreq += val;
                    }
                }

                int weight = (int)Math.Round(sumFreq);
                if (weight > 0)
                {
                    items.Add((word, weight));
                }
            }

            _wordDistribution = new DiscreteDistribution<string>(items, _random);
        }

        public string Generate(int count)
        {
            if (count <= 0) return string.Empty;
            var sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                var nextWord = _wordDistribution.Next();
                sb.Append(nextWord);
                if (i < count - 1)
                    sb.Append(' ');
            }
            return sb.ToString();
        }
    }
}
