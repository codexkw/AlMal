namespace AlMal.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(AlMal.Domain.Entities.ApplicationUser user, IList<string> roles);
    string GenerateRefreshToken();
}
