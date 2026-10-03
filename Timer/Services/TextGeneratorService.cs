using System;
using System.Collections.Generic;
using System.Linq;
namespace Timer.Services;
public sealed class TextGeneratorService
{
    public static readonly IReadOnlyList<string> Roles = Array.AsReadOnly(new[] { "TOP", "JUG", "MID", "AD", "SUP" });
    public string Generate(IReadOnlyDictionary<string, long> recoverySeconds, long currentGameSeconds, bool mayhemMode = false) =>
        string.Join(" ", Roles.Where(role => recoverySeconds.TryGetValue(role, out long time) && time > currentGameSeconds)
            .Select(role => $"{Label(role, mayhemMode)}{recoverySeconds[role] / 60:00}{recoverySeconds[role] % 60:00}"));
    public static string Label(string role, bool mayhemMode) => mayhemMode ? (Array.IndexOf(Roles.ToArray(), role) + 1) + "楼" : role.ToLowerInvariant();
}
