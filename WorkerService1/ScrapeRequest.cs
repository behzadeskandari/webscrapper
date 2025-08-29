using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService1
{
    public class ScrapeRequest
    {
        public int? MaxPages { get; set; }
    }


    public record ScrapePageCommand
    {
        public int PageNumber { get; init; }
    }
}
