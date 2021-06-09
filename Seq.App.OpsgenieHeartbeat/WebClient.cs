using System.Threading.Tasks;
using Flurl.Http;

namespace Seq.App.OpsGenieHeartbeat
{
    public static class WebClient
    {
        /// <summary>
        ///     Configure Flurl.Http to use an ApiClient, given the configured parameters
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="useProxy"></param>
        /// <param name="proxy"></param>
        /// <param name="proxyUser"></param>
        /// <param name="proxyPass"></param>
        /// <param name="proxyBypass"></param>
        /// <param name="localUrls"></param>
        public static void SetFlurlConfig(string appName, bool useProxy, string proxy = null, string proxyUser = null,
            string proxyPass = null, bool proxyBypass = false, string[] localUrls = null)
        {
            FlurlHttp.Configure(config =>
            {
                config.HttpClientFactory = new HeartbeatClient(appName, useProxy, proxy, proxyUser, proxyPass,
                    proxyBypass, localUrls);
            });
        }

        /// <summary>
        ///     Send a Heartbeat to the configured OpsGenie URL using the configured API key
        /// </summary>
        /// <param name="apiUrl"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public static async Task<int> SendHeartbeat(string apiUrl, string apiKey)
        {
            var response = await apiUrl.WithHeader("Authorization", "GenieKey " + apiKey).GetAsync();

            return response.StatusCode;
        }
    }
}