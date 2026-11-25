using System.Buffers.Binary;
using GameServer.Core.Accounts;
using GameServer.Core.Protocol.Instance;
using GameServer.Modules.M_questobjects;
using Xunit;

namespace GameServer.Gateway.Tests;

/// <summary>The lord a character helps in Deal with the Bandits stands as an ally: no vote again, no target, no damage.
/// Before this it stayed the held hostile: one hit turned its entrance grace off, it hit back, and it could be killed
/// for nothing while its camp still waited for a choice.</summary>
public sealed class BanditAllyTests
{
    static readonly string[] NpcComponents = ["Positioned", "NPC", "Life"];

    sealed class Camp : IDisposable
    {
        readonly IDisposable questModule;
        internal readonly Player Player;
        internal readonly InstanceState Instance = new();
        internal readonly SpawnedMonster Leader;
        internal readonly SpawnedNpc Speaker;
        internal readonly NpcCatalog.Talk VoteTalk;
        internal readonly List<byte[]> Outbox = [];

        internal Camp(BanditChoices.Bandit bandit)
        {
            questModule = Quests.Register("bandit-ally-test", QuestObjectsModule.Acts());
            var layout = new AreaLayout(34, bandit.Area, 300, 300, 50, 50, (_, _) => true, []);
            var league = new LeagueRuntime(new League("bandit-ally-test", "Standard", false, false),
                new HashSet<string>(["bandit-ally-test", "Game"]));
            Player = new Player(layout, 1, 1)
            {
                Character = { Name = "AllyTest" + Guid.NewGuid().ToString("N")[..8] },
                Instance = Instance,
                PlayerLife = 1000,
                Connection =
                {
                    PlayerSent = true,
                    Outbox = (bytes, _) => { Outbox.Add([.. bytes]); return Task.CompletedTask; },
                },
                LeagueModule = league,
            };
            Player.Clock.Start();
            Instance.AddPlayer(Player);
            var variety = Game.Monsters.Find(bandit.Variety)!;
            Leader = new SpawnedMonster(bandit.Name, Player.NewMonsterWalk(9001, 52, 50, 0, 0, unique: true), 20, 8, 500)
            {
                Hostile = true, Dead = false, Aggro = false, Life = 500, Variety = variety, Level = 20,
            };
            Instance.World.Area.Monsters[9001] = Leader;
            Instance.World.MonsterOrigins[9001] = new(0,
                new MonsterObject(52, 50, 500, NpcComponents) { HasDialogue = true }, 0, 20, MonsterRarity.Unique);
            var npc = Game.Npcs.Find(bandit.Variety)!;
            VoteTalk = Game.Npcs.TalksOf(npc).First(t => t.Script.Contains("BanditCallVote", StringComparison.Ordinal));
            Speaker = new SpawnedNpc(npc, 52, 50);
        }

        internal ushort[] Packets(ushort id) => [.. Outbox
            .Where(p => BinaryPrimitives.ReadUInt16BigEndian(p) == id)
            .Select(p => p.Length >= 4 ? BinaryPrimitives.ReadUInt16BigEndian(p.AsSpan(p.Length - 2)) : (ushort)0)];

        public void Dispose() => questModule.Dispose();
    }

    [Fact]
    public async Task HelpedLordBecomesAnAllyAndOffersNoSecondVote()
    {
        using var camp = new Camp(BanditChoices.Alira);
        var alira = BanditChoices.Alira;

        Assert.True(await BanditChoices.OpenVote(camp.Player, 9001, camp.Speaker, camp.VoteTalk));
        await BanditChoices.VoteAnswer(camp.Player, help: true);

        // The help quest and the ally flag are the character's; no fight was started.
        var saved = CharacterSaves.Load(camp.Player.Character.Name!)!.QuestFlags!;
        Assert.Contains(alira.Ally, saved);
        Assert.Contains(alira.Started, saved);
        Assert.False(camp.Instance.HasState("QuestObjects.BanditFight"));

        // Server-side it is no longer a hostile: it is not ordered about, and it takes no damage.
        Assert.True(camp.Leader.QuestAlly);
        Assert.False(camp.Leader.Hostile);
        Assert.True(camp.Leader.Invulnerable);

        // The client is told team 0 (S->C 0x1be), the switch a Conversion Trap uses, so it is not a target.
        var teams = camp.Packets(InstanceProtocol.SetTeamId);
        Assert.NotEmpty(teams);
        Assert.Contains((ushort)0, teams);

        // The choice is permanent: the lord offers no second vote, and its rivals' camps are no longer held.
        camp.Outbox.Clear();
        Assert.True(await BanditChoices.OpenVote(camp.Player, 9001, camp.Speaker, camp.VoteTalk));
        Assert.DoesNotContain(camp.Outbox, p => BinaryPrimitives.ReadUInt16BigEndian(p) == InstanceNpcs.VoteDisplayId);
        Assert.False(BanditChoices.Held(camp.Player, BanditChoices.Oak));
        Assert.False(BanditChoices.Held(camp.Player, BanditChoices.Kraityn));
    }

    [Fact]
    public async Task EnteringTheAreaAgainPutsTheHelpedLordBackInItsAllyState()
    {
        using var camp = new Camp(BanditChoices.Oak);
        var oak = BanditChoices.Oak;
        string name = camp.Player.Character.Name!;
        CharacterSaves.ChangeQuestFlags(name, [oak.Ally, oak.Started], true);
        camp.Player.Character.QuestFlags.Add(oak.Ally);
        camp.Player.Character.QuestFlags.Add(oak.Started);

        // A fresh instance spawns the lord as the held hostile again (HasDialogue, Aggro false, team 1).
        Assert.True(camp.Leader.Hostile);
        await BanditChoices.OnAreaGenerated(camp.Player);

        Assert.True(camp.Leader.QuestAlly);
        Assert.False(camp.Leader.Hostile);
        Assert.Contains((ushort)0, camp.Packets(InstanceProtocol.SetTeamId));
    }
}
