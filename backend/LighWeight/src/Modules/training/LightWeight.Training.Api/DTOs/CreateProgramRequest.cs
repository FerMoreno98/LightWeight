namespace LightWeight.Training.Api.DTOs;

public sealed record CreateProgramRequest
(
    string Name,
    string Periodization,
    List<string> AimMuscleGroups
);
