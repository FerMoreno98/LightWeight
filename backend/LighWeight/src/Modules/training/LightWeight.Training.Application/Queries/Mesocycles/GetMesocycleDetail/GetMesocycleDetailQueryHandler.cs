using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Application.Utils;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Repositories;

namespace LightWeight.Training.Application.Queries.Mesocycles.GetMesocycleDetail;

public sealed class GetMesocycleDetailQueryHandler : IQueryHandler<GetMesocycleDetailQuery, GetMesocycleDetailResponse>
{
    private readonly IMesocycleRepository _mesocycleRepository;
    private readonly IMicrocycleRepository _microcycleRepository;
    private readonly IProgramRepository _programRepository;

    public GetMesocycleDetailQueryHandler(IMesocycleRepository mesocycleRepository, IMicrocycleRepository microcycleRepository, IProgramRepository programRepository)
    {
        _mesocycleRepository = mesocycleRepository;
        _microcycleRepository = microcycleRepository;
        _programRepository = programRepository;
    }

    public async Task<GetMesocycleDetailResponse> HandleAsync(GetMesocycleDetailQuery query, CancellationToken ct = default)
    {
        Mesocycle mesocycle = await _mesocycleRepository.GetByIdAsync(query.MesocycleId)
            ?? throw new MesocycleNotFoundException();
        if(mesocycle.UserId != query.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        Program program = await _programRepository.GetByIdAsync(mesocycle.ProgramId)
            ?? throw new ProgramNotFoundApplicationException();
        // Soft deleted templates are not loaded, so a week may point to a template that is no longer there
        Dictionary<Guid, TrainingTemplate> templates = program.trainingTemplates.ToDictionary(t => t.Id);
        List<Microcycle> microcycles = await _microcycleRepository.GetByMesocycleIdAsync(mesocycle.Id);

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
            microcycles
                .OrderBy(m => m.WeekNumber)
                .Select(m =>
                {
                    TrainingTemplate? template = templates.GetValueOrDefault(m.TrainingTemplateId);
                    return new MesocycleMicrocycleResponse(m.Id, m.WeekNumber, m.TrainingTemplateId, template?.Name, template?.DurationInDays);
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
