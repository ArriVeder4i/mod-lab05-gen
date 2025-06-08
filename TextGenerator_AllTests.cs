using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using TextGenerator;

namespace TextGenerator.Tests
{
    // Tests for DiscreteDistribution<T>
    public class DiscreteDistributionTests
    {
        [Fact(DisplayName = "Constructor throws if all weights ≤ 0")]
        public void Constructor_EmptyOrZeroWeights_ThrowsException()
        {
            var items = new List<(string Item, int Weight)>
            {
                ("A", 0),
                ("B", 0),
                ("C", 0)
            };

            var ex = Assert.Throws<InvalidOperationException>(() =>
                new DiscreteDistribution<string>(items)
            );
            Assert.Contains("нет элементов с положительным весом", ex.Message);
        }

        [Fact(DisplayName = "Single item always returned")]
        public void Next_SingleItem_ReturnsThatItem()
        {
            var items = new List<(int Item, int Weight)>
            {
                (42, 100)
            };
            var dist = new DiscreteDistribution<int>(items, new Random(12345));
            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(42, dist.Next());
            }
        }

        [Fact(DisplayName = "Items with zero weight are excluded")]
        public void Next_ZeroWeightExcluded()
        {
            var items = new List<(string Item, int Weight)>
            {
                ("X", 50),
                ("Y", 0)
            };
            var dist = new DiscreteDistribution<string>(items, new Random(0));
            for (int i = 0; i < 20; i++)
            {
                string result = dist.Next();
                Assert.Equal("X", result);
            }
        }
    }

    // Tests for BigramTextGenerator
    public class BigramTextGeneratorTests
    {
        private string CreateTempBigramFile(string[] lines)
        {
            string tempPath = Path.GetTempFileName();
            File.WriteAllLines(tempPath, lines);
            return tempPath;
        }

        [Fact(DisplayName = "Generate returns string of specified length")]
        public void Generate_CorrectLength()
        {
            string[] content = { "AA\t100" };
            string file = CreateTempBigramFile(content);

            var random = new Random(0);
            var generator = new BigramTextGenerator(file, random);
            string result = generator.Generate(50);

            Assert.Equal(50, result.Length);
            File.Delete(file);
        }

        [Fact(DisplayName = "All output characters appear in initial distribution")]
        public void Generate_OutputCharsValid()
        {
            string[] content =
            {
                "AB\t10",
                "BC\t20",
                "CA\t30"
            };
            string file = CreateTempBigramFile(content);

            var random = new Random(123);
            var generator = new BigramTextGenerator(file, random);
            string result = generator.Generate(200);

            var allowed = new HashSet<char> { 'A', 'B', 'C' };
            foreach (char c in result)
            {
                Assert.Contains(c, allowed);
            }
            File.Delete(file);
        }

        [Fact(DisplayName = "Single bigram leads to repeated pattern")]
        public void Generate_SingleBigram_RepeatedPattern()
        {
            string[] content = { "XY\t100" };
            string file = CreateTempBigramFile(content);

            var random = new Random(0);
            var generator = new BigramTextGenerator(file, random);
            string result = generator.Generate(10);

            string expected = "XYXYXYXYXY";
            Assert.Equal(expected, result);
            File.Delete(file);
        }
    }

    // Tests for WordFrequencyTextGenerator
    public class WordFrequencyTextGeneratorTests
    {
        private string CreateTempWordFile(string[] lines)
        {
            string tempPath = Path.GetTempFileName();
            File.WriteAllLines(tempPath, lines);
            return tempPath;
        }

        [Fact(DisplayName = "Generate returns correct word count")]
        public void Generate_CorrectCount()
        {
            string[] content =
            {
                "1\tapple\tapple\tnoun\t100\t50",
                "2\tbanana\tbanana\tnoun\t200\t100"
            };
            string file = CreateTempWordFile(content);

            var random = new Random(0);
            var generator = new WordFrequencyTextGenerator(file, random);
            string result = generator.Generate(30);

            var words = result.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(30, words.Length);
            File.Delete(file);
        }

        [Fact(DisplayName = "All generated words exist in source")]
        public void Generate_ValidWordsOnly()
        {
            string[] content =
            {
                "1\tcat\tcat\tnoun\t10\t0",
                "2\tdog\tdog\tnoun\t0\t20",
                "3\tmouse\tmouse\tnoun\t0\t0"
            };
            string file = CreateTempWordFile(content);

            var random = new Random(1);
            var generator = new WordFrequencyTextGenerator(file, random);
            string result = generator.Generate(50);

            var words = result.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var allowed = new[] { "cat", "dog" }.ToHashSet();
            foreach (var w in words)
            {
                Assert.Contains(w, allowed);
            }
            File.Delete(file);
        }

        [Fact(DisplayName = "Single-word file always returns that word")]
        public void Generate_SingleWord_Repeated()
        {
            string[] content =
            {
                "1\thello\thello\tother\t500"
            };
            string file = CreateTempWordFile(content);

            var random = new Random(5);
            var generator = new WordFrequencyTextGenerator(file, random);
            string result = generator.Generate(10);

            var words = result.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Assert.All(words, w => Assert.Equal("hello", w));
            File.Delete(file);
        }

        [Fact(DisplayName = "Sum of frequencies yields correct distribution weights")]
        public void LoadWords_FrequencySum_SetsCorrectWeights()
        {
            string[] content =
            {
                "1\ttest\ttest\tv\t10.2\t5.8",
                "2\texam\texam\tn\t0.4\t0.4",
                "3\tzero\tzero\tx\t0\t0"
            };
            string file = CreateTempWordFile(content);

            var generator = new WordFrequencyTextGenerator(file);

            var random = new Random(0);
            generator = new WordFrequencyTextGenerator(file, random);

            var sample = generator.Generate(20).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int countTest = sample.Count(w => w == "test");
            int countExam = sample.Count(w => w == "exam");

            Assert.True(countTest > countExam, "Expected 'test' more frequent than 'exam'");
            File.Delete(file);
        }
    }
}
