namespace LightWeight.UserProfile.Api.DTOs;

/// <param name="Sex">Male or Female (case-insensitive)</param>
public record UpdateProfileRequest(
    string FullName,
    string Sex,
    DateTime DateOfBirth);
