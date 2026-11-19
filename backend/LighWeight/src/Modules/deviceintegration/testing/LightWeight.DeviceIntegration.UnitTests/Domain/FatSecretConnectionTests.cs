using LightWeight.DeviceIntegration.Domain.Aggregates;
using LightWeight.DeviceIntegration.Domain.Exceptions;

namespace LightWeight.DeviceIntegration.UnitTests.Domain;

public class FatSecretConnectionTests
{
    private static readonly DateTime Now = new(2026, 11, 19, 8, 0, 0, DateTimeKind.Utc);

    private static FatSecretConnection Pending()
    {
        FatSecretConnection connection = FatSecretConnection.Create(Guid.CreateVersion7());
        connection.StartLinking("request", "request-secret", Now);
        return connection;
    }

    private static FatSecretConnection Linked()
    {
        FatSecretConnection connection = Pending();
        connection.CompleteLinking("request", "access", "access-secret", Now);
        return connection;
    }

    [Fact]
    public void Create_WithoutUser_Throws()
    {
        Assert.Throws<UserIdEmptyDomainException>(() => FatSecretConnection.Create(Guid.Empty));
    }

    [Fact]
    public void StartLinking_IsNotLinkedYet()
    {
        FatSecretConnection connection = Pending();

        Assert.False(connection.IsLinked);
        Assert.Equal("request", connection.RequestToken);
        Assert.Equal(Now, connection.RequestedAt);
    }

    [Fact]
    public void CompleteLinking_StoresTheAccessTokenAndForgetsTheRequest()
    {
        FatSecretConnection connection = Pending();

        connection.CompleteLinking("request", "access", "access-secret", Now.AddMinutes(5));

        Assert.True(connection.IsLinked);
        Assert.Equal("access", connection.AccessToken);
        Assert.Equal("access-secret", connection.AccessTokenSecret);
        Assert.Equal(Now.AddMinutes(5), connection.LinkedAt);
        Assert.Null(connection.HistoryImportedAt);
        Assert.Null(connection.RequestToken);
        Assert.Null(connection.RequestTokenSecret);
        Assert.Null(connection.RequestedAt);
    }

    [Fact]
    public void CompleteLinking_WithoutPendingRequest_Throws()
    {
        FatSecretConnection connection = FatSecretConnection.Create(Guid.CreateVersion7());

        Assert.Throws<NoPendingLinkingDomainException>(() => connection.CompleteLinking("request", "access", "secret", Now));
    }

    [Fact]
    public void CompleteLinking_WithAnotherRequestToken_Throws()
    {
        FatSecretConnection connection = Pending();

        Assert.Throws<LinkingRequestMismatchDomainException>(() => connection.CompleteLinking("other", "access", "secret", Now));
        Assert.False(connection.IsLinked);
    }

    [Fact]
    public void CompleteLinking_AfterTheLifetime_Throws()
    {
        FatSecretConnection connection = Pending();

        Assert.Throws<LinkingRequestExpiredDomainException>(() =>
            connection.CompleteLinking("request", "access", "secret", Now + FatSecretConnection.PendingRequestLifetime + TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void CompleteLinking_WithEmptyAccessToken_Throws()
    {
        FatSecretConnection connection = Pending();

        Assert.Throws<InvalidOAuthTokenDomainException>(() => connection.CompleteLinking("request", "", "secret", Now));
    }

    [Fact]
    public void StartLinking_WhenLinked_KeepsTheCurrentAccessToken()
    {
        FatSecretConnection connection = Linked();

        connection.StartLinking("request-2", "secret-2", Now.AddDays(1));

        Assert.True(connection.IsLinked);
        Assert.Equal("access", connection.AccessToken);
        Assert.Equal("request-2", connection.RequestToken);
    }

    [Fact]
    public void MarkAccessRevoked_StopsBeingLinked_AndLinkingAgainRestoresIt()
    {
        FatSecretConnection connection = Linked();
        connection.MarkHistoryImported(Now);

        connection.MarkAccessRevoked(Now.AddDays(3));
        Assert.False(connection.IsLinked);
        Assert.Equal(Now.AddDays(3), connection.RevokedAt);

        connection.StartLinking("request-2", "secret-2", Now.AddDays(4));
        connection.CompleteLinking("request-2", "access-2", "access-secret-2", Now.AddDays(4));

        Assert.True(connection.IsLinked);
        Assert.Null(connection.RevokedAt);
        // The history is imported again: it may be another FatSecret account
        Assert.Null(connection.HistoryImportedAt);
    }

    [Fact]
    public void MarkHistoryImported_WhenNotLinked_Throws()
    {
        Assert.Throws<FatSecretNotLinkedDomainException>(() => Pending().MarkHistoryImported(Now));
    }
}
