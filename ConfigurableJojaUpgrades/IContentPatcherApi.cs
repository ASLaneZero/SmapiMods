using StardewModdingAPI;

namespace ConfigurableJojaPrices;

public interface IContentPatcherApi
{
    void RegisterToken(
        IManifest mod,
        string name,
        Func<IEnumerable<string>?> getValue
    );
}