using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Triggers;

namespace ASLane.WarpSickness
{
    internal sealed class ModEntry : Mod
    {
        /// <summary>
        /// The Content Patcher asset containing the supported warp item IDs.
        /// </summary>
        private const string WarpItemsAssetName =
            "Mods/ASLane.WS/WarpItems";

        /// <summary>
        /// A cached copy of warp item IDs.
        /// </summary>
        private HashSet<string>? cachedWarpItemIds;

        /// <summary>
        /// When the player most recently attempted to use a supported warp item.
        /// </summary>
        private DateTime? pendingWarpStartedAt;

        /// <summary>
        /// How long to cache the above before discarding.
        /// </summary>
        private static readonly TimeSpan WarpConfirmationWindow =
        TimeSpan.FromSeconds(10);

        /// <param name="helper">SMAPI's mod helper.</param>
        public override void Entry(IModHelper helper)
        {
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Player.Warped += this.OnWarped;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
            helper.Events.Content.AssetsInvalidated +=
                this.OnAssetsInvalidated;
        }

        /// <summary>
        /// Detects the player attempting to use a supported warp item.
        /// </summary>
        private void OnButtonPressed(
            object? sender,
            ButtonPressedEventArgs e
        )
        {
            if (!Context.IsWorldReady)
                return;

            if (!e.Button.IsUseToolButton())
                return;

            Item? item = Game1.player.CurrentItem;

            if (item is null)
                return;

            HashSet<string> supportedItems = this.GetWarpItemIds();

            if (!supportedItems.Contains(item.QualifiedItemId))
                return;

            this.pendingWarpStartedAt = DateTime.UtcNow;

            this.Monitor.Log(
                $"Detected activation of {item.QualifiedItemId}; "
                + "waiting to confirm a warp.",
                LogLevel.Trace
            );
        }

        /// <summary>
        /// Applies the debuffs after a supported item causes a warp.
        /// </summary>
        private void OnWarped(
    object? sender,
    WarpedEventArgs e
)
        {
            if (!e.Player.IsLocalPlayer)
                return;

            if (this.pendingWarpStartedAt is null)
                return;

            TimeSpan elapsed =
                DateTime.UtcNow - this.pendingWarpStartedAt.Value;

            if (elapsed <= WarpConfirmationWindow)
            {
                this.ApplyWarpSickness();

                this.Monitor.Log(
                    $"Applied warp sickness after arriving in "
                    + $"{e.NewLocation.NameOrUniqueName}.",
                    LogLevel.Trace
                );
            }

            this.pendingWarpStartedAt = null;
        }

        private void OnUpdateTicked(
            object? sender,
            UpdateTickedEventArgs e
        )
        {
            if (this.pendingWarpStartedAt is null)
                return;

            TimeSpan elapsed =
                DateTime.UtcNow - this.pendingWarpStartedAt.Value;

            if (elapsed > WarpConfirmationWindow)
                this.pendingWarpStartedAt = null;
        }

        /// <summary>
        /// Clears temporary state when returning to the title screen.
        /// </summary>
        private void OnReturnedToTitle(
            object? sender,
            ReturnedToTitleEventArgs e
        )
        {
            this.pendingWarpStartedAt = null;
            this.cachedWarpItemIds = null;
        }

        /// <summary>
        /// Updates the cached warp item list from Content Patcher.
        /// </summary>
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

        /// <summary>
        /// Gets the item IDs registered in the Content Patcher asset.
        /// </summary>
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

        /// <summary>
        /// Applies the Warp Sickness debuffs
        /// </summary>
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

        /// <summary>
        /// Runs trigger action.
        /// </summary>
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