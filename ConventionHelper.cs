using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace efcore_transactions;

public delegate string RenamingConvention(string defaultName);

public static class ConventionHelper
{
    public static void SetColumnNamesByIfConventionNotSet(
        this ModelBuilder modelBuilder,
        RenamingConvention convention)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Since the table name is set via a convention when the model
            // is initially created, we can't reliably check if the value has been overwritten.
            // However, with EF Core 7 we can define a custom convention to override that.
            if (false)
            {
                var tableName = entity.GetTableName();
                var tableName1 = entity.FindAnnotation(RelationalAnnotationNames.TableName);
                if (tableName1 is null)
                    entity.SetTableName(entity.GetDefaultTableName());
            }

            foreach (var p in entity.GetProperties())
            {
                var name = p.FindAnnotation(RelationalAnnotationNames.ColumnName)?.Value;
                if (name is null)
                {
                    var defaultName = p.GetDefaultColumnName();
                    var conventionalName = convention(defaultName);
                    p.SetColumnName(conventionalName);
                }
            }
            
            foreach (var k in entity.GetKeys())
            {
                // Cannot check if it's been set without copy-pasting too much, so just check for the default.
                var defaultName = k.GetDefaultName();
                var name = k.GetName();
                if (defaultName is not null && name == defaultName)
                {
                    var conventionalName = convention(defaultName);
                    k.SetName(conventionalName);
                }
            } 
            
            foreach (var fk in entity.GetForeignKeys())
            {
                var name = fk.FindAnnotation(RelationalAnnotationNames.Name)?.Value;
                if (name is null)
                {
                    var defaultName = fk.GetDefaultName() ?? throw new NotImplementedException();
                    var conventionalName = convention(defaultName);
                    fk.SetConstraintName(conventionalName);
                }
            }
            
            foreach (var i in entity.GetIndexes())
            {
                var name = i.FindAnnotation(RelationalAnnotationNames.Name)?.Value ?? i.Name;
                if (name is null)
                {
                    var defaultName = i.GetDefaultDatabaseName() ?? throw new NotImplementedException();
                    var conventionalName = convention(defaultName);
                    i.SetDatabaseName(conventionalName);
                }
            }
        }
    }
}
