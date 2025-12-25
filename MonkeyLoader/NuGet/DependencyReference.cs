using NuGet.Packaging.Core;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MonkeyLoader.NuGet
{
    public sealed class DependencyReference
    {
        private bool _allDependenciesLoaded = false;

        [MemberNotNullWhen(true, nameof(LoadedPackage))]
        public bool AllDependenciesLoaded
        {
            get
            {
                // If the thread is already in a lock(this)
                if (Monitor.IsEntered(this))
                    return IsLoaded;

                lock (this)
                {
                    if (!_allDependenciesLoaded)
                        _allDependenciesLoaded = IsLoaded && LoadedPackage.AllDependenciesLoaded;

                    return _allDependenciesLoaded;
                }
            }
        }

        public PackageDependency Dependency { get; }

        public string Id => Dependency.Id;

        [MemberNotNullWhen(true, nameof(LoadedPackage))]
        public bool IsLoaded => LoadedPackage is not null || HasLoadedAssembly;

        public ILoadedNuGetPackage? LoadedPackage { get; private set; }

        public NuGetManager NuGet { get; }

        private bool HasLoadedAssembly
        {
            // Todo: adjust this?
            get
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(assembly => Id.Equals(assembly.GetName().Name, StringComparison.OrdinalIgnoreCase));

                if (assembly is null)
                    return false;

                LoadedPackage = new LoadedNuGetPackage(new PackageIdentity(assembly.GetName().Name, new NuGetVersion(assembly.GetName().Version ?? Version.Parse("1.0"))), NuGetHelper.Framework);
                return true;
            }
        }

        internal DependencyReference(NuGetManager nuGetManager, PackageDependency dependency)
        {
            NuGet = nuGetManager;
            Dependency = dependency;
        }

        /// <summary>
        /// Determines whether this (transitively) references the given <see cref="ILoadedNuGetPackage">package</see>.
        /// </summary>
        /// <param name="package"></param>
        /// <returns><c>true</c> if this (transitively) references the given package; otherwise, <c>false</c>.</returns>
        public bool TransitivelyReferences(ILoadedNuGetPackage package) => TransitivelyReferences(package.Identity.Id);

        /// <summary>
        /// Determines whether this (transitively) references a package with the given id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns><c>true</c> if this (transitively) references a package with the given id; otherwise, <c>false</c>.</returns>
        public bool TransitivelyReferences(string id)
            => TransitivelyReferences(id, []);

        [MemberNotNullWhen(true, nameof(LoadedPackage))]
        public bool TryResolve()
        {
            if (IsLoaded)
                return true;

            if (!NuGet.TryResolve(Id, out var package))
                return false;

            LoadedPackage = package;
            return true;
        }

        private bool TransitivelyReferences(string id, HashSet<string> visited)
        {
            if (visited.Contains(Id))
                return false;

            visited.Add(Id);

            return Id == id || (TryResolve() && LoadedPackage.Dependencies.Any(d => d.TransitivelyReferences(id, visited)));
        }
    }
}