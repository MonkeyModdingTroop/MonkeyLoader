using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Components
{
    public static class EntityExtensions
    {
        public static void Add<TEntity>(this TEntity entity, IComponent<TEntity> component)
            where TEntity : IEntity<TEntity>
            => entity.Components.Add(component);
    }
}