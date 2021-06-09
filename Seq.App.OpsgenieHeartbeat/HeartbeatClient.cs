// Original HTTP Health Check code is Copyright 2019 Datalust and contributors. 
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Net;
using System.Net.Http;
using Flurl.Http.Configuration;

namespace Seq.App.OpsGenieHeartbeat
{
    /// <summary>
    ///     HTTP Client for sending heartbeats
    /// </summary>
    public class HeartbeatClient : DefaultHttpClientFactory
    {
        /// <summary>
        ///     HeartbeatClient instance
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="useProxy"></param>
        /// <param name="proxy"></param>
        /// <param name="proxyUser"></param>
        /// <param name="proxyPass"></param>
        /// <param name="proxyBypass"></param>
        /// <param name="localUrls"></param>
        public HeartbeatClient(string appName, bool useProxy, string proxy = null, string proxyUser = null,
            string proxyPass = null, bool proxyBypass = false, string[] localUrls = null)
        {
            AppName = appName;
            if (useProxy && !string.IsNullOrEmpty(proxy))
            {
                UseProxy = true;
                Proxy = new WebProxy
                {
                    Address = new Uri(proxy),
                    BypassProxyOnLocal = proxyBypass,
                    BypassList = localUrls,
                    UseDefaultCredentials = false
                };

                if (!string.IsNullOrEmpty(proxyUser) && !string.IsNullOrEmpty(proxyPass))
                    Proxy.Credentials = new NetworkCredential(proxyUser, proxyPass);
                else
                    Proxy.UseDefaultCredentials = true;
            }
            else
            {
                UseProxy = false;
            }
        }

        private string AppName { get; }
        private bool UseProxy { get; }
        private WebProxy Proxy { get; }

        /// <summary>
        ///     Heartbeat client message handler
        /// </summary>
        /// <returns></returns>
        public override HttpMessageHandler CreateMessageHandler()
        {
            if (UseProxy)
                return new HttpClientHandler
                {
                    Proxy = Proxy,
                    UseProxy = UseProxy,
                    UseDefaultCredentials = true,
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };
            return new HttpClientHandler
            {
                UseDefaultCredentials = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
        }

        /// <summary>
        ///     Heartbeat Client HttpClient
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public override HttpClient CreateHttpClient(HttpMessageHandler handler)
        {
            var httpClient = base.CreateHttpClient(handler);
            httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(AppName);
            httpClient.Timeout = new TimeSpan(0, 2, 0);
            httpClient.MaxResponseContentBufferSize = 262144;

            return httpClient;
        }
    }
}