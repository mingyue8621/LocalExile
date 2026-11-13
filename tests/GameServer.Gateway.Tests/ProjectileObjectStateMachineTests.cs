using GameServer.Core;

namespace GameServer.Gateway.Tests;

/// <summary>A shot's 0x2bb blob writes Positioned and Animated only. A projectile type whose .ot appends a StateMachine
/// (the mortars) then needs the empty machine's two counts before the task byte, or the client reads the task byte as a
/// state count and shows "Buffer Underflow" (HeistThugTorchBombMortar, live 2026-11-13 in The Hidden Underbelly).</summary>
public sealed class ProjectileObjectStateMachineTests
{
    [Fact]
    public void Mortar_types_get_the_empty_state_machine_from_their_type_data()
    {
        if (!ClientTables.Has("object_types")) return;   // client.db is not built in this checkout

        Assert.True(Game.ProjectileAppendsStateMachine("Metadata/Projectiles/HeistThugTorchBombMortar"));
        Assert.True(Game.ProjectileAppendsStateMachine("Metadata/Projectiles/DelveMonkeyMortar"));
        Assert.True(Game.ProjectileAppendsStateMachine("Metadata/Projectiles/HarvestMonkeyBombMortar"));

        // No StateMachine in the type, or one whose place in the component order this blob does not write.
        Assert.False(Game.ProjectileAppendsStateMachine("Metadata/Projectiles/FrostFuryMortar"));
        Assert.False(Game.ProjectileAppendsStateMachine("Metadata/Projectiles/BurningArrow"));
        Assert.False(Game.ProjectileAppendsStateMachine("Metadata/Projectiles/NotARealProjectile"));
    }

    [Fact]
    public void The_blob_grows_by_the_two_counts_before_the_task_byte()
    {
        var plain = new GameServer.Core.Protocol.Instance.ProjectileObject("Metadata/Projectiles/FrozenOrb", 10, 20).Blob();
        var mortar = new GameServer.Core.Protocol.Instance.ProjectileObject("Metadata/Projectiles/FrozenOrb", 10, 20)
            { StateMachine = new([], 0) }.Blob();
        Assert.Equal(plain.Length + 2, mortar.Length);
        Assert.Equal(new byte[] { 0, 0, 0 }, mortar[^3..]);
    }
}
