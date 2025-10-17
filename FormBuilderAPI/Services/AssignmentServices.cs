// Services/AssignmentService.cs
using Microsoft.EntityFrameworkCore;
using FormBuilderAPI.Data;
using FormBuilderAPI.Models.SqlModels;

namespace FormBuilderAPI.Services
{
    public class AssignmentService
    {
        private readonly SqlDbContext _db;
        public AssignmentService(SqlDbContext db) => _db = db;

        public async Task<FormAssignment> AssignAsync(string formId, long userId)
        {
            // idempotency: prevent duplicate (FormId,UserId)
            var exists = await _db.FormAssignments
                .AnyAsync(a => a.FormId == formId && a.UserId == userId);
            if (exists)
                throw new InvalidOperationException("Already assigned.");

            // Compute next per-form sequence in a retry loop (handles rare race)
            for (var attempt = 0; attempt < 3; attempt++)
            {
                // get current max
                var nextSeq = (await _db.FormAssignments
                    .Where(a => a.FormId == formId)
                    .MaxAsync(a => (int?)a.SequenceNo)) ?? 0;
                nextSeq += 1;

                var a = new FormAssignment
                {
                    FormId = formId,
                    UserId = userId,
                    AssignedAt = DateTime.UtcNow,
                    SequenceNo = nextSeq
                };

                _db.FormAssignments.Add(a);
                try
                {
                    await _db.SaveChangesAsync();
                    return a; // success
                }
                catch (DbUpdateException)
                {
                    // likely unique conflict on (FormId, SequenceNo) — retry
                    _db.Entry(a).State = EntityState.Detached;
                    await Task.Delay(25);
                }
            }

            throw new InvalidOperationException("Failed to assign after retries.");
        }

        public async Task<bool> UnassignAsync(string formId, long userId)
        {
            var a = await _db.FormAssignments
                .FirstOrDefaultAsync(x => x.FormId == formId && x.UserId == userId);
            if (a == null) return false;

            _db.FormAssignments.Remove(a);
            await _db.SaveChangesAsync();
            return true;
        }

        public Task<List<FormAssignment>> ListAssigneesAsync(string formId) =>
            _db.FormAssignments
               .Where(a => a.FormId == formId)
               .OrderBy(a => a.SequenceNo)
               .ToListAsync();

        public Task<List<FormAssignment>> ListForUserAsync(long userId) =>
            _db.FormAssignments
               .Where(a => a.UserId == userId)
               .OrderByDescending(a => a.AssignedAt)
               .ToListAsync();

        public Task<bool> IsUserAssignedAsync(string formId, long userId) =>
            _db.FormAssignments.AnyAsync(a => a.FormId == formId && a.UserId == userId);

        public Task<bool> HasAnyAssignmentsAsync(string formId) =>
            _db.FormAssignments.AnyAsync(a => a.FormId == formId);
    }
}