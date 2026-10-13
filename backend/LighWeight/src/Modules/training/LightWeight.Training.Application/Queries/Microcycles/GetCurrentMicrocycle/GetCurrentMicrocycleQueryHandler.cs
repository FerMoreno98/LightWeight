using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Repositories;

namespace LightWeight.Training.Application.Queries.Microcycles.GetCurrentMicrocycle;

public sealed class GetCurrentMicrocycleQueryHandler : IQueryHandler<GetCurrentMicrocycleQuery, GetCurrentMicrocycleResponse>
{
    private readonly IMacrocycleRepository _macrocycleRepository;
    private readonly IProgramRepository _programRepository;

    public GetCurrentMicrocycleQueryHandler(IMacrocycleRepository macrocycleRepository, IProgramRepository programRepository)
    {
        _macrocycleRepository = macrocycleRepository;
        _programRepository = programRepository;
    }

    public async Task<GetCurrentMicrocycleResponse> HandleAsync(GetCurrentMicrocycleQuery query, CancellationToken ct = default)
    {
        Macrocycle? macrocycle = await _macrocycleRepository.GetActiveOfAUserAsync(query.UserId);
        if(macrocycle is null)
        {
            return new GetCurrentMicrocycleResponse(CurrentMicrocycleStatus.NoActiveMacrocycle, null, null, null, null, null, null);
        }
        Mesocycle? mesocycle = macrocycle.ActiveMesocycle;
        if(mesocycle is null)
        {
            return new GetCurrentMicrocycleResponse(CurrentMicrocycleStatus.NoActiveMesocycle, macrocycle.Id, null, null, null, null, null);
        }

        Program program = await _programRepository.GetByIdAsync(mesocycle.ProgramId)
            ?? throw new ProgramNotFoundApplicationException();
        Microcycle? microcycle = mesocycle.ActiveMicrocycle;
        if(microcycle is null)
        {
            int nextWeek = mesocycle.Microcycles.Count == 0 ? 1 : mesocycle.Microcycles.Max(m => m.WeekNumber) + 1;
            return new GetCurrentMicrocycleResponse(CurrentMicrocycleStatus.PendingPlanning, macrocycle.Id, mesocycle.Id, program.Id, program.Name, nextWeek, null);
        }

        // Soft deleted templates are not loaded, so the week may point to a template that is no longer there
        TrainingTemplate? template = program.trainingTemplates.SingleOrDefault(t => t.Id == microcycle.TrainingTemplateId);
        return new GetCurrentMicrocycleResponse
        (
            CurrentMicrocycleStatus.InProgress,
            macrocycle.Id,
            mesocycle.Id,
            program.Id,
            program.Name,
            null,
            new CurrentMicrocycleResponse
            (
                microcycle.Id,
                microcycle.WeekNumber,
                microcycle.StartedAt,
                microcycle.TrainingTemplateId,
                template?.Name,
                template?.DurationInDays
            )
        );
    }
}
