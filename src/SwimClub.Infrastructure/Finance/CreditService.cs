using System;
using System.Threading.Tasks;
using SwimClub.Application.Finance;
using SwimClub.Application.Security;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Persistence;

namespace SwimClub.Infrastructure.Finance;

public class CreditService : ICreditService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CreditService(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<int> GrantCreditAsync(string generatedFromType, int generatedFromId, decimal amount, string description)
    {
        string swimmerId = "";

        if (generatedFromType == "TRAINING_SUBSCRIPTION")
        {
            var ts = await _dbContext.TrainingSubscriptions.FindAsync(generatedFromId);
            if (ts == null) throw new ArgumentException("Source not found");
            swimmerId = ts.SwimmerId;
        }
        else if (generatedFromType == "PACKAGE")
        {
            var pkg = await _dbContext.Packages.FindAsync(generatedFromId);
            if (pkg == null) throw new ArgumentException("Source not found");
            swimmerId = pkg.SwimmerId;
        }
        else
        {
            throw new ArgumentException("Credit can only be generated from TRAINING_SUBSCRIPTION or PACKAGE.");
        }

        var credit = new Credit
        {
            SwimmerId = swimmerId,
            GeneratedFromType = generatedFromType,
            GeneratedFromId = generatedFromId,
            Amount = amount,
            RemainingAmount = amount,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Credits.Add(credit);
        
        // Note: No Transaction is produced at grant time (Decision 1)
        await _dbContext.SaveChangesAsync();

        return credit.CreditId;
    }

    public async Task<CreditResult> UseCreditAsync(int creditId, decimal amount, string usageType, int usageId)
    {
        var credit = await _dbContext.Credits.FindAsync(creditId);
        if (credit == null) return CreditResult.NotFound;

        if (credit.RemainingAmount < amount) return CreditResult.InsufficientBalance;

        credit.RemainingAmount -= amount;

        var usage = new CreditUsage
        {
            CreditId = creditId,
            AppliedToEntityType = usageType, // "TRAINING_SUBSCRIPTION" or "PACKAGE"
            AppliedToEntityId = usageId,
            AmountUsed = amount,
            UsedAt = DateTime.UtcNow,
            RecordedBy = _currentUserService.CurrentUser?.UserId ?? 0
        };
        _dbContext.CreditUsages.Add(usage);

        var userId = _currentUserService.CurrentUser?.UserId ?? 0;

        // "Revenue recognized at usage date via Transaction(CREDIT_USAGE, positive)"
        var transaction = new Transaction
        {
            TransactionType = "CREDIT_USAGE",
            Amount = amount, // Positive revenue
            RelatedEntityType = usageType,
            RelatedEntityId = usageId,
            RecordedBy = userId,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Transactions.Add(transaction);

        await _dbContext.SaveChangesAsync();

        return CreditResult.Success;
    }
}
