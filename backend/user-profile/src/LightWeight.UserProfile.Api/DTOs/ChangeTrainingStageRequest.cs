namespace LightWeight.UserProfile.Api.DTOs;

public sealed record ChangeTrainingStageRequest
{
    public Guid UserId{get;set;}
    public string Stage{get;set;}
}