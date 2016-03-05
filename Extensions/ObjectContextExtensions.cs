using System;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Linq;

namespace Damasio34.GraphDiff.Extensions
{
    public static class ObjectContextExtensions
    {

        public static EntitySet TryGetEntitySet<T>(this ObjectContext objectContext, T entity)
        {
            return TryGetEntitySet(objectContext, entity.GetType());
        }

        public static EntitySet TryGetEntitySet(this ObjectContext objectContext, Type entityType)
        {
            var entityContainter = objectContext.MetadataWorkspace.GetEntityContainer(objectContext.DefaultContainerName,
                DataSpace.CSpace);

            var entitySet = entityContainter.EntitySets.SingleOrDefault(x => x.ElementType.Name == entityType.Name);

            if (entitySet != null)
                return entitySet;
            else
            {
                // tenta descobrir pela herança
                return entityType.BaseType != null ? TryGetEntitySet(objectContext, entityType.BaseType) : null;
            }
        }

    }
}
