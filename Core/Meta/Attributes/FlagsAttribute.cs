namespace Core.Meta.Attributes
{
    /// <summary>
    /// An attribute that marks an enum as a set of bit flags.
    /// </summary>
    public sealed class FlagsAttribute : BaseAttribute
    {
        public FlagsAttribute() { }

        public override bool TryValidate(out string message)
        {
            message = "";
            return true;
        }
    }
}