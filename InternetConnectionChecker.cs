  public class InternetConnectionChecker
    {
        private DateTime? lastCheckTime = null;
        private List<string> dnsCollection = new List<string>();
        private List<string> urlCollection = new List<string>();
        private bool lastResult = false;
        public InternetConnectionChecker()
        {
            dnsCollection.Add("8.8.8.8");
            urlCollection.Add("https://www.google.com.ua");
        }


        public async Task<bool> IsInternetAvaible(CancellationToken token)
        {
            if (lastCheckTime != null && (DateTime.Now - lastCheckTime.Value).TotalMinutes < 1)
                return lastResult;
            else
            {
                ManualResetEvent ev = new ManualResetEvent(false);
                bool dnsActive = false;
                foreach (var dns in dnsCollection)
                {
                    Task.Factory.StartNew(async () =>
                    {
                        await CheckDnsAvailability(dns).ContinueWith(s =>
                        {
                            if (s.Result)
                            {
                                dnsActive = true;
                                ev.Set();
                            }
                        });
                    }, token);
                }
                token.ThrowIfCancellationRequested();
                ev.WaitOne(TimeSpan.FromSeconds(5));
                token.ThrowIfCancellationRequested();
                if (dnsActive)
                {
                    ev.Reset();
                    bool urlActive = false;
                    foreach (var url in urlCollection)
                    {
                        Task.Factory.StartNew(async () =>
                        {
                            await CheckUrlAvailability(url).ContinueWith(s =>
                            {
                                if (s.Result)
                                {
                                    urlActive = true;
                                    ev.Set();
                                }
                            });
                        }, token);
                    }
                    token.ThrowIfCancellationRequested();
                    ev.WaitOne(TimeSpan.FromSeconds(10));
                    token.ThrowIfCancellationRequested();
                    if (urlActive)
                    {
                        lastCheckTime = DateTime.Now;
                        lastResult = true;
                    }
                    else
                    {
                        lastCheckTime = DateTime.Now;
                        lastResult = false;
                    }
                }
                else
                {
                    lastCheckTime = DateTime.Now;
                    lastResult = false;
                }

            }
            return lastResult;
        }
        private async Task<bool> CheckDnsAvailability(string dns)
        {
            try
            {
                await Dns.GetHostEntryAsync(dns);
                return true;
            }
            catch
            {
                return false;
            }
        }
        private async Task<bool> CheckUrlAvailability(string url)
        {
            try
            {
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                myHttpWebRequest.UserAgent = "okhttp/3.14.9";
                // Assign the response object of 'HttpWebRequest' to a 'HttpWebResponse' variable.
                var myHttpWebResponse = await myHttpWebRequest.GetResponseAsync();
                if ((myHttpWebResponse as HttpWebResponse).StatusCode == HttpStatusCode.OK)
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;
            }

        }
    }
