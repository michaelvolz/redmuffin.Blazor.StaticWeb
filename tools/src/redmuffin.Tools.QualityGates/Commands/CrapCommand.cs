namespace redmuffin.Tools.QualityGates.Commands;

using System.CommandLine;

public static class CrapCommand
{
    public static Command Create()
    {
        var projectOption = new Option<DirectoryInfo>("--project")
        {
            Description = "Path to the project directory to analyze",
            Required = true,
        };

        var coverageOption = new Option<FileInfo>("--coverage-file")
        {
            Description = "Path to the Cobertura XML coverage file",
            Required = true,
        };

        var maxCrapOption = new Option<int>("--max-crap")
        {
            Description = "Maximum CRAP score allowed per method",
            DefaultValueFactory = _ => 8,
        };

        var changedOption = new Option<bool>("--changed")
        {
            Description = "Only analyze files modified since HEAD (requires git)",
        };

        var command = new Command("crap", "Analyze CRAP score per method")
        {
            projectOption,
            coverageOption,
            maxCrapOption,
            changedOption,
        };

        command.SetAction(parseResult =>
        {
            var projectPath = parseResult.GetValue(projectOption)!.FullName;
            var coveragePath = parseResult.GetValue(coverageOption)!.FullName;
            var maxCrap = parseResult.GetValue(maxCrapOption);
            var changedOnly = parseResult.GetValue(changedOption);

            return Execute(projectPath, coveragePath, maxCrap, changedOnly);
        });

        return command;
    }

    internal static int Execute(
        string projectPath,
        string coveragePath,
        int maxCrap,
        bool changedOnly)
    {
        try
        {
            if (!Directory.Exists(projectPath))
            {
                Console.Error.WriteLine($"Project directory not found: {projectPath}");
                return 1;
            }

            if (!File.Exists(coveragePath))
            {
                Console.Error.WriteLine($"Coverage file not found: {coveragePath}");
                return 1;
            }

            var methods = Analysis.CyclomaticComplexity.Analyze(projectPath);

            if (changedOnly)
            {
                methods = GitFileFilter.FilterChanged(methods, projectPath, m => m.FilePath);
            }

            var coverage = Analysis.CoverageParser.Parse(coveragePath);
            var results = Analysis.MethodMapper.Map(methods, coverage);

            return CrapHandler.Run(results, maxCrap);
        }
        catch (System.Xml.XmlException)
        {
            Console.Error.WriteLine($"Failed to parse coverage file: {coveragePath}");
            return 1;
        }
    }
}
