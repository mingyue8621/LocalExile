using GameServer.Core.Chat;
using GameServer.Core.Protocol.Instance;

namespace GameServer.Core.Tests;

public sealed class ChatOnlineCountTests
{
    [Fact]
    public void Joining_a_channel_and_count_requests_report_the_online_headcount()
    {
        var hub = new ChatHub();
        List<ChatFrame> first = [], second = [];
        var a = hub.Enter("alice", first.Add)!;
        var b = hub.Enter("bob", second.Add)!;
        var join = new ChatFrame(ChatFrame.Join, Kind: ChatChannelKind.Global, Channel: 1);

        hub.Handle(a, join);
        Assert.Equal("当前在线 2 人。", first.Single(f => f.Type == ChatFrame.Online).Text);

        hub.Handle(b, join);
        Assert.Equal("当前在线 2 人。", second.Single(f => f.Type == ChatFrame.Online).Text);

        first.Clear();
        hub.Handle(a, new ChatFrame(ChatFrame.Count));
        Assert.Equal("当前在线 2 人。", Assert.Single(first, f => f.Type == ChatFrame.Online).Text);
    }
}
