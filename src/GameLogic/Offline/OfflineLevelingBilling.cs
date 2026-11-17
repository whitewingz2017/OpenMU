// <copyright file="OfflineLevelingBilling.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Offline;

/// <summary>
/// Bills a paid offline session while it runs. Prepaid hours end the session when they are over; a session paid
/// every hour pays the next hour when the paid one ends, and stops when it can't. Protection which can't be paid
/// any more ends, the leveling goes on.
/// </summary>
internal sealed class OfflineLevelingBilling
{
    private static readonly TimeSpan Hour = TimeSpan.FromHours(1);

    private readonly OfflinePlayer _player;
    private readonly OfflineLevelingPlan _plan;
    private readonly OfflineLevelingSettings _settings;
    private DateTime _paidUntilUtc;

    /// <summary>Initializes a new instance of the <see cref="OfflineLevelingBilling"/> class.</summary>
    /// <param name="player">The offline player, which paid the start.</param>
    /// <param name="plan">The plan.</param>
    /// <param name="settings">The settings.</param>
    public OfflineLevelingBilling(OfflinePlayer player, OfflineLevelingPlan plan, OfflineLevelingSettings settings)
    {
        this._player = player;
        this._plan = plan;
        this._settings = settings;
        this._paidUntilUtc = player.StartTimestamp + (Hour * Math.Max(1, plan.Hours));
    }

    /// <summary>
    /// Gets a value indicating whether the MU Helper's zen is paid besides: level-based zen, paid as it goes.
    /// </summary>
    public bool PaysMuHelperZen => this._plan.Hours == 0 && OfflineLevelingCosts.IsLevelBasedZen(this._settings, this._plan.Currency);

    /// <summary>Pays the next hour when the paid one ended.</summary>
    /// <returns><c>false</c> when the session ends.</returns>
    public bool Tick()
    {
        if (DateTime.UtcNow < this._paidUntilUtc)
        {
            return true;
        }

        if (this._plan.Hours > 0)
        {
            this._player.Logger.LogInformation("The {Hours} prepaid offline hours of {Name} are over.", this._plan.Hours, this._player.Name);
            return false;
        }

        if (!this.PayLeveling())
        {
            this._player.Logger.LogInformation("{Name} can't pay the next offline hour; the session ends.", this._player.Name);
            return false;
        }

        this.PayProtection();
        this._paidUntilUtc += Hour;
        return true;
    }

    private bool PayLeveling()
        => OfflineLevelingCosts.IsLevelBasedZen(this._settings, this._plan.Currency)
           || OfflineLevelingCosts.TryPay(this._player, [OfflineLevelingCosts.LevelingPerHour(this._player, this._settings, this._plan.Currency)]);

    private void PayProtection()
    {
        if (!this._player.IsProtectedFromPlayers
            || OfflineLevelingCosts.TryPay(this._player, [OfflineLevelingCosts.ProtectionPerHour(this._settings, this._plan.Currency)]))
        {
            return;
        }

        this._player.IsProtectedFromPlayers = false;
        this._player.Logger.LogInformation("{Name} can't pay the next hour of protection; players can damage it now.", this._player.Name);
    }
}
