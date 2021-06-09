using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Seq.Apps;
using Seq.Apps.LogEvents;

namespace Seq.App.OpsGenieHeartbeat
{
    [SeqApp("OpsGenie Heartbeat ",
        Description = "Periodically connect to the OpsGenie Heartbeat API to provide a heartbeat")]
    // ReSharper disable once UnusedType.Global
    public class HeartbeatReactor : SeqApp
    {
        private Timer _timer; // ReSharper disable UnusedAutoPropertyAccessor.Global

        // ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
        // ReSharper disable MemberCanBePrivate.Global
        [SeqAppSetting(
            DisplayName = "Target URLs",
            HelpText = "The OpsGenie API URL that the heartbeat will periodically GET.",
            InputType = SettingInputType.Text)]
        public string TargetUrl { get; set; }

        [SeqAppSetting(
            DisplayName = "Interval (seconds)",
            IsOptional = true,
            HelpText = "The time between checks; the default is 60.")]
        public int IntervalSeconds { get; set; } = 60;

        [SeqAppSetting(
            DisplayName = "API key",
            IsOptional = true,
            HelpText = "The API key for OpsGenie.")]
        public string ApiKey { get; set; }

        [SeqAppSetting(
            DisplayName = "Proxy address",
            HelpText = "Proxy address (if proxy is required).",
            IsOptional = true)]
        public string Proxy { get; set; }

        [SeqAppSetting(
            DisplayName = "Proxy bypass local addresses",
            HelpText = "Bypass local addresses for proxy.")]
        public bool BypassLocal { get; set; }

        [SeqAppSetting(
            DisplayName = "Local addresses for proxy bypass",
            HelpText = "Local addresses to bypass, comma separated.",
            IsOptional = true)]
        public string LocalAddresses { get; set; }

        [SeqAppSetting(
            DisplayName = "Proxy username, if authentication is required.",
            HelpText = "Username for proxy authentication.",
            IsOptional = true)]
        public string ProxyUser { get; set; }

        [SeqAppSetting(
            DisplayName = "Proxy password, if password is required.",
            HelpText = "Username for proxy authentication.",
            IsOptional = true,
            InputType = SettingInputType.Password)]
        public string ProxyPass { get; set; }

        protected override void OnAttached()
        {
            var useProxy = false;
            string proxy = null;
            string proxyPass = null;
            string proxyUser = null;
            var bypassLocal = false;
            var localAddresses = new string[] { };


            LogEvent(LogEventLevel.Debug, "{AppName} initialising ...", App.Title);
            if (!string.IsNullOrEmpty(Proxy))
            {
                useProxy = true;
                proxy = Proxy;
                bypassLocal = BypassLocal;
                localAddresses = LocalAddresses.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim()).ToArray();
                proxyUser = ProxyUser;
                proxyPass = ProxyPass;
                LogEvent(LogEventLevel.Debug,
                    "Use Proxy {UseProxy}, Proxy Address {Proxy}, BypassLocal {BypassLocal}, Authentication {Authentication} ...",
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    useProxy, proxy, bypassLocal, !string.IsNullOrEmpty(proxyUser) && !string.IsNullOrEmpty(proxyPass));
            }
            else
            {
                LogEvent(LogEventLevel.Debug, "Use Proxy false ...");
            }

            WebClient.SetFlurlConfig(App.Title, useProxy, proxy, proxyUser, proxyPass, bypassLocal, localAddresses);

            LogEvent(LogEventLevel.Debug, "Starting Heartbeat timer with interval of {Time} seconds ...",
                IntervalSeconds);
            _timer = new Timer(IntervalSeconds * 1000)
            {
                AutoReset = true
            };
            _timer.Elapsed += TimerOnElapsed;
            _timer.Start();

            LogEvent(LogEventLevel.Debug, "Heartbeat timer started ...");
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            // "{AppName} {Method} {TargetUrl} {Outcome} with status code {StatusCode} in {Elapsed:0.000} ms"
            var sw = Stopwatch.StartNew();
            Task<int> statusCode = null;
            try
            {
                statusCode = WebClient.SendHeartbeat(TargetUrl, ApiKey);
            }
            catch (Exception ex)
            {
                LogEvent(LogEventLevel.Error, ex, "Error {Error} sending Heartbeat ...", ex.Message);
            }

            sw.Stop();

            if (statusCode == null) return;
            switch (statusCode.Result)
            {
                case 202:
                    LogEvent(LogEventLevel.Debug,
                        "{AppName} {Method} {TargetUrl} {Outcome} with status code {StatusCode} in {Elapsed:0.000} ms");
                    break;
                default:
                    LogEvent(LogEventLevel.Warning,
                        "{AppName} {Method} {TargetUrl} {Outcome} with status code {StatusCode} in {Elapsed:0.000} ms");
                    break;
            }
        }


        /// <summary>
        ///     Output a log event to Seq stream
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        private void LogEvent(LogEventLevel logLevel, string message, params object[] args)
        {
            var logArgsList = args.ToList();
            var logArgs = logArgsList.ToArray();

            Log.ForContext("AppName", App.Title).Write((Serilog.Events.LogEventLevel) logLevel, message, logArgs);
        }

        /// <summary>
        ///     Output an exception log event to Seq stream
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="exception"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        private void LogEvent(LogEventLevel logLevel, Exception exception, string message, params object[] args)
        {
            var logArgsList = args.ToList();
            var logArgs = logArgsList.ToArray();

            Log.ForContext("AppName", App.Title)
                .Write((Serilog.Events.LogEventLevel) logLevel, exception, message, logArgs);
        }
    }
}