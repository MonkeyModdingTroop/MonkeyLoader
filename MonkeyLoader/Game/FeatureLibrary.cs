﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Game
{
    public static class FeatureLibrary<TFeature> where TFeature : GameFeature, new()
    {
        private static readonly Lazy<TFeature> _instance = new();

        public static TFeature Instance => _instance.Value;
    }
}