using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SM5SimCoupler
{
    class Program
    {
        static void Main(string[] args)
        {
            var folder = Path.GetDirectoryName(args[0]);
            var fpattern = (Directory.Exists(folder)) ? "*.s*" : Path.GetFileName(args[0]);
            foreach (var file in Directory.GetFiles(folder, fpattern, SearchOption.AllDirectories))
            {
                if (Path.GetExtension(file).Equals(".sm", StringComparison.InvariantCultureIgnoreCase) ||
                    Path.GetExtension(file).Equals(".ssc", StringComparison.InvariantCultureIgnoreCase))
                {
                    PatchFile(file);
                }
            }
            Console.ReadLine();
        }

        private static void PatchFile(string fileOrig)
        {
            Console.WriteLine("".PadRight(75, '='));
            Console.WriteLine($"=== Parsing {Path.GetFileName(fileOrig)}");
            //Console.WriteLine("".PadRight(75, '='));

            bool isSsc = fileOrig.EndsWith(".ssc");
            var parts = new Dictionary<int, List<string>>();
            var index = new Dictionary<int, string>();
            int idx = 0;
            parts.Add(idx, new List<string>());
            index.Add(idx, string.Empty);
            foreach (var line in File.ReadAllLines(fileOrig))
            {
                if (line.Trim().StartsWith(@"//")) continue;
                if (line.Trim().StartsWith("#NOTES:") ||
                    line.Trim().StartsWith("#NOTEDATA:"))
                {
                    parts.Add(++idx, new List<string>());
                    index.Add(idx, string.Empty);
                }
                parts[idx].Add(line);
            }

            foreach (var key in parts.Keys)
            {
                if (parts[key][0].Trim().StartsWith("#NOTEDATA:"))
                {
                    isSsc = true;
                    string chart =
                        parts[key].Where(l => l.Trim().StartsWith("#STEPSTYPE:")).Select(l => l.Split(new char[] { ':', ';' })[1]).FirstOrDefault() +
                        "||" +
                        parts[key].Where(l => l.Trim().StartsWith("#DIFFICULTY:")).Select(l => l.Split(new char[] { ':', ';' })[1]).FirstOrDefault();
                    index[key] = chart;
                }
                if (!isSsc && parts[key][0].Trim().StartsWith("#NOTES:"))
                {
                    string chart = $"{parts[key][1].Trim().Replace(":", "")}||{parts[key][3].Trim().Replace(":", "")}";
                    index[key] = chart;
                }
            }

            bool coupleAdded = false;
            var keys = new List<int>(parts.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                if (index[key].StartsWith("dance-single"))
                {
                    var cplStr = index[key].Replace("single", "couple");
                    if (index.Values.Where(c => c.Equals(cplStr, StringComparison.InvariantCultureIgnoreCase)).Count() == 0)
                    {
                        Console.WriteLine($"Found {index[key]}, adding notes for {cplStr}...");
                        parts.Add(++idx, new List<string>());
                        index.Add(idx, cplStr);
                        keys.Add(idx);
                        coupleAdded = true;
                        foreach (var line in parts[key])
                        {
                            if (line.Contains("dance-single"))
                            {
                                parts[idx].Add(line.Replace("dance-single", "dance-couple"));
                            }
                            else if (line.Trim().Length == 4)
                            {
                                parts[idx].Add($"{line}{line}");
                            }
                            else
                            {
                                parts[idx].Add(line);
                            }
                        }
                    }
                }
            }

            if (coupleAdded)
            {
                Console.WriteLine("Creating backup...");
                File.Copy(
                    fileOrig,
                    $"{fileOrig}.BAK_{DateTime.Now.ToString("yyMMdd_HHmmss")}",
                    true);

                Console.WriteLine($"Saving {Path.GetFileName(fileOrig)}...");
                File.WriteAllLines(
                    fileOrig,
                    parts.Values.SelectMany(p => p).ToList()
                    );
            }
            Console.WriteLine("=== DONE!");
            Console.WriteLine("".PadRight(75, '='));
            Console.WriteLine();
        }
    }
}
