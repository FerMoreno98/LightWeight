using FluentValidation.Validators;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Exceptions;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.UnitTests.Domain;

public class TemplateSetTests
{
    [Theory]
    [InlineData(6,8,8,MuscleGroups.Back)]
    [InlineData(16,12,7.5,MuscleGroups.Chest)]
    [InlineData(7,15,10,MuscleGroups.Biceps)]
    public void Create_WithValidData_ReturnSetTemplate
    (
        int min,
        int max,
        double rpe,
        MuscleGroups muscleGroups
    )
    {
        // Arrange
        Guid exerciseId = Guid.CreateVersion7();
        RepetitionRange repetitionRange = RepetitionRange.Create(max,min);
        List<MuscleGroups> aimMuscles = new List<MuscleGroups>();
        aimMuscles.Add(muscleGroups);
        // Act
        TemplateSet set = TemplateSet.Create
        (
            exerciseId,
            repetitionRange,
            (decimal)rpe,
            aimMuscles
        );
        // Assert
        Assert.Equal(exerciseId,set.ExerciseId);
        Assert.Equal(repetitionRange,set.RepetitionRange);
        Assert.Equal((decimal)rpe,set.ExpectedRPE);
        Assert.Equal(muscleGroups,set.AimMuscleGroups.SingleOrDefault());
    }

}