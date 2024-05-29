using MonkeyLoader.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// Implements a base for plain tags for <see cref="ConfigSection"/>s and <see cref="IDefiningConfigKey"/>s.
    /// </summary>
    public abstract class ConfigTag : Tag, IConfigTag
    { }

    /// <summary>
    /// Defines the interface for tags for <see cref="ConfigSection"/>s and <see cref="IDefiningConfigKey"/>s.
    /// </summary>
    /// <inheritdoc/>
    public interface IConfigTag : ITag
    { }
}