using Azure.Data.Tables;
using System;

namespace NeuroSpeech.Eternity
{
    public static class TableEntityExtensions
    {
        public static TableEntity ToTableEntity<T>(this T item, string partitionKey, string rowKey)
            where T : class, new()
        {
            Type type = typeof(T);
            var entity = new TableEntity(partitionKey, rowKey);
            foreach (var property in type.GetProperties())
            {
                if (property.CanRead && property.CanWrite)
                {
                    Type propertyType = property.PropertyType;
                    if (propertyType.IsEnum)
                    {
                        entity.Add(property.Name, propertyType.GetEnumName(property.GetValue(item)));
                        continue;
                    }
                    entity.Add(property.Name, property.GetValue(item));
                }
            }
            return entity;
        }

        public static T ToObject<T>(this TableEntity entity)
        {
            Type type = typeof(T);
            var result = Activator.CreateInstance<T>();
            foreach (var property in type.GetProperties())
            {
                if (property.CanRead && property.CanWrite)
                {
                    if (entity.TryGetValue(property.Name, out var text))
                    {
                        Type propertyType = property.PropertyType;
                        if (propertyType.IsEnum)
                        {
                            property.SetValue(result, Enum.Parse(propertyType, text.ToString()));
                            continue;
                        }
                        property.SetValue(result, text);
                    }
                }
            }
            return result;
        }
    }
}
