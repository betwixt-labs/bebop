using System.Threading.Tasks;

namespace Bebop.Extensions
{
    internal static class TaskExtensions
    {
        /// <summary>
        ///     Fires and forgets a <see cref="Task"/>
        /// </summary>
        /// <param name="task">The task to be executed.</param>
        internal static void Forget(this Task task)
        {
            if (!task.IsCompleted || task.IsFaulted)
            {
                _ = ForgetAwaited(task);
            }

            static async Task ForgetAwaited(Task task)
            {
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch
                {
                    // Nothing to do here
#pragma warning disable ERP022 // Unobserved exception in a generic exception handler
                }
#pragma warning restore ERP022 // Unobserved exception in a generic exception handler
            }
        }
    }
}