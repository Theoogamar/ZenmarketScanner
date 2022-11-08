using System;
using System.Collections.Generic;
using System.Text;

namespace ZenmarketScanner.Models
{
    public class ZenmarketSearch
    {
        public string Name { get; set; }

        public string Search { get; set; }

        public Dictionary<string, ZenmarketItem> Data { get; set; }

        public DateTime Date { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
