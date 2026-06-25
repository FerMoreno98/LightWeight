using LightWeight.shared.BuildingBlocks;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.Domain.Entities;

public sealed class TemplateExercise : Entity<Guid>
{
    public string Exercise {get;private set;}
    public bool IsBilateral {get; private set;}
   
    private List<MuscleGroups> _aimMuscleGruop = new();
    public IReadOnlyCollection<MuscleGroups> AimMuscleGroups => _aimMuscleGruop.AsReadOnly();
    private List<TemplateSerie> _series = new();

    private TemplateExercise
    (
        Guid Id,
        string exercise, 
        bool isBilateral, 
        List<MuscleGroups> aimMuscleGroup
    ) : base(Id)
    {
        Exercise = exercise;
        IsBilateral = isBilateral;
        _aimMuscleGruop = aimMuscleGroup;
    }

    public IReadOnlyCollection<TemplateSerie> Series => _series.AsReadOnly();
    
}