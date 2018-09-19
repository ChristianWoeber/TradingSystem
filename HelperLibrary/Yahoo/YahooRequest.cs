using HelperLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HelperLibrary.Extensions;

namespace HelperLibrary.Yahoo
{
    public class YahooRequest
    {
        private YahooRequestBuilder _builder;

        public YahooRequest(YahooRequestType type)
        {
            _builder = new YahooRequestBuilder(type);
        }

        public YahooRequestBuilder Tickers(params string[] tickers)
        {
            _builder.Tickers(tickers);
            return _builder;
        }

        public YahooRequestBuilder Tickers(ICollection<string> tickers)
        {
            _builder.Tickers(tickers);
            return _builder;
        }

        public YahooRequestBuilder From(DateTime dt)
        {
            _builder.From(dt);
            return _builder;
        }

        public YahooRequestBuilder To(DateTime? dt = null)
        {
            _builder.To(dt ?? DateTime.Today);
            return _builder;
        }

        public YahooRequestBuilder SetCrumb(string crumb)
        {
            _builder.SetCrumb(crumb);
            return _builder;
        }

        public YahooRequestBuilder Interval(YahooRequestInterval? interval = null)
        {
            _builder.Interval(interval ?? YahooRequestInterval.Daily);
            return _builder;
        }

        public YahooRequestBuilder Build()
        {
            return _builder.BuildUrls();
        }
    }

    public class YahooRequestBuilder
    {
        private YahooRequestType _type;
        private StringBuilder _sb;
        private string _crumb;
        public List<string> TickersCollection = new List<string>();
        public List<YahooUrlItem> Urls = new List<YahooUrlItem>();


        public YahooRequestBuilder(YahooRequestType type)
        {
            _type = type;
            _sb = new StringBuilder();

            switch (type)
            {
                case YahooRequestType.Single:
                    _sb.Append(Settings.Default.SingleBaseUrl);
                    break;
                case YahooRequestType.Historical:
                    _sb.Append(Settings.Default.HistoricalBaseUrlNew);
                    break;
            }
        }

        public YahooRequestBuilder From(DateTime dt)
        {
            if (_type == YahooRequestType.Single)
                throw new ArgumentException("Requests vom typen Single können keine historischen Daten abfragen");

            AppendToBase();
            AppendFrom(dt);

            return this;
        }

        private void AppendToBase()
        {
            // update seit Mai 2017 API changed//       
            _sb.Append(YahooFieldsList.TICKER_PLACEHOLDER);
            _sb.Append(YahooFieldsList.QUESTIONMARK);
        }

        private void AppendFrom(DateTime dt)
        {
            //append keyword period1
            _sb.Append(YahooFieldsList.FROM);

            //append =
            _sb.Append(YahooFieldsList.EQUALS);

            //Append unixtimestamp from//
            _sb.Append($"{dt.ToUnixSeconds()}");
        }

        public YahooRequestBuilder Tickers(string[] tickers)
        {
            if (tickers.Length <= 0)
                return null;

            TickersCollection.AddRange(tickers);
            return this;
        }

        public YahooRequestBuilder Ticker(string ticker)
        {
            if (ticker.Length <= 0)
                return null;

            TickersCollection.Add(ticker);
            return this;
        }


        public YahooRequestBuilder Tickers(ICollection<string> tickers)
        {
            if (tickers.Count <= 0)
                return null;

            TickersCollection.AddRange(tickers);
            return this;
        }

        private void AppendTo(DateTime dt)
        {
            //append &
            _sb.Append(YahooFieldsList.AND);

            //append keyword period2
            _sb.Append(YahooFieldsList.TO);

            //append =
            _sb.Append(YahooFieldsList.EQUALS);

            //Append unixtimestamp from//
            _sb.Append($"{dt.ToUnixSeconds()}");
        }

        public YahooRequestBuilder SetCrumb(string crumb)
        {
            _crumb = crumb;
            return this;
        }

        public YahooRequestBuilder To(DateTime? dt)
        {
            if (dt <= DateTime.MinValue)
                dt = DateTime.Today;

            AppendTo(dt.Value);
            return this;
        }

        public YahooRequestBuilder BuildUrls()
        {
            string replacableUrl = null;

            foreach (var ticker in TickersCollection)
            {
                var baseUrl = Settings.Default.SingleBaseUrl;
                if (_type == YahooRequestType.Single)
                {
                    baseUrl += "=";
                    baseUrl += ticker;

                    //&f= fields, n=name, o=open, p = previous close, s=symbol, d1=asof
                    baseUrl += "&f=nopsd1";
                    Urls.Add(new YahooUrlItem(baseUrl, ticker));
                }
                else
                {
                    if (replacableUrl == null)
                    {
                        // history appendix for url
                        _sb.Append($"{YahooFieldsList.AND}{YahooFieldsList.HISTORY}");

                        //crumb stamp
                        _sb.Append($"{YahooFieldsList.AND}{YahooFieldsList.CRUMB}{YahooFieldsList.EQUALS}{_crumb}");

                        //store replaceable URl
                        replacableUrl = _sb.ToString();
                    }
                    
                    var url = replacableUrl.Replace("@", ticker);
                    Urls.Add(new YahooUrlItem(url, ticker));
                }
            }
            return this;
        }


        public YahooRequestBuilder Interval(YahooRequestInterval intervalType)
        {
            _sb.Append($"{YahooFieldsList.AND}{YahooFieldsList.INTERVAL}=1{intervalType.GetAttribute<YahooChar>().Name}");
            return this;
        }
    }

    public class YahooUrlItem : Tuple<string, string>
    {
        public string Url => Item1;
        public string Ticker => Item2;

        public YahooUrlItem(string url, string ticker) : base(url, ticker)
        {

        }
    }
}
