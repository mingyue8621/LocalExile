using GameServer.Core.Protocol.Instance;
using GameServer.Core.World;

namespace GameServer.Gateway.Tests;

/// <summary>A released essence rare must be able to walk to the player. Its kit's first entry is the attack the AI
/// chases and swings with: an attack that plays where the actor stands (a spell, a summon) has no chase in
/// PlanMonsterAi, so a kit of essence summons left the rare standing in the broken crystal doing nothing.</summary>
public sealed class EssenceKitChaseTests : IDisposable
{
    readonly IGameLogic? previousLogic = Program.GameLogicOrNull;
    readonly GameServer.Modding.IModuleLifetime? previousLifetime = Game.CombatLifetime;
    readonly ModuleLifetime lifetime = new("Game");
    public EssenceKitChaseTests()
    {
        Game.CombatLifetime = lifetime;
        Program.SetGameLogic(new GameServer.Modules.Game.GameLogic());
    }
    public void Dispose()
    {
        lifetime.Stop(ex => throw ex);
        Game.CombatLifetime = previousLifetime;
        Program.SetGameLogic(previousLogic!);
    }
    static Player Connection() => new(new(1, "essence-kit", 300, 300, 100, 100, null, []), 1, 1) { Instance = new() };

    [Fact]
    public void A_released_rare_leads_its_kit_with_a_swing_it_can_chase_with()
    {
        var player = Connection();
        // The live 2026-11-26 rare: a melee Carrion Swarmer whose essences were summons alone.
        var variety = Game.Monsters.Find("Metadata/Monsters/InsectMinion/InsectWild")!;
        var mods = Game.MonsterModRolls.Create([], [], MonsterRarity.Rare, 26);
        var kit = global::MapEssences.EssenceKit(player, variety, 26, [], mods);
        var lead = Assert.Single(kit);
        Assert.False(lead.Attack.InPlace);
        Assert.Null(lead.Attack.Summon);
        Assert.False(lead.Attack.Damage.IsZero);
        // Its reach is the variety's own: the rare walks to where it fights from, never to a spell's aggro edge.
        Assert.Equal(variety.AttackRange, lead.Attack.Range);
    }
}
