using HarmonyLib;

namespace MonkeyLoader
{
    public interface IPrioritizable
    {
        public int Priority { get; }
    }
}