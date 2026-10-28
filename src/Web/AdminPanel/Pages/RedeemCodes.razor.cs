// <copyright file="RedeemCodes.razor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Pages;

using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence;

/// <summary>Administrator-only management of gift codes and redemption receipts.</summary>
public partial class RedeemCodes
{
    private RedeemCode _input = new();
    private Guid? _editingId;
    private List<RedeemCode> _codes = new();
    private List<RedeemCodeClaim> _claims = new();
    private List<RedeemItemReward> _items = new();
    private List<ItemDefinition> _availableItems = new();
    private string _status = string.Empty;
    private bool _busy = true, _saving;
    [Inject] private ILogger<RedeemCodes> Logger { get; set; } = null!;
    [Inject] private IPersistenceContextProvider Contexts { get; set; } = null!;
    [Inject] private IDataSource<GameConfiguration> ConfigurationSource { get; set; } = null!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        try
        {
            var config = await this.ConfigurationSource.GetOwnerAsync().ConfigureAwait(true);
            this._availableItems = config?.Items.OrderBy(i => i.Name.ValueInNeutralLanguage).ToList() ?? new();
            await this.LoadAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Loading redeem codes failed");
            this._status = "Could not load redeem codes. Check the database migration and server logs.";
        }
        finally { this._busy = false; }
    }

    private async Task LoadAsync()
    {
        using var context = this.Contexts.CreateNewContext();
        this._codes = (await context.GetAsync<RedeemCode>().ConfigureAwait(true)).OrderBy(c => c.Code).ToList();
        this._claims = (await context.GetAsync<RedeemCodeClaim>().ConfigureAwait(true)).OrderByDescending(c => c.RedeemedAt).Take(100).ToList();
    }

    private void NewCode() { this._input = new(); this._items = new(); this._editingId = null; }
    private void AddItem()
    {
        if (this._items.Count < 32 && this._availableItems.FirstOrDefault() is { } first)
            this._items.Add(new() { ItemCode = first.Group * 512 + first.Number });
    }
    private void Edit(RedeemCode code)
    {
        this._input = JsonSerializer.Deserialize<RedeemCode>(JsonSerializer.Serialize(code))!;
        this._editingId = code.Id;
        this._items = JsonSerializer.Deserialize<List<RedeemItemReward>>(code.ItemsJson) ?? new();
    }
    private async Task SaveAsync()
    {
        if (this._saving) return;
        this._saving = true;
        try
        {
            var normalized = RedeemCodeRules.Normalize(this._input.Code);
            if (normalized.Length == 0) throw new ArgumentException("Use 4–32 letters, digits or hyphens for the code.");
            this._input.ItemsJson = JsonSerializer.Serialize(this._items);
            var rewards = RedeemCodeRules.Rewards(this._input);
            if (rewards.Any(r => !this._availableItems.Any(i => i.Group * 512 + i.Number == r.ItemCode))) throw new ArgumentException("Choose valid reward items.");
            using var context = this.Contexts.CreateNewContext();
            var id = RedeemCodeRules.Key(normalized);
            var code = await context.GetByIdAsync<RedeemCode>(id).ConfigureAwait(true);
            if (this._editingId == null && code != null) throw new ArgumentException("This code already exists. Edit it instead.");
            if (this._editingId != null && (code == null || code.Revision != this._input.Revision)) throw new ArgumentException("This code changed since you opened it. Reload and try again.");
            code ??= context.CreateNew<RedeemCode>();
            code.Id = id; code.Code = normalized; code.Description = this._input.Description;
            code.Enabled = this._input.Enabled; code.ExpiresAt = this._input.ExpiresAt is { } expires ? DateTime.SpecifyKind(expires, DateTimeKind.Utc) : null;
            code.MaximumUses = this._input.MaximumUses; code.ItemsJson = this._input.ItemsJson;
            code.WCoinC = this._input.WCoinC; code.WCoinP = this._input.WCoinP; code.GoblinPoints = this._input.GoblinPoints;
            if (this._editingId == null) code.PerCharacter = this._input.PerCharacter;
            code.Revision = Guid.NewGuid();
            if (!await context.SaveChangesAsync().ConfigureAwait(true)) throw new InvalidOperationException("Save was not committed.");
            this._status = "Code saved.";
            this.NewCode();
            await this.LoadAsync().ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is ArgumentException or JsonException) { this._status = ex.Message; }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Saving redeem code failed");
            this._status = "Could not save the code. Reload and try again; check server logs if it persists.";
        }
        finally { this._saving = false; }
    }
}
