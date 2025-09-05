namespace Biblioteca.Api.Models;

public class Placement
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public Guid BookId { get; set; }
	public Guid ShelfId { get; set; }
	public string? Position { get; set; }

	public Book Book { get; set; } = null!;
	public Shelf Shelf { get; set; } = null!;
}
