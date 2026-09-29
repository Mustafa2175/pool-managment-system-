using System.Threading.Tasks;

namespace SwimClub.Application.Finance;

public enum CreditResult
{
    Success,
    InsufficientBalance,
    NotFound
}

public interface ICreditService
{
    Task<int> GrantCreditAsync(string generatedFromType, int generatedFromId, decimal amount, string description);
    Task<CreditResult> UseCreditAsync(int creditId, decimal amount, string usageType, int usageId);
}
