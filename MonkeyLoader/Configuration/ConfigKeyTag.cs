using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// Implements a base for plain tags for <see cref="IDefiningConfigKey"/>s.
    /// </summary>
    public abstract class ConfigKeyTag : ConfigTag
    { }

    /// <summary>
    /// Defines the interface for tags for <see cref="IDefiningConfigKey"/>s.
    /// </summary>
    /// <inheritdoc/>
    public interface IConfigKeyTag : IConfigTag
    { }
}