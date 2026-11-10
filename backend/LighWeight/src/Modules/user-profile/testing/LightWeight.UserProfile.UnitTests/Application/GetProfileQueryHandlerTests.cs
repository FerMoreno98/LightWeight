using LightWeight.UserProfile.Application.Exceptions;
using LightWeight.UserProfile.Application.Queries.GetProfile;
using LightWeight.UserProfile.Domain.Aggregates;
using LightWeight.UserProfile.Domain.Enum;
using LightWeight.UserProfile.Domain.Repository;
using NSubstitute;
using Xunit;

namespace LightWeight.UserProfile.UnitTests.Application;

public class GetProfileQueryHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();

    private static readonly Guid FixedUserId = Guid.NewGuid();
    private static readonly DateTime DateOfBirth = new(1990, 5, 12, 0, 0, 0, DateTimeKind.Utc);

    private GetProfileQueryHandler BuildHandler() => new(_userRepository);

    [Fact]
    public async Task HandleAsync_WhenProfileDoesNotExist_ThrowsUserNotFound()
    {
        var handler = BuildHandler();

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => handler.HandleAsync(new GetProfileQuery(FixedUserId)));
    }

    [Fact]
    public async Task HandleAsync_ReturnsTheProfileData()
    {
        var user = User.Create(FixedUserId, "Fernando", Sex.Female, DateOfBirth, DateTime.UtcNow);
        _userRepository.FindByIdAsync(FixedUserId).Returns(user);
        var handler = BuildHandler();

        var profile = await handler.HandleAsync(new GetProfileQuery(FixedUserId));

        Assert.Equal(new ProfileResponse("Fernando", "Female", DateOfBirth), profile);
    }
}
