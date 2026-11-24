// <copyright file="Ashwing.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Maps;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence.Initialization.Skills;

/// <summary>
/// The custom Icarus phoenix and its single spawn.
/// </summary>
internal static class Ashwing
{
    internal const short Number = 669;
    private const short SpawnNumber = 669;

    internal static void Ensure(IContext context, GameConfiguration config, GameMapDefinition map)
    {
        var monster = config.Monsters.FirstOrDefault(m => m.Number == Number);
        if (monster is null)
        {
            monster = context.CreateNew<MonsterDefinition>();
            monster.Number = Number;
            monster.SetGuid(Number);
            monster.Designation = "Ashwing";
            monster.MoveRange = 2;
            monster.ViewRange = 7;
            monster.AttackRange = 5;
            monster.MoveDelay = TimeSpan.FromMilliseconds(420);
            monster.AttackDelay = TimeSpan.FromSeconds(2);
            monster.RespawnDelay = TimeSpan.FromMinutes(3);
            monster.Attribute = 2;
            monster.NumberOfMaximumItemDrops = 1;
            monster.AttackSkill = config.Skills.Single(s => s.Number == (short)SkillNumber.FireBall);
            monster.AddAttributes(new Dictionary<AttributeDefinition, float>
            {
                { Stats.Level, 110 },
                { Stats.MaximumHealth, 110000 },
                { Stats.MinimumPhysBaseDmg, 930 },
                { Stats.MaximumPhysBaseDmg, 1040 },
                { Stats.DefenseBase, 610 },
                { Stats.AttackRatePvm, 920 },
                { Stats.DefenseRatePvm, 355 },
            }, context, config);
            config.Monsters.Add(monster);
        }

        if (monster.Designation.ToString() != "Ashwing" || map.MonsterSpawns.Any(s => s.MonsterDefinition == monster || s.GetId() == GuidHelper.CreateGuid<MonsterSpawnArea>(map.Number, SpawnNumber)))
        {
            return;
        }

        var spawn = context.CreateNew<MonsterSpawnArea>();
        spawn.SetGuid(map.Number, SpawnNumber);
        spawn.GameMap = map;
        spawn.MonsterDefinition = monster;
        spawn.X1 = spawn.X2 = 47;
        spawn.Y1 = spawn.Y2 = 189;
        spawn.Quantity = 1;
        spawn.SpawnTrigger = SpawnTrigger.Automatic;
        spawn.Direction = Direction.Undefined;
        map.MonsterSpawns.Add(spawn);
    }
}
