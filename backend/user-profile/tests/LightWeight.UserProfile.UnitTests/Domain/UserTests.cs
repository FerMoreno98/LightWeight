namespace LightWeight.UserProfile.UnitTests.Domain;

using LightWeight.UserProfile.Domain.Aggregates;
using LightWeight.UserProfile.Domain.Enum;
using LightWeight.UserProfile.Domain.Events;
using Xunit;



public class UserTests
{
    // ─── Fixtures ────────────────────────────────────────────────────────────────

    private static readonly Guid    FixedUserId = Guid.NewGuid();
    private static readonly DateTime FixedNow   = new(2024, 6, 1, 10, 0, 0, DateTimeKind.Utc);

    private static User BuildUser(
        Guid?          userId       = null,
        string         name         = "Fernando",
        Sex            sex          = Sex.Male,
        DateTime?      dateOfBirth  = null,
        TrainingStage  stage        = TrainingStage.Bulk,
        DateTime?      now          = null) =>
        User.Create(
            userId      ?? FixedUserId,
            name,
            sex,
            dateOfBirth ?? new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            stage,
            now         ?? FixedNow);

    // ─── User.Create ─────────────────────────────────────────────────────────────

    [Fact]
    public void Create_SetsIdFromParameter()
    {
        var user = BuildUser(userId: FixedUserId);

        Assert.Equal(FixedUserId, user.Id);
    }

    [Fact]
    public void Create_SetsName()
    {
        var user = BuildUser(name: "Fernando");

        Assert.Equal("Fernando", user.Name);
    }

    [Fact]
    public void Create_SetsSex()
    {
        var user = BuildUser(sex: Sex.Female);

        Assert.Equal(Sex.Female, user.Sex);
    }

    [Fact]
    public void Create_SetsDateOfBirth()
    {
        var dob  = new DateTime(1990, 5, 15, 0, 0, 0, DateTimeKind.Utc);
        var user = BuildUser(dateOfBirth: dob);

        Assert.Equal(dob, user.DateOfBirth);
    }

    [Fact]
    public void Create_SetsCurrentStage()
    {
        var user = BuildUser(stage: TrainingStage.Cut);

        Assert.Equal(TrainingStage.Cut, user.CurrentStage);
    }

    [Fact]
    public void Create_SetsStageStartedAtToNow()
    {
        var user = BuildUser(now: FixedNow);

        Assert.Equal(FixedNow, user.StageStartedAt);
    }

    [Fact]
    public void Create_RaisesUserCompletedDomainEvent()
    {
        var user = BuildUser();

        var domainEvent = user.DomainEvents.OfType<UserCompletedDomainEvent>().SingleOrDefault();
        Assert.NotNull(domainEvent);
    }

    [Fact]
    public void Create_DomainEvent_ContainsCorrectUserId()
    {
        var user = BuildUser(userId: FixedUserId);

        var domainEvent = user.DomainEvents.OfType<UserCompletedDomainEvent>().Single();
        Assert.Equal(FixedUserId, domainEvent.UserId);
    }

    [Fact]
    public void Create_DomainEvent_ContainsCorrectNow()
    {
        var user = BuildUser(now: FixedNow);

        var domainEvent = user.DomainEvents.OfType<UserCompletedDomainEvent>().Single();
        Assert.Equal(FixedNow, domainEvent.OccurredAtUtc);
    }

    // ─── User.Modify ─────────────────────────────────────────────────────────────

    [Fact]
    public void Modify_UpdatesName()
    {
        var user = BuildUser(name: "Fernando");

        user.Modify("Carlos", Sex.Male, new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal("Carlos", user.Name);
    }

    [Fact]
    public void Modify_UpdatesSex()
    {
        var user = BuildUser(sex: Sex.Male);

        user.Modify("Fernando", Sex.Female, new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(Sex.Female, user.Sex);
    }

    [Fact]
    public void Modify_UpdatesDateOfBirth()
    {
        var user   = BuildUser();
        var newDob = new DateTime(1995, 3, 20, 0, 0, 0, DateTimeKind.Utc);

        user.Modify("Fernando", Sex.Male, newDob);

        Assert.Equal(newDob, user.DateOfBirth);
    }

    [Fact]
    public void Modify_DoesNotChangeCurrentStage()
    {
        var user = BuildUser(stage: TrainingStage.Bulk);

        user.Modify("Fernando", Sex.Male, new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(TrainingStage.Bulk, user.CurrentStage);
    }

    [Fact]
    public void Modify_DoesNotChangeStageStartedAt()
    {
        var user = BuildUser(now: FixedNow);

        user.Modify("Fernando", Sex.Male, new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(FixedNow, user.StageStartedAt);
    }

    // ─── User.ChangeStage ────────────────────────────────────────────────────────

    [Fact]
    public void ChangeStage_UpdatesCurrentStage()
    {
        var user = BuildUser(stage: TrainingStage.Bulk);

        user.ChangeStage(TrainingStage.Cut);

        Assert.Equal(TrainingStage.Cut, user.CurrentStage);
    }

    [Fact]
    public void ChangeStage_ToSameStage_DoesNotThrow()
    {
        var user = BuildUser(stage: TrainingStage.Bulk);

        var exception = Record.Exception(() => user.ChangeStage(TrainingStage.Bulk));

        Assert.Null(exception);
    }

    [Fact]
    public void ChangeStage_DoesNotChangeName()
    {
        var user = BuildUser(name: "Fernando");

        user.ChangeStage(TrainingStage.Maintenance);

        Assert.Equal("Fernando", user.Name);
    }

    [Fact]
    public void ChangeStage_DoesNotChangeDateOfBirth()
    {
        var dob  = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var user = BuildUser(dateOfBirth: dob);

        user.ChangeStage(TrainingStage.Cut);

        Assert.Equal(dob, user.DateOfBirth);
    }
}