namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// A component of a <see cref="IDefiningConfigKey"/>, e.g. description or range.
    /// A component cannot be removed from a config key once it was added.
    /// </summary>
    public interface IConfigKeyComponent<in TKey> where TKey : IDefiningConfigKey
    {
        /// <summary>
        /// Initialized this component when it is added to a <see cref="IDefiningConfigKey"/>.
        /// </summary>
        /// <param name="config">The <see cref="IDefiningConfigKey"/> this component was added to.</param>
        public void Initialize(TKey config);
    }
}