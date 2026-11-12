/// <summary>Whether terrain stands between two cells: a monster's shot, mortar or area spell must not reach the
/// player through a wall. The client's own projectile grid says which cells stop a projectile (0 blocks, 1..5 pass,
/// including water and the cells the walk map blocks), and the area layout already holds that grid for the connection
/// (AreaLayouts: the server-generated level's .projectile cells, or a capture's). A layout with no grid at all — an
/// area we have neither a capture nor a generated level for — answers nothing, and the attack goes through as it did
/// before, rather than every attack of that area being refused.</summary>
static partial class Game
{
    /// <summary>True when the straight line between <paramref name="from"/> and <paramref name="to"/> crosses a cell the
    /// client's projectile grid blocks. Both ends are excluded: an attacker standing on a door frame and a player
    /// standing on one are not "behind a wall" from each other.</summary>
    public static bool LineOfFireBlocked(ClientState conn, (int X, int Y) from, (int X, int Y) to)
    {
        if (conn.Layout.Sight is not { } sight) return false;
        int dx = to.X - from.X, dy = to.Y - from.Y;
        int steps = Math.Max(Math.Abs(dx), Math.Abs(dy));
        if (steps <= 1) return false;
        for (int i = 1; i < steps; i++)
        {
            int x = (int)Math.Round(from.X + dx * (double)i / steps, MidpointRounding.AwayFromZero);
            int y = (int)Math.Round(from.Y + dy * (double)i / steps, MidpointRounding.AwayFromZero);
            if (sight(x, y) == 0) return true;
        }
        return false;
    }
}
