/// <summary>The one thing the PlayerBots module and the game module exchange about a bot's gear: the module owns the choice
/// (the character's class, level and build), the game module equips it on the character's rows, which is the only place
/// that can. Everything else about a bot's look the module now does itself (BotCosmetics/BotGear are its files).</summary>
public static class BotHooks
{
    /// <summary>The gear a party bot wears when its controlled player is made, by character name: base item ids of the game
    /// data for its class and level. The PlayerBots module fills this in (it owns the choice); the game module equips them,
    /// because only it can put items in the character's rows. Null or empty keeps whatever the character already has.</summary>
    public static Func<string, IReadOnlyList<string>>? Gear;
}
