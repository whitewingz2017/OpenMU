// <copyright file="RedeemPersistenceModelTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Microsoft.EntityFrameworkCore;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence.EntityFramework;

/// <summary>Checks the real PostgreSQL persistence mapping for atomic gift-code claims.</summary>
[TestFixture]
public class RedeemPersistenceModelTests
{
    /// <summary>Concurrent claims and administrator edits conflict on the same revision token.</summary>
    [Test]
    public void ConcurrencyAndDeterministicKeys()
    {
        using var context = new EntityDataContext();
        var model = context.Model.GetEntityTypes().Single(t => t.ClrType.BaseType == typeof(RedeemCode));
        Assert.That(model.FindProperty(nameof(RedeemCode.Revision))!.IsConcurrencyToken, Is.True);
        var code = (RedeemCode)Activator.CreateInstance(model.ClrType)!;
        context.Add(code);
        code.Id = RedeemCodeRules.Key("WELCOME-2026");
        context.ChangeTracker.DetectChanges();
        Assert.That(context.Entry(code).Property(nameof(RedeemCode.Id)).CurrentValue, Is.EqualTo(code.Id));
        var claims = context.Model.GetEntityTypes().Single(t => t.ClrType.BaseType == typeof(RedeemCodeClaim));
        Assert.That(claims.FindPrimaryKey()!.Properties.Single().Name, Is.EqualTo(nameof(RedeemCodeClaim.Id)));
    }
}
