using System.Reflection;
using System.Globalization;
using System.Text.RegularExpressions;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Menus;

namespace ConfigurableJojaPrices;

internal sealed class ModEntry : Mod
{

    internal static ModConfig ActiveConfig { get; private set; } = new();

    public override void Entry(IModHelper helper)
    {
        ActiveConfig = helper.ReadConfig<ModConfig>();
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

        // Error handling
        MethodInfo? target = AccessTools.Method(typeof(JojaCDMenu), "getPriceFromButtonNumber");
        if (target is null)
        {
            this.Monitor.Log(
                "Couldn't find JojaCDMenu.getPriceFromButtonNumber. The game may have changed; no patches were applied.",
                LogLevel.Error
            );
            return;
        }

        var harmony = new Harmony(this.ModManifest.UniqueID);
        harmony.Patch(
            original: target,
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(AfterGetPrice))
        );
        harmony.CreateClassProcessor(typeof(JojaMartPricePatches)).Patch();
        harmony.CreateClassProcessor(typeof(JojaDevelopmentListConstructorPatch)).Patch();
        harmony.CreateClassProcessor(typeof(JojaDevelopmentListDrawPatch)).Patch();
        harmony.CreateClassProcessor(typeof(TheaterDeclinePatch)).Patch();

        this.Monitor.Log("Configurable Joja prices loaded from config.json.", LogLevel.Trace);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        // Register Content Patcher tokens
        IContentPatcherApi? contentPatcher =
            this.Helper.ModRegistry.GetApi<IContentPatcherApi>(
                "Pathoschild.ContentPatcher"
            );

        contentPatcher?.RegisterToken(
            this.ModManifest,
            "MovieTheaterPrice",
            () => new[]
            {
            ActiveConfig.MovieTheater.ToString(CultureInfo.InvariantCulture)
            }
        );

        contentPatcher?.RegisterToken(
            this.ModManifest,
            "MovieTheaterPriceFormatted",
            () => new[]
            {
            ActiveConfig.MovieTheater.ToString(
                "N0",
                CultureInfo.InvariantCulture
            )
            }
        );

        // Existing GMCM registration
        IGenericModConfigMenuApi? gmcm =
            this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(
                "spacechase0.GenericModConfigMenu"
            );

        if (gmcm is null)
            return;

        gmcm.Register(
            mod: this.ModManifest,
            reset: () => ActiveConfig = new ModConfig(),
            save: () => this.Helper.WriteConfig(ActiveConfig)
        );

        gmcm.AddSectionTitle(
            this.ModManifest,
            () => this.Helper.Translation.Get("config.section.membership")
        );
        this.AddPriceOption(gmcm, () => ActiveConfig.Membership, value => ActiveConfig.Membership = value,
            "config.membership.name", "config.membership.description");

        gmcm.AddSectionTitle(
            this.ModManifest,
            () => this.Helper.Translation.Get("config.section.developments")
        );
        this.AddPriceOption(gmcm, () => ActiveConfig.Minecarts, value => ActiveConfig.Minecarts = value,
            "config.minecarts.name", "config.minecarts.description");
        this.AddPriceOption(gmcm, () => ActiveConfig.Panning, value => ActiveConfig.Panning = value,
            "config.panning.name", "config.panning.description");
        this.AddPriceOption(gmcm, () => ActiveConfig.QuarryBridge, value => ActiveConfig.QuarryBridge = value,
            "config.quarry-bridge.name", "config.quarry-bridge.description");
        this.AddPriceOption(gmcm, () => ActiveConfig.Greenhouse, value => ActiveConfig.Greenhouse = value,
            "config.greenhouse.name", "config.greenhouse.description");
        this.AddPriceOption(gmcm, () => ActiveConfig.Bus, value => ActiveConfig.Bus = value,
            "config.bus.name", "config.bus.description");

        gmcm.AddSectionTitle(
            this.ModManifest,
            () => this.Helper.Translation.Get("config.section.theater")
        );
        this.AddPriceOption(gmcm, () => ActiveConfig.MovieTheater, value => ActiveConfig.MovieTheater = value,
            "config.theater.name", "config.theater.description");
    }

    private void AddPriceOption(
        IGenericModConfigMenuApi gmcm,
        Func<int> getValue,
        Action<int> setValue,
        string nameKey,
        string tooltipKey
    )
    {
        gmcm.AddTextOption(
            mod: this.ModManifest,
            getValue: () => getValue().ToString(CultureInfo.InvariantCulture),
            setValue: value =>
            {
                if (int.TryParse(
                    value,
                    NumberStyles.Integer | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture,
                    out int parsed
                ) && parsed >= 0)
                {
                    setValue(parsed);
                }
            },
            name: () => this.Helper.Translation.Get(nameKey),
            tooltip: () => this.Helper.Translation.Get(tooltipKey)
        );
    }

    private static void AfterGetPrice(ref int __result)
    {
        __result = __result switch
        {
            15_000 => ActiveConfig.Minecarts,
            20_000 => ActiveConfig.Panning,
            25_000 => ActiveConfig.QuarryBridge,
            35_000 => ActiveConfig.Greenhouse,
            40_000 => ActiveConfig.Bus,
            _ => __result
        };
    }
}