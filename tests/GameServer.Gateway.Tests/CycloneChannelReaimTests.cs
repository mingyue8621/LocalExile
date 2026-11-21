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
        var conn = new ClientState(new(1, "cyclone-reaim", 400, 400, 100, 100, null, []), 1, 1)
        {
            LockstepClient = true,
            ChannelSkill = 0x010e,
            ChannelSkillSequence = 0x8001,
            ChannelMovement = (0x010e, -28),
            LastChannelUpdateAt = 0,
        };
        conn.ApplyMovementSpeedAt(0);
        conn.LockstepSkill.Press((110, 100), null, 0x010e, 0x0409, repeats: false);
        conn.LockstepSkill.Channelling(0x8001);
        var (channel, generation, action) = (conn.ChannelSkill, conn.ChannelGeneration, conn.ActionCount);

        Game.OnLockstepRequest("reaim", conn, InstanceProtocol.LockstepMoveStartId, Use(30, 5, 0x010e, 0x0409));

        // The channel is the one that was running: same skill, same generation, same action it was paid under.
        Assert.Equal(channel, conn.ChannelSkill);
        Assert.Equal(generation, conn.ChannelGeneration);
        Assert.Equal(action, conn.ActionCount);
        // The aim is the client's cell plus the click's offset. The read handler only notes it: the walk to it is a path
        // search, and one per packet stalled every input behind it (live 2026-11-21: 0x18e took 440 ms).
        Assert.Equal((130, 105), conn.LockstepSkill.Aim);
        Assert.Equal((130, 105), conn.LockstepChannelAim);
        Assert.True(conn.LockstepChannelAimPending);
        // The frame applies it once and answers it with a 0x1ad; the walk is set up for the new point.
        Assert.True(Game.ApplyLockstepChannelAim(conn, 1.0));
        Assert.Equal((130, 105), conn.LockstepChannelAimedAt);
        Assert.False(conn.LockstepChannelAimPending);
        Assert.True(conn.LastChannelUpdateAt >= 0);
    }

    // A moving cursor re-aims several times a frame: the path search happens at most once per ChannelAimSeconds, and the
    // last aim is kept until it is applied (so the cursor's final cell is never lost).
    [Fact]
    public void The_aims_path_search_is_throttled_and_the_last_aim_is_kept()
    {
        var conn = new ClientState(new(1, "cyclone-throttle", 400, 400, 100, 100, null, []), 1, 1)
        {
            LockstepClient = true,
            ChannelSkill = 0x010e,
            ChannelSkillSequence = 0x8001,
            ChannelMovement = (0x010e, -30),
            LastChannelUpdateAt = 0,
        };
        conn.ApplyMovementSpeedAt(0);
        conn.LockstepSkill.Press((110, 100), null, 0x010e, 0x0409, repeats: false);
        conn.LockstepSkill.Channelling(0x8001);

        Game.OnLockstepRequest("t1", conn, InstanceProtocol.LockstepMoveUpdateId, Aim(20, 0));
        Assert.True(Game.ApplyLockstepChannelAim(conn, 1.0));
        Assert.Equal((120, 100), conn.LockstepChannelAimedAt);

        // Ten milliseconds later, a new cell: the search waits, and the aim stays owed.
        Game.OnLockstepRequest("t2", conn, InstanceProtocol.LockstepMoveUpdateId, Aim(24, 0));
        var newest = conn.LockstepChannelAim;
        Assert.False(Game.ApplyLockstepChannelAim(conn, 1.01));
        Assert.True(conn.LockstepChannelAimPending);

        // Once the interval is over, the newest cell is the one the walk is set up for.
        Assert.True(Game.ApplyLockstepChannelAim(conn, 1.06));
        Assert.Equal(newest, conn.LockstepChannelAimedAt);

        // Another aim right after is throttled too, and the one after the interval is applied.
        Game.OnLockstepRequest("t3", conn, InstanceProtocol.LockstepMoveUpdateId, Aim(30, 0));
        Assert.False(Game.ApplyLockstepChannelAim(conn, 1.061));
        Assert.True(conn.LockstepChannelAimPending);
        Assert.True(Game.ApplyLockstepChannelAim(conn, 1.12));
    }

    // Anything else still starts an action of its own (and ends the channel), as before.
    [Fact]
    public void Another_skill_pressed_while_cyclone_runs_is_a_new_action()
    {
        var conn = new ClientState(new(1, "cyclone-other", 400, 400, 100, 100, null, []), 1, 1)
        {
            LockstepClient = true,
            ChannelSkill = 0x010e,
            ChannelSkillSequence = 0x8001,
            ChannelMovement = (0x010e, -28),
            LastChannelUpdateAt = 0,
        };
        conn.ApplyMovementSpeedAt(0);
        conn.LockstepSkill.Press((110, 100), null, 0x010e, 0x0409, repeats: false);
        conn.LockstepSkill.Channelling(0x8001);

        Game.OnLockstepRequest("other", conn, InstanceProtocol.LockstepMoveStartId, Use(10, 0, 0x0103, 0x0408));

        Assert.Equal(0x0103, conn.LockstepSkill.Skill);
        Assert.True(conn.LockstepSkill.Pending);
        Assert.Equal((0x010e, -28), conn.ChannelMovement);
    }

    // C→S 0x192 carries the held channel's newest aim, in place of a predictive client's 0x197. It re-aims the channel
    // (the server's walk goes there: the estimate is the cell the next 0x1ab puts the client's actor on) and owes the
    // frame a C→S 0x1ad, the only way a lockstep client learns where its channel points.
    [Fact]
    public void The_held_channels_new_aim_reaims_it_and_owes_a_0x1ad()
    {
        var conn = new ClientState(new(1, "cyclone-aim", 400, 400, 100, 100, null, []), 1, 1)
        {
            LockstepClient = true,
            PlayerLife = 1000,
            ChannelSkill = 0x010e,
            ChannelSkillSequence = 0x8001,
            ChannelMovement = (0x010e, -30),
            LastChannelUpdateAt = 0,
        };
        conn.ApplyMovementSpeedAt(0);
        conn.LockstepSkill.Press((110, 100), null, 0x010e, 0x0409, repeats: false);
        conn.LockstepSkill.Channelling(0x8001);

        Game.OnLockstepRequest("aim", conn, InstanceProtocol.LockstepMoveUpdateId, Aim(20, -6));

        Assert.Equal((120, 94), conn.LockstepSkill.Aim);
        Assert.Equal((120, 94), conn.LockstepChannelAim);
        Assert.True(conn.LockstepChannelAimPending);
        // The read handler notes the aim; the frame walks to it and answers with the 0x1ad.
        Assert.True(Game.ApplyLockstepChannelAim(conn, 1.0));
        Assert.Equal((120, 94), conn.LockstepChannelAimedAt);
        Assert.Equal(0x8001, conn.ChannelSkillSequence);
        // The held button itself keeps the channel alive: a lockstep client has no 0x197 to refresh the aim silence, so a
        // still cursor used to release a held Cyclone after a second (the whirl vanished mid-fight).
        Assert.True(Game.ChannelCurrent(conn, 0x010e, conn.ActionCount, conn.ChannelGeneration));
    }

    // 10 bytes: u16 id, i32 dx, i32 dy.
    static byte[] Aim(int dx, int dy) =>
        new LoginWriter().U16(InstanceProtocol.LockstepMoveUpdateId).U32((uint)dx).U32((uint)dy).ToArray();

    // 15 bytes: u16 id, i32 dx, i32 dy, u16 skill, u16 flags, u8.
    static byte[] Use(int dx, int dy, ushort skill, ushort flags) =>
        new LoginWriter().U16(InstanceProtocol.LockstepMoveStartId).U32((uint)dx).U32((uint)dy).U16(skill).U16(flags).U8(0).ToArray();
}
