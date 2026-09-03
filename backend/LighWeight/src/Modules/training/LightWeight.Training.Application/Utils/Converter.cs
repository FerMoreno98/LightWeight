using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Application.Utils;

internal static class Converters
{
    public static string MapMuscleGroup(MuscleGroups muscleGroups) => muscleGroups switch
    {
        MuscleGroups.Shoulder => "Shoulder",
        MuscleGroups.Back => "Back",
        MuscleGroups.Chest => "Chest",
        MuscleGroups.Biceps => "Biceps",
        MuscleGroups.Triceps => "Triceps",
        MuscleGroups.Glutes => "Glutes",
        MuscleGroups.Quads => "Quads",
        MuscleGroups.Hamstring => "Hamstring",
        MuscleGroups.calves => "Calves",
        _ => throw new ArgumentOutOfRangeException(nameof(muscleGroups), muscleGroups, null)
    };

    public static string VolumeLandmarkConverter(VolumeLandmarks volumeLandmark) => volumeLandmark switch
    {
        VolumeLandmarks.MV => "MV",
        VolumeLandmarks.MEV => "MEV",
        VolumeLandmarks.MAV => "MAV",
        VolumeLandmarks.MRV => "MRV",
        _ => throw new ArgumentOutOfRangeException(nameof(volumeLandmark), volumeLandmark, null)
    };

    public static string TrainingDistributionConverter(TrainingDistribution trainingDistribution) => trainingDistribution switch
    {
        TrainingDistribution.PushPullLegs => "PushPullLegs",
        TrainingDistribution.UpperLower => "UpperLower",
        TrainingDistribution.Weider => "Weider",
        TrainingDistribution.Phat => "Phat",
        TrainingDistribution.FullBody => "FullBody",
        TrainingDistribution.Other => "Other",
        _ => throw new ArgumentOutOfRangeException(nameof(trainingDistribution), trainingDistribution, null)
    };
}
