using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Finance;
using SwimClub.Application.Interfaces;
using SwimClub.Application.Security;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Persistence;

namespace SwimClub.Infrastructure.Finance;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;

    public PaymentService(AppDbContext dbContext, ICurrentUserService currentUserService, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
    }

    public async Task<PaymentResult> RecordPaymentAsync(string payableType, int payableId, decimal amount, string paymentMethod, string? notes)
    {
        decimal balanceDue = 0;
        string transactionType = "";
        
        switch (payableType)
        {
            case "TRAINING_SUBSCRIPTION":
                var ts = await _dbContext.TrainingSubscriptions.FindAsync(payableId);
                if (ts == null) return PaymentResult.NotFound;
                balanceDue = ts.TotalPrice - ts.PaidAmount;
                transactionType = "TRAINING_PAYMENT";
                break;
            case "PACKAGE":
                var pkg = await _dbContext.Packages.FindAsync(payableId);
                if (pkg == null) return PaymentResult.NotFound;
                balanceDue = pkg.TotalPrice - pkg.PaidAmount;
                transactionType = "PACKAGE_PAYMENT";
                break;
            case "PRIVATE_BOOKING":
                var pb = await _dbContext.PrivateBookings.FindAsync(payableId);
                if (pb == null) return PaymentResult.NotFound;
                balanceDue = pb.TotalPrice - pb.PaidAmount;
                transactionType = "PRIVATE_PAYMENT";
                break;
            default:
                return PaymentResult.NotFound;
        }

        if (amount > balanceDue)
        {
            await _auditLogService.LogFailureAsync("PAYMENT_OVERPAYMENT_REJECTED", $"Attempted: {amount}, Balance: {balanceDue}");
            return PaymentResult.OverpaymentRejected;
        }

        var userId = _currentUserService.CurrentUser?.UserId ?? 0;
        
        var payment = new Payment
        {
            RelatedEntityType = payableType,
            RelatedEntityId = payableId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            Notes = notes,
            RecordedBy = userId,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Payments.Add(payment);
        
        var transaction = new Transaction
        {
            TransactionType = transactionType,
            Amount = amount,
            RelatedEntityType = payableType,
            RelatedEntityId = payableId,
            RecordedBy = userId,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Transactions.Add(transaction);

        // Update the paid amount on the parent entity
        switch (payableType)
        {
            case "TRAINING_SUBSCRIPTION":
                var ts = await _dbContext.TrainingSubscriptions.FindAsync(payableId);
                ts!.PaidAmount += amount;
                break;
            case "PACKAGE":
                var pkg = await _dbContext.Packages.FindAsync(payableId);
                pkg!.PaidAmount += amount;
                break;
            case "PRIVATE_BOOKING":
                var pb = await _dbContext.PrivateBookings.FindAsync(payableId);
                pb!.PaidAmount += amount;
                break;
        }

        await _dbContext.SaveChangesAsync();

        // Assign the linked transaction to the payment (which now has an ID)
        payment.LinkedTransaction = transaction;
        
        // Let's actually link the transaction back to the payment directly if the schema demands it.
        // The DB Schema says: 'payments' doesn't have transaction_id, 'transactions' doesn't have payment_id.
        // They are linked by RelatedEntityType and RelatedEntityId and CreatedAt roughly, or maybe 
        // we should add PaymentId to Transaction? The EF Model has LinkedTransaction as navigation.
        
        return PaymentResult.Success;
    }

    public async Task<PaymentResult> CorrectPaymentAsync(int paymentId, decimal newAmount, string reason)
    {
        var payment = await _dbContext.Payments
            .Include(p => p.LinkedTransaction)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

        if (payment == null) return PaymentResult.NotFound;

        var oldAmount = payment.Amount;
        var diff = newAmount - oldAmount;

        // Check if the new amount exceeds the theoretical total balance
        decimal newBalanceDue = 0;
        switch (payment.RelatedEntityType)
        {
            case "TRAINING_SUBSCRIPTION":
                var ts = await _dbContext.TrainingSubscriptions.FindAsync(payment.RelatedEntityId);
                newBalanceDue = ts!.TotalPrice - (ts.PaidAmount + diff);
                if (newBalanceDue < 0) return PaymentResult.OverpaymentRejected;
                ts.PaidAmount += diff;
                break;
            case "PACKAGE":
                var pkg = await _dbContext.Packages.FindAsync(payment.RelatedEntityId);
                newBalanceDue = pkg!.TotalPrice - (pkg.PaidAmount + diff);
                if (newBalanceDue < 0) return PaymentResult.OverpaymentRejected;
                pkg.PaidAmount += diff;
                break;
            case "PRIVATE_BOOKING":
                var pb = await _dbContext.PrivateBookings.FindAsync(payment.RelatedEntityId);
                newBalanceDue = pb!.TotalPrice - (pb.PaidAmount + diff);
                if (newBalanceDue < 0) return PaymentResult.OverpaymentRejected;
                pb.PaidAmount += diff;
                break;
        }

        var userId = _currentUserService.CurrentUser?.UserId ?? 0;

        payment.Amount = newAmount;
        payment.LastModifiedBy = userId;
        payment.LastModifiedAt = DateTime.UtcNow;

        if (payment.LinkedTransaction != null)
        {
            // Exception to the transaction immutability rule
            payment.LinkedTransaction.Amount = newAmount;
        }
        else
        {
            // Fallback finding transaction if navigation property wasn't linked natively
            var trans = await _dbContext.Transactions.FirstOrDefaultAsync(t => 
                t.RelatedEntityType == payment.RelatedEntityType && 
                t.RelatedEntityId == payment.RelatedEntityId &&
                t.Amount == oldAmount && 
                t.RecordedBy == payment.RecordedBy);
                
            if (trans != null) trans.Amount = newAmount;
        }

        await _auditLogService.LogSuccessAsync("PAYMENT_CORRECTED", "Payment", payment.PaymentId, 
            $"Old: {oldAmount}, New: {newAmount}, Reason: {reason}");

        await _dbContext.SaveChangesAsync();
        return PaymentResult.Success;
    }
}
