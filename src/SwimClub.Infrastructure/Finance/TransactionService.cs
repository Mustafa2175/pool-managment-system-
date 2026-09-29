using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Finance;
using SwimClub.Infrastructure.Persistence;

namespace SwimClub.Infrastructure.Finance;

public class TransactionService : ITransactionService
{
    private readonly AppDbContext _dbContext;

    public TransactionService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<decimal> GetRevenueAsync(DateTime startDate, DateTime endDate)
    {
        // Revenue explicitly classified by these transaction types per Decision 11
        var revenueTypes = new[]
        {
            "TRAINING_PAYMENT",
            "PACKAGE_PAYMENT",
            "PRIVATE_PAYMENT",
            "RECREATIONAL_TICKET",
            "OUTSTANDING_PAYMENT",
            "CREDIT_USAGE",
            "REFUND",             // Negative
            "REFUND_ADJUSTMENT"   // Signed
        };

        var amounts = await _dbContext.Transactions
            .Where(t => revenueTypes.Contains(t.TransactionType) && t.CreatedAt >= startDate && t.CreatedAt <= endDate)
            .Select(t => t.Amount)
            .ToListAsync();

        return amounts.Sum();
    }

    public async Task<decimal> GetExpensesAsync(DateTime startDate, DateTime endDate)
    {
        var expenseTypes = new[]
        {
            "EXPENSE",
            "PAYROLL_PAYMENT",
            "PAYROLL_ADJUSTMENT"
        };

        var amounts = await _dbContext.Transactions
            .Where(t => expenseTypes.Contains(t.TransactionType) && t.CreatedAt >= startDate && t.CreatedAt <= endDate)
            .Select(t => t.Amount)
            .ToListAsync();

        return Math.Abs(amounts.Sum());
    }

    public async Task<decimal> GetProfitAsync(DateTime startDate, DateTime endDate)
    {
        var revenue = await GetRevenueAsync(startDate, endDate);
        var expenses = await GetExpensesAsync(startDate, endDate);
        return revenue - expenses;
    }
}
