using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService1
{
    public  class Property
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
        // فیلدهای جدید
        public string Description { get; set; } // توضیحات ملک
        public Dictionary<string, string> FinancialDetails { get; set; } // ارزیابی شهری و مالیات‌ها
        public string BrokerName { get; set; } // نام کارگزار
        public string BrokerPhone { get; set; } // شماره تماس کارگزار
        public int PhotoCount { get; set; } // تعداد عکس‌ها
        public List<string> AdditionalPhotoUrls { get; set; } // لیست URLهای عکس‌های اضافی
    }
}
