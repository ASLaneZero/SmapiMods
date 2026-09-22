using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewValley.Locations;

namespace ConfigurableJojaPrices;

// Replaces Joja Membership and Movie Theater prices
[HarmonyPatch]
internal static class JojaMartPricePatches
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        const BindingFlags Flags = BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.DeclaredOnly;

        return typeof(JojaMart)
            .GetMethods(Flags)
            .Where(method =>
                !method.IsAbstract
                && !method.ContainsGenericParameters
                && method.GetMethodBody() is not null
            );
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.LoadsConstant(5_000))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = AccessTools.Method(typeof(JojaMartPricePatches), nameof(GetMembershipPrice));
            }
            else if (instruction.LoadsConstant(500_000))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = AccessTools.Method(typeof(JojaMartPricePatches), nameof(GetMovieTheaterPrice));
            }

            yield return instruction;
        }
    }

    private static int GetMembershipPrice() => ModEntry.ActiveConfig.Membership;

    private static int GetMovieTheaterPrice() => ModEntry.ActiveConfig.MovieTheater;
}
