using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace webscrapper
{
    public class Property
    {
        public string Meta_Content { get; set; }
        public string Property_URl { get; set; }
        public string PropertyImage { get; set; }
        public string MlsNumberNoStealth { get; set; }
        public string PriceCurrency { get; set; }
        public string Price { get; set; }
        public string Category { get; set; }
        public string Address { get; set; }
        public string Orgazination_Name { get; set; }
        public Dictionary<string, string> Amenities { get; set; }
        public string Latetude { get; set; }
        public string Longitude { get; set; }
        public string Description { get; set; }
        public Dictionary<string, string> FinancialDetails { get; set; }
        public List<string> BrokerNames { get; set; } // Changed to List
        public List<string> BrokerPhones { get; set; } // Changed to List
        public int PhotoCount { get; set; }
        public List<string> AdditionalPhotoUrls { get; set; }
    }
}
