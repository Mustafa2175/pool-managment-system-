using System;
using System.Threading.Tasks;

namespace SwimClub.Application.Finance;

public interface IExpenseService
{
    Task<int> RecordExpenseAsync(decimal amount, string description, string paymentMethod, DateTime date);
}
