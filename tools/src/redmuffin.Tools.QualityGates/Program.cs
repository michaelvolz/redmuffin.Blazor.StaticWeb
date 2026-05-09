using System.CommandLine;
using redmuffin.Tools.QualityGates.Commands;

var rootCommand = new RootCommand("redmuffin Quality Gates — code quality analysis tool");
rootCommand.Subcommands.Add(CrapCommand.Create());
rootCommand.Subcommands.Add(ScrapCommand.Create());
rootCommand.Subcommands.Add(AllCommand.Create());

return await rootCommand.Parse(args).InvokeAsync().ConfigureAwait(false);
