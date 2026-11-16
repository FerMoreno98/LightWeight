using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.Application.Utils;

internal static class Converters
{
    public static string MapMuscleGroup(MuscleGroups muscleGroups) => muscleGroups switch
    {
        MuscleGroups.Chest => "Chest",
        MuscleGroups.Back => "Back",
        MuscleGroups.Traps => "Traps",
        MuscleGroups.FrontDelts => "FrontDelts",
        MuscleGroups.SideDelts => "SideDelts",
        MuscleGroups.RearDelts => "RearDelts",
        MuscleGroups.Triceps => "Triceps",
        MuscleGroups.Biceps => "Biceps",
        MuscleGroups.Forearms => "Forearms",
        MuscleGroups.Abs => "Abs",
        MuscleGroups.Glutes => "Glutes",
        MuscleGroups.Quads => "Quads",
        MuscleGroups.Hamstring => "Hamstring",
        MuscleGroups.Calves => "Calves",
        MuscleGroups.Adductors => "Adductors",
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

    public static string? MapTechnique(AdvanceTrainingTechniques technique)
    {
        if (technique.IsDropSet) return "DropSet";
        if (technique.IsCluster) return "Cluster";
        if (technique.IsMyoRep) return "MyoRep";
        return null;
    }
}
