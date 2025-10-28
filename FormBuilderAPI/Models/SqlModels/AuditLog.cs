namespace FormBuilderAPI.Models.SqlModels
{
    public class AuditLog
    {
        public int Id { get; set; }
        public long? UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? ActorRole { get; set; }
        public string? ActorUserId { get; set; }   // ✅ string not int
        public string? EntityId { get; set; }
        public string? Entity { get; set; }
        public string? PayloadJson { get; set; }
        public string? DetailsJson { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}