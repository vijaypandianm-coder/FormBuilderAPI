using FormBuilderAPI.Data;
using FormBuilderAPI.Models.SqlModels;

namespace FormBuilderAPI.Services
{
    public class AuditService
    {
        private readonly SqlDbContext _sql;
        public AuditService(SqlDbContext sql) { _sql = sql; }

        public async Task LogAsync(string action, string actorRole, string? actorId, string? entityId, string? detailsJson = null)
        {
            _sql.AuditLogs.Add(new AuditLog
            {
                Action = action,
                ActorRole = actorRole,
                ActorUserId = actorId,   // ✅ string now matches model
                EntityId = entityId,
                DetailsJson = detailsJson
            });
            await _sql.SaveChangesAsync();
        }
    }
}
