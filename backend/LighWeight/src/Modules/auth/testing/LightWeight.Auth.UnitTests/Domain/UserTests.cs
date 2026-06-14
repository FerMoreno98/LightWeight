using System;
using System.Collections.Generic;
using Xunit;
using LightWeight.Auth.Domain.Aggregates;
using LightWeight.Auth.Domain.Entities;

namespace LightWeight.Auth.UnitTests.Domain;

public class UserTests
{

        private static readonly DateTime BaseTime =
        new(2024, 6, 1, 10, 0, 0, DateTimeKind.Utc);
    // ─── User.Create ────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidEmail_ReturnsUserWithCorrectEmail()
    {
        var email = "test@example.com";

        var user = User.Create(email,BaseTime);

        Assert.Equal(email, user.Email);
    }

    [Fact]
    public void Create_GeneratesNonEmptyGuid()
    {
        var user = User.Create("test@example.com",BaseTime);

        Assert.NotEqual(Guid.Empty, user.Id);
    }

    [Fact]
    public void Create_TwoUsers_HaveDifferentIds()
    {
        var user1 = User.Create("a@example.com",BaseTime);
        var user2 = User.Create("b@example.com",BaseTime);

        Assert.NotEqual(user1.Id, user2.Id);
    }

    [Fact]
    public void Create_NewUser_HasEmptyCollections()
    {
        var user = User.Create("test@example.com",BaseTime);
        Assert.Empty(user.DeviceTokens);
        Assert.Empty(user.OtpCodes);
    }

    // ─── User.RegisterDevice — nuevo dispositivo ────────────────────────────────

    [Fact]
    public void RegisterDevice_NewDevice_AddsToDeviceTokens()
    {
        var user = User.Create("test@example.com",BaseTime);
        var now  = DateTime.UtcNow;

        user.RegisterDevice("device-001", "Mi Phone", "Android", now);

        Assert.Single(user.DeviceTokens);
    }

    [Fact]
    public void RegisterDevice_NewDevice_ReturnsDeviceWithCorrectFields()
    {
        var user = User.Create("test@example.com",BaseTime);
        var now  = DateTime.UtcNow;

        var device = user.RegisterDevice("device-001", "Mi Phone", "Android", now);

        Assert.Equal("device-001", device.DeviceIdentifier);
        Assert.Equal("Mi Phone",   device.DeviceName);
        Assert.Equal("Android",    device.Platform);
        Assert.Equal(user.Id,      device.UserId);
    }

    [Fact]
    public void RegisterDevice_NewDevice_SetsRegisteredAndLastSeenToNow()
    {
        var user = User.Create("test@example.com",BaseTime);
        var now  = new DateTime(2024, 6, 1, 10, 0, 0, DateTimeKind.Utc);

        var device = user.RegisterDevice("device-001", "Mi Phone", "Android", now);

        Assert.Equal(now, device.CreatedAt);
        Assert.Equal(now, device.LastSeenAt);
    }

    [Fact]
    public void RegisterDevice_NewDevice_GeneratesNonEmptyId()
    {
        var user   = User.Create("test@example.com",BaseTime);
        var device = user.RegisterDevice("device-001", "Mi Phone", "Android", DateTime.UtcNow);

        Assert.NotEqual(Guid.Empty, device.Id);
    }

    [Fact]
    public void RegisterDevice_WithRevokedAt_SetsRevokedAt()
    {
        var user      = User.Create("test@example.com",BaseTime);
        var now       = DateTime.UtcNow;
        var revokedAt = now.AddDays(30);

        var device = user.RegisterDevice("device-001", "Mi Phone", "Android", now, revokedAt);

        Assert.Equal(revokedAt, device.RevokedAt);
    }

    [Fact]
    public void RegisterDevice_WithoutRevokedAt_RevokedAtIsNull()
    {
        var user   = User.Create("test@example.com",BaseTime);
        var device = user.RegisterDevice("device-001", "Mi Phone", "Android", DateTime.UtcNow);

        Assert.Null(device.RevokedAt);
    }

    // ─── User.RegisterDevice — dispositivo existente ────────────────────────────

    [Fact]
    public void RegisterDevice_ExistingDevice_DoesNotAddDuplicate()
    {
        var user = User.Create("test@example.com",BaseTime);
        var now  = DateTime.UtcNow;

        user.RegisterDevice("device-001", "Mi Phone", "Android", now);
        user.RegisterDevice("device-001", "Mi Phone", "Android", now.AddHours(1));

        Assert.Single(user.DeviceTokens);
    }

    [Fact]
    public void RegisterDevice_ExistingDevice_ReturnsTheSameInstance()
    {
        var user = User.Create("test@example.com",BaseTime);
        var now  = DateTime.UtcNow;

        var first  = user.RegisterDevice("device-001", "Mi Phone", "Android", now);
        var second = user.RegisterDevice("device-001", "Mi Phone", "Android", now.AddHours(1));

        Assert.Same(first, second);
    }

    [Fact]
    public void RegisterDevice_ExistingDevice_UpdatesLastSeen()
    {
        var user    = User.Create("test@example.com",BaseTime);
        var now     = new DateTime(2024, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        var laterOn = now.AddHours(5);

        user.RegisterDevice("device-001", "Mi Phone", "Android", now);
        var device = user.RegisterDevice("device-001", "Mi Phone", "Android", laterOn);

        Assert.Equal(laterOn, device.LastSeenAt);
    }

    [Fact]
    public void RegisterDevice_ExistingDevice_DoesNotChangeRegisteredAt()
    {
        var user    = User.Create("test@example.com",BaseTime);
        var now     = new DateTime(2024, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        var laterOn = now.AddHours(5);

        user.RegisterDevice("device-001", "Mi Phone", "Android", now);
        var device = user.RegisterDevice("device-001", "Mi Phone", "Android", laterOn);

        Assert.Equal(now, device.CreatedAt);
    }

    // ─── User.RegisterDevice — múltiples dispositivos ───────────────────────────

    [Fact]
    public void RegisterDevice_MultipleDistinctDevices_AddsAll()
    {
        var user = User.Create("test@example.com",BaseTime);
        var now  = DateTime.UtcNow;

        user.RegisterDevice("device-001", "Phone",  "Android", now);
        user.RegisterDevice("device-002", "Tablet", "iOS",     now);
        user.RegisterDevice("device-003", "Watch",  "WearOS",  now);

        Assert.Equal(3, user.DeviceTokens.Count);
    }

    [Fact]
    public void RegisterDevice_MultipleDistinctDevices_EachHasUniqueId()
    {
        var user = User.Create("test@example.com",BaseTime);
        var now  = DateTime.UtcNow;

        var d1 = user.RegisterDevice("device-001", "Phone",  "Android", now);
        var d2 = user.RegisterDevice("device-002", "Tablet", "iOS",     now);

        Assert.NotEqual(d1.Id, d2.Id);
    }

    // ─── Colecciones son read-only ───────────────────────────────────────────────

    [Fact]
    public void DeviceTokens_IsReadOnlyCollection()
    {
        var user = User.Create("test@example.com",BaseTime);

        Assert.IsAssignableFrom<IReadOnlyCollection<DeviceToken>>(user.DeviceTokens);
    }


    [Fact]
    public void OtpCodes_IsReadOnlyCollection()
    {
        var user = User.Create("test@example.com",BaseTime);

        Assert.IsAssignableFrom<IReadOnlyCollection<OtpCode>>(user.OtpCodes);
    }
}