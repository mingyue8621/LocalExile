using GameServer.Core.Protocol.Instance;

/// <summary>
/// 3.x socket colours. A socket generated on a drop takes red, green or blue, in proportion to the base's attribute
/// requirements; white sockets are not rolled (the game gives them through the bench's all-white craft, a few uniques'
/// own mods and corrupted implicits, none of which go through <see cref="Roll"/>). The bench's Chromatic-like crafts
/// ask for the same three colours.
///
/// (Until 2026-11-14 this class made every socket white first and rolled a colour only at a chance that rose with the
/// item level — 21% at level 65 — which turned nearly every dropped item into five or six white sockets, live items in
/// saves included. Item 58742's staff reads [4,4,4,4,2,4]: five white, one green.)
/// </summary>
public static class SocketColours
{
    /// <summary>The weight every colour gets on top of its attribute requirement, so a base can roll all three colours.
    /// The client's exact weights are unknown.</summary>
    public const int AttributeWeight = 12;

    /// <summary>One freshly generated socket: red, green or blue, weighted by the base's attribute requirements.
    /// <paramref name="noWhite"/> (the 3.29 hidden property some uniques carry, <c>local_sockets_cannot_be_white</c>)
    /// changes nothing now that white is never rolled, but callers still pass it and the signature stays.</summary>
    public static SocketColour Roll((int Str, int Dex, int Int) requirements, int itemLevel, int quality, Random random,
        bool chromatic = false, bool noWhite = false) => Colour(requirements, random);

    /// <summary>A forced colour: red, green or blue, weighted by the base's strength, dexterity and intelligence
    /// requirements plus <see cref="AttributeWeight"/>.</summary>
    public static SocketColour Colour((int Str, int Dex, int Int) requirements, Random random)
    {
        int r = requirements.Str + AttributeWeight, g = requirements.Dex + AttributeWeight, b = requirements.Int + AttributeWeight;
        int value = random.Next(r + g + b);
        return value < r ? SocketColour.Red : value < r + g ? SocketColour.Green : SocketColour.Blue;
    }

    /// <summary>Red, green or blue: a socket that gets the +10% matching-gem-quality bonus.</summary>
    public static bool IsColoured(SocketColour colour) =>
        colour is SocketColour.Red or SocketColour.Green or SocketColour.Blue;
}
