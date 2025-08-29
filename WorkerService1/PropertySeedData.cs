using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService1
{
    public static class PropertySeedData
    {
        public static List<Property> GenerateSeedData(int count)
        {
            var properties = new List<Property>();
            for (int i = 1; i <= count; i++)
            {
                var property = new Property
                {
                    Meta_Content = $"Property {i} - Condo for Sale",
                    Property_URl = $"https://www.centris.ca/en/properties~for-sale/{i}",
                    PropertyImage = $"https://www.centris.ca/images/property_{i}.jpg",
                    MlsNumberNoStealth = $"MLS{i:D8}",
                    PriceCurrency = "CAD",
                    Price = $"{100000 + (i * 50000)}",
                    Category = "Condo",
                    Address = $"123{i} Sample Street, Montreal, QC",
                    Orgazination_Name = "Sample Realty Inc.",
                    Amenities = new Dictionary<string, string>
                    {
                        { "Rooms", $"{4 + i}" },
                        { "Bedrooms", $"{2 + (i % 3)}" },
                        { "Bathrooms", $"{1 + (i % 2)}" },
                        { "WalkScore", $"{80 + (i % 20)}" },
                        { "YearBuilt", $"{2000 + (i % 20)}" }
                    },
                    Latetude = $"45.5017{(i % 10)}",
                    Longitude = $"-73.5673{(i % 10)}"
                };
                properties.Add(property);
            }
            return properties;
        }
    }
}
