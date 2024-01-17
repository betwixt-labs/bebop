namespace Chord.Runtime;

/// <summary>
/// Represents runtime errors that occur during the execution of an extension in the Chord runtime.
/// </summary>
public sealed class ExtensionRuntimeException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionRuntimeException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    internal ExtensionRuntimeException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionRuntimeException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    internal ExtensionRuntimeException(string message, Exception innerException) : base(message, innerException)
    {
    }
}