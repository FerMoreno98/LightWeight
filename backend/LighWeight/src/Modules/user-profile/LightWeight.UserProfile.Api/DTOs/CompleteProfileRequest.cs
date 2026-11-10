namespace LightWeight.UserProfile.Api.DTOs;

/// <param name="Sex">Male or Female (case-insensitive)</param>
public record CompleteProfileRequest(
    string FullName,
    DateTime BirthDate,
    string Sex);
