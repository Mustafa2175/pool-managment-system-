using System.Threading.Tasks;

namespace SwimClub.Application.Finance;

public enum PaymentResult
{
    Success,
    OverpaymentRejected,
    NotFound
}

public interface IPaymentService
{
    Task<PaymentResult> RecordPaymentAsync(string payableType, int payableId, decimal amount, string paymentMethod, string? notes);
    Task<PaymentResult> CorrectPaymentAsync(int paymentId, decimal newAmount, string reason);
}
