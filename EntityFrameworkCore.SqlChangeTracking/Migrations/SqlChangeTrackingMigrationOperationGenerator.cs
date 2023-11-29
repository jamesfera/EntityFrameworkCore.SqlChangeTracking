using EntityFrameworkCore.SqlChangeTracking.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System.Reflection;

namespace EntityFrameworkCore.SqlChangeTracking.Migrations
{
    public class SqlChangeTrackingMigrationOperationGenerator(CSharpMigrationOperationGeneratorDependencies dependencies)
        : CSharpMigrationOperationGenerator(dependencies)
    {
        protected ICSharpHelper Code
            => Dependencies.CSharpHelper;

        protected override void Generate(MigrationOperation operation, IndentedStringBuilder builder)
        {
            var typesToScan = new List<Type>();

            var type = GetType();

            while (type != typeof(CSharpMigrationOperationGenerator) && type is not null)
            {
                typesToScan.Add(type);
                type = type.BaseType;
            }

            var generateMethod = typesToScan
                .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
                .Where(m => m.Name == nameof(Generate))
                .FirstOrDefault(m => m.GetParameters().Select(p => p.ParameterType).Contains(operation.GetType()));

            if (generateMethod is not null)
                generateMethod.Invoke(this, new object?[] { operation, builder });
            else
                base.Generate(operation, builder);
        }

        void Generate(EnableChangeTrackingForTableOperation operation, IndentedStringBuilder builder)
        {
            builder.AppendLine($".{nameof(MigrationBuilderExtensions.EnableChangeTrackingForTable)}(");

            using (builder.Indent())
            {
                builder
                    .Append("table: ")
                    .Append(Code.Literal(operation.Table));

                if (operation.Schema != null)
                {
                    builder
                        .AppendLine(",")
                        .Append("schema: ")
                        .Append(Code.Literal(operation.Schema));
                }

                if (operation.TrackColumns)
                {
                    builder
                        .AppendLine(",")
                        .Append("trackColumns: ")
                        .Append(Code.Literal(operation.TrackColumns));
                }

                builder.Append(")");
            }
        }

        void Generate(DisableChangeTrackingForTableOperation operation, IndentedStringBuilder builder)
        {
            builder.AppendLine($".{nameof(MigrationBuilderExtensions.DisableChangeTrackingForTable)}(");

            using (builder.Indent())
            {
                builder
                    .Append("table: ")
                    .Append(Code.Literal(operation.Table));

                if (operation.Schema != null)
                {
                    builder
                        .AppendLine(",")
                        .Append("schema: ")
                        .Append(Code.Literal(operation.Schema));
                }

                builder.Append(")");
            }
        }

        void Generate(EnableChangeTrackingForDatabaseOperation operation, IndentedStringBuilder builder)
        {
            builder.AppendLine($".{nameof(MigrationBuilderExtensions.EnableChangeTrackingForDatabase)}(");

            using (builder.Indent())
            {
                builder
                    .Append("changeRetentionDays: ")
                    .Append(Code.Literal(operation.RetentionDays));

                builder
                    .AppendLine(",")
                    .Append("autoCleanUp: ")
                    .Append(Code.Literal(operation.AutoCleanup));

                builder.Append(")");
            }
        }

        void Generate(DisableChangeTrackingForDatabaseOperation operation, IndentedStringBuilder builder)
        {
            builder.Append($".{nameof(MigrationBuilderExtensions.DisableChangeTrackingForDatabase)}()");
        }

        void Generate(SnapshotIsolationOperation operation, IndentedStringBuilder builder)
        {
            builder.AppendLine($".{nameof(MigrationBuilderExtensions.SnapshotIsolation)}(");

            using (builder.Indent())
            {
                builder
                    .Append("enabled: ")
                    .Append(Code.Literal(operation.Enabled));

                builder.Append(")");
            }
        }
    }
}
