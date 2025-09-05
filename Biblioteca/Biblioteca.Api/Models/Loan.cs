namespace Biblioteca.Api.Models;

public static class LoanStatus
{
	public const string PENDING = "PENDING";
	public const string OPEN = "OPEN";
	public const string RETURNED = "RETURNED";
	public const string REJECTED = "REJECTED";
}

public class Loan
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public Guid BookId { get; set; }
	public Book Book { get; set; } = default!;

	public Guid UserId { get; set; }
	public Biblioteca.Api.Models.User User { get; set; } = default!; // << AQUI

	public DateOnly LoanDate { get; set; }
	public string Status { get; set; } = LoanStatus.PENDING;

	public Guid? ApprovedById { get; set; }
	public DateTimeOffset? ApprovedAt { get; set; }
	public string? RejectionReason { get; set; }

	public DateOnly? ReturnDate { get; set; }
}
