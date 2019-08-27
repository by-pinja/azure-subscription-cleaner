using CommandLine;

namespace Protacon.AzureSubscriptionCleaner.CommandLine
{
    public class Options
    {
        [Option('s', "simulate", HelpText = "If enabled, no actual changing operations are done.")]
        public bool Simulate { get; }

        public Options(bool simulate)
        {
            Simulate = simulate;
        }
    }
}