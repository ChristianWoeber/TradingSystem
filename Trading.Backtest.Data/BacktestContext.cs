using Trading.Backtest.Data.Models;

namespace Trading.Backtest.Data
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class BacktestContext : DbContext
    {
        public BacktestContext()
            : base("name=BacktestContext")
        {
            //ich erstelle die Datenbank immer neu, damit hab ich pro backtest eine Datenbank
            //wenn der Backtest ok, dann exportiere ich die Settings + DB ?
            //DropCreateDatabaseAlways

            Database.SetInitializer(new CreateDatabaseIfNotExists<BacktestContext>());
        }


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }

        public DbSet<TransactionEf> Transactions { get; set; }

        public DbSet<PortfolioValuationEf> PortfolioValueHistory { get; set; }

    }
}
