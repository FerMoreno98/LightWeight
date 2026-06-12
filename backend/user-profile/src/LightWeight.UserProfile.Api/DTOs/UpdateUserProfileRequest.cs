

namespace LightWeight.UserProfile.Api.DTOs;

public sealed record UpdateUserProfileRequest
{
    public string Name{get;set;}
    public string Sex{get;set;}
    public DateTime DateOfBirth{get;set;}
}