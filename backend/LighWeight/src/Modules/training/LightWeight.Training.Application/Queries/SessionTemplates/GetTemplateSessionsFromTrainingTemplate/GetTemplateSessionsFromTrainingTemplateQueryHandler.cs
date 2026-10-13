using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Repositories;

namespace LightWeight.Training.Application.Queries.SessionTemplates.GetTemplateSessionsFromTrainingTemplate;

public sealed class GetTemplateSessionsFromTrainingTemplateQueryHandler : IQueryHandler<GetTemplateSessionsFromTrainingTemplateQuery, List<GetTemplateSessionsFromTrainingTemplateResponse>>
{
    private readonly IProgramRepository _programRepository;

    public GetTemplateSessionsFromTrainingTemplateQueryHandler(IProgramRepository programRepository)
    {
        _programRepository = programRepository;
    }

    public async Task<List<GetTemplateSessionsFromTrainingTemplateResponse>> HandleAsync(GetTemplateSessionsFromTrainingTemplateQuery query, CancellationToken ct = default)
    {
        Program program = await _programRepository.GetByTrainingTemplateIdAsync(query.TrainingTemplateId)
            ?? throw new TrainingTemplateNotFoundApplicationException();
        if(program.UserId != query.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        TrainingTemplate trainingTemplate = program.trainingTemplates.SingleOrDefault(t => t.Id == query.TrainingTemplateId)
            ?? throw new TrainingTemplateNotFoundApplicationException();

        // The repository loads the sessions in creation order
        return trainingTemplate.TemplateSessions
            .Select(s => new GetTemplateSessionsFromTrainingTemplateResponse
            (
                s.Id,
                s.Name,
                s.TemplateExercises.Count,
                s.TemplateExercises.Select(e => e.ExerciseId).Distinct().Count()
            ))
            .ToList();
    }
}
