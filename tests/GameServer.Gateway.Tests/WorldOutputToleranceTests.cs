using System.Buffers.Binary;
using GameServer.Core.Protocol.Instance;

namespace GameServer.Gateway.Tests;

/// <summary>A record for an object this connection never showed is dropped, not refused: refusing the whole batch made
/// "player updates failed" repeat and never delivered anything more to that client (live 2026-11-11, The Riverways: the
/// ground view synchronised an item whose birth another producer had prepared and cancelled).</summary>
public sealed class WorldOutputToleranceTests
{
    static byte[] Record(uint id)
    {
        var bytes = InstanceProtocol.EnterWakeZone(id);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(6), uint.MaxValue);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(10), 0);
        return bytes;
    }

    [Fact]
    public void A_presentation_without_a_birth_is_dropped_and_the_batch_still_belongs_to_its_own_objects()
    {
        var conn = new ClientState(new(1, "world-tolerance", 300, 300, 100, 100, null, []), 1, 1) { LockstepClient = true };
        var output = conn.WorldOutput;
        output.Begin(3);
        // A birth another producer reserved and never admitted (its producer dropped the batch).
        var orphan = output.NewObject(40);
        // The one this batch does show, created and presented in the same batch.
        var admission = new WorldAdmission(output, []);
        var born = admission.NewObject(41);
        var create = InstanceEntities.CreateObject(41, 0x1234, [0, 0]);
        BinaryPrimitives.WriteUInt32BigEndian(create.AsSpan(6), uint.MaxValue);
        BinaryPrimitives.WriteUInt16BigEndian(create.AsSpan(10), 0);
        var batch = new OutputBatch(
            OutputRecord.Create(born, create),
            OutputRecord.AfterBirth(born, WorldOrder.Last, Record(41)),
            OutputRecord.AfterBirth(orphan, WorldOrder.Last, Record(40)));

        var immediate = output.Admit(batch, admission, framed: true);
        Assert.Empty(immediate);
        // The orphan's presentation is gone; the born object's create is in the frame, its presentation waits for the
        // create to be written (born is not published yet).
        var frame = output.TakeFrame();
        Assert.Equal(41u, Assert.Single(frame.Births).Id);
        Assert.Single(frame.Packets);
        Assert.Empty(frame.Removals);
        admission.Finish(true);
        Assert.False(orphan.IsReady);
    }

    [Fact]
    public void Contains_reports_the_incarnations_a_producer_already_created()
    {
        var conn = new ClientState(new(1, "world-contains", 300, 300, 100, 100, null, []), 1, 1) { LockstepClient = true };
        var output = conn.WorldOutput;
        Assert.False(output.Contains(0x100000));
        output.NewObject(0x100000);
        Assert.True(output.Contains(0x100000));
        Assert.True(output.Contains(0x100000));
    }
}
