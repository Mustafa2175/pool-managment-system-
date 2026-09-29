using System.Threading.Tasks;

namespace SwimClub.Application.Finance;

public enum RefundResult
{
    Success,
    AlreadyConfirmed,
    NotFound
}

public interface IRefundService
{
    Task<RefundResult> ConfirmRefundAsync(int refundId);
    Task<RefundResult> AdjustRefundAsync(int refundId, decimal adjustmentAmount, string reason);
}
