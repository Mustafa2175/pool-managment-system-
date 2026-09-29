using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Finance;
using SwimClub.Application.Security;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Persistence;

namespace SwimClub.Infrastructure.Finance;

public class RefundService : IRefundService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public RefundService(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<RefundResult> ConfirmRefundAsync(int refundId)
    {
        var refund = await _dbContext.Refunds
            .Include(r => r.LinkedTransaction)
            .FirstOrDefaultAsync(r => r.RefundId == refundId);

        if (refund == null) return RefundResult.NotFound;

        // If it already has a transaction, it was already confirmed
        if (refund.LinkedTransaction != null)
        {
            return RefundResult.AlreadyConfirmed;
        }

        var userId = _currentUserService.CurrentUser?.UserId ?? 0;

        var transaction = new Transaction
        {
            TransactionType = "REFUND",
            Amount = -Math.Abs(refund.Amount), // Refunds are strictly negative in the ledger
            RelatedEntityType = "REFUND",
            RelatedEntityId = refund.RefundId,
            RecordedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Transactions.Add(transaction);
        refund.LinkedTransaction = transaction;

        await _dbContext.SaveChangesAsync();
        return RefundResult.Success;
    }

    public async Task<RefundResult> AdjustRefundAsync(int refundId, decimal adjustmentAmount, string reason)
    {
        var refund = await _dbContext.Refunds.FindAsync(refundId);
        if (refund == null) return RefundResult.NotFound;

        var userId = _currentUserService.CurrentUser?.UserId ?? 0;

        // "Once a Refund is confirmed, it is PERMANENTLY LOCKED... AdjustRefundUseCase creates a REFUND_ADJUSTMENT transaction"
        var transaction = new Transaction
        {
            TransactionType = "REFUND_ADJUSTMENT",
            Amount = adjustmentAmount, // Can be positive or negative depending on adjustment
            RelatedEntityType = "REFUND",
            RelatedEntityId = refund.RefundId,
            RecordedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Transactions.Add(transaction);

        await _dbContext.SaveChangesAsync();
        return RefundResult.Success;
    }
}
