namespace SwimClub.Domain.Entities;

/// <summary>Expense record — club operational expenses.</summary>
public class Expense
{
    public int ExpenseId { get; set; }
    public string Description { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateOnly ExpenseDate { get; set; }
    public string? Category { get; set; }
    public int RecordedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public User RecordedByUser { get; set; } = null!;
    public Transaction? LinkedTransaction { get; set; }
}
