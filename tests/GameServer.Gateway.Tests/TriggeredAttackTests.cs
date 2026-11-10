using GameServer.Core.Protocol.Instance;
using GameServer.Core.Protocol.Login;
using League = GameServer.Core.Accounts.League;

namespace GameServer.Gateway.Tests;

/// <summary>A trigger support that links an attack (Manaforged Arrows, Cast on Crit) must not take the attack out of the
/// player's hands: only the triggered copy waits for an executor (Skills.GemSkillInstanceDisabled). Reported live on
/// 2026-11-10: a bow character with Manaforged Arrows could fire neither Lightning Arrow nor Tornado Shot.</summary>
public class TriggeredAttackTests
{
    [Theory]
    [InlineData("SkillGemLightningArrow")]
    [InlineData("SkillGemTornadoShot")]
    public void An_attack_linked_to_a_trigger_support_is_still_used_by_hand(string attack)
    {
        WithCharacter(conn =>
        {
            EquipBow(conn, attack, "SupportGemManaforgedArrows");
            var gem = Game.GemSkillEffects(conn).Single(g => g.Name is "Lightning Arrow" or "Tornado Shot");
            Assert.Contains("Manaforged", string.Join(",", Game.SupportedSkill(conn, gem.Id).Supports.Select(s => s.Effect.Id)));
            // The trigger the support carries is still refused by name, but the manual payload is not.
            Assert.Null(Game.GemSkillInstanceDisabled(conn, gem.Effect, gem.Id));
        });
    }

    [Fact]
    public void A_spell_linked_to_a_trigger_support_stays_trigger_only()
    {
        WithCharacter(conn =>
        {
            EquipBow(conn, "SkillGemFireball", "SupportGemCastOnDamageTaken", bow: false);
            var gem = Game.GemSkillEffects(conn).Single(g => g.Name == "Fireball");
            Assert.Contains("trigger", Game.GemSkillInstanceDisabled(conn, gem.Effect, gem.Id) ?? "");
        });
    }

    /// <summary>A bow with the attack and the support in one link group, so the support applies to it.</summary>
    static void EquipBow(ClientState conn, string active, string support, bool bow = true)
    {
        var attack = ItemCatalog.Create(Program.Items.Find("Metadata/Items/Gems/" + active)!);
        var trigger = ItemCatalog.Create(Program.Items.Find("Metadata/Items/Gems/" + support)!);
        var weapon = ItemCatalog.Create(Program.Items.Find(bow ? "Crude Bow" : "Driftwood Wand")!) with
        {
            Sockets = [new(SocketColour.White, attack), new(SocketColour.White, trigger)],
            Links = [2],
        };
        Game.ItemsOf(conn).Add(PlayerInventories.Weapon1, 0, 0, weapon);
    }

    static void WithCharacter(Action<ClientState> run)
    {
        string path = Path.Combine(Path.GetTempPath(), $"triggered-attack-{Guid.NewGuid():N}.db");
        var old = Program.GameLogicOrNull;
        var oldLifetime = Game.CombatLifetime;
        var lifetime = new ModuleLifetime("Game");
        Game.CombatLifetime = lifetime;
        Saves.Open(path);
        Program.SetGameLogic(new GameServer.Modules.Game.GameLogic());
        var conn = new ClientState(new(1, "trigger", 1000, 1000, 100, 100, null, []), 1, 1)
        {
            CriticalRandom = new ZeroRandom(), Character = "Trigger" + Guid.NewGuid().ToString("N"), PlayerLife = 1000, PlayerMana = 1000,
            Instance = new(),
            Pools = new(100, 1000, 1000, int.MaxValue, 0, Strength: 1000, Dexterity: 1000, Intelligence: 1000),
            LeagueModule = new(new League("test", "Standard", false, false), new HashSet<string>(["Game"])),
        };
        conn.Clock.Stop();
        foreach (string name in new[] { "virtual_strength_for_requirements", "virtual_dexterity_for_requirements", "virtual_intelligence_for_requirements" })
            conn.PlayerStats[Program.Stats.Key(name)!.Value] = 1000;
        try { run(conn); }
        finally
        {
            PlayerInventories.Forget(conn.Character!);
            lifetime.Stop(_ => { });
            Game.CombatLifetime = oldLifetime;
            Program.SetGameLogic(old!);
            TestEnvironment.ReleaseSaves(path);
        }
    }

    sealed class ZeroRandom : Random { public override int Next(int maxValue) => 0; }
}
