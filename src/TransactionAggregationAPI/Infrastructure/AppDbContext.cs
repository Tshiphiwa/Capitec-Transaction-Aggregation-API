using Microsoft.EntityFrameworkCore;
using Capitec_Transaction_Aggregation_API.Models;

namespace Capitec_Transaction_Aggregation_API.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionSource>  TransactionSources => Set<TransactionSource>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        ConfigureTransaction(modelBuilder);
        ConfigureTransactionSource(modelBuilder);
        ConfigureUser(modelBuilder);
    }
    
    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Email).HasMaxLength(256).IsRequired();
            entity.Property(u => u.UserName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.PasswordHash).HasMaxLength(60).IsRequired();
            entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);

            entity.HasIndex(s => s.Email).HasDatabaseName("IX_Users_Email");
            entity.HasIndex(s => s.UserName).HasDatabaseName("IX_Users_UserName");
        });
    }

    private void ConfigureTransaction(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("Transactions");
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(t => t.Currency).HasMaxLength(3).IsRequired();
            entity.Property(t => t.Description).HasMaxLength(500).IsRequired();
            entity.Property(t => t.MerchantName).HasMaxLength(200).IsRequired();
            entity.Property(t => t.MccCode).HasMaxLength(4);
            entity.Property(t => t.Category).HasMaxLength(100).IsRequired();
            entity.Property(t => t.Reference).HasMaxLength(100).IsRequired();
            entity.Property(t => t.FromAccount).HasMaxLength(50).IsRequired();
            entity.Property(t => t.ToAccount).HasMaxLength(50).IsRequired();

            // store enums as strings for readability on the db
            entity.Property(t => t.Direction).HasConversion<string>().HasMaxLength(20);
            entity.Property(t => t.TransactionType).HasConversion<string>().HasMaxLength(30);
            entity.Property(t => t.CategorySource).HasConversion<string>().HasMaxLength(20);

            // rule to ensure that transactions don't get processed twice
            entity.HasIndex(t => new { t.Reference, t.SourceId })
                .IsUnique()
                .HasDatabaseName("IX_Transactions_Reference_SourceId");

            entity.HasIndex(t => t.TransactionDate)
                .HasDatabaseName("IX_Transactions_TransactionDate");

            entity.HasIndex(t => t.Category)
                .HasDatabaseName("IX_Transactions_Category");

            entity.HasOne(t => t.Source)
                .WithMany(t => t.Transactions)
                .HasForeignKey(t => t.SourceId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureTransactionSource(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TransactionSource>(entity =>
            {
                entity.ToTable("TransactionSources");
                entity.HasKey(s => s.Id);
                
                entity.Property(s => s.Name).HasMaxLength(100).IsRequired();
                entity.Property(s => s.Code).HasMaxLength(20).IsRequired();
                entity.Property(s => s.BaseUrl).HasMaxLength(500).IsRequired();
                
                entity.HasIndex(s => s.Code)
                    .IsUnique()
                    .HasDatabaseName("IX_TransactionSources_Code");
            });
        }
}