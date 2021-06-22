using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroSpeech.EFCoreLiveMigration
{
    public static class IdentityExtensions
    {

        public static bool IsIdentityColumn(this IProperty property, IEntityType source)
        {
            if(property.GetValueGenerationStrategy() == SqlServerValueGenerationStrategy.IdentityColumn)
            {
                var entity = source;
                var baseEntity = source.BaseType;
                if (baseEntity == null)
                {
                    return true;
                }
                var entityTable = entity.GetTableName();
                var baseEntityTable = baseEntity.GetTableName();
                return entityTable == baseEntityTable;
            }
            return false;
        }

    }
}
