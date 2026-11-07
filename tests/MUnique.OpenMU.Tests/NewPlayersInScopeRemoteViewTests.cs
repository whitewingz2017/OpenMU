// <copyright file="NewPlayersInScopeRemoteViewTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Buffers.Binary;
using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameServer;
using MUnique.OpenMU.GameServer.RemoteView;
using MUnique.OpenMU.GameServer.RemoteView.World;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.Pathfinding;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Tests the packets which are sent when players come into the scope of a player.
/// </summary>
[TestFixture]
public class NewPlayersInScopeRemoteViewTests
{
    private const ushort EliteSkeletonSkin = 372;

    /// <summary>
    /// The offset of the skin of the first character in <see cref="AddTransformedCharactersToScope"/>.
    /// </summary>
    private const int SkinOffsetOfFirstCharacter = 9;

    private static ClientVersion ExtendedClient { get; } = new(106, 3, ClientLanguage.Invariant);

    /// <summary>
    /// A transformed player is sent to a client with extended appearance data in the extended layout of 0x45:
    /// the appearance data of the extended serializer, followed by the effects.
    /// </summary>
    [Test]
    public async ValueTask TransformedPlayerIsSentWithExtendedAppearanceAsync()
    {
        var (observer, output) = CreateObserver(ExtendedClient);
        var added = await CreateTransformedPlayerAsync().ConfigureAwait(false);

        await new NewPlayersInScopeExtendedPlugIn(observer).NewPlayersInScopeAsync([added]).ConfigureAwait(false);

        var packets = SplitPackets(output.ToArray());
        Assert.That(packets, Has.Count.EqualTo(1));
        AddTransformedCharacterToScopeExtended packet = packets[0].AsMemory();
        var serializer = observer.AppearanceSerializer;
        var expectedAppearance = new byte[serializer.NeededSpace];
        serializer.WriteAppearanceData(expectedAppearance, added.AppearanceData, true);
        Assert.Multiple(() =>
        {
            Assert.That(serializer, Is.InstanceOf<AppearanceSerializerExtended>());
            Assert.That(packet.Header.Type, Is.EqualTo(0xC2));
            Assert.That(packet.Header.Code, Is.EqualTo(0x45));
            Assert.That(packet.Header.Length, Is.EqualTo(packets[0].Length));
            Assert.That(packet.CharacterCount, Is.EqualTo(1));
            Assert.That(packet.Id & 0x7FFF, Is.EqualTo(added.GetId(observer)));
            Assert.That(packet.Skin, Is.EqualTo(EliteSkeletonSkin));
            Assert.That(packet.Name, Is.EqualTo(added.SelectedCharacter!.Name));
            Assert.That(packet.AppearanceAndEffects[..serializer.NeededSpace].ToArray(), Is.EqualTo(expectedAppearance));
            Assert.That(packet.AppearanceAndEffects[serializer.NeededSpace], Is.EqualTo(0), "effect count");
            Assert.That(packet.AppearanceAndEffects.Length, Is.EqualTo(serializer.NeededSpace + 1));
        });
    }

    /// <summary>
    /// The bytes of a transformed player in the extended layout of 0x45 are at the offsets where the
    /// client reads them (PCREATE_TRANSFORM_EXTENDED), independent of the generated packet accessors.
    /// </summary>
    /// <param name="isSpawned">If set to <c>true</c>, the player is sent as spawned.</param>
    [TestCase(true)]
    [TestCase(false)]
    public async ValueTask TransformedPlayerBytesMatchTheClientLayoutAsync(bool isSpawned)
    {
        var (observer, output) = CreateObserver(ExtendedClient);
        var added = await CreateTransformedPlayerAsync().ConfigureAwait(false);
        added.Id = 0x1234;
        added.Position = new Point(130, 140);
        added.Rotation = Direction.South;
        await added.MagicEffectList.AddEffectAsync(new MagicEffect(TimeSpan.FromMinutes(1), new MagicEffectDefinition { Number = 5, InformObservers = true })).ConfigureAwait(false);
        await added.MagicEffectList.AddEffectAsync(new MagicEffect(TimeSpan.FromMinutes(1), new MagicEffectDefinition { Number = 9, InformObservers = true })).ConfigureAwait(false);

        await new NewPlayersInScopeExtendedPlugIn(observer).NewPlayersInScopeAsync([added], isSpawned).ConfigureAwait(false);

        var packet = SplitPackets(output.ToArray()).Single();
        var id = added.GetId(observer);
        var effects = packet[52..].OrderBy(e => e).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(packet, Has.Length.EqualTo(52 + 2), "5 header bytes, 47 fixed entry bytes and 2 effects");
            Assert.That(packet[..5], Is.EqualTo(new byte[] { 0xC2, 0, 54, 0x45, 1 }));
            Assert.That(packet[5], Is.EqualTo((byte)((id >> 8) | (isSpawned ? 0x80 : 0))), "KeyH");
            Assert.That(packet[6], Is.EqualTo((byte)id), "KeyL");
            Assert.That(packet[7..9], Is.EqualTo(new byte[] { 130, 140 }), "position");
            Assert.That(packet[9..11], Is.EqualTo(new byte[] { EliteSkeletonSkin >> 8, EliteSkeletonSkin & 0xFF }), "TypeH, TypeL");
            Assert.That(packet[11..21], Is.EqualTo("Skeleton\0\0"u8.ToArray()), "name");
            Assert.That(packet[21..23], Is.EqualTo(new byte[] { 130, 140 }), "target");
            Assert.That(packet[23] >> 4, Is.EqualTo(Direction.South.ToPacketByte()), "rotation");
            Assert.That(packet[51], Is.EqualTo(2), "effect count");
            Assert.That(effects, Is.EqualTo(new byte[] { 5, 9 }), "effects");
        });
    }

    /// <summary>
    /// A transformed player doesn't prevent the players after it from being sent.
    /// </summary>
    [Test]
    public async ValueTask PlayersAfterTransformedPlayerAreSentAsync()
    {
        var (observer, output) = CreateObserver(ExtendedClient);
        var transformed = await CreateTransformedPlayerAsync().ConfigureAwait(false);
        var normal = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        normal.SelectedCharacter!.Name = "Normal";

        await new NewPlayersInScopeExtendedPlugIn(observer).NewPlayersInScopeAsync([transformed, normal]).ConfigureAwait(false);

        var packets = SplitPackets(output.ToArray());
        Assert.That(packets.Select(p => p[3]), Is.EqualTo(new byte[] { AddTransformedCharacterToScopeExtendedRef.Code, AddCharacterToScopeExtendedRef.Code }));
    }

    /// <summary>
    /// A transformed player is still sent to a client with the standard appearance data in the standard layout of 0x45.
    /// </summary>
    [Test]
    public async ValueTask TransformedPlayerIsSentWithStandardAppearanceAsync()
    {
        var (observer, output) = CreateObserver(new ClientVersion(6, 3, ClientLanguage.Invariant));
        var added = await CreateTransformedPlayerAsync().ConfigureAwait(false);

        await new NewPlayersInScopePlugIn(observer).NewPlayersInScopeAsync([added]).ConfigureAwait(false);

        var packets = SplitPackets(output.ToArray());
        Assert.That(packets, Has.Count.EqualTo(1));
        AddTransformedCharactersToScope packet = packets[0].AsMemory();
        Assert.Multiple(() =>
        {
            Assert.That(observer.AppearanceSerializer, Is.InstanceOf<AppearanceSerializer>());
            Assert.That(packet.Header.Code, Is.EqualTo(0x45));
            Assert.That(packet.CharacterCount, Is.EqualTo(1));
            Assert.That(BinaryPrimitives.ReadUInt16BigEndian(packets[0].AsSpan(SkinOffsetOfFirstCharacter)), Is.EqualTo(EliteSkeletonSkin));
        });
    }

    /// <summary>Companion appearance follows the normal or transformed character without replacing its mount.</summary>
    /// <param name="transformed">Whether to send a transformed character.</param>
    [TestCase(false)]
    [TestCase(true)]
    public async ValueTask CompanionIsSentAfterCharacterAsync(bool transformed)
    {
        var (observer, output) = CreateObserver(ExtendedClient);
        var added = transformed ? await CreateTransformedPlayerAsync().ConfigureAwait(false)
            : await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var slot = new MUnique.OpenMU.Persistence.BasicModel.ItemSlotType();
        slot.ItemSlots.Add(GameLogic.Pets.CompanionEquipmentRules.Slot);
        var pet = new MUnique.OpenMU.Persistence.BasicModel.Item
        {
            Definition = new MUnique.OpenMU.Persistence.BasicModel.ItemDefinition
            {
                Group = 13, Number = 200,
                ItemSlot = slot,
            },
            ItemSlot = GameLogic.Pets.CompanionEquipmentRules.Slot,
            Durability = 1,
        };
        added.SelectedCharacter!.Inventory!.Items.Add(pet);
        await new NewPlayersInScopeExtendedPlugIn(observer).NewPlayersInScopeAsync([added]).ConfigureAwait(false);
        var packets = SplitPackets(output.ToArray());
        Assert.That(packets, Has.Count.EqualTo(2));
        AppearanceChangedExtended appearance = packets[1].AsMemory();
        Assert.That(appearance.ItemSlot, Is.EqualTo(GameLogic.Pets.CompanionEquipmentRules.Slot));
        Assert.That(appearance.ItemGroup, Is.EqualTo(13));
        Assert.That(appearance.ItemNumber, Is.EqualTo(200));
    }

    /// <summary>Observers receive the same custom set and level both on entry and equipment changes.</summary>
    /// <param name="level">The equipped skin's upgrade level.</param>
    [TestCase(0)]
    [TestCase(15)]
    public async ValueTask CustomSetKeepsItsModelsAndLevelForObserversAsync(byte level)
    {
        const short setNumber = 418;
        var (observer, output) = CreateObserver(ExtendedClient);
        var added = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var skinSlot = new MUnique.OpenMU.Persistence.BasicModel.ItemSlotType();
        skinSlot.ItemSlots.Add(GameLogic.Cosmetics.CosmeticArmorRules.FirstSlot + 1);
        for (byte group = 7; group <= 11; ++group)
        {
            added.GameContext.Configuration.Items.Add(new MUnique.OpenMU.Persistence.BasicModel.ItemDefinition
            {
                Group = group, Number = setNumber, ItemSlot = group == 8 ? skinSlot : null,
            });
        }

        var skin = new MUnique.OpenMU.Persistence.BasicModel.Item
        {
            Definition = added.GameContext.Configuration.Items.Single(i => i.Group == 8 && i.Number == setNumber),
            ItemSlot = GameLogic.Cosmetics.CosmeticArmorRules.FirstSlot + 1,
            Level = level,
            Durability = 1,
        };
        added.SelectedCharacter!.Inventory!.Items.Add(skin);
        await new NewPlayersInScopeExtendedPlugIn(observer).NewPlayersInScopeAsync([added]).ConfigureAwait(false);
        for (byte slot = 2; slot <= 6; ++slot)
        {
            await new AppearanceChangedExtendedPlugIn(observer).AppearanceChangedAsync(added, new TemporaryItem { ItemSlot = slot }, true).ConfigureAwait(false);
        }

        var packets = SplitPackets(output.ToArray());
        Assert.That(packets, Has.Count.EqualTo(6));
        AddCharacterToScopeExtended initial = packets[0].AsMemory();
        for (var part = 0; part < 5; ++part)
        {
            var offset = 8 + part * 3; // Class/flags, two weapons, then five armor pieces.
            var appearance = initial.AppearanceAndEffects.ToArray();
            AppearanceChangedExtended changed = packets[part + 1].AsMemory();
            Assert.Multiple(() =>
            {
                Assert.That(appearance[offset] >> 4, Is.EqualTo(7 + part));
                Assert.That(((appearance[offset] & 0xF) << 8) | appearance[offset + 1], Is.EqualTo(setNumber));
                Assert.That(appearance[offset + 2] >> 4, Is.EqualTo(level == 15 ? 7 : 0));
                Assert.That(changed.ItemSlot, Is.EqualTo(2 + part));
                Assert.That(changed.ItemGroup, Is.EqualTo(7 + part));
                Assert.That(changed.ItemNumber, Is.EqualTo(setNumber));
                Assert.That(changed.ItemLevel, Is.EqualTo(level));
            });
        }

        Assert.That(skin.Level, Is.EqualTo(level));
    }

    private static async ValueTask<Player> CreateTransformedPlayerAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.SelectedCharacter!.Name = "Skeleton";
        player.Attributes!.GetOrCreateAttribute(Stats.TransformationSkin);
        player.Attributes.GetComposableAttribute(Stats.TransformationSkin)!.AddElement(new SimpleElement(EliteSkeletonSkin, AggregateType.AddRaw));
        return player;
    }

    private static (RemotePlayer Player, MemoryStream Output) CreateObserver(ClientVersion clientVersion)
    {
        var manager = new PlugInManager(null, new NullLoggerFactory(), null, null);
        manager.RegisterPlugIn<IViewPlugIn, AppearanceSerializer>();
        manager.RegisterPlugIn<IViewPlugIn, AppearanceSerializerExtended>();
        manager.RegisterPlugIn<IViewPlugIn, AppearanceChangedExtendedPlugIn>();
        var gameContext = new Mock<IGameServerContext>();
        gameContext.Setup(c => c.PersistenceContextProvider).Returns(new Mock<IPersistenceContextProvider>().Object);
        gameContext.Setup(c => c.Configuration).Returns(new GameConfiguration());
        gameContext.Setup(c => c.PlugInManager).Returns(manager);
        gameContext.Setup(c => c.LoggerFactory).Returns(new NullLoggerFactory());
        var (player, output) = CastleSiegeRemoteViewTestHelper.CreatePlayer(gameContext.Object);
        player.ClientVersion = clientVersion;
        return (player, output);
    }

    private static List<byte[]> SplitPackets(byte[] data)
    {
        var result = new List<byte[]>();
        var offset = 0;
        while (offset < data.Length)
        {
            var length = data[offset] is 0xC1 or 0xC3
                ? data[offset + 1]
                : (data[offset + 1] << 8) | data[offset + 2];
            result.Add(data[offset..(offset + length)]);
            offset += length;
        }

        return result;
    }
}
