using System;
using System.Threading.Tasks;
using SwimClub.Application.Finance;
using SwimClub.Application.Security;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Persistence;

namespace SwimClub.Infrastructure.Finance;

public class ExpenseService : IExpenseService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public ExpenseService(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<int> RecordExpenseAsync(decimal amount, string description, string paymentMethod, DateTime date)
    {
        var userId = _currentUserService.CurrentUser?.UserId ?? 0;

        var expense = new Expense
        {
            Amount = amount,
            Description = description,
            Category = paymentMethod,
            ExpenseDate = DateOnly.FromDateTime(date),
            RecordedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Expenses.Add(expense);

        // "Direct 1:1, unchanged â€” one Expense, one Transaction, no other path creates an EXPENSE-type transaction."
        var transaction = new Transaction
        {
            TransactionType = "EXPENSE",
            Amount = -Math.Abs(amount), // Expenses are negative
            RelatedEntityType = "EXPENSE",
            RecordedBy = userId,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Transactions.Add(transaction);

        await _dbContext.SaveChangesAsync();

        // EF Core populates ExpenseId after SaveChangesAsync
        transaction.RelatedEntityId = expense.ExpenseId;
        expense.LinkedTransaction = transaction;

        await _dbContext.SaveChangesAsync();

        return expense.ExpenseId;
    }
}
