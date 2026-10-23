using LightWeight.Dashboard.Application.Calculations.Training;
using LightWeight.Dashboard.Application.Projections.Training;
using LightWeight.shared.Mediator;

namespace LightWeight.Dashboard.Application.Queries.Training.GetTrainingVolume;

/// <summary>Sets per muscle group of every training week of the user, from the oldest</summary>
public sealed record GetTrainingVolumeQuery(Guid UserId) : IQuery<List<WeeklyVolumePoint>>;
