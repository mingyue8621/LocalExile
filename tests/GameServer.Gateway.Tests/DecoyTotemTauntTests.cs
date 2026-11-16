using GameServer.Core.Accounts;
using GameServer.Core.Protocol.Instance;
using GameServer.Core.World;

namespace GameServer.Gateway.Tests;

/// <summary>Temporary: Decoy Totem taunts monsters into hitting the totem, not the player.</summary>
public sealed class DecoyTotemTauntTests
{
    static Item Gem(string name) => ItemCatalog.Create(Program.Items.Find("Metadata/Items/Gems/" + name)!);

    [Fact]
    public void Monsters_taunted_by_the_decoy_hit_its_life()
    {
        string path = Path.Combine(Path.GetTempPath(), $"decoy-taunt-{Guid.NewGuid():N}.db");
        var previous = Program.GameLogicOrNull;
        var previousLifetime = Game.CombatLifetime;
        var lifetime = new ModuleLifetime("Game");
        Saves.Open(path);
        Game.CombatLifetime = lifetime;
        Program.SetGameLogic(new GameServer.Modules.Game.GameLogic());
        try
        {
            var conn = SpellTotemExecutionTests.Connection("DecoyTaunt");
            var items = Game.ItemsOf(conn);
            foreach (var old in items.EquipmentSnapshot().Where(e => e.Row == PlayerInventories.Weapon1))
                items.Remove(PlayerInventories.Weapon1, old.Item.Id);
            var sword = ItemCatalog.Create(Program.Items.Find("Metadata/Items/Weapons/OneHandWeapons/OneHandSwords/OneHandSword1")!);
            items.Add(PlayerInventories.Weapon1, 0, 0, sword with
            {
                Sockets = [new(SocketColour.White, Gem("SkillGemDecoyTotem"))], Links = [1],
            });
            ushort skill = Game.GemSkillEffects(conn).Single().Id;
            var source = Game.SupportedSkill(conn, skill);
            var gem = Game.GemSkillEffects(conn).Single();
            double now = conn.Clock.Elapsed.TotalSeconds;
            var totem = new TotemState.Totem(777, skill, (100, 100), 1000, now + 60, null)
            { Effect = gem.Effect, SourceGem = gem.Gem, SourceHost = source.Host, SourceItem = source.SourceGem };
            conn.Totems.Place(totem, 1, now);
            var monster = new SpawnedMonster("taunted", new(110, 100, 0), 60, 5, 10000) { Hostile = true };
            conn.Monsters[901] = monster;

            Game.DecoyTauntLocked(conn, totem, now, 4);
            Assert.Equal(totem.Id, monster.TauntTotemId);
            Assert.True(monster.TauntTotemUntil > now);
            Assert.True(monster.TauntedUntil > now);

            var attack = new MonsterAttack(SkillCatalog.FirstSkillId, 1, 8,
                DamagePacket.Of(DamageType.Physical, new DamageRange(100, 100)));
            uint beforeLife = conn.PlayerLife;
            uint beforeTotem = totem.Life;
            bool taken = Game.TauntedTotemHitLocked(conn, monster, attack, now, Game.CharacterPlayerStats(conn));
            Assert.True(taken);
            Assert.Equal(beforeTotem - 100, totem.Life);
            Assert.Equal(beforeLife, conn.PlayerLife);
        }
        finally
        {
            Program.SetGameLogic(previous!);
            Game.CombatLifetime = previousLifetime;
            lifetime.Stop(_ => { });
            PlayerInventories.Forget("DecoyTaunt");
            TestEnvironment.ReleaseSaves(path);
        }
    }
}
