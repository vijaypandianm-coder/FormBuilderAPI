using Microsoft.EntityFrameworkCore;
using FormBuilderAPI.Models.SqlModels;

namespace FormBuilderAPI.Data
{
    public class SqlDbContext : DbContext
    {
        public SqlDbContext(DbContextOptions<SqlDbContext> options) : base(options) { }

        // ---------- DbSets ----------
        public DbSet<FormKey> FormKeys => Set<FormKey>();
        public DbSet<FormAssignment> FormAssignments => Set<FormAssignment>();
        public DbSet<FormResponse> FormResponses => Set<FormResponse>();
        public DbSet<FormResponseAnswer> FormResponseAnswers => Set<FormResponseAnswer>();
        public DbSet<User> Users => Set<User>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---------- Users ----------
            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("users");
                e.HasKey(u => u.Id);
                e.Property(u => u.Id).ValueGeneratedOnAdd();

                e.Property(u => u.Email).IsRequired().HasMaxLength(256);
                e.HasIndex(u => u.Email).IsUnique();

                e.Property(u => u.PasswordHash).IsRequired().HasMaxLength(512);
                e.Property(u => u.Role).IsRequired().HasMaxLength(64);

                e.Property(u => u.IsActive).HasDefaultValue(true);
                e.Property(u => u.CreatedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // ---------- AuditLogs ----------
            modelBuilder.Entity<AuditLog>(e =>
            {
                e.ToTable("auditlogs");
                e.HasKey(a => a.Id);
                e.Property(a => a.Id).ValueGeneratedOnAdd();

                e.Property(a => a.UserId).IsRequired(false);
                e.Property(a => a.Action).IsRequired().HasMaxLength(128);
                e.Property(a => a.Entity).HasMaxLength(128);
                e.Property(a => a.EntityId).HasMaxLength(64);
                e.Property(a => a.PayloadJson).IsRequired(false);

                e.Property(a => a.CreatedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                e.HasIndex(a => a.CreatedAt);
                e.HasIndex(a => new { a.Action, a.Entity });
            });

            // ---------- FormKeys ----------
            modelBuilder.Entity<FormKey>(e =>
            {
                e.ToTable("formkeys");
                e.HasKey(k => k.Id);
                e.Property(k => k.Id).ValueGeneratedOnAdd();
                e.Property(k => k.FormId).IsRequired().HasMaxLength(24);
                e.HasIndex(k => k.FormId);
            });

            // ---------- FormAssignments ----------
            modelBuilder.Entity<FormAssignment>(e =>
            {
                e.ToTable("formassignments");
                e.HasKey(a => a.Id);
                e.Property(a => a.Id).ValueGeneratedOnAdd();

                e.Property(a => a.FormId).IsRequired().HasMaxLength(24);
                e.Property(a => a.UserId).IsRequired();
                e.Property(a => a.AssignedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                e.Property(a => a.SequenceNo).IsRequired(false);

                e.HasIndex(a => new { a.FormId, a.UserId }).IsUnique();
                e.HasIndex(a => new { a.FormId, a.SequenceNo }).IsUnique();
            });

            // ---------- FormResponses (header rows) ----------
            modelBuilder.Entity<FormResponse>(e =>
            {
                e.ToTable("formresponses");
                e.HasKey(r => r.Id);
                e.Property(r => r.Id).ValueGeneratedOnAdd();

                e.Property(r => r.FormId).IsRequired().HasMaxLength(24);
                e.Property(r => r.FormKey).IsRequired();
                e.Property(r => r.UserId).IsRequired();
                e.Property(r => r.SubmittedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                

                e.HasIndex(r => new { r.FormId, r.UserId, r.SubmittedAt });
            });

            // ---------- FormResponseAnswers (detail rows) ----------
            modelBuilder.Entity<FormResponseAnswer>(e =>
            {
                e.ToTable("formresponseanswers");
                e.HasKey(a => a.Id);
                e.Property(a => a.Id).ValueGeneratedOnAdd();

                e.Property(a => a.ResponseId).IsRequired();
                e.Property(a => a.FormKey).IsRequired(false);
                e.Ignore(a => a.FormId);
                //e.Property(a => a.FormId).HasColumnType("varchar(255)").IsRequired(false);
                e.Property(a => a.UserId).IsRequired();
                e.Property(a => a.FieldId).HasColumnType("varchar(255)").IsRequired();


                e.Property(a => a.FieldType)
                    .HasMaxLength(32)
                    .IsRequired(false);

                // If you actually added FieldType column, uncomment this
                // e.Property(a => a.FieldType).HasMaxLength(32).IsRequired(false);

                e.Property(a => a.AnswerValue)
                    .HasMaxLength(4000)
                    .IsRequired(false);

                e.Property(a => a.SubmittedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }
    }
}