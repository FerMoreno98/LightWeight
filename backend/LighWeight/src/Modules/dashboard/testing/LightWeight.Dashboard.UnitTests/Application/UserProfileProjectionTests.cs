using LightWeight.Dashboard.Application.Calculations;
using LightWeight.Dashboard.Application.Projections.UserProfile;
using LightWeight.Dashboard.Application.Projectors.UserProfile;
using LightWeight.Dashboard.UnitTests.Fakes;
using LightWeight.shared.Contracts.UserProfile;

namespace LightWeight.Dashboard.UnitTests.Application;

public class UserProfileProjectionTests
{
    private static readonly DateTime Now = new(2026, 11, 10, 8, 0, 0, DateTimeKind.Utc);

    private readonly InMemoryUserProfileProjectionStore _store = new();
    private readonly Guid _userId = Guid.CreateVersion7();

    // ---------------------------------------------------------------- Projector

    [Fact]
    public async Task ProfileRecorded_StoresTheSex()
    {
        // Act
        await new UserProfileProjector(_store).HandleAsync(new ProfileRecordedIntegrationEvent(_userId, "Fernando", "Female", Now));
        // Assert
        UserProfileProjection row = Assert.Single(_store.Profiles.Values);
        Assert.Equal(_userId, row.UserId);
        Assert.Equal("Female", row.Sex);
    }

    [Fact]
    public async Task ProfileRecorded_Again_OverwritesTheSex()
    {
        var projector = new UserProfileProjector(_store);
        // Act
        await projector.HandleAsync(new ProfileRecordedIntegrationEvent(_userId, "Fernando", "Female", Now));
        await projector.HandleAsync(new ProfileRecordedIntegrationEvent(_userId, "Fernando", "Male", Now.AddDays(1)));
        // Assert
        UserProfileProjection row = Assert.Single(_store.Profiles.Values);
        Assert.Equal("Male", row.Sex);
    }

    // ---------------------------------------------------------------- BodyFat

    [Theory]
    [InlineData("Male", 8.89)]     // 0.1051 * 60 + 2.585 = 8.891
    [InlineData("male", 8.89)]
    [InlineData("Female", 12.87)]  // 0.1548 * 60 + 3.580 = 12.868
    public void EstimatePercentage_UsesTheFormulaOfTheSex(string sex, double expected)
    {
        Assert.Equal((decimal)expected, BodyFat.EstimatePercentage(60m, sex));
    }

    [Fact]
    public void EstimatePercentage_WithoutSummatory_IsNull()
    {
        Assert.Null(BodyFat.EstimatePercentage(null, "Male"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Other")]
    public void EstimatePercentage_WithoutAKnownSex_IsNull(string? sex)
    {
        Assert.Null(BodyFat.EstimatePercentage(60m, sex));
    }
}
