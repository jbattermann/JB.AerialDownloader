using System.Threading;
using System.Threading.Tasks;
using JB.AerialDownloader.Options;

namespace JB.AerialDownloader.Commands
{
    public interface ICommand<in TOptions> where TOptions : IOptions
    {
        /// <summary>
        /// Executes the command and returns an exit code.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<int> ExecuteAndReturnExitCode(TOptions options, CancellationToken cancellationToken = default(CancellationToken));
    }
}