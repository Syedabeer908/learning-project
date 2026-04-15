using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data.SoftDelete.Cascade;
using WebApplication1.Entities;

namespace WebApplication1.Data.SoftDelete.Executor
{
    public class SoftDeleteExecutor
    {
        private readonly AppDbContext _context;

        public SoftDeleteExecutor(AppDbContext context)
        {
            _context = context;
        }

        public async Task DeleteAsync<TEntity>(Guid id, Guid userId, CascadePlan plan)
            where TEntity : BaseEntity
        {
            var now = DateTime.UtcNow;

            using var tx = await _context.Database.BeginTransactionAsync();

            foreach (var step in plan.Steps.AsEnumerable().Reverse())
            {
                var table = _context.Model.FindEntityType(step.EntityType)!.GetTableName();

                var sql = $@"
                UPDATE [{table}]
                SET IsDeleted = 1, DeletedAt = @now, DeletedBy = @userId
                WHERE [{step.ForeignKey}] = @id AND IsDeleted = 0";

                await _context.Database.ExecuteSqlRawAsync(
                    sql,
                    new SqlParameter("@id", id),
                    new SqlParameter("@now", now),
                    new SqlParameter("@userId", userId)
                );
            }

            var keyName = typeof(TEntity).Name + "Id";

            await _context.Set<TEntity>()
                .Where(x => EF.Property<object>(x, keyName).Equals(id))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.IsDeleted, true)
                    .SetProperty(x => x.DeletedAt, now)
                    .SetProperty(x => x.DeletedBy, userId));
            await tx.CommitAsync();
        }

        public async Task RestoreAsync<TEntity>(Guid id, Guid userId, CascadePlan plan)
            where TEntity : BaseEntity
        {
            var now = DateTime.UtcNow;

            using var tx = await _context.Database.BeginTransactionAsync();
            foreach (var step in plan.Steps)
            {
                var table = _context.Model.FindEntityType(step.EntityType)!.GetTableName();
                var sql = $@"
                UPDATE [{table}]
                SET LastUpdatedAt = @now, LastUpdatedBy = @userId, IsDeleted = 0,
                DeletedAt = NULL, DeletedBy = NULL
                WHERE [{step.ForeignKey}] = @id AND IsDeleted = 1";
                await _context.Database.ExecuteSqlRawAsync(
                    sql,
                    new SqlParameter("@id", id),
                    new SqlParameter("@now", now),
                    new SqlParameter("@userId", userId)
                );
            }

            var keyName = typeof(TEntity).Name + "Id";

            await _context.Set<TEntity>()
                .Where(x => EF.Property<object>(x, keyName).Equals(id))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.LastUpdatedAt, now)
                    .SetProperty(x => x.LastUpdatedBy, userId)
                    .SetProperty(x => x.IsDeleted, false)
                    .SetProperty(x => x.DeletedAt, (DateTime?)null)
                    .SetProperty(x => x.DeletedBy, (Guid?)null));
            await tx.CommitAsync();
        }
    }
}
