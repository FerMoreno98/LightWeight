using LightWeight.shared.BuildingBlocks;

namespace LightWeight.Training.Domain.Entities;

public sealed class Serie : Entity<Guid>
{
    public int Repetitions{get; private set;}
    public bool IsCorporalWeight {get; private set;}
    public decimal Weight {get; private set;}
    public decimal RPE {get; private set;}
    
}