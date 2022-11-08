using System;
using System.Collections.Generic;
using System.Text;

namespace ZenmarketScanner.Models
{
    public class ZenmarketItem
    {
        public string ImgUrl;
        public string Price;
        public int Bids;

        public ZenmarketItem(string imgUrl, string price, int bids)
        {
            ImgUrl = imgUrl;
            Price = price;
            Bids = bids;
        }

        public ZenmarketItem(ZenmarketItem zenmarketItem)
        {
            ImgUrl = zenmarketItem.ImgUrl;
            Price = zenmarketItem.Price;
            Bids = zenmarketItem.Bids;
        }

        public ZenmarketItem()
        {

        }
    }
}
