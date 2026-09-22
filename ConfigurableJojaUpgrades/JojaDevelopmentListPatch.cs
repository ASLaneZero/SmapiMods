using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;

namespace ConfigurableJojaPrices;

[HarmonyPatch]
internal static class JojaDevelopmentListConstructorPatch
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods()
    {
        return AccessTools.GetDeclaredConstructors(typeof(JojaCDMenu));
    }

    [HarmonyPostfix]
    private static void AfterConstructor(JojaCDMenu __instance)
    {
        JojaDevelopmentListMenu.Layout(__instance);
    }
}

[HarmonyPatch(typeof(JojaCDMenu), nameof(JojaCDMenu.draw))]
internal static class JojaDevelopmentListDrawPatch
{
    [HarmonyPrefix]
    private static bool BeforeDraw(JojaCDMenu __instance, SpriteBatch b)
    {
        JojaDevelopmentListMenu.Draw(__instance, b);
        return false;
    }
}