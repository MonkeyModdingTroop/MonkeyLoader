// Adapted from the NeosModLoader project.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace MonkeyLoader
{
    internal static class Util
    {
        // check if a type is allowed to have null assigned
        internal static bool CanBeNull(this Type type) => !type.CannotBeNull();

        // check if a type cannot possibly have null assigned
        internal static bool CannotBeNull(this Type type)
            => type.IsValueType && Nullable.GetUnderlyingType(type) is null;

        /// <summary>
        /// Used to debounce calls to a given method. The given method will be called after there have been no additional calls
        /// for the given number of milliseconds.
        /// <para/>
        /// The <see cref="Action{T}"/> returned by this method has internal state used for debouncing,
        /// so you will need to store and reuse the Action for each call.
        /// </summary>
        /// <typeparam name="T">The type of the debounced method's input.</typeparam>
        /// <param name="func">The method to be debounced.</param>
        /// <param name="milliseconds">How long to wait before a call to the debounced method gets passed through.</param>
        /// <returns>A debouncing wrapper for the given method.</returns>
        // credit: https://stackoverflow.com/questions/28472205/c-sharp-event-debounce
        internal static Action<T> Debounce<T>(this Action<T> func, int milliseconds)
        {
            // this variable gets embedded in the returned Action via the magic of closures
            CancellationTokenSource? cancelTokenSource = null;

            return arg =>
            {
                // if there's already a scheduled call, then cancel it
                cancelTokenSource?.Cancel();
                cancelTokenSource = new CancellationTokenSource();

                // schedule a new call
                Task.Delay(milliseconds, cancelTokenSource.Token)
              .ContinueWith(t =>
              {
                  if (t.IsCompletedSuccessfully())
                  {
                      Task.Run(() => func(arg));
                  }
              }, TaskScheduler.Default);
            };
        }

        //credit to delta for this method https://github.com/XDelta/
        internal static string GenerateSHA256(string filepath)
        {
            using var hasher = SHA256.Create();
            using var stream = File.OpenRead(filepath);
            var hash = hasher.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "");
        }

        internal static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T>? comparer = null) => new(source, comparer);

        // shim because this doesn't exist in .NET 4.6
        private static bool IsCompletedSuccessfully(this Task task)
            => task.IsCompleted && !task.IsFaulted && !task.IsCanceled;
    }
}