namespace LightWeight.UserProfile.Api.DTOs;
public record CompleteProfileRequest(
    Guid UserId,
    string FullName,
    DateTime BirthDate,
    string Sex,
    string Stage
    );