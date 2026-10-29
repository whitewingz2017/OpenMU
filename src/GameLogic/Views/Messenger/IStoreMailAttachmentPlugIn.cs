// <copyright file="IStoreMailAttachmentPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.Messenger;

/// <summary>Advertises a claimable item only for a server-owned mail attachment.</summary>
public interface IStoreMailAttachmentPlugIn : IViewPlugIn
{
    /// <summary>Updates the attachment state of the letter at the given index.</summary>
    ValueTask ShowAttachmentAsync(ushort index, bool available);
}
