namespace Biblioteca.Api.Contracts;

// ====== AUTH ======
public record RegisterDto(
	string Name,
	string Email,
	string Password,
	string Role // "USER" | "LIBRARIAN"
);

public record LoginDto(
	string Email,
	string Password
);

public record LoanApproveDto(
	string? Reason
);

// ====== USERS (legado) ======
public record UserCreateDto(
	string Name,
	string Email,
	bool Active
);

public record UserUpdateDto(
	string Name,
	string Email,
	bool Active
);

// ====== BOOKS ======
public record BookCreateDto(
	string Title,
	string Author,
	string? Isbn
);

public record BookUpdateDto(
	string Title,
	string Author,
	string? Isbn
);

// ====== SHELVES ======
public record ShelfCreateDto(
	string Code,
	string? Description
);

public record ShelfUpdateDto(
	string Code,
	string? Description
);

// ====== PLACEMENT ======
public record PlacementUpsertDto(
	Guid BookId,
	Guid ShelfId,
	string? Position
);

// ====== LOANS ======
public record LoanCreateDto(
	Guid UserId,
	Guid BookId,
	string LoanDate // yyyy-MM-dd
);
