using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelperLibrary.Database.Models
{
    public class Security
    {
        [Column(Storage = "SECURITY_ID")]
        public int SecurityId { get; set; }
        [Column(Storage = "TICKER")]
        public string Ticker { get; set; }
        [Column(Storage = "NAME")]
        public string Name { get; set; }
        [Column(Storage = "ISIN")]
        public string ISIN { get; set; }
        [Column(Storage = "SECTOR")]
        public string Sector { get; set; }
        [Column(Storage = "ACTIVE")]
        public int Active { get; set; }
        [Column(Storage = "SECURITY_TYPE")]
        public string SecurityType { get; set; }
        [Column(Storage = "CURRENCY")]
        public string Ccy { get; set; }
        [Column(Storage = "DESCRIPTION")]
        public string Description { get; set; }
        [Column(Storage = "COUNTRY")]
        public string Country { get; set; }

        [Column(Storage = "INDEX_MEMBER_OF")]
        public int? IndexMemberOf { get; set; }

        public DateTime LastPriceDate { get; set; }
        public decimal LastPrice { get; set; }

    }
}
