namespace Core.Meta.Attributes
{
    public abstract class BaseAttribute
    {
        public string Value { get; set; } = null!;
        public abstract bool TryValidate(out string reason);
    }
}
