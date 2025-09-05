namespace Biblioteca.Api.Models;

public class User
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Name { get; set; } = default!;
	public string Email { get; set; } = default!;
	public bool Active { get; set; } = true;

	public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
