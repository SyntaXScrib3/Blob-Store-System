using BlobStoreSystem.WebApi.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BlobStoreSystem.WebAPI.Services;

public class CurrentUserService : ICurrentUserService
{
    public Guid UserId { get; }
    public bool IsAuthenticated { get; }

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            IsAuthenticated = true;
            var sub = user.FindFirst(ClaimTypes.NameIdentifier) ??
                      user.FindFirst(JwtRegisteredClaimNames.Sub);
            if (sub != null && Guid.TryParse(sub.Value, out var guid))
            {
                UserId = guid;
            }
        }
    }
}
