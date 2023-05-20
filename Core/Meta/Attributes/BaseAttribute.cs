namespace Core.Meta.Attributes
{
    public abstract class BaseAttribute
    {
        public string Name {get; set;}  = null!;
        public string Value { get; set; } = null!;
        public abstract bool TryValidate(out string reason);
    }
}
