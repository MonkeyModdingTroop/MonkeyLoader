using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MonkeyLoader.Components
{
    /// <summary>
    /// Implements a component list for <see cref="IEntity{TEntity}">entities</see>.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public sealed class ComponentList<TEntity> : IComponentList<TEntity> where TEntity : IEntity<TEntity>
    {
        /// <summary>
        /// Caches components retrieved using <see cref="Get()"/> and <see cref="TryGet{TComponent}(out TComponent)"/>.
        /// </summary>
        private readonly AnyMap _componentCache = new();

        private readonly List<IComponent<TEntity>> _components = new();

        /// <inheritdoc/>
        public TEntity Entity { get; }

        /// <summary>
        /// Creates a component list for the given entity.
        /// </summary>
        /// <param name="entity">The entity that this component list is for.</param>
        public ComponentList(TEntity entity)
        {
            Entity = entity;
        }

        /// <inheritdoc/>
        public void Add(IComponent<TEntity> component)
        {
            component.Initialize(Entity);
            _components.Add(component);
        }

        /// <inheritdoc/>
        public TComponent Get<TComponent>() where TComponent : IComponent<TEntity>
        {
            if (!TryGet<TComponent>(out var component))
                throw new ComponentNotFoundException();

            return component;
        }

        /// <inheritdoc/>
        public TComponent Get<TComponent>(Predicate<TComponent> predicate) where TComponent : IComponent<TEntity>
        {
            if (!TryGet(predicate, out var component))
                throw new ComponentNotFoundException();

            return component;
        }

        /// <inheritdoc/>
        public IEnumerable<TComponent> GetAll<TComponent>() where TComponent : IComponent<TEntity>
            => _components.SelectCastable<IComponent<TEntity>, TComponent>();

        /// <inheritdoc/>
        public IEnumerable<TComponent> GetAll<TComponent>(Predicate<TComponent> predicate)
            where TComponent : IComponent<TEntity>
            => GetAll<TComponent>().Where(component => predicate(component));

        /// <inheritdoc/>
        public bool TryGet<TComponent>([NotNullWhen(true)] out TComponent? component)
            where TComponent : IComponent<TEntity>
        {
            if (_componentCache.TryGetValue(out component!))
                return true;

            var all = GetAll<TComponent>();
            if (!all.Any())
                return false;

            // Cache never needs to be invalidated because we can only add
            // components and we only care about the first one matching.
            component = all.First();
            _componentCache.Add(component);

            return true;
        }

        /// <inheritdoc/>
        public bool TryGet<TComponent>(Predicate<TComponent> predicate, [NotNullWhen(true)] out TComponent? component)
            where TComponent : IComponent<TEntity>
        {
            component = default;

            var all = GetAll(predicate);
            if (!all.Any())
                return false;

            // Cache never needs to be invalidated because we can only add
            // components and we only care about the first one matching.
            component = all.First();
            _componentCache.Add(component);

            return true;
        }
    }

    /// <summary>
    /// Defines the interface for the component list of <see cref="IEntity{TEntity}">entities</see>.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public interface IComponentList<out TEntity> where TEntity : IEntity<TEntity>
    {
        /// <summary>
        /// Gets the entity that this component list is for.
        /// </summary>
        public TEntity Entity { get; }

        /// <summary>
        /// Adds a component to this <see cref="Entity">Entity</see>'s component list.
        /// </summary>
        /// <param name="component">The component to add.</param>
        public void Add(IComponent<TEntity> component);

        /// <summary>
        /// Gets the first component (in order of insertion) assignable to <typeparamref name="TComponent"/>.
        /// </summary>
        /// <typeparam name="TComponent">The type of the component. Should be an interface.</typeparam>
        /// <returns>The found <typeparamref name="TComponent"/>.</returns>
        /// <exception cref="ComponentNotFoundException">When there is no <typeparamref name="TComponent"/>.</exception>
        public TComponent Get<TComponent>() where TComponent : IComponent<TEntity>;

        /// <summary>
        /// Gets the first component (in order of insertion) assignable to <typeparamref name="TComponent"/>.
        /// </summary>
        /// <typeparam name="TComponent">The type of the component. Should be an interface.</typeparam>
        /// <param name="predicate">A function to test each component for a condition.</param>
        /// <returns>The found <typeparamref name="TComponent"/>.</returns>
        /// <exception cref="ComponentNotFoundException">When there is no <typeparamref name="TComponent"/> that satisfies the <paramref name="predicate"/>.</exception>
        public TComponent Get<TComponent>(Predicate<TComponent> predicate) where TComponent : IComponent<TEntity>;

        /// <summary>
        /// Gets a sequence of all components assignable to <typeparamref name="TComponent"/>, in order of insertion.
        /// </summary>
        /// <typeparam name="TComponent">The type of the component(s). Should be an interface.</typeparam>
        /// <returns>A sequence of all matching components in order of insertion.</returns>
        public IEnumerable<TComponent> GetAll<TComponent>() where TComponent : IComponent<TEntity>;

        /// <summary>
        /// Gets a sequence of all components assignable to <typeparamref name="TComponent"/>, in order of insertion.
        /// </summary>
        /// <typeparam name="TComponent">The type of the component(s). Should be an interface.</typeparam>
        /// <param name="predicate">A function to test each component for a condition.</param>
        /// <returns>A sequence of all matching components in order of insertion that satisfy the <paramref name="predicate"/>.</returns>
        public IEnumerable<TComponent> GetAll<TComponent>(Predicate<TComponent> predicate) where TComponent : IComponent<TEntity>;

        /// <summary>
        /// Tries to get the first component (in order of insertion) assignable to <typeparamref name="TComponent"/>.
        /// </summary>
        /// <typeparam name="TComponent">The type of the component. Should be an interface.</typeparam>
        /// <param name="component">The component if one was found; otherwise, <c>default(<typeparamref name="TComponent"/>)</c>.</param>
        /// <returns><c>true</c> if a <typeparamref name="TComponent"/> was found; otherwise, <c>false</c>.</returns>
        public bool TryGet<TComponent>([NotNullWhen(true)] out TComponent? component) where TComponent : IComponent<TEntity>;

        /// <summary>
        /// Tries to get the first component (in order of insertion) assignable to
        /// <typeparamref name="TComponent"/> that satisfied the <paramref name="predicate"/>.
        /// </summary>
        /// <typeparam name="TComponent">The type of the component. Should be an interface.</typeparam>
        /// <param name="component">The component if one was found; otherwise, <c>default(<typeparamref name="TComponent"/>)</c>.</param>
        /// <param name="predicate">A function to test each component for a condition.</param>
        /// <returns><c>true</c> if a <typeparamref name="TComponent"/> satisfying the <paramref name="predicate"/> was found; otherwise, <c>false</c>.</returns>
        public bool TryGet<TComponent>(Predicate<TComponent> predicate, [NotNullWhen(true)] out TComponent? component) where TComponent : IComponent<TEntity>;
    }
}