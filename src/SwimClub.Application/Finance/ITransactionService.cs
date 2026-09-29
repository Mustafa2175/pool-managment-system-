using System;
using System.Threading.Tasks;

namespace SwimClub.Application.Finance;

public interface ITransactionService
{
    Task<decimal> GetRevenueAsync(DateTime startDate, DateTime endDate);
    Task<decimal> GetExpensesAsync(DateTime startDate, DateTime endDate);
    Task<decimal> GetProfitAsync(DateTime startDate, DateTime endDate);
}
