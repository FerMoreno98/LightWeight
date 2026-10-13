using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Application.Utils;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Repositories;

namespace LightWeight.Training.Application.Queries.Mesocycles.GetMesocycleDetail;

public sealed class GetMesocycleDetailQueryHandler : IQueryHandler<GetMesocycleDetailQuery, GetMesocycleDetailResponse>
{
    private readonly IMacrocycleRepository _macrocycleRepository;
    private readonly IProgramRepository _programRepository;

    public GetMesocycleDetailQueryHandler(IMacrocycleRepository macrocycleRepository, IProgramRepository programRepository)
    {
        _macrocycleRepository = macrocycleRepository;
        _programRepository = programRepository;
    }

    public async Task<GetMesocycleDetailResponse> HandleAsync(GetMesocycleDetailQuery query, CancellationToken ct = default)
    {
        Macrocycle macrocycle = await _macrocycleRepository.GetByMesocycleIdAsync(query.MesocycleId)
            ?? throw new MesocycleNotFoundException();
        if(macrocycle.UserId != query.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        Mesocycle mesocycle = macrocycle.Mesocycles.Single(m => m.Id == query.MesocycleId);
        Program program = await _programRepository.GetByIdAsync(mesocycle.ProgramId)
            ?? throw new ProgramNotFoundApplicationException();
        // Soft deleted templates are not loaded, so a week may point to a template that is no longer there
        Dictionary<Guid, TrainingTemplate> templates = program.trainingTemplates.ToDictionary(t => t.Id);

        return new GetMesocycleDetailResponse
        (
            mesocycle.Id,
            mesocycle.MacrocycleId,
            program.Id,
            program.Name,
            mesocycle.StartedAt,
            mesocycle.FinishedAt,
            mesocycle.MotivationLevel,
            mesocycle.Injuries,
            mesocycle.Comments,
            mesocycle.Microcycles
                .OrderBy(m => m.WeekNumber)
                .Select(m =>
                {
                    TrainingTemplate? template = templates.GetValueOrDefault(m.TrainingTemplateId);
                    return new MesocycleMicrocycleResponse(m.Id, m.WeekNumber, m.TrainingTemplateId, template?.Name, template?.DurationInDays, m.StartedAt, m.FinishedAt);
                })
                .ToList(),
            program.trainingTemplates
                .OrderBy(t => t.Order)
                .Select(t => new MesocycleTemplateResponse
                (
                    t.Id,
                    t.Name,
                    t.Order,
                    t.DurationInDays,
                    Converters.VolumeLandmarkConverter(t.VolumeLandmark),
                    Converters.TrainingDistributionConverter(t.TrainingDistribution)
                ))
                .ToList()
        );
    }
}
