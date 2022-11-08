using System;
using System.Collections.Generic;
using System.Text;

namespace ZenmarketScanner.Models
{
    public class ZenmarketItem2 : ZenmarketItem
    {
        public string Id;

        public ZenmarketItem2(string id, string imgUrl, string price, int bids) : base(imgUrl, price, bids)
        {
            Id = id;
        }
        public ZenmarketItem2(KeyValuePair<string, ZenmarketItem> pair) : base(pair.Value)
        {
            Id = pair.Key;
        }
    }
}
