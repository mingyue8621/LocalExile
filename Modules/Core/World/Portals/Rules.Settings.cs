using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using GameServer.Core.Protocol.Instance;
using GameServer.Core.Protocol.Login;
using WalkEstimator = GameServer.Core.World.WalkEstimator;
using PlayerWalk = GameServer.Core.World.PlayerWalk;
using ClientPathfinder = GameServer.Core.World.ClientPathfinder;
using Progression = GameServer.Core.World.Progression;
using DamageModel = GameServer.Core.World.DamageModel;
using DamageType = GameServer.Core.World.DamageType;
using SaveFiles = GameServer.Core.Saves.SaveFiles;
using DamagePacket = GameServer.Core.World.DamagePacket;
using DamageRange = GameServer.Core.World.DamageRange;
using DamageOverTime = GameServer.Core.World.DamageOverTime;
using GameServer.Core.Diagnostics;

static partial class PortalsRules
{
    // How close the player walks to the map device before it opens (its size is not known yet).
    internal const double StashReach = 6;

    /// <summary>Each character's open town portal (Portal Scroll or the Portal skill): where it stands (area, instance,
    /// cell) and where it leads (a town's HASH16, or the hideout's). While the server runs.</summary>
    internal static readonly System.Collections.Concurrent.ConcurrentDictionary<string, OpenPortal> OpenPortals = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>那道「回野外」的门在城镇里立在哪个格子（城镇的 HASH16 与实例）。第一次进城时定下，之后用传送点
    /// 离开再回城它仍在原处 —— 否则每次进城的出生点不同（传送点回城会把出生点改成传送点旁），门会跟着角色跑。
    /// 换城镇实例、又开了一道新门、或门过期时重新找位置。</summary>
    internal static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (ushort Area, ulong Instance, int X, int Y)> BackPortalCells = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>回城时登记的待办：进入这个城镇/藏身处时，在出生点旁建一道回野外的门。键是目标区域的 HASH16，
    /// 值是回去的区域、实例与门所在的格子。进入该区域建好就删 —— 这条路不依赖新连接上的角色名，
    /// 因为点门回城是一条新连接，区域生成那一刻角色名可能还没绑上。</summary>
    internal static readonly System.Collections.Concurrent.ConcurrentDictionary<ushort, (ushort FromArea, ulong FromInstance, int X, int Y)> PendingBackPortals = new();

    // WorldAreas' own mods (AreaModCatalog); loaded with the first Portal Scroll.
    internal static readonly Lazy<AreaModCatalog> AreaMods = new(() => AreaModCatalog.Load());
    internal const string PortalScroll = "Metadata/Items/Currency/CurrencyPortal";
}
