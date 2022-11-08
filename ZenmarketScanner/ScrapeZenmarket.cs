using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZenmarketScanner.Models;

namespace ZenmarketScanner
{
    // event args for changing progressbar and the percentage text
    public class LoadProgressChangedEventArgs : EventArgs
    {
        public int Percentage { get; private set; }

        public string Info { get; private set; }

        public LoadProgressChangedEventArgs(int perc, string info)
        {
            Percentage = perc;
            Info = info;
        }
    }

    // scrapes zenmarket
    public class ScrapeZenmarket
    {
        // event args for telling the user stuff is happening
        public event EventHandler<LoadProgressChangedEventArgs> LoadProgressChanged;

        // event args for telling the user loading is complete
        public event EventHandler LoadComplete;

        // client to do the http requests
        private readonly HttpClient httpClient = new HttpClient();

        // the site the class scrapes
        private const string Site = "https://zenmarket.jp/en";

        // image shown in place if there is no image
        private const string NoImage = "https://cdn.discordapp.com/attachments/215332701283155971/506437931926421504/Screen_Shot_2017-07-06_at_11.58.57_pm.png";

        // the maximum amount of pages the scraper will scrape
        private const int MaxPages = 100;

        public ScrapeZenmarket()
        {
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:100.0) Gecko/20100101 Firefox/100.0");
            httpClient.BaseAddress = new Uri(Site);
        }

        // scrapes ZenMarket's search query (depricated)
        public async Task<(Dictionary<string, ZenmarketItem>, int)> ScrapeZenMarket(string search)
        {
            // Initializing the helper classes
            HtmlDocument htmlDocument = new HtmlDocument();
            Dictionary<string, ZenmarketItem> data = new Dictionary<string, ZenmarketItem>();

            // loops through a maxium of 100 pages
            for (int i = 1; i < MaxPages + 1; i++)
            {
                // get the HTML doc of website  
                string url = $"{Site}/yahoo.aspx?q={search}&p={i}";
                string html = await httpClient.GetStringAsync(url);

                // Load HTML doc
                htmlDocument.LoadHtml(html);

                // get all the items in an array
                HtmlNode[] items = htmlDocument.DocumentNode.Descendants("div")
                            .Where(node => node.GetAttributeValue("class", "")
                            .Equals("col-md-12 yahoo-search-result")).ToArray();

                // checks if it has ran out of pages
                if (items.Length == 0)
                {
                    // return early
                    LoadComplete.Raise(this, EventArgs.Empty);
                    return (data, i);
                }

                // loop through all the indivual items
                for (int j = 0; j < items.Length; j++)
                {
                    // get the image
                    string pic = items[j].Descendants("img").LastOrDefault().GetAttributeValue("src", NoImage);

                    // get the item id
                    string id = items[j].Descendants("a").FirstOrDefault().GetAttributeValue("href", "")[22..]; //.Substring(22)

                    // get the item price
                    string price = items[j].Descendants("span").LastOrDefault().GetAttributeValue("data-aud", "");

                    // get the number of bids
                    int bids = int.Parse(items[j].Descendants("span").FirstOrDefault().InnerText);

                    // add item to dict
                    data.Add(id, new ZenmarketItem(pic, price, bids));
                }

                // update progress bar
                LoadProgressChanged.Raise(this, new LoadProgressChangedEventArgs(i, "📚"));
            }

            LoadComplete.Raise(this, EventArgs.Empty);
            return (data, MaxPages);
        }

        public async Task<(Dictionary<string, ZenmarketItem>, int)> APIZenMarket(string search)
        {
            // Initializing classes
            HtmlDocument htmlDocument = new HtmlDocument();
            Dictionary<string, ZenmarketItem> data = new Dictionary<string, ZenmarketItem>();

            // loops through a maxium of 100 pages
            int i;
            for (i = 1; i < MaxPages + 1; i++)
            {
                // construct the url
                string url = $"yahoo.aspx/getProducts?q={HttpUtility.UrlEncode(search)}";

                // construct the post content
                StringContent content = new StringContent($"{{\"page\":{i}}}", Encoding.UTF8, "application/json");

                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Content = content;
                    HttpResponseMessage response = await httpClient.SendAsync(request);

                    //response.EnsureSuccessStatusCode();

                    string responseString = await response.Content.ReadAsStringAsync();

                    // deserialize zenmarket's json object
                    JObject jObject = JObject.Parse(responseString);
                    string jsonString = jObject.First.First.ToString();
                    IEnumerable<ZenmarketJson> items = JsonConvert.DeserializeObject<IEnumerable<ZenmarketJson>>(jsonString);

                    if (items.Count() == 0)
                        break;

                    foreach (ZenmarketJson item in items)
                    {
                        // the price comes as a html element
                        htmlDocument.LoadHtml(item.PriceTextControl);

                        // add item to dict
                        data.Add(item.AuctionID,
                            new ZenmarketItem(item.Thumbnail, htmlDocument.DocumentNode.InnerText, int.Parse(item.Bids)));
                    }
                }

                // update progress bar
                LoadProgressChanged.Raise(this, new LoadProgressChangedEventArgs(i, "📚"));
            }

            LoadComplete.Raise(this, EventArgs.Empty);
            return (data, i);
        }

        // checks if an item has acually been removed or not
        public async Task<(List<ZenmarketItem2>, List<ZenmarketItem2>)> CheckListingIsSold(Dictionary<string, ZenmarketItem> oldData, Dictionary<string, ZenmarketItem> newData)
        {
            // Initializing the helper class
            HtmlDocument htmlDocument = new HtmlDocument();

            // initalize the return lists
            List<ZenmarketItem2> itemsAdded = new List<ZenmarketItem2>();
            List<ZenmarketItem2> itemsSold = new List<ZenmarketItem2>();

            int i = 0;

            // gets all the listings that has been removed
            foreach (KeyValuePair<string, ZenmarketItem> pair in oldData)
            {
                // compare old data to new data
                if (!newData.ContainsKey(pair.Key))
                {
                    // checks if the item has been removed or not
                    var tuple = await isItemRemoved(pair.Key);
                    if (tuple.Item2 > 0)
                        itemsSold.Add(new ZenmarketItem2(pair.Key, pair.Value.ImgUrl, tuple.Item1, tuple.Item2));
                    else
                        newData.Add(pair.Key, new ZenmarketItem(pair.Value.ImgUrl, tuple.Item1, tuple.Item2));

                    // wait so don't send to many request at once
                    await Task.Delay(500);

                    // update progress bar
                    LoadProgressChanged.Raise(this, new LoadProgressChangedEventArgs(++i, "📄"));
                }
            }

            // gets all the listings that has been added
            foreach (KeyValuePair<string, ZenmarketItem> pair in newData)
            {
                // compare new data to old data
                if (!oldData.ContainsKey(pair.Key))
                    itemsAdded.Add(new ZenmarketItem2(pair));
            }

            LoadComplete.Raise(this, EventArgs.Empty);
            return (itemsAdded, itemsSold);

            // checks the bids on the item if 0 it has not been removed
            async Task<(string, int)> isItemRemoved(string id)
            {
                // get the HTML doc of website
                string url = GetItemUrl(id);
                string html = await httpClient.GetStringAsync(url);

                // Load HTML doc
                htmlDocument.LoadHtml(html);

                // exstract the number of bids in the listing
                HtmlNode bids = htmlDocument.GetElementbyId("bidNum");

                string price = "";
                if (bids != null)
                {
                    int numberOfBids = int.Parse(bids.InnerText);
                    price = htmlDocument.GetElementbyId("lblPriceAlt").InnerText;

                    return (price, numberOfBids);
                }
                else
                {
                    return (price, 0);
                }
            }
        }

        // gets a full url from the item code
        public static string GetItemUrl(string itemCode)
        {
            return $"{Site}/auction.aspx?itemCode={itemCode}";
        }

        public static string GetSearchUrl(string searchTerm)
        {
            return $"{Site}/yahoo.aspx?q={HttpUtility.UrlEncode(searchTerm)}";
        }

    }

    public static class EventExtensions
    {
        /// <summary>Rasises the event on the UI thread if avaiable.</summary>
        public static object Raise(this MulticastDelegate multicastDelegate, object sender, EventArgs e)
        {
            object retVal = null;

            MulticastDelegate threadSafeMulticastDelegate = multicastDelegate;
            if (threadSafeMulticastDelegate != null)
            {
                foreach (Delegate d in threadSafeMulticastDelegate.GetInvocationList())
                {
                    ISynchronizeInvoke synchronizeInvoke = d.Target as ISynchronizeInvoke;
                    if ((synchronizeInvoke != null) && synchronizeInvoke.InvokeRequired)
                    {
                        retVal = synchronizeInvoke.EndInvoke(synchronizeInvoke.BeginInvoke(d, new[] { sender, e }));
                    }
                    else
                    {
                        retVal = d.DynamicInvoke(new[] { sender, e });
                    }
                }
            }

            return retVal;
        }
    }
}
