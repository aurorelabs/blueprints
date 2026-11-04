using System.Threading;
using System.Threading.Tasks;

namespace Aurore.Blueprints.ApiService.Core.Clients.GitHub;

public interface IGitHubClient
{
    public Task<GitHubUser?> GetUserAsync(string login, CancellationToken cancellationToken);
}
