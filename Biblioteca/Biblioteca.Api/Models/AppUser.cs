namespace Biblioteca.Api.Models;

public enum Role
{
	USER = 0,
	LIBRARIAN = 1
}

public class AppUser
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Name { get; set; } = default!;
	public string Email { get; set; } = default!;
	public string PasswordHash { get; set; } = default!;
	public Role Role { get; set; } = Role.USER;
}
