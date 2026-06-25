using LightWeight.shared.BuildingBlocks;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Domain.ValueObjects;

public sealed record Exercise : ValueObject
{
    public string Name{get; private set;}
    public bool IsBilateral{get; private set;}
    
    private List<MuscleGroups> _aimMuscleGruop = new();
    public IReadOnlyCollection<MuscleGroups> AimMuscleGroups => _aimMuscleGruop.AsReadOnly();

}