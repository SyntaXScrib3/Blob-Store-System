using System.Security.Claims;

namespace BlobStoreSystem.WebApi.Services;

public interface ICurrentUserService
{
    Guid UserId { get; }
    bool IsAuthenticated { get; }
}