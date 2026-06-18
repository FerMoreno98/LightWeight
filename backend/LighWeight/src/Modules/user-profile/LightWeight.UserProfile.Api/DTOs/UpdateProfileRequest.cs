namespace LightWeight.UserProfile.Api.DTOs;
public record UpdateProfileRequest(
    Guid UserId,
    string FullName,
    string Sex,
    DateTime DateOfBirth);