using WebApplication1.Entities;

namespace WebApplication1.Data.SoftDelete.Config
{
    public class SoftDeleteConfig
    {
        public static readonly Dictionary<Type, List<Type>> CascadeMap = new()
        {
            { typeof(Role), new() { typeof(User) } },
            { typeof(User), new() { typeof(Risk), typeof(Control) } },
            { typeof(Risk), new() { typeof(RiskControl) } },
            { typeof(Control), new() { typeof(RiskControl) } }
        };
    }
} 
