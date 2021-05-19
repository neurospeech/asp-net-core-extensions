using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Collections.Generic;
using System.Linq;

namespace NeuroSpeech.EFCoreLiveMigration
{
    public class SqlIndexEx
    {
        public readonly string Name;
        public readonly IEntityType DeclaringEntityType;
        public readonly IReadOnlyList<IProperty> Properties;
        public readonly IReadOnlyList<string> IncludedProperties;
        public readonly string Filter;

        public SqlIndexEx(IIndex index, ModelMigrationBase modelMigration)
        {
            this.Name = index.GetName();
            this.DeclaringEntityType = index.DeclaringEntityType;
            this.Properties = index.Properties;
            this.IncludedProperties = index.GetIncludeProperties();
            this.Filter = index.GetFilter();

            if(this.Filter == null)
            {
                // create filter based on nullable foreign key...
                
                if(index.DeclaringEntityType.BaseType != null)
                {
                    this.Filter = "(" + string.Join(" AND ", 
                        Properties.Select(x => modelMigration.Escape(x.GetColumnName()) + " IS NOT NULL" )
                        ) + ")";
                }
            }
        }

        public string GetName() => Name;
        public string GetFilter() => Filter;
    }
}