namespace WebApplication1.Data.SoftDelete.Cascade
{
    public class CascadeStep
    {
        public Type EntityType { get; set; }
        public string ForeignKey { get; set; }
    }
}
