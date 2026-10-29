// <copyright file="StorePricePart.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;

/// <summary>One required currency component of a personal-store price.</summary>
public sealed record StorePricePart(int Type, int Amount);
