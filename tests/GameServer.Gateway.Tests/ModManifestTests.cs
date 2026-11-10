using GameServer.Modding;

namespace GameServer.Gateway.Tests;

/// <summary>A mod's manifest as LocalExile-Mods writes it: the fields the Gateway reads plus the presentation ones Waypoint
/// shows (name, description, author, compatibleClientVersions). The extra keys must not stop a mod from loading — the
/// deployed mods carry them, and a manifest the Gateway rejects would leave /mod listing a mod that never loaded.</summary>
public class ModManifestTests
{
    /// <summary>mods/bossloot/mod.json as published (1.0.4): id, name, description, author, compatibleClientVersions,
    /// kind, depends, enabled — note there is no "assembly", so the id names the DLL.</summary>
    const string BossLoot = """
    {
      "id": "bossloot",
      "name": "Boss Loot",
      "description": "An extra item from every monster of the chosen rarities.",
      "author": "LocalExile",
      "version": "1.0.4",
      "compatibleClientVersions": ["0.2.0-beta", "0.2.1-beta", "0.2.2-beta", "0.3.0-beta"],
      "kind": "feature",
      "depends": [],
      "enabled": false
    }
    """;

    [Fact]
    public void A_published_manifest_reads_with_its_presentation_fields_ignored()
    {
        var manifest = ModHost.ReadManifest("bossloot", BossLoot);
        Assert.Equal("bossloot", manifest.Id);
        Assert.Equal("1.0.4", manifest.Version);
        Assert.Equal(ModKind.Feature, manifest.Kind);
        Assert.Equal("bossloot.dll", manifest.Assembly);
        Assert.Empty(manifest.Depends);
        // The mod ships off: a player turns it on with /mod_enable or the launcher.
        Assert.False(manifest.Enabled);
    }

    /// <summary>liveeconomy's manifest has no "enabled", and a manifest without it loads: the two internet mods are turned
    /// off through the mods' state file instead (Mods/Binaries/mod-state.txt), not by rewriting their manifests.</summary>
    [Fact]
    public void A_manifest_without_enabled_loads()
    {
        var manifest = ModHost.ReadManifest("liveeconomy",
            """{ "id": "liveeconomy", "name": "Live Economy", "version": "1.0.4", "kind": "feature", "depends": [] }""");
        Assert.True(manifest.Enabled);
        Assert.Equal("liveeconomy.dll", manifest.Assembly);
    }
}
