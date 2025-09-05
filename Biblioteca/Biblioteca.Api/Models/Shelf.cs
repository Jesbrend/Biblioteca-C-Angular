namespace Biblioteca.Api.Models;

public class Shelf
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Code { get; set; } = null!;
	public string? Description { get; set; }

	public List<Placement> Placements { get; set; } = new();
}
