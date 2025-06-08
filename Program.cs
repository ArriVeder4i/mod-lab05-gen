using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ScottPlot;
using System.Drawing;

namespace TextGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            // Основная директория (туда же, куда скопируются все *.txt и получится папка Results)
            string baseDir = AppContext.BaseDirectory;
            // Пути до входных файлов
            string bigramsFile = Path.Combine(baseDir, "bigrams_frequency.txt");
            string wordsFile = Path.Combine(baseDir, "words_frequency.txt");

            // Проверяем наличие входных файлов
            if (!File.Exists(bigramsFile) || !File.Exists(wordsFile))
            {
                Console.WriteLine("Не найдены исходные файлы частот: bigrams_frequency.txt или words_frequency.txt.");
                return;
            }

            // Создаём папку Results (если ещё нет)
            string resultsDir = Path.Combine(baseDir, "Results");
            Directory.CreateDirectory(resultsDir);

            // Случайный генератор
            var random = new Random();

            // ─────────────── Генерация 1: «биграммный» текст ───────────────

            // 1) Инициализируем генератор на основе биграмм
            var bigramGenerator = new BigramTextGenerator(bigramsFile, random);

            // 2) Генерируем 1000 символов (каждый символ выбирается по предыдущему через биграммную модель)
            string generatedBigramText = bigramGenerator.Generate(1000);

            // 3) Сохраняем результат в Results/gen-1.txt
            string gen1TxtPath = Path.Combine(resultsDir, "gen-1.txt");
            File.WriteAllText(gen1TxtPath, generatedBigramText, System.Text.Encoding.UTF8);

            // 4) Строим распределение «ожидаемое vs реальное» для первых 20 самых частотных символов (по входному файлу биграмм)

            //    4.1) Считаем исходную частоту первых символов (initialCounts) из bigrams_frequency.txt
            //         Для этого повторим логику из конструктора BigramTextGenerator, но вынесем только initialCounts.
            var initialCounts = new Dictionary<char, int>();
            foreach (var line in File.ReadLines(bigramsFile))
            {
                var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;
                string bigram = parts[0];
                if (bigram.Length != 2) continue;
                char first = bigram[0];
                if (!int.TryParse(parts[1], out int freq)) continue;

                if (!initialCounts.ContainsKey(first))
                    initialCounts[first] = 0;
                initialCounts[first] += freq;
            }

            if (initialCounts.Count == 0)
            {
                Console.WriteLine("Не удалось прочитать биграммы для построения распределения gen-1.");
            }
            else
            {
                //    4.2) Берём топ-20 символов по исходному initialCounts
                var top20Chars = initialCounts
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(20)
                    .Select(kvp => kvp.Key)
                    .ToArray();

                //    4.3) Формируем ожидаемое распределение (нормируем initialCounts для топ-20)
                double sumInitialAll = initialCounts.Values.Sum();
                double[] expectedCharFreq = top20Chars
                    .Select(ch => initialCounts[ch] / sumInitialAll)
                    .ToArray();

                //    4.4) Считаем реальные частоты этих символов в только что сгенерированном тексте
                var actualCharCounts = new Dictionary<char, int>();
                foreach (char c in generatedBigramText)
                {
                    if (!actualCharCounts.ContainsKey(c))
                        actualCharCounts[c] = 0;
                    actualCharCounts[c]++;
                }

                double totalChars = generatedBigramText.Length;
                double[] actualCharFreq = top20Chars
                    .Select(ch => actualCharCounts.ContainsKey(ch) ? actualCharCounts[ch] / totalChars : 0.0)
                    .ToArray();

                //    4.5) Строим график через ScottPlot
                int nChars = top20Chars.Length;
                double[] positionsChars = Enumerable.Range(0, nChars).Select(i => (double)i).ToArray();
                var plt1 = new Plot(1000, 600);

                // Столбики «Ожидаемые»
                var barExpectedChars = plt1.AddBar(expectedCharFreq, positionsChars);
                barExpectedChars.BarWidth = 0.3;
                barExpectedChars.FillColor = Color.Blue;
                barExpectedChars.Label = "Expected";

                // Столбики «Реальные» (смещаем X на 0.35)
                double[] shiftedPosChars = positionsChars.Select(x => x + 0.35).ToArray();
                var barActualChars = plt1.AddBar(actualCharFreq, shiftedPosChars);
                barActualChars.BarWidth = 0.3;
                barActualChars.FillColor = Color.Red;
                barActualChars.Label = "Actual";

                plt1.XTicks(shiftedPosChars, top20Chars.Select(c => c.ToString()).ToArray());
                plt1.Title("Gen-1 Character Distribution");
                plt1.Legend();

                string gen1PngPath = Path.Combine(resultsDir, "gen-1.png");
                plt1.SaveFig(gen1PngPath);
            }

            // ─────────────── Генерация 2: «словесный» текст ───────────────

            // 1) Инициализируем генератор по частоте слов
            var wordGenerator = new WordFrequencyTextGenerator(wordsFile, random);

            // 2) Генерируем 1000 слов
            string generatedWordText = wordGenerator.Generate(1000);

            // 3) Сохраняем результат в Results/gen-2.txt
            string gen2TxtPath = Path.Combine(resultsDir, "gen-2.txt");
            File.WriteAllText(gen2TxtPath, generatedWordText, System.Text.Encoding.UTF8);

            // 4) Строим распределение «ожидаемое vs реальное» для топ-20 самых частотных слов (по входному words_frequency.txt)

            //    4.1) Читаем файл words_frequency.txt и собираем словарь: слово → суммарная частота
            var sourceWordFreq = new Dictionary<string, int>();

            foreach (var line in File.ReadLines(wordsFile))
            {
                var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                // Ожидается хотя бы 5 колонок: индекс, слово, лемма, POS, freq1, freq2, ...
                if (parts.Length < 5) continue;

                string word = parts[1];
                double sumFreq = 0;
                for (int i = 4; i < parts.Length; i++)
                {
                    if (double.TryParse(
                        parts[i].Replace(',', '.'),
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
                    if (!sourceWordFreq.ContainsKey(word))
                        sourceWordFreq[word] = 0;
                    sourceWordFreq[word] += weight;
                }
            }

            if (sourceWordFreq.Count == 0)
            {
                Console.WriteLine("Не удалось прочитать слова для построения распределения gen-2.");
            }
            else
            {
                //    4.2) Берём топ-20 самых частотных слов из sourceWordFreq
                var top20Words = sourceWordFreq
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(20)
                    .Select(kvp => kvp.Key)
                    .ToArray();

                //    4.3) Формируем ожидаемое распределение (нормируем суммы частот для топ-20)
                double sumAllWordFreq = sourceWordFreq.Values.Sum();
                double[] expectedWordFreq = top20Words
                    .Select(w => sourceWordFreq[w] / sumAllWordFreq)
                    .ToArray();

                //    4.4) Считаем реальные частоты этих слов в только что сгенерированном тексте
                var actualWordCounts = new Dictionary<string, int>();
                // Разбиваем сгенерированный текст на слова по пробелу
                var wordsList = generatedWordText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (string w in wordsList)
                {
                    if (!actualWordCounts.ContainsKey(w))
                        actualWordCounts[w] = 0;
                    actualWordCounts[w]++;
                }
                double totalWordsCount = wordsList.Length;
                double[] actualWordFreq = top20Words
                    .Select(w => actualWordCounts.ContainsKey(w) ? actualWordCounts[w] / totalWordsCount : 0.0)
                    .ToArray();

                //    4.5) Строим график через ScottPlot
                int nWords = top20Words.Length;
                double[] positionsWords = Enumerable.Range(0, nWords).Select(i => (double)i).ToArray();
                var plt2 = new Plot(1000, 600);

                var barExpectedWords = plt2.AddBar(expectedWordFreq, positionsWords);
                barExpectedWords.BarWidth = 0.3;
                barExpectedWords.FillColor = Color.Blue;
                barExpectedWords.Label = "Expected";

                double[] shiftedPosWords = positionsWords.Select(x => x + 0.35).ToArray();
                var barActualWords = plt2.AddBar(actualWordFreq, shiftedPosWords);
                barActualWords.BarWidth = 0.3;
                barActualWords.FillColor = Color.Red;
                barActualWords.Label = "Actual";

                plt2.XTicks(shiftedPosWords, top20Words);
                plt2.Title("Gen-2 Word Distribution");
                plt2.Legend();

                string gen2PngPath = Path.Combine(resultsDir, "gen-2.png");
                plt2.SaveFig(gen2PngPath);
            }

            Console.WriteLine("Генерация завершена. Смотрите папку Results для txt и png.");
        }
    }
}
