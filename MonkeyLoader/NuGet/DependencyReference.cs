﻿using NuGet.Packaging.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.NuGet
{
    public sealed class DependencyReference
    {
        private bool _allDependenciesLoaded = false;

        private bool _checkingDependencies = false;

        [MemberNotNullWhen(true, nameof(LoadedPackage))]
        public bool AllDependenciesLoaded
        {
            get
            {
                lock (this)
                {
                    // Must be loaded if recursing
                    if (_checkingDependencies)
                        return IsLoaded;

                    _checkingDependencies = true;

                    if (!_allDependenciesLoaded)
                        _allDependenciesLoaded = IsLoaded && LoadedPackage.AllDependenciesLoaded;

                    _checkingDependencies = false;

                    return _allDependenciesLoaded;
                }
            }
        }

        public PackageDependency Dependency { get; }
        public string Id => Dependency.Id;

        [MemberNotNullWhen(true, nameof(LoadedPackage))]
        public bool IsLoaded => LoadedPackage is not null;

        public ILoadedNuGetPackage? LoadedPackage { get; private set; }
        public NuGetManager NuGet { get; }

        internal DependencyReference(NuGetManager nuGetManager, PackageDependency dependency)
        {
            NuGet = nuGetManager;
            Dependency = dependency;
        }

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
    }
}