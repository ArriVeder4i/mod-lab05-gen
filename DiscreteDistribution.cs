using System;
using System.Collections.Generic;
using System.Linq;

namespace TextGenerator
{
    public class DiscreteDistribution<T>
    {
        private readonly List<(T Value, double CumulativeWeight)> _cumulativeWeights;
        private readonly Random _random;

        public DiscreteDistribution(IEnumerable<(T Item, int Weight)> items, Random random = null)
        {
            _random = random ?? new Random();
            _cumulativeWeights = new List<(T, double)>();

            var filtered = items.Where(p => p.Weight > 0).ToList();
            if (!filtered.Any())
                throw new InvalidOperationException("DiscreteDistribution: нет элементов с положительным весом.");

            double totalWeight = filtered.Sum(p => p.Weight);
            double cumulative = 0;
            foreach (var (item, weight) in filtered)
            {
                cumulative += weight;
                _cumulativeWeights.Add((item, cumulative / totalWeight));
            }
        }

        public T Next()
        {
            double value = _random.NextDouble();

            foreach (var (val, cumWeight) in _cumulativeWeights)
            {
                if (cumWeight >= value)
                    return val;
            }

            return _cumulativeWeights[^1].Value;
        }
    }
}
