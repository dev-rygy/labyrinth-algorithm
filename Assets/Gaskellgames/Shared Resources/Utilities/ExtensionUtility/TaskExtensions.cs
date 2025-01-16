using System;
using System.Threading.Tasks;

namespace Gaskellgames
{
    /// <summary>
    /// Code created by Gaskellgames
    /// WaitWhile and WaitUntil from StackExchange: https://stackoverflow.com/questions/29089417/c-sharp-wait-until-condition-is-true
    /// </summary>

    public static class TaskExtensions
    {
        /// <summary>
        /// Blocks until delay is complete.
        /// </summary>
        /// <param name="seconds"></param>
        public static async Task WaitForSeconds(float seconds)
        {
            var milliseconds = (int)(seconds * 1000);
            await Task.Delay(milliseconds);
        }
        
        /// <summary>
        /// Blocks while condition is true or timeout occurs.
        /// </summary>
        /// <param name="condition">The condition that will perpetuate the block.</param>
        /// <param name="frequency">The frequency at which the condition will be check, in milliseconds.</param>
        /// <param name="timeout">Timeout in milliseconds. Default is -1, which is infinite timeout.</param>
        /// <param name="throwTimeoutExceptionOnTimeout">False by default. If set to true, on timeout this method will throw timeout exception, otherwise will end quietly.</param>
        /// <exception cref="TimeoutException">Exception thrown if task did not finish before timeout and <paramref name="throwTimeoutExceptionOnTimeout"/> is set to true.</exception>
        /// <returns>Awaitable task.</returns>
        public static async Task WaitWhile(Func<bool> condition, int frequency = 25, int timeout = -1, bool throwTimeoutExceptionOnTimeout = false)
        {
            var waitTask = Task.Run(async () => { while (condition()) await Task.Delay(frequency); });
            if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)))
            {
                if (throwTimeoutExceptionOnTimeout)
                {
                    throw new TimeoutException();
                }
            }
        }

        /// <summary>
        /// Blocks until condition is true or timeout occurs.
        /// </summary>
        /// <param name="condition">The break condition.</param>
        /// <param name="frequency">The frequency at which the condition will be checked.</param>
        /// <param name="timeout">The timeout in milliseconds. Default is -1, which is infinite timeout.</param>
        /// <param name="throwTimeoutExceptionOnTimeout">False by default. If set to true, on timeout this method will throw timeout exception, otherwise will end quietly.</param>
        /// <exception cref="TimeoutException">Exception thrown if task did not finish before timeout and <paramref name="throwTimeoutExceptionOnTimeout"/> is set to true.</exception>
        /// <returns>Awaitable task.</returns>
        public static async Task WaitUntil(Func<bool> condition, int frequency = 25, int timeout = -1, bool throwTimeoutExceptionOnTimeout = false)
        {
            var waitTask = Task.Run(async () => { while (!condition()) await Task.Delay(frequency); });
            if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)))
            {
                if (throwTimeoutExceptionOnTimeout)
                {
                    throw new TimeoutException();
                }
            }
        }

    } // class end
}
