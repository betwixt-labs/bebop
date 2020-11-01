using System;

namespace Core.Logging
{

    /// <summary>
    /// A central logging factory
    /// </summary>
    public class Lager
    {
        private readonly string _component;

        private Lager(string component)
        {
            _component = component.ToUpper();
        }

        public void Error(string message, Exception? ex = default)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.Write($"[{_component}] {message}");
            Console.ResetColor();
            if (ex != null)
            {
                Console.Error.WriteLine(ex.ToString());
            }
            Console.WriteLine();
        }

        public void Success(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Error.Write($"[{_component}] {message}");
            Console.ResetColor();
            Console.WriteLine();
        }

        public void Warn(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Error.Write($"[{_component}] {message}");
            Console.ResetColor();
            Console.WriteLine();
        }

        public void Info(string message)
        {
            Console.Out.WriteLine($"[{_component}] {message}");
        }

        /// <summary>
        /// Gets the logger with the full name of the current class, so namespace and class name.
        /// </summary>
        public static Lager CreateLogger(string component)
        {
            return new Lager(component);
        }

    }
}