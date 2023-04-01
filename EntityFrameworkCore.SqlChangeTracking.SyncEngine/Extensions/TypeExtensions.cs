using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.SqlChangeTracking.SyncEngine
{
    internal static class TypeExtensionsInternal
    {
        public static string PrettyName(this Type type)
        {
            if (type.IsGenericType)
                return $"{type.FullName.Substring(0, type.FullName.LastIndexOf("`", StringComparison.InvariantCulture))}<{string.Join(", ", type.GetGenericArguments().Select(PrettyName))}>";

            return type.FullName;
        }
    }
}

namespace EntityFrameworkCore.SqlChangeTracking.SyncEngine.Extensions
{
    public static class TypeExtensions
    {
        static Type ProcessorInterfaceType = typeof(IChangeSetBatchProcessor<,>);

        //public static bool IsChangeProcessorForType<TContext>(this Type type, Type processorType) where TContext : DbContext
        //{
        //    return type.IsChangeProcessor<TContext>() && type.GetTypesForChangeProcessor<TContext>().Any(t => t == processorType);
        //}

        public static bool IsChangeProcessor<TContext>(this Type type) where TContext : DbContext
        {
            return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == ProcessorInterfaceType && i.GenericTypeArguments[1] == typeof(TContext));
        }

        public static Type[] GetTypesForChangeProcessor<TContext>(this Type changeProcessorType) where TContext : DbContext
        {
            if (!changeProcessorType.IsChangeProcessor<TContext>())
                return new Type[0];

            return changeProcessorType.GetChangeProcessorInterfaces<TContext>().Select(i => i.GetGenericArguments()[0]).ToArray();
        }

        public static Type[] GetChangeProcessorInterfaces<TContext>(this Type changeProcessorType) where TContext : DbContext
        {
            return changeProcessorType.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == ProcessorInterfaceType).ToArray();
        }

        public static Type[] GetAssignableTypesForEntity(this Type entityType)
        {
            var typeList = new List<Type>() { entityType };

            var interfaces = entityType.GetInterfaces().Where(i => !i.IsGenericType && !i.IsGenericTypeDefinition);

            typeList.AddRange(interfaces);

            return typeList.ToArray();
        }
    }
}
