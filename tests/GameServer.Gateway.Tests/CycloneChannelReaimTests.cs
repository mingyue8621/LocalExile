using GameServer.Core.Protocol.Instance;
using GameServer.Core.Protocol.Login;

namespace GameServer.Gateway.Tests;

public sealed class CycloneChannelReaimTests
{
    // A lockstep client sends no C→S 0x197 while Cyclone is held (live 2026-11-21: no 0x197/0x198/0x199 at all in a
    // 22 s channel): the cursor's moves come back as the same use, C→S 0x18e. They are re-aims — the channel runs on
    // and the player walks toward the new point — instead of the channel being ended and started again, which restarted
    // the whirl in place and left the player turning on the spot.
    [Fact]
    public void A_repeated_cyclone_use_reaims_the_running_channel_and_walks_toward_the_cursor()
    {
        var player = new Player(new(1, "cyclone-reaim", 400, 400, 100, 100, null, []), 1, 1)
        {
            Connection = { LockstepClient = true },
            ChannelSkill = 0x010e,
            ChannelSkillSequence = 0x8001,
            ChannelMovement = (0x010e, -28),
            LastChannelUpdateAt = 0,
        };
        player.ApplyMovementSpeedAt(0);
        player.LockstepSkill.Press((110, 100), null, 0x010e, 0x0409, repeats: false);
        player.LockstepSkill.Channelling(0x8001);
        var (channel, generation, action) = (player.ChannelSkill, player.ChannelGeneration, player.ActionCount);

        Game.OnLockstepRequest(player, InstanceProtocol.LockstepMoveStartId, Use(30, 5, 0x010e, 0x0409));

        // The channel is the one that was running: same skill, same generation, same action it was paid under.
        Assert.Equal(channel, player.ChannelSkill);
        Assert.Equal(generation, player.ChannelGeneration);
        Assert.Equal(action, player.ActionCount);
        // The aim is the client's cell plus the click's offset. The read handler only notes it: the walk to it is a path
        // search, and one per packet stalled every input behind it (live 2026-11-21: 0x18e took 440 ms).
        Assert.Equal((130, 105), player.LockstepSkill.Aim);
        Assert.Equal((130, 105), player.LockstepChannelAim);
        Assert.True(player.LockstepChannelAimPending);
        // The frame applies it once and answers it with a 0x1ad; the walk is set up for the new point.
        Assert.True(Game.ApplyLockstepChannelAim(player, 1.0));
        Assert.Equal((130, 105), player.LockstepChannelAimedAt);
        Assert.False(player.LockstepChannelAimPending);
        Assert.True(player.LastChannelUpdateAt >= 0);
    }

    // A moving cursor re-aims several times a frame: the path search happens at most once per ChannelAimSeconds, and the
    // last aim is kept until it is applied (so the cursor's final cell is never lost).
    [Fact]
    public void The_aims_path_search_is_throttled_and_the_last_aim_is_kept()
    {
        var player = new Player(new(1, "cyclone-throttle", 400, 400, 100, 100, null, []), 1, 1)
        {
            Connection = { LockstepClient = true },
            ChannelSkill = 0x010e,
            ChannelSkillSequence = 0x8001,
            ChannelMovement = (0x010e, -30),
            LastChannelUpdateAt = 0,
        };
        player.ApplyMovementSpeedAt(0);
        player.LockstepSkill.Press((110, 100), null, 0x010e, 0x0409, repeats: false);
        player.LockstepSkill.Channelling(0x8001);

        Game.OnLockstepRequest(player, InstanceProtocol.LockstepMoveUpdateId, Aim(20, 0));
        Assert.True(Game.ApplyLockstepChannelAim(player, 1.0));
        Assert.Equal((120, 100), player.LockstepChannelAimedAt);

        // Ten milliseconds later, a new cell: the search waits, and the aim stays owed.
        Game.OnLockstepRequest(player, InstanceProtocol.LockstepMoveUpdateId, Aim(24, 0));
        var newest = player.LockstepChannelAim;
        Assert.False(Game.ApplyLockstepChannelAim(player, 1.01));
        Assert.True(player.LockstepChannelAimPending);

        // Once the interval is over, the newest cell is the one the walk is set up for.
        Assert.True(Game.ApplyLockstepChannelAim(player, 1.06));
        Assert.Equal(newest, player.LockstepChannelAimedAt);

        // Another aim right after is throttled too, and the one after the interval is applied.
        Game.OnLockstepRequest(player, InstanceProtocol.LockstepMoveUpdateId, Aim(30, 0));
        Assert.False(Game.ApplyLockstepChannelAim(player, 1.061));
        Assert.True(player.LockstepChannelAimPending);
        Assert.True(Game.ApplyLockstepChannelAim(player, 1.12));
    }

    // Anything else still starts an action of its own (and ends the channel), as before.
    [Fact]
    public void Another_skill_pressed_while_cyclone_runs_is_a_new_action()
    {
        var player = new Player(new(1, "cyclone-other", 400, 400, 100, 100, null, []), 1, 1)
        {
            Connection = { LockstepClient = true },
            ChannelSkill = 0x010e,
            ChannelSkillSequence = 0x8001,
            ChannelMovement = (0x010e, -28),
            LastChannelUpdateAt = 0,
        };
        player.ApplyMovementSpeedAt(0);
        player.LockstepSkill.Press((110, 100), null, 0x010e, 0x0409, repeats: false);
        player.LockstepSkill.Channelling(0x8001);

        Game.OnLockstepRequest(player, InstanceProtocol.LockstepMoveStartId, Use(10, 0, 0x0103, 0x0408));

        Assert.Equal(0x0103, player.LockstepSkill.Skill);
        Assert.True(player.LockstepSkill.Pending);
        Assert.Equal((0x010e, -28), player.ChannelMovement);
    }

    // C→S 0x192 carries the held channel's newest aim, in place of a predictive client's 0x197. It re-aims the channel
    // (the server's walk goes there: the estimate is the cell the next 0x1ab puts the client's actor on) and owes the
    // frame a C→S 0x1ad, the only way a lockstep client learns where its channel points.
    [Fact]
    public void The_held_channels_new_aim_reaims_it_and_owes_a_0x1ad()
    {
        var player = new Player(new(1, "cyclone-aim", 400, 400, 100, 100, null, []), 1, 1)
        {
            Connection = { LockstepClient = true },
            PlayerLife = 1000,
            ChannelSkill = 0x010e,
            ChannelSkillSequence = 0x8001,
            ChannelMovement = (0x010e, -30),
            LastChannelUpdateAt = 0,
        };
        player.ApplyMovementSpeedAt(0);
        player.LockstepSkill.Press((110, 100), null, 0x010e, 0x0409, repeats: false);
        player.LockstepSkill.Channelling(0x8001);

        Game.OnLockstepRequest(player, InstanceProtocol.LockstepMoveUpdateId, Aim(20, -6));

        Assert.Equal((120, 94), player.LockstepSkill.Aim);
        Assert.Equal((120, 94), player.LockstepChannelAim);
        Assert.True(player.LockstepChannelAimPending);
        // The read handler notes the aim; the frame walks to it and answers with the 0x1ad.
        Assert.True(Game.ApplyLockstepChannelAim(player, 1.0));
        Assert.Equal((120, 94), player.LockstepChannelAimedAt);
        Assert.Equal(0x8001, player.ChannelSkillSequence);
        // The held button itself keeps the channel alive: a lockstep client has no 0x197 to refresh the aim silence, so a
        // still cursor used to release a held Cyclone after a second (the whirl vanished mid-fight).
        Assert.True(Game.ChannelCurrent(player, 0x010e, player.ActionCount, player.ChannelGeneration));
    }

    // 10 bytes: u16 id, i32 dx, i32 dy.
    static byte[] Aim(int dx, int dy) =>
        new LoginWriter().U16(InstanceProtocol.LockstepMoveUpdateId).U32((uint)dx).U32((uint)dy).ToArray();

    // 15 bytes: u16 id, i32 dx, i32 dy, u16 skill, u16 flags, u8.
    static byte[] Use(int dx, int dy, ushort skill, ushort flags) =>
        new LoginWriter().U16(InstanceProtocol.LockstepMoveStartId).U32((uint)dx).U32((uint)dy).U16(skill).U16(flags).U8(0).ToArray();
}
