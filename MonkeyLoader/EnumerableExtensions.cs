using EnumerableToolkit;
using HarmonyLib;
using MonkeyLoader;
using NuGet.Protocol.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

[assembly: TypeForwardedTo(typeof(TryConverter<,>))]
[assembly: TypeForwardedTo(typeof(EnumerableExtensions))]