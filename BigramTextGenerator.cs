using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TextGenerator
{
    public class BigramTextGenerator
    {
        private Dictionary<char, DiscreteDistribution<char>> _distributions;
        private DiscreteDistribution<char> _initialDistribution;
        private Random _random;

        public BigramTextGenerator(string filePath, Random random = null)
        {
            _random = random ?? new Random();
            LoadBigrams(filePath);
        }

        private void LoadBigrams(string filePath)
        {
            var rawData = new Dictionary<char, List<(char Next, int Weight)>>();
            var initialCounts = new Dictionary<char, int>();

            foreach (var line in File.ReadLines(filePath))
            {
                var parts = line.Split('\t');
                if (parts.Length < 2) continue;

                string bigram = parts[0];
                if (bigram.Length != 2) continue;

                char first = bigram[0];
                char second = bigram[1];
                if (!int.TryParse(parts[1], out int freq)) continue;

                if (!initialCounts.ContainsKey(first))
                    initialCounts[first] = 0;
                initialCounts[first] += freq;

                if (!rawData.ContainsKey(first))
                    rawData[first] = new List<(char, int)>();
                rawData[first].Add((second, freq));
            }

            var initItems = initialCounts
                .Select(kvp => (Item: kvp.Key, Weight: kvp.Value))
                .ToList();
            _initialDistribution = new DiscreteDistribution<char>(initItems, _random);

            _distributions = new Dictionary<char, DiscreteDistribution<char>>();
            foreach (var kvp in rawData)
            {
                char symbol = kvp.Key;
                var list = kvp.Value;
                var items = list.Select(p => (Item: p.Next, Weight: p.Weight)).ToList();
                _distributions[symbol] = new DiscreteDistribution<char>(items, _random);
            }
        }

        public string Generate(int length)
        {
            if (length <= 0) return string.Empty;

            char current = _initialDistribution.Next();
            var sb = new StringBuilder();
            sb.Append(current);

            for (int i = 1; i < length; i++)
            {
                if (_distributions.ContainsKey(current))
                {
                    current = _distributions[current].Next();
                }
                else
                {
                    current = _initialDistribution.Next();
                }
                sb.Append(current);
            }

            return sb.ToString();
        }
    }
}
