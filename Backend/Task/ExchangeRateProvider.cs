﻿using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System;
using System.IO;
using System.Net;

namespace ExchangeRateUpdater
{
    public class ExchangeRateProvider
    {
        /// <summary>
        /// Should return exchange rates among the specified currencies that are defined by the source. But only those defined
        /// by the source, do not return calculated exchange rates. E.g. if the source contains "EUR/USD" but not "USD/EUR",
        /// do not return exchange rate "USD/EUR" with value calculated as 1 / "EUR/USD". If the source does not provide
        /// some of the currencies, ignore them.
        /// </summary>
        ///
        //holds list of rates
        private List<ExchangeRate> ExRates = new List<ExchangeRate>();

        //set default Currency - CZK in case of CNB
        private static Currency defaultCurrency = new Currency("CZK");

        //open URL and parse rates
        private void AddRates(String url, IEnumerable<Currency> currencies)
        {

            try
            {
                //try to open URL in StreamReader
                WebClient client = new WebClient();
                Stream stream = client.OpenRead(url);
                StreamReader reader = new StreamReader(stream);

                List<Currency> listCur = currencies.ToList();
                string line;
                //checks if list contain default currency
                bool defaultSet = listCur.Exists(x => x.Code == defaultCurrency.Code);
                if (defaultSet)
                {
                    //set number delimiter to ","
                    var numberFormatInfo = new NumberFormatInfo
                    { NumberDecimalSeparator = "," };

                    while ((line = reader.ReadLine()) != null)
                    {
                        //split and convert line into proper values
                        string[] values = line.Split('|');
                        //check if line have proper format
                        if (values.Length == 5)
                        {
                            decimal quantity = 0m;
                            decimal.TryParse(values[2], out quantity);
                            decimal rate = 0m;
                            decimal.TryParse(values[4], NumberStyles.Any, numberFormatInfo, out rate);

                            //if list of currencies caontain this currency, adds new rate to final list
                            if (listCur.Exists(x => x.Code == values[3]) && quantity > 0m && rate > 0m)
                            {
                                ExRates.Add(new ExchangeRate(new Currency(values[3]), defaultCurrency, rate / quantity));
                            }
                        }
                    }
                }
                stream.Close();
            }
            catch (WebException webex)
            {
                HttpWebResponse webResp = (HttpWebResponse)webex.Response;
                if (webex.Status == WebExceptionStatus.ProtocolError)
                {
                    Console.WriteLine("URL address: " + url + " is probably wrong or server is not working");
                }
                else
                {
                    Console.WriteLine("You are probably not connected to internet. Check connection!");
                }
            }
        }

        public IEnumerable<ExchangeRate> GetExchangeRates(IEnumerable<Currency> currencies)
        {
            //Most frequent currencies
            AddRates("https://www.cnb.cz/cs/financni-trhy/devizovy-trh/kurzy-devizoveho-trhu/kurzy-devizoveho-trhu/denni_kurz.txt", currencies);
            //Other currencies
            AddRates("https://www.cnb.cz/cs/financni-trhy/devizovy-trh/kurzy-ostatnich-men/kurzy-ostatnich-men/kurzy.txt", currencies);

            return ExRates;
        }
    }
}
