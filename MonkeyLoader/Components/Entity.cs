using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Components
{
    public abstract class Entity<TEntity> : IEntity<TEntity>
        where TEntity : Entity<TEntity>
    {
        public IComponentList<TEntity> Components { get; }

        protected Entity()
        {
            Components = new ComponentList<TEntity>((TEntity)this);
        }

        IEnumerator<IComponent<TEntity>> IEnumerable<IComponent<TEntity>>.GetEnumerator()
            => Components.GetAll<IComponent<TEntity>>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => Components.GetAll<IComponent<TEntity>>().GetEnumerator();
    }
}