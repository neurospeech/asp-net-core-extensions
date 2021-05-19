using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace NeuroSpeech.EFCoreLiveMigration
{
    public static class SqlServerMigrationExtensions
    {
        public static ModelMigrationBase MigrateSqlServer(this DbContext context)
        {
            return new ModelMigration(context);
        }
    }

}
