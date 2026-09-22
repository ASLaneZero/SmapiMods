using HarmonyLib;
using StardewValley;
using StardewValley.Locations;

namespace ConfigurableJojaPrices;

[HarmonyPatch(typeof(JojaMart), "buyMovieTheater")]
internal static class TheaterDeclinePatch
{
    [HarmonyPostfix]
    private static void AfterBuyMovieTheater(int response)
    {
        if (response == 0)
            return;

        JojaMart.Morris.setNewDialogue(
            "Data\\ExtraDialogue:Morris_TheaterDeclined"
        );

        Game1.drawDialogue(JojaMart.Morris);
    }
}