using GameServer.Core.World;

namespace GameServer.Core.Tests;

// 锁步客户端按住鼠标时，每 70 ms 一条 0x192，服务端每次都把当前目标重新下一次单（Walk.Order）。这些测试锁住一件事：
// 重下一次单不能让服务端的估计位置往回退 —— 0x1ab 会把客户端摆到服务端报的 from 格，一退就是玩家眼里"角色往回走"。
public class LockstepRepathTests
{
    const double Interval = 0.07;

    // 一堵墙（x = 10，y 在 −6 … 6），只能从 y > 6 绕过去。
    static bool Wall(int x, int y) => !(x == 10 && y >= -6 && y <= 6);

    [Fact]
    public void Repeated_orders_for_one_target_never_walk_back()
    {
        var w = new PlayerWalk(0, 0, speed: 10);
        w.Order(40, 0, now: 0);
        int last = 0;
        for (double t = Interval; t < 3.0; t += Interval)
        {
            var here = w.Position(t);
            w.Order(40, 0, t);
            var after = w.Position(t);
            Assert.True(after.X >= here.X, $"t={t:0.00}: order again moved the walker back from {here.X} to {after.X}");
            Assert.True(after.X >= last, $"t={t:0.00}: the walk went back from {last} to {after.X}");
            last = after.X;
        }
        Assert.Equal((40, 0), w.Position(10));
    }

    [Fact]
    public void Orders_that_follow_the_cursor_keep_walking_forward()
    {
        // 按住鼠标：光标一直在角色前方 6 格，客户端每次 0x192 报的偏移随之变小。
        var w = new PlayerWalk(0, 0, speed: 10);
        w.Order(6, 0, now: 0);
        int last = 0;
        for (double t = Interval; t < 3.0; t += Interval)
        {
            var here = w.Position(t);
            w.Order(here.X + 6, 0, t);
            var after = w.Position(t);
            Assert.True(after.X >= here.X, $"t={t:0.00}: a following order moved the walker back from {here.X} to {after.X}");
            Assert.True(after.X >= last, $"t={t:0.00}: the walk went back from {last} to {after.X}");
            last = after.X;
        }
        Assert.True(last > 15, $"the walker only reached {last}");
    }

    [Fact]
    public void Re_ordering_around_a_wall_never_walks_back_up_the_path()
    {
        // 绕墙：路径先向北再向东。每次重下订单都不能让沿路前进的进度倒退。
        var w = new PlayerWalk(0, 0, speed: 10, walkable: Wall);
        w.Order(20, 0, now: 0);
        var last = w.Position(0);
        double travelled = 0;
        for (double t = Interval; t < 8.0; t += Interval)
        {
            var here = w.Position(t);
            w.Order(20, 0, t);
            var after = w.Position(t);
            double moved = Math.Abs(after.X - here.X) + Math.Abs(after.Y - here.Y);
            Assert.True(moved <= 1, $"t={t:0.00}: re-ordering jumped the walker from {here} to {after}");
            travelled += Math.Abs(after.X - last.X) + Math.Abs(after.Y - last.Y);
            last = after;
        }
        Assert.True(travelled >= 26, $"the walker only covered {travelled} cells of the 26-cell way round");
    }
}
