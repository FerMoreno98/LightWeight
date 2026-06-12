

namespace LightWeight.UserProfile.Api.DTOs;

public sealed record CompleteUserProfileRequest
{
    public string Name{get;set;}
    public DateTime DateOfBirth{get; set;}
    public string Sex{get;set;}
    public string CurrentStage{get;set;}
    

}