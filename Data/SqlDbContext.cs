using Microsoft.EntityFrameworkCore;
using FormBuilderAPI.Models.SqlModels;

namespace FormBuilderAPI.Data
{
  public class SqlDbContext : DbContext
  {
    public SqlDbContext(DbContextOptions<SqlDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<FormResponse> FormResponses => Set<FormResponse>();
    public DbSet<FormResponseAnswer> FormResponseAnswers => Set<FormResponseAnswer>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
      mb.Entity<User>().ToTable("users");               // ensure lowercase table
      mb.Entity<FormResponse>().ToTable("formresponses");
      mb.Entity<FormResponseAnswer>().ToTable("formresponseanswers");
      mb.Entity<AuditLog>().ToTable("auditlogs");

      // FormResponse → Answers (cascade)
      mb.Entity<FormResponse>()
        .HasMany(r => r.Answers)
        .WithOne(a => a.FormResponse!)
        .HasForeignKey(a => a.ResponseId)
        .OnDelete(DeleteBehavior.Cascade);

      // FormResponse → User (restrict delete)
      mb.Entity<FormResponse>()
        .HasOne(r => r.User)
        .WithMany()
        .HasForeignKey(r => r.UserId)
        .OnDelete(DeleteBehavior.Restrict);

      // Helpful indexes
      mb.Entity<FormResponse>().HasIndex(r => new { r.FormId, r.SubmittedAt });
      mb.Entity<FormResponseAnswer>().HasIndex(a => a.FieldId);
      mb.Entity<AuditLog>().HasIndex(l => l.ActorRole);
    }
  }
}