using WebApplication1.Data.SoftDelete.Cascade;
using WebApplication1.Data.SoftDelete.Executor;
using WebApplication1.Entities;

namespace WebApplication1.Data.SoftDelete.Services
{
    public class SoftDeleteService
    {
        private readonly CascadePlanBuilder _builder;
        private readonly SoftDeleteExecutor _executor;

        public SoftDeleteService(
            CascadePlanBuilder builder,
            SoftDeleteExecutor executor)
        {
            _builder = builder;
            _executor = executor;
        }

        public async Task DeleteAsync<TEntity>(Guid id, Guid userId) where TEntity : BaseEntity
        {
            var plan = _builder.Build<TEntity>();
            await _executor.DeleteAsync<TEntity>(id, userId, plan);
        }

        public async Task RestoreAsync<TEntity>(Guid id, Guid userId) where TEntity : BaseEntity
        {
            var plan = _builder.Build<TEntity>();
            await _executor.RestoreAsync<TEntity>(id, userId, plan);
        }
    }
}
