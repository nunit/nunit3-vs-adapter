using System.Diagnostics;

namespace NUnit.VisualStudio.TestAdapter.Internal
{
    public class TimingLogger
    {
        private readonly IAdapterSettings settings;
        private readonly ITestLogger logger;
        public Stopwatch Stopwatch { get; }

        public TimingLogger(IAdapterSettings settings, ITestLogger logger)
        {
            this.settings = settings;
            this.logger = logger;
            if (settings.Verbosity < 5)
                return;
            Stopwatch = Stopwatch.StartNew();
        }

        public TimingLogger ReStart()
        {
            if (settings.Verbosity < 5)
                return this;
            Stopwatch.StartNew();
            return this;
        }

        public TimingLogger LogTime(string leadText = "")
        {
            if (settings.Verbosity < 5 || Stopwatch == null)
                return this;
            var ts = Stopwatch.Elapsed;
            string elapsedTime = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
            logger.Info($"{leadText} :Elapsed: {elapsedTime}");
            return this;
        }
    }
}
