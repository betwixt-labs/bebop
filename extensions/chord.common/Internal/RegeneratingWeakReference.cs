namespace Chord.Common.Internal;
internal readonly struct RegeneratingWeakReference<T>(Func<T> regenerator)
    where T : class
{
    private readonly WeakReference<T> reference = new WeakReference<T>(regenerator(), false);
    private readonly Func<T> regenerator = regenerator;

    public T Reference
    {
        get
        {
            if (!reference.TryGetTarget(out var value))
                reference.SetTarget(value = regenerator());

            return value;
        }
    }

    public static implicit operator T(RegeneratingWeakReference<T> reference) => reference.Reference;
}