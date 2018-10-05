﻿using System;
using System.Data.Linq.Mapping;
using HelperLibrary.Trading.PortfolioManager;
using HelperLibrary.Util.Atrributes;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Database.Models
{
    public class PortfolioValuation : IPortfolioValuation
    {
        public PortfolioValuation()
        {
            
        }

        public PortfolioValuation(IPortfolioValuation portfolioValuation)
        {
            PortfolioAsof = portfolioValuation.PortfolioAsof;
            PortfolioValue = portfolioValuation.PortfolioValue;
            AllocationToRisk = portfolioValuation.AllocationToRisk;
        }

        /// <summary>
        /// Der primary Key des Tables - Das Datum des Portfolios
        /// </summary>
        [InputMapping(KeyWords = new[] {  nameof(PortfolioAsof) })]
        [Column(Storage = "PORTFOLIO_ASOF")]
        public DateTime PortfolioAsof { get; set; }

        /// <summary>
        /// der Wert zum Zeitpunkt
        /// </summary>
        [InputMapping(KeyWords = new[] { nameof(PortfolioValue) })]
        [Column(Storage = "PORTFOLIO_VALUE")]
        public decimal PortfolioValue { get; set; }

        /// <summary>
        /// Der Investitionsgrad in Risikoreiche Wertpapiere (Aktienquote)
        /// </summary>
        [InputMapping(KeyWords = new[] { nameof(AllocationToRisk) })]
        [Column(Storage = "ALLOCATION_TO_RISK")]
        public decimal AllocationToRisk { get; set; }

    }
}