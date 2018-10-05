using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trading.DataStructures.Interfaces;

namespace Trading.Backtest.Data.Models
{
    public class PortfolioValuationEf : IPortfolioValuation
    {
        public PortfolioValuationEf()
        {

        }

        public PortfolioValuationEf(IPortfolioValuation portfolioValuation)
        {
            PortfolioAsof = portfolioValuation.PortfolioAsof;
            PortfolioValue = portfolioValuation.PortfolioValue;
            AllocationToRisk = portfolioValuation.AllocationToRisk;
        }

        /// <summary>
        /// Der primary Key des Tables - Das Datum des Portfolios
        /// </summary>
        [Key]
        public DateTime PortfolioAsof { get; set; }

        /// <summary>
        /// der Wert zum Zeitpunkt
        /// </summary>
        public decimal PortfolioValue { get; set; }

        /// <summary>
        /// Der Investitionsgrad in Risikoreiche Wertpapiere (Aktienquote)
        /// </summary>
        public decimal AllocationToRisk { get; set; }

    }
}
