using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Queries.SessionTemplates.GetNumberOfSeriesPerGroupPerSession;

public sealed record GetNumberOfSeriesPerGroupPerSessionQuery(Guid TrainingTemplateId) : IQuery<List<GetNumberOfSeriesPerGroupPerSessionResponse>>;

  public sealed record GetNumberOfSeriesPerGroupPerSessionResponse(
      Guid SessionId,
      string SessionName,
      Dictionary<string, int> Series
  );