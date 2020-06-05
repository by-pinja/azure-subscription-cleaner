using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace Pinja.AzureSubscriptionCleaner.CommandLine
{
    public class ProgramOptions
    {
        [Option('s', "simulate", HelpText = "If enabled, no actual changing operations are done.")]
        public bool Simulate { get; }

        [Option('c', "channel", HelpText = "If defined, summary is sent to this slack channel.")]
        public string Channel { get; }

        public ProgramOptions(bool simulate, string channel)
        {
            Simulate = simulate;
            Channel = channel;
        }

        private static readonly IEnumerable<UnParserSettings> _exampleSettings = new[]
        {
            new UnParserSettings { PreferShortName = false }
        };

        [Usage(ApplicationAlias = "dotnet run --")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>
                {
                    new Example("Delete all resource groups which are not locked", _exampleSettings, new ProgramOptions(false, string.Empty)),
                    new Example("Simulate deletion of all resource groups which are not locked (this just prints results to console)", _exampleSettings, new ProgramOptions(true, string.Empty)),
                    new Example("Simulate deletion of all resource groups which are not locked and report it to slack channel 'test-channel'", _exampleSettings, new ProgramOptions(true, "test-channel")),
                    new Example("Delete all resource groups which are not locked and report it to slack channel 'test-channel'", _exampleSettings, new ProgramOptions(false, "test-channel"))
                };
            }
        }
    }
}