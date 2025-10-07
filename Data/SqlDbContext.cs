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
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>(); // keep if you already added AuditLog

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            // Users
            mb.Entity<User>()
              .HasIndex(u => u.Email)
              .IsUnique();

            // FormResponses
            mb.Entity<FormResponse>()
              .HasMany(r => r.Answers)
              .WithOne(a => a.FormResponse!)
              .HasForeignKey(a => a.ResponseId)
              .OnDelete(DeleteBehavior.Cascade);

            mb.Entity<FormResponse>()
              .HasIndex(r => new { r.FormId, r.SubmittedAt });

            // Answers
            mb.Entity<FormResponseAnswer>()
              .HasIndex(a => a.FieldId);

            // If you have AuditLog, keep its indexes here as before
            // mb.Entity<AuditLog>().HasIndex(l => l.ActorRole);
        }
    }
}
