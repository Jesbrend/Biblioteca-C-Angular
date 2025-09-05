using System.Security.Claims;
using System.Text;

using Biblioteca.Api.Contracts;
using Biblioteca.Api.Data;
using Biblioteca.Api.Models;
using Biblioteca.Api.Utils;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

// ============================ 1) Builder / Services ==========================
var builder = WebApplication.CreateBuilder(args);

// EF Core + Npgsql
builder.Services.AddDbContext<BibliotecaDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS (Angular em localhost:4200)
const string CorsPolicy = "DevCors";
builder.Services.AddCors(opt =>
{
    opt.AddPolicy(CorsPolicy, p =>
        p.WithOrigins("http://localhost:4200")
         .AllowAnyHeader()
         .AllowAnyMethod());
});

// Auth + JWT (lendo do appsettings Json: Jwt:Key)
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException("Jwt:Key precisa ter pelo menos 32 caracteres no appsettings.json.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("LibrarianOnly", p => p.RequireRole("LIBRARIAN"));
});

// ============================ 2) Build =======================================
var app = builder.Build();

// ============================ 3) DB Migrate + Seed ============================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BibliotecaDbContext>();
    db.Database.Migrate();

    // Seed de um bibliotecário se não existir
    if (!await db.AppUsers.AnyAsync(x => x.Role == Role.LIBRARIAN))
    {
        db.AppUsers.Add(new AppUser
        {
            Name = "Admin",
            Email = "admin@local",
            PasswordHash = AuthUtils.Hash("admin123"),
            Role = Role.LIBRARIAN
        });
        await db.SaveChangesAsync();
        Console.WriteLine("Seed: bibliotecário admin@local / admin123");
    }
}

// ============================ 4) Middlewares =================================
app.UseCors(CorsPolicy);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger"));

// ============================ 5) AUTH ========================================
var auth = app.MapGroup("/api/auth");

// Registro
auth.MapPost("/register", async (BibliotecaDbContext db, RegisterDto dto) =>
{
    var ok = Enum.TryParse<Role>(dto.Role, true, out var role);
    if (!ok) return Results.BadRequest(new { message = "Role inválida. Use USER ou LIBRARIAN." });

    var email = dto.Email.Trim().ToLowerInvariant();
    if (await db.AppUsers.AnyAsync(x => x.Email == email))
        return Results.Conflict(new { message = "E-mail já cadastrado." });

    var u = new AppUser
    {
        Name = dto.Name.Trim(),
        Email = email,
        PasswordHash = AuthUtils.Hash(dto.Password),
        Role = role
    };
    db.AppUsers.Add(u);
    await db.SaveChangesAsync();
    return Results.Created($"/api/auth/users/{u.Id}", new { u.Id, u.Name, u.Email, Role = u.Role.ToString() });
});

// Login
auth.MapPost("/login", async (BibliotecaDbContext db, IConfiguration cfg, LoginDto dto) =>
{
    var email = dto.Email.Trim().ToLowerInvariant();
    var hash = AuthUtils.Hash(dto.Password);
    var u = await db.AppUsers.FirstOrDefaultAsync(x => x.Email == email && x.PasswordHash == hash);
    if (u is null) return Results.Unauthorized();

    var token = AuthUtils.CreateJwt(u, cfg["Jwt:Key"]!);
    return Results.Ok(new { token, user = new { u.Id, u.Name, u.Email, Role = u.Role.ToString() } });
});

// ============================ 6) USERS =======================================
var users = app.MapGroup("/api/users");

users.MapGet("/", async (BibliotecaDbContext db)
    => Results.Ok(await db.Users.AsNoTracking().ToListAsync()));

users.MapGet("/{id:guid}", async (BibliotecaDbContext db, Guid id) =>
{
    var u = await db.Users.FindAsync(id);
    return u is null ? Results.NotFound() : Results.Ok(u);
});

users.MapPost("/", async (BibliotecaDbContext db, UserCreateDto dto) =>
{
    var exists = await db.Users.AnyAsync(x => x.Email == dto.Email);
    if (exists) return Results.Conflict(new { message = "E-mail já cadastrado." });

    var u = new User { Name = dto.Name, Email = dto.Email, Active = dto.Active };
    db.Users.Add(u);
    await db.SaveChangesAsync();
    return Results.Created($"/api/users/{u.Id}", u);
});

users.MapPut("/{id:guid}", async (BibliotecaDbContext db, Guid id, UserUpdateDto dto) =>
{
    var u = await db.Users.FindAsync(id);
    if (u is null) return Results.NotFound();

    if (!string.Equals(u.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
    {
        var emailTaken = await db.Users.AnyAsync(x => x.Email == dto.Email && x.Id != id);
        if (emailTaken) return Results.Conflict(new { message = "E-mail já cadastrado." });
    }

    u.Name = dto.Name;
    u.Email = dto.Email;
    u.Active = dto.Active;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

users.MapDelete("/{id:guid}", async (BibliotecaDbContext db, Guid id) =>
{
    var u = await db.Users.FindAsync(id);
    if (u is null) return Results.NotFound();
    db.Users.Remove(u);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// ============================ 7) BOOKS =======================================
var books = app.MapGroup("/api/books");

books.MapGet("/", async (BibliotecaDbContext db)
    => Results.Ok(await db.Books.AsNoTracking().ToListAsync()));

books.MapGet("/{id:guid}", async (BibliotecaDbContext db, Guid id) =>
{
    var b = await db.Books.FindAsync(id);
    return b is null ? Results.NotFound() : Results.Ok(b);
});

books.MapPost("/", async (BibliotecaDbContext db, BookCreateDto dto) =>
{
    if (!string.IsNullOrWhiteSpace(dto.Isbn))
    {
        var exists = await db.Books.AnyAsync(x => x.Isbn == dto.Isbn);
        if (exists) return Results.Conflict(new { message = "ISBN já cadastrado." });
    }

    var b = new Book
    {
        Title = dto.Title,
        Author = dto.Author,
        Isbn = string.IsNullOrWhiteSpace(dto.Isbn) ? null : dto.Isbn,
        IsLoaned = false
    };
    db.Books.Add(b);
    await db.SaveChangesAsync();
    return Results.Created($"/api/books/{b.Id}", b);
});

books.MapPut("/{id:guid}", async (BibliotecaDbContext db, Guid id, BookUpdateDto dto) =>
{
    var b = await db.Books.FindAsync(id);
    if (b is null) return Results.NotFound();

    if (!string.Equals(b.Isbn, dto.Isbn, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(dto.Isbn))
    {
        var isbnTaken = await db.Books.AnyAsync(x => x.Isbn == dto.Isbn && x.Id != id);
        if (isbnTaken) return Results.Conflict(new { message = "ISBN já cadastrado." });
    }

    b.Title = dto.Title;
    b.Author = dto.Author;
    b.Isbn = string.IsNullOrWhiteSpace(dto.Isbn) ? null : dto.Isbn;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

books.MapDelete("/{id:guid}", async (BibliotecaDbContext db, Guid id) =>
{
    var book = await db.Books.FindAsync(id);
    if (book is null) return Results.NotFound();

    var hasPlacement = await db.Placements.AnyAsync(p => p.BookId == id);
    if (hasPlacement)
        return Results.Conflict(new { message = "Livro está alocado em uma estante. Remova a alocação antes de excluir." });

    var hasOpenLoan = await db.Loans.AnyAsync(l => l.BookId == id && l.Status == LoanStatus.OPEN);
    if (hasOpenLoan)
        return Results.Conflict(new { message = "Livro está emprestado. Faça a devolução antes de excluir." });

    var pastLoans = await db.Loans.Where(l => l.BookId == id).ToListAsync();
    if (pastLoans.Count > 0) db.Loans.RemoveRange(pastLoans);

    db.Books.Remove(book);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// ============================ 8) SHELVES =====================================
var shelves = app.MapGroup("/api/shelves");

shelves.MapGet("/", async (BibliotecaDbContext db)
    => Results.Ok(await db.Shelves.AsNoTracking().ToListAsync()));

shelves.MapGet("/{id:guid}", async (BibliotecaDbContext db, Guid id) =>
{
    var s = await db.Shelves.FindAsync(id);
    return s is null ? Results.NotFound() : Results.Ok(s);
});

shelves.MapPost("/", async (BibliotecaDbContext db, ShelfCreateDto dto) =>
{
    var exists = await db.Shelves.AnyAsync(x => x.Code == dto.Code);
    if (exists) return Results.Conflict(new { message = "Código de estante já cadastrado." });

    var s = new Shelf { Code = dto.Code, Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description };
    db.Shelves.Add(s);
    await db.SaveChangesAsync();
    return Results.Created($"/api/shelves/{s.Id}", s);
});

shelves.MapPut("/{id:guid}", async (BibliotecaDbContext db, Guid id, ShelfUpdateDto dto) =>
{
    var s = await db.Shelves.FindAsync(id);
    if (s is null) return Results.NotFound();

    if (!string.Equals(s.Code, dto.Code, StringComparison.OrdinalIgnoreCase))
    {
        var taken = await db.Shelves.AnyAsync(x => x.Code == dto.Code && x.Id != id);
        if (taken) return Results.Conflict(new { message = "Código de estante já cadastrado." });
    }

    s.Code = dto.Code;
    s.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

shelves.MapDelete("/{id:guid}", async (BibliotecaDbContext db, Guid id) =>
{
    var shelf = await db.Shelves.FindAsync(id);
    if (shelf is null) return Results.NotFound();

    var hasBooks = await db.Placements.AnyAsync(p => p.ShelfId == id);
    if (hasBooks)
        return Results.Conflict(new { message = "Estante possui livros alocados. Remova os livros antes de excluir." });

    db.Shelves.Remove(shelf);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// ============================ 9) PLACEMENTS ==================================
var placements = app.MapGroup("/api/placements");

placements.MapGet("/", async (BibliotecaDbContext db)
    => Results.Ok(await db.Placements.AsNoTracking().ToListAsync()));

placements.MapPost("/", async (BibliotecaDbContext db, PlacementUpsertDto dto) =>
{
    var existing = await db.Placements.FirstOrDefaultAsync(p => p.BookId == dto.BookId);
    if (existing is not null)
    {
        existing.ShelfId = dto.ShelfId;
        existing.Position = string.IsNullOrWhiteSpace(dto.Position) ? null : dto.Position;
        await db.SaveChangesAsync();
        return Results.Ok(existing);
    }

    var p = new Placement { BookId = dto.BookId, ShelfId = dto.ShelfId, Position = string.IsNullOrWhiteSpace(dto.Position) ? null : dto.Position };
    db.Placements.Add(p);
    await db.SaveChangesAsync();
    return Results.Created($"/api/placements/{p.Id}", p);
});

placements.MapDelete("/{id:guid}", async (BibliotecaDbContext db, Guid id) =>
{
    var p = await db.Placements.FindAsync(id);
    if (p is null) return Results.NotFound();
    db.Placements.Remove(p);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// ============================ 10) LOANS (com aprovação) =======================
var loans = app.MapGroup("/api/loans");

// Listagem (qualquer autenticado)
loans.MapGet("/", [Authorize] async (BibliotecaDbContext db, string? status) =>
{
    var q = db.Loans.AsNoTracking().AsQueryable();
    if (!string.IsNullOrWhiteSpace(status))
        q = q.Where(l => l.Status == status);

    var list = await q.OrderByDescending(l => l.LoanDate).ToListAsync();
    return Results.Ok(list);
});

// Solicitar (USER) → PENDING
loans.MapPost("/", [Authorize(Roles = "USER")] async (BibliotecaDbContext db, LoanCreateDto dto) =>
{
    var book = await db.Books.FindAsync(dto.BookId);
    if (book is null) return Results.BadRequest(new { message = "Livro não encontrado." });

    var pendOpen = await db.Loans.AnyAsync(l =>
        l.BookId == dto.BookId &&
        (l.Status == LoanStatus.PENDING || l.Status == LoanStatus.OPEN));

    if (pendOpen) return Results.Conflict(new { message = "Livro já está emprestado ou aguardando aprovação." });

    var patron = await db.Users.FindAsync(dto.UserId);
    if (patron is null) return Results.BadRequest(new { message = "Usuário (patrono) não encontrado." });

    var loan = new Loan
    {
        BookId = book.Id,
        UserId = patron.Id,
        LoanDate = DateOnly.Parse(dto.LoanDate),
        Status = LoanStatus.PENDING
    };

    db.Loans.Add(loan);
    await db.SaveChangesAsync();

    return Results.Created($"/api/loans/{loan.Id}", new
    {
        loan.Id,
        loan.BookId,
        BookTitle = book.Title,
        loan.UserId,
        UserName = patron.Name,
        LoanDate = loan.LoanDate,
        loan.Status
    });
});

// Aprovar (LIBRARIAN) → OPEN
loans.MapPut("/{id:guid}/approve", [Authorize(Policy = "LibrarianOnly")] async (BibliotecaDbContext db, Guid id, ClaimsPrincipal me, LoanApproveDto body) =>
{
    var loan = await db.Loans.FindAsync(id);
    if (loan is null) return Results.NotFound(new { message = "Empréstimo não encontrado." });
    if (loan.Status != LoanStatus.PENDING) return Results.Conflict(new { message = "Apenas PENDING pode ser aprovado." });

    var book = await db.Books.FindAsync(loan.BookId);
    if (book is null) return Results.BadRequest(new { message = "Livro do empréstimo não encontrado." });
    if (book.IsLoaned) return Results.Conflict(new { message = "Livro já está emprestado." });

    var approverId = Guid.Parse(me.FindFirstValue(ClaimTypes.NameIdentifier)!);

    book.IsLoaned = true;
    loan.Status = LoanStatus.OPEN;
    loan.ApprovedById = approverId;
    loan.ApprovedAt = DateTimeOffset.UtcNow;
    loan.RejectionReason = null;

    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Aprovado.", loan.Id, loan.Status });
});

// Rejeitar (LIBRARIAN)
loans.MapPut("/{id:guid}/reject", [Authorize(Policy = "LibrarianOnly")] async (BibliotecaDbContext db, Guid id, ClaimsPrincipal me, LoanApproveDto body) =>
{
    var loan = await db.Loans.FindAsync(id);
    if (loan is null) return Results.NotFound(new { message = "Empréstimo não encontrado." });
    if (loan.Status != LoanStatus.PENDING) return Results.Conflict(new { message = "Apenas PENDING pode ser rejeitado." });

    var approverId = Guid.Parse(me.FindFirstValue(ClaimTypes.NameIdentifier)!);

    loan.Status = LoanStatus.REJECTED;
    loan.ApprovedById = approverId;
    loan.ApprovedAt = DateTimeOffset.UtcNow;
    loan.RejectionReason = string.IsNullOrWhiteSpace(body?.Reason) ? "Sem justificativa." : body!.Reason!.Trim();

    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Rejeitado.", loan.Id, loan.Status });
});

// Devolver (USER ou LIBRARIAN)
loans.MapPut("/{id:guid}/return", [Authorize] async (BibliotecaDbContext db, Guid id, ClaimsPrincipal me) =>
{
    var loan = await db.Loans.FindAsync(id);
    if (loan is null) return Results.NotFound(new { message = "Empréstimo não encontrado." });
    if (loan.Status != LoanStatus.OPEN) return Results.Conflict(new { message = "Somente empréstimos OPEN podem ser devolvidos." });

    loan.Status = LoanStatus.RETURNED;
    loan.ReturnDate = DateOnly.FromDateTime(DateTime.UtcNow);

    var book = await db.Books.FindAsync(loan.BookId);
    if (book is not null) book.IsLoaned = false;

    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Devolvido com sucesso.", loan.Id, loan.Status, loan.ReturnDate });
});

// ============================ 11) Run ========================================
app.Run();
