namespace LightWeight.UserProfile.Api.DTOs;
public record ChangeTrainingStageRequest(
    Guid UserId,
    string NewStage);