using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;
using StardewValley.Triggers;

namespace ASLane.WarpSickness
{
    internal sealed class ModEntry : Mod
    {
        private static ModEntry? Instance;

        // The Content Patcher asset for warp item IDs.
        private const string WarpItemsAssetName =
            "Mods/ASLane.WS/WarpItems";

        // Cached copy of the above.
        private HashSet<string>? cachedWarpItemIds;

        /// <param name="helper">SMAPI's mod helper.</param>
        public override void Entry(IModHelper helper)
        {
            Instance = this;

            helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
            helper.Events.Content.AssetsInvalidated +=
                this.OnAssetsInvalidated;

            Harmony harmony = new(this.ModManifest.UniqueID);

            harmony.Patch(
                original: AccessTools.Method(
                    typeof(StardewValley.Object),
                    nameof(StardewValley.Object.performUseAction),
                    new[] { typeof(GameLocation) }
                ),
                postfix: new HarmonyMethod(
                    typeof(ModEntry),
                    nameof(AfterObjectPerformUseAction)
                )
            );

            harmony.Patch(
                original: AccessTools.Method(
                    typeof(Wand),
                    nameof(Wand.DoFunction),
                    new[]
                    {
                        typeof(GameLocation),
                        typeof(int),
                        typeof(int),
                        typeof(int),
                        typeof(Farmer)
                    }
                ),
                postfix: new HarmonyMethod(
                    typeof(ModEntry),
                    nameof(AfterWandDoFunction)
                )
            );
        }

        // Warp Totem use.
        private static void AfterObjectPerformUseAction(
            StardewValley.Object __instance,
            bool __result
        )
        {
            if (!__result || Instance is null)
                return;

            Instance.HandleSuccessfulWarpItemUse(__instance);
        }

        // Return Scepter use.
        private static void AfterWandDoFunction(
            Wand __instance,
            Farmer who
        )
        {
            if (Instance is null || !who.IsLocalPlayer)
                return;

            Instance.HandleSuccessfulWarpItemUse(__instance);
        }

        // Warp Sickness application.
        private void HandleSuccessfulWarpItemUse(Item item)
        {
            if (!Context.IsWorldReady)
                return;

            if (!this.GetWarpItemIds().Contains(item.QualifiedItemId))
                return;

            this.ApplyWarpSickness();

            this.Monitor.Log(
                $"Successful use of {item.QualifiedItemId}; "
                + "applied warp sickness.",
                LogLevel.Info
            );
        }

        // Title Screen state clear.
        private void OnReturnedToTitle(
            object? sender,
            ReturnedToTitleEventArgs e
        )
        {
            this.cachedWarpItemIds = null;
        }

        // Warp item list expiry.
        private void OnAssetsInvalidated(
            object? sender,
            AssetsInvalidatedEventArgs e
        )
        {
            bool warpItemAssetChanged =
                e.NamesWithoutLocale.Any(
                    name => name.IsEquivalentTo(WarpItemsAssetName)
                );

            if (!warpItemAssetChanged)
                return;

            this.cachedWarpItemIds = null;

            this.Monitor.Log(
                "The supported warp-item list was invalidated; "
                + "it will be reloaded when next needed.",
                LogLevel.Trace
            );
        }

        // Warp item list update.

        private HashSet<string> GetWarpItemIds()
        {
            if (this.cachedWarpItemIds is null)
            {
                Dictionary<string, string> entries =
                    this.Helper.GameContent.Load<
                        Dictionary<string, string>
                    >(WarpItemsAssetName);

                this.cachedWarpItemIds =
                    entries.Keys.ToHashSet(
                        StringComparer.OrdinalIgnoreCase
                    );

                this.Monitor.Log(
                    $"Loaded {this.cachedWarpItemIds.Count} "
                    + "supported warp-item IDs.",
                    LogLevel.Trace
                );
            }

            return this.cachedWarpItemIds;
        }

        // Applies debuffs
        private void ApplyWarpSickness()
        {
            this.RunTriggerAction(
                "AddBuff ASLane.WS_WarpSickness 15000"
            );

            this.RunTriggerAction(
                "AddBuff 26 15000"
            );

            this.RunTriggerAction(
                "AddBuff 25 60000"
            );
        }

        // Runs trigger action.
        private void RunTriggerAction(string action)
        {
            bool succeeded =
                TriggerActionManager.TryRunAction(
                    action,
                    out string error,
                    out Exception exception
                );

            if (!succeeded)
            {
                this.Monitor.Log(
                    $"Couldn't run trigger action '{action}': "
                    + $"{error}\n{exception}",
                    LogLevel.Error
                );
            }
        }
    }
}
