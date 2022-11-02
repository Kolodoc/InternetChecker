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
            //check cache
            if (lastCheckTime != null && lastCheckTime.Value == DateTime.Now)
                return lastResult;
            //start check
            else
            {           
                bool dnsActive = false;
                int count = 0;
                //check all dns async
                foreach (var dns in dnsCollection)
                {
                    Task.Factory.StartNew(async () =>
                    {
                        await CheckDnsAvailability(dns).ContinueWith(s =>
                        {
                            if (s.Result)
                            {
                                dnsActive = true;                         
                            }
                            //multithread safe increment
                            Interlocked.Increment(ref count);
                        });
                    }, token);
                }
                token.ThrowIfCancellationRequested();
                //any dns active or all fail
                while(!dnsActive || count != dnsCollection.Count)
                {
                    //stop every 0.5 sec
                    await Task.Delay(500);
                    //stop waiting if operation canceled from code
                    token.ThrowIfCancellationRequested();
                }             
                if (dnsActive)
                {
                    count = 0;
                    bool urlActive = false;
                    //check all http async
                    foreach (var url in urlCollection)
                    {
                        Task.Factory.StartNew(async () =>
                        {
                            await CheckUrlAvailability(url).ContinueWith(s =>
                            {
                                if (s.Result)
                                {
                                    urlActive = true;
                                }
                                Interlocked.Increment(ref count);
                            });
                        }, token);
                    }
                    token.ThrowIfCancellationRequested();
                    while (!urlActive || count != urlCollection.Count)
                    {
                        await Task.Delay(500);
                        token.ThrowIfCancellationRequested();
                    }                   
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
                var result = await Dns.GetHostEntryAsync(dns);
                //if ip addresses from resolve dns > 0
                if (result.AddressList.Length > 0)
                    return true;
                else
                    return false;
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
