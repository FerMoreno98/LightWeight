using LightWeight.shared.Mediator;
using LightWeight.UserProfile.Application.Exceptions;
using LightWeight.UserProfile.Domain.Repository;

namespace LightWeight.UserProfile.Application.Queries.GetProfile;

public sealed class GetProfileQueryHandler : IQueryHandler<GetProfileQuery, ProfileResponse>
{
    private readonly IUserRepository _userRepository;

    public GetProfileQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ProfileResponse> HandleAsync(GetProfileQuery query, CancellationToken ct = default)
    {
        var user = await _userRepository.FindByIdAsync(query.UserId) ?? throw new UserNotFoundException();
        return new ProfileResponse(user.Name, user.Sex.ToString(), user.DateOfBirth);
    }
}
