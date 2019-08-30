using CommandLine;

namespace Protacon.AzureSubscriptionCleaner.CommandLine
{
    public class ProgramOptions
    {
        [Option('s', "simulate", HelpText = "If enabled, no actual changing operations are done.")]
        public bool Simulate { get; }

        public ProgramOptions(bool simulate)
        {
            Simulate = simulate;
        }
    }
}