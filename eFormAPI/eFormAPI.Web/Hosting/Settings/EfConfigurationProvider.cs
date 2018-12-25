﻿using System;
using System.Linq;
using eFormAPI.Web.Infrastructure.Database;
using eFormAPI.Web.Infrastructure.Database.Seed.SeedItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace eFormAPI.Web.Hosting.Settings
{
    public class EfConfigurationProvider : ConfigurationProvider
    {
        public EfConfigurationProvider(Action<DbContextOptionsBuilder> optionsAction)
        {
            OptionsAction = optionsAction;
            Program.ReloadDbConfigurationDelegate = ReloadConfiguration;
        }

        private void ReloadConfiguration()
        {
            Load();
            OnReload();
        }

        private Action<DbContextOptionsBuilder> OptionsAction { get; }

        // Load config data from EF DB.
        public override void Load()
        {
            var builder = new DbContextOptionsBuilder<BaseDbContext>();
            OptionsAction(builder);
            try
            {
                using (var dbContext = new BaseDbContext(builder.Options))
                {
                    dbContext.Database.Migrate();
                    
                    Data = dbContext.ConfigurationValues
                        .AsNoTracking()
                        .ToDictionary(c => c.Id, c => c.Value);
                }
            }
            catch (Exception)
            {
                var seedData = ConfigurationSeed.Data;
                Data = seedData.ToDictionary(
                    item => item.Id,
                    item => item.Value);
            }
        }
    }
}