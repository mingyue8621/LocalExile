namespace GameServer.Gateway.Tests;

public class ObjectCreateRefusalTests
{
    // A second birth of one id used to throw "World object ID is still reserved or published" with no id, and the
    // projectile output's catch closed the session on it (live 2026-11-11). The message now names the id and its state so
    // a live log says which object was refused.
    [Fact]
    public void A_second_birth_of_one_id_names_the_id_and_its_state()
    {
        var output = new WorldOutput();
        output.NewObject(0x04000017);

        var refusal = Assert.Throws<InvalidOperationException>(() => output.NewObject(0x04000017));

        Assert.Contains("0x4000017", refusal.Message);
        Assert.Contains("Reserved", refusal.Message);
    }
}
