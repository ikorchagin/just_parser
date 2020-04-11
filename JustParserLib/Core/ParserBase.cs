using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace JustParserLib
{
    public abstract class ParserBase<T>
    {
        public event Action<T, double> NewData;

        public event Action<object> WorkDone;

        public event Action<ErrorType> ThrowError;

        private double step = 100;

        public string PhoneAgent { get; private set; } = "Dalvik/2.1.0 (Linux; U; Android 9; SM-N950U Build/PPR1.180610.011)";

        public string DesktopAgent { get; private set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.135 Safari/537.36 Edge/12.246";

        public IParserSettings Settings { get; set; }

        public ParserBase(IParserSettings settings)
        {
            Settings = settings;
        }

        public bool Status { get; private set; } = true;

        protected async Task<HtmlDocument> GetDocument(string url, string agent)
        {
            var document = new HtmlDocument();
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", agent);
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var html = await response.Content.ReadAsStringAsync();
                    await Task.Delay(2500);
                    await Task.Run(() => document.LoadHtml(html));
                    return document;
                }
                return null;
            }
        }

        protected async Task<HtmlDocument> GetDocument(string url)
        {
            return await GetDocument(url, PhoneAgent);
        }

        protected async Task<HtmlDocument> GetDocument(string url, int id)
        {
            return await GetDocument($"{url}{Settings.Prefix.Replace("{value}", id.ToString())}");
        }

        protected async Task<HtmlDocument> GetDocument(string url, int id, string agent)
        {
            return await GetDocument($"{url}{Settings.Prefix.Replace("{value}", id.ToString())}", agent);
        }

        public void Abort()
        {
            Status = false;
        }

        private void FailTask(ErrorType type)
        {
            ThrowError?.Invoke(ErrorType.WrongUrl);
            Status = false;
            WorkDone?.Invoke(this);
        }

        public abstract string[] FindLinks(HtmlDocument document);

        public abstract Task<T> GetStrictData(string link);

        public abstract Task<T> GetSoftData(string link);

        public abstract bool GetMode(HtmlDocument document);

        public abstract int GetItemsCount(HtmlDocument document);

        public abstract Task<HtmlDocument> ParseDocument(int page);

        public async void Work()
        {
            HtmlDocument document = null;
            try
            {
                document = await ParseDocument(1);
            }
            catch (InvalidOperationException)
            {
                FailTask(ErrorType.WrongUrl);
                return;
            }

            if (document == null)
            {
                FailTask(ErrorType.Null);
                return;
            }

            var links = FindLinks(document);

            if (links == null)
            {
                FailTask(ErrorType.WrongUrl);
                return;
            }

            var itemsCount = GetItemsCount(document);
            if (itemsCount <= 0)
            {
                FailTask(ErrorType.WrongUrl);
                return;
            }

            if (Settings.ItemsCount > 0)
            {
                itemsCount = Settings.ItemsCount;
            }
            var counter = 0;
            var pages = 2;
            step /= itemsCount;
            while (counter < itemsCount && Status)
            {
                foreach (var link in links)
                {
                    if (GetMode(document))
                    {
                        var data = await GetStrictData(link);
                        if (!Status || counter >= itemsCount)
                        {
                            break;
                        }
                        NewData?.Invoke(data, step);
                    }
                    else
                    {
                        var data = await GetSoftData(link);
                        if (!Status || counter >= itemsCount)
                        {
                            break;
                        }
                        NewData?.Invoke(data, step);
                    }
                    counter++;
                }

                if (!Status || counter >= itemsCount)
                {
                    break;
                }
                document = await ParseDocument(pages++);
                links = FindLinks(document);

                if (links == null)
                {
                    Status = false;
                }
            }

            Status = false;
            WorkDone?.Invoke(this);
        }

    }
}
