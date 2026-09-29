using System.Collections.Generic;
using System.Threading.Tasks;
using SwimClub.Domain.Entities;

namespace SwimClub.Application.Training;

public interface ITrainingConfigService
{
    /// <summary>
    /// Returns all programs (Regular, Star, Team).
    /// </summary>
    Task<IReadOnlyList<Program>> GetProgramsAsync();

    /// <summary>
    /// Returns the currently active price for a given program and member status.
    /// </summary>
    Task<decimal> GetActivePriceAsync(int programId, string memberStatus);

    /// <summary>
    /// Sets a new effective price for the program and member status.
    /// Closes the previous active configuration if any.
    /// </summary>
    Task UpdatePriceAsync(int programId, string memberStatus, decimal newPrice);

    /// <summary>
    /// Retrieves the global session count configured for Training.
    /// Used during subscription generation (Decision 3).
    /// Defaults to 12 if not set.
    /// </summary>
    Task<int> GetGlobalSessionCountAsync();

    /// <summary>
    /// Updates the global session count.
    /// </summary>
    Task SetGlobalSessionCountAsync(int count);
}
