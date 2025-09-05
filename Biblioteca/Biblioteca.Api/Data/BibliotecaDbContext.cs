using Biblioteca.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Api.Data;

public class BibliotecaDbContext(DbContextOptions<BibliotecaDbContext> options) : DbContext(options)
{
	public DbSet<Biblioteca.Api.Models.User> Users => Set<Biblioteca.Api.Models.User>();
	public DbSet<Biblioteca.Api.Models.AppUser> AppUsers => Set<Biblioteca.Api.Models.AppUser>(); // <-- AQUI
	public DbSet<Book> Books => Set<Book>();
	public DbSet<Shelf> Shelves => Set<Shelf>();
	public DbSet<Placement> Placements => Set<Placement>();
	public DbSet<Loan> Loans => Set<Loan>();

	protected override void OnModelCreating(ModelBuilder b)
	{
		// AppUser
		b.Entity<Biblioteca.Api.Models.AppUser>(e =>  // <-- AQUI
		{
			e.Property(x => x.Name).IsRequired();
			e.Property(x => x.Email).IsRequired();
			e.HasIndex(x => x.Email).IsUnique();
			e.Property(x => x.PasswordHash).IsRequired();
			e.Property(x => x.Role).HasConversion<string>();
		});

		// User (legado)
		b.Entity<Biblioteca.Api.Models.User>(e =>
		{
			e.Property(x => x.Name).IsRequired();
			e.Property(x => x.Email).IsRequired();
			e.HasIndex(x => x.Email).IsUnique();
		});

		// Book
		b.Entity<Book>(e =>
		{
			e.Property(x => x.Title).IsRequired();
			e.Property(x => x.Author).IsRequired();
			e.Property(x => x.IsLoaned).HasDefaultValue(false);
			e.HasIndex(x => x.Isbn).IsUnique();
		});

		// Shelf
		b.Entity<Shelf>(e =>
		{
			e.Property(x => x.Code).IsRequired();
			e.HasIndex(x => x.Code).IsUnique();
		});

		// Placement
		b.Entity<Placement>(e =>
		{
			e.HasIndex(x => x.BookId).IsUnique();

			e.HasOne(x => x.Book)
			 .WithOne(x => x.Placement)
			 .HasForeignKey<Placement>(x => x.BookId)
			 .OnDelete(DeleteBehavior.Cascade);

			e.HasOne(x => x.Shelf)
			 .WithMany(x => x.Placements)
			 .HasForeignKey(x => x.ShelfId)
			 .OnDelete(DeleteBehavior.Restrict);
		});

		// Loan
		b.Entity<Loan>(e =>
		{
			e.Property(x => x.Status).IsRequired();
			e.Property(x => x.LoanDate).HasColumnType("date");
			e.Property(x => x.ReturnDate).HasColumnType("date");
			e.Property(x => x.ApprovedAt).HasColumnType("timestamptz");

			e.HasOne(x => x.Book)
			 .WithMany(x => x.Loans)
			 .HasForeignKey(x => x.BookId)
			 .OnDelete(DeleteBehavior.Restrict);

			e.HasOne(x => x.User)
			 .WithMany(x => x.Loans)
			 .HasForeignKey(x => x.UserId)
			 .OnDelete(DeleteBehavior.Restrict);

			e.HasOne<Biblioteca.Api.Models.AppUser>()      // <-- AQUI
			 .WithMany()
			 .HasForeignKey(x => x.ApprovedById)
			 .OnDelete(DeleteBehavior.SetNull);
		});
	}
}
