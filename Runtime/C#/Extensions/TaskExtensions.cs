using System.Threading.Tasks;

namespace Bebop.Extensions
{
    public static class TaskExtensions
    {
        /// <summary>
        ///     Fires and forgets a <see cref="Task"/>
        /// </summary>
        /// <param name="task">The task to be executed.</param>
        public static void Forget(this Task task)
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
                }
            }
        }
    }
}