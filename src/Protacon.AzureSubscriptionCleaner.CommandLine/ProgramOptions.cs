using CommandLine;

namespace Protacon.AzureSubscriptionCleaner.CommandLine
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
    }
}