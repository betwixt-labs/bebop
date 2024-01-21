using System;
using System.Text.Json.Serialization;

namespace Core.Logging
{
    [JsonConverter(typeof(JsonStringEnumConverter<LogFormatter>))]
    /// <summary>
    ///     Formatters that control the way <see cref="Lager"/> writes data.
    /// </summary>
    public enum LogFormatter : uint
    {
        /// <summary>
        ///     Data is formatted for MSBuild comparability. View the
        ///     <see
        ///         href="https://docs.microsoft.com/en-us/cpp/build/formatting-the-output-of-a-custom-build-step-or-build-event?view=msvc-160">
        ///         MSDocs for more information.
        ///     </see>
        /// </summary>
        MSBuild,
        /// <summary>
        /// Data is serialized to JSON before being written.
        /// </summary>
        JSON,
        /// <summary>
        /// Data is rendered in a human-readable, enhanced format.
        /// </summary>
        Enhanced
    }
}
