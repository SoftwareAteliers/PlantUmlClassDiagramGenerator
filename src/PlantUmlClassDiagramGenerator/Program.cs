using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PlantUmlClassDiagramGenerator.Library;
using System.Runtime.InteropServices;

namespace PlantUmlClassDiagramGenerator
{
    class Program
    {
        enum OptionType
        {
            Value,
            Switch
        }

        private static readonly Dictionary<string, OptionType> Options = new()
        {
            ["-dir"] = OptionType.Switch,
            ["-public"] = OptionType.Switch,
            ["-ignore"] = OptionType.Value,
            ["-excludePaths"] = OptionType.Value,
            ["-createAssociation"] = OptionType.Switch,
            ["-allInOne"] = OptionType.Switch
        };

        static int Main(string[] args)
        {
            Dictionary<string, string> parameters = MakeParameters(args);
            if (!parameters.ContainsKey("in"))
            {
                Console.WriteLine("Specify a source file name or directory name.");
                return -1;
            }
            if (parameters.ContainsKey("-dir"))
            {
                if (!GeneratePlantUmlFromDir(parameters)) { return -1; }
            }
            else
            {
                if (!GeneratePlantUmlFromFile(parameters)) { return -1; }
            }
            return 0;
        }

        private static bool GeneratePlantUmlFromFile(Dictionary<string, string> parameters)
        {
            var inputFileName = parameters["in"];
            if (!File.Exists(inputFileName))
            {
                Console.WriteLine($"\"{inputFileName}\" does not exist.");
                return false;
            }
            string outputFileName;
            if (parameters.ContainsKey("out"))
            {
                outputFileName = parameters["out"];
                try
                {
                    var outdir = Path.GetDirectoryName(outputFileName);
                    Directory.CreateDirectory(outdir);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return false;
                }
            }
            else
            {
                outputFileName = CombinePath(Path.GetDirectoryName(inputFileName),
                    Path.GetFileNameWithoutExtension(inputFileName) + ".puml");
            }

            try
            {
                GeneratePlantUml(parameters, inputFileName, outputFileName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
            return true;
        }

        private static bool GeneratePlantUmlFromDir(Dictionary<string, string> parameters)
        {
            var inputRoot = parameters["in"];
            if (!Directory.Exists(inputRoot))
            {
                Console.WriteLine($"Directory \"{inputRoot}\" does not exist.");
                return false;
            }

            // Use GetFullPath to fully support relative paths.
            var outputRoot = Path.GetFullPath(inputRoot);
            if (parameters.ContainsKey("out"))
            {
                outputRoot = parameters["out"];
                try
                {
                    Directory.CreateDirectory(outputRoot);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return false;
                }
            }

            var excludePaths = new List<string>();
            var pumlexclude = CombinePath(inputRoot, ".pumlexclude");
            if (File.Exists(pumlexclude))
            {
                excludePaths = File
                    .ReadAllLines(pumlexclude)
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .Select(path => path.Trim())
                    .ToList();
            }
            if (parameters.ContainsKey("-excludePaths"))
            {
                const StringSplitOptions splitOptions = StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries;
                excludePaths.AddRange(parameters["-excludePaths"].Split(',', splitOptions));
            }

            var files = Directory.EnumerateFiles(inputRoot, "*.cs", SearchOption.AllDirectories);

            var includeRefs = new StringBuilder();
            includeRefs.AppendLine("@startuml");

            var failedFiles = new List<string>();
            foreach (var inputFile in files)
            {
                if (excludePaths
                    .Select(p => CombinePath(inputRoot, p))
                    .Any(p => inputFile.StartsWith(p, StringComparison.InvariantCultureIgnoreCase)))
                {
                    Console.WriteLine($"Skipped \"{inputFile}\"...");
                    continue;
                }
                Console.WriteLine($"Processing \"{inputFile}\"...");
                try
                {
                    var outputDir = CombinePath(outputRoot, Path.GetDirectoryName(inputFile)?.Replace(inputRoot, ""));
                    Directory.CreateDirectory(outputDir);
                    var outputFile = CombinePath(outputDir,
                        Path.GetFileNameWithoutExtension(inputFile) + ".puml");

                    GeneratePlantUml(parameters, inputFile, outputFile);

                    if (parameters.ContainsKey("-allInOne"))
                    {
                        var lines = File.ReadAllLines(outputFile);
                        foreach (var line in lines.Skip(1).SkipLast(1))
                        {
                            includeRefs.AppendLine(line);
                        }
                    }
                    else
                    {
                        var newRoot = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @".\" : @".";
                        includeRefs.AppendLine("!include " + outputFile.Replace(outputRoot, newRoot));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    failedFiles.Add(inputFile);
                }
            }
            includeRefs.AppendLine("@enduml");
            File.WriteAllText(CombinePath(outputRoot, "include.puml"), includeRefs.ToString());

            if (failedFiles.Any())
            {
                Console.WriteLine("There were files that could not be processed:");
                failedFiles.ForEach(file => Console.WriteLine(file + Environment.NewLine));
                return false;
            }
            return true;
        }
        private static void GeneratePlantUml(Dictionary<string, string> parameters, string inputFile, string outputFile)
        {
            using var stream = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
            var tree = CSharpSyntaxTree.ParseText(SourceText.From(stream));
            var root = tree.GetRoot();
            var ignoreAcc = GetIgnoreAccessibilities(parameters);

            using var filestream = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
            using var writer = new StreamWriter(filestream);
            var gen = new ClassDiagramGenerator(writer, "    ", ignoreAcc, parameters.ContainsKey("-createAssociation"));
            gen.Generate(root);
        }

        private static Accessibilities GetIgnoreAccessibilities(Dictionary<string, string> parameters)
        {
            var ignoreAcc = Accessibilities.None;
            if (parameters.ContainsKey("-public"))
            {
                ignoreAcc = Accessibilities.Private | Accessibilities.Internal
                                                    | Accessibilities.Protected | Accessibilities.ProtectedInternal;
            }
            else if (parameters.ContainsKey("-ignore"))
            {
                var ignoreItems = parameters["-ignore"].Split(',');
                foreach (var item in ignoreItems)
                {
                    if (Enum.TryParse(item, true, out Accessibilities acc))
                    {
                        ignoreAcc |= acc;
                    }
                }
            }
            return ignoreAcc;
        }

        private static Dictionary<string, string> MakeParameters(string[] args)
        {
            var currentKey = "";
            var parameters = new Dictionary<string, string>();

            foreach (var arg in args)
            {
                if (currentKey != string.Empty)
                {
                    parameters.Add(currentKey, arg);
                    currentKey = "";
                    continue;
                }

                if (Options.ContainsKey(arg))
                {
                    if (Options[arg] == OptionType.Value)
                    {
                        currentKey = arg;
                    }
                    else
                    {
                        parameters.Add(arg, string.Empty);
                    }
                }
                else if (!parameters.ContainsKey("in"))
                {
                    parameters.Add("in", arg);
                }
                else if (!parameters.ContainsKey("out"))
                {
                    parameters.Add("out", arg);
                }
            }
            return parameters;
        }

        private static string CombinePath(string first, string second)
        {
            return first.TrimEnd(Path.DirectorySeparatorChar)
                   + Path.DirectorySeparatorChar
                   + second.TrimStart(Path.DirectorySeparatorChar);
        }
    }
}