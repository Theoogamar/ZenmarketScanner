using System;
using System.Collections.Generic;
using System.Text;

namespace ZenmarketScanner.Models
{
    public class ZenmarketJson
    {
        public string AuctionID { get; set; }
        public string Title { get; set; }
        public string Thumbnail { get; set; }
        public string EndTime { get; set; }
        public string Bids { get; set; }
        public bool IsNewItemFlag { get; set; }
        public bool IsFreeShippingFlag { get; set; }
        public bool IsShop { get; set; }
        public IEnumerable<Category> Categories { get; set; }
        public string AuctionURL { get; set; }
        public string PriceTextControl { get; set; }
        public string PriceBidOrBuyTextControl { get; set; }
        public bool IsInWatchList { get; set; }
        public object Comment { get; set; }
    }

    public class Category
    {
        public string NameLocalized { get; set; }
        public int ID { get; set; }
        public int ParentID { get; set; }
        public int CategoryID { get; set; }
        public int Level { get; set; }
        public string NameEN { get; set; }
        public string NameRU { get; set; }
        public string NameUA { get; set; }
        public string NameJP { get; set; }
        public string NameCN { get; set; }
        public string NameTW { get; set; }
        public string NameES { get; set; }
        public string NameFR { get; set; }
        public string NameMS { get; set; }
        public string NameVI { get; set; }
        public string NameAR { get; set; }
        public string NameDE { get; set; }
        public string NameID { get; set; }
        public string NameTH { get; set; }
        public string NameIT { get; set; }
        public string NamePT { get; set; }
        public string NameTR { get; set; }
        public object RelatedRakutenCategoryId { get; set; }
    }
}
