namespace LightWeight.BodyMetrics.Api.DTOs;

/// <summary>Body weight in kg and when it was measured (UTC; a date without zone is taken as UTC)</summary>
public sealed record WeightRequest(decimal Weight, DateTime MeasuredAt);
