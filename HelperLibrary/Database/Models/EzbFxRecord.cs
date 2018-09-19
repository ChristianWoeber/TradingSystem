using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HelperLibrary.Util.Atrributes;

namespace HelperLibrary.Database.Models
{
    public class EzbFxRecord
    {
        [InputMapping(KeyWords = new string[] { "USD", "Iso" })]
        [Column(Storage = "USD")]
        public decimal Eur_Usd { get; set; }

        [InputMapping(KeyWords = new string[] { "JPY", "Iso" })]
        [Column(Storage = "JPY")]
        public decimal Eur_Jpy { get; set; }

        [InputMapping(KeyWords = new string[] { "GBP", "Iso" })]
        [Column(Storage = "GBP")]
        public decimal Eur_Gbp { get; set; }

        [InputMapping(KeyWords = new string[] { "CHF", "Iso" })]
        [Column(Storage = "CHF")]
        public decimal Eur_Chf { get; set; }

        [InputMapping(KeyWords = new string[] { "AUD", "Iso" })]
        [Column(Storage = "AUD")]
        public decimal Eur_Aud { get; set; }


        //[InputMapping(KeyWords = new string[] { "AUD", "CHF", "GBP","JPY","USD" })]
        //[Column(Storage = "Iso")]
        //public List<Tuple<string,decimal>> FxRate { get; set; }

        //[InputMapping(KeyWords = new string[] { "AUD", "Iso" })]
        //[Column(Storage = "Rate")]
        //public decimal Rate { get; set; }

        [InputMapping(KeyWords = new string[] { "Date", "DateTime" })]
        [Column(Storage = "ASOF")]
        public DateTime AsOf { get; set; }
    }
}
