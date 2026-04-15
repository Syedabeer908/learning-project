using WebApplication1.Data.SoftDelete.Config;

namespace WebApplication1.Data.SoftDelete.Cascade
{
    public class CascadePlanBuilder
    {
        private readonly AppDbContext _context;

        public CascadePlanBuilder(AppDbContext context)
        {
            _context = context;
        }

        public CascadePlan Build<TEntity>()
        {
            var plan = new CascadePlan();
            BuildRecursive(typeof(TEntity), plan);
            return plan;
        }

        private void BuildRecursive(Type parent, CascadePlan plan)
        {
            if (!SoftDeleteConfig.CascadeMap.ContainsKey(parent))
                return;

            foreach (var child in SoftDeleteConfig.CascadeMap[parent])
            {
                var fk = _context.Model
                    .FindEntityType(child)!
                    .GetForeignKeys()
                    .First(f => f.PrincipalEntityType.ClrType == parent);

                plan.Steps.Add(new CascadeStep
                {
                    EntityType = child,
                    ForeignKey = fk.Properties.First().Name
                });

                BuildRecursive(child, plan);
            }
        }
    }
}
