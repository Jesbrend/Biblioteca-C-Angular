namespace Biblioteca.Api.Models;

public class Book
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Title { get; set; } = null!;
	public string Author { get; set; } = null!;
	public string? Isbn { get; set; }
	public bool IsLoaned { get; set; } = false;

	public List<Loan> Loans { get; set; } = new();
	public Placement? Placement { get; set; }
}
