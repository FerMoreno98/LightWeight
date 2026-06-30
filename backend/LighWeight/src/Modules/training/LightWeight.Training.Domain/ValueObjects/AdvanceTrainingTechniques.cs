using LightWeight.Training.Domain.Exceptions;
using LightWeight.shared.BuildingBlocks;

namespace LightWeight.Training.Domain.ValueObjects;

public sealed record AdvanceTrainingTechniques : ValueObject
{
    private AdvanceTrainingTechniques(bool isDropSet, bool isCluster, bool isMyoRep)
    {
        IsDropSet = isDropSet;
        IsCluster = isCluster;
        IsMyoRep = isMyoRep;
    }

    /// <summary>Whether a drop set is applied</summary>
    public bool IsDropSet { get; init; }
    /// <summary>Whether a cluster set is applied</summary>
    public bool IsCluster { get; init; }
    /// <summary>Whether a myo-rep set is applied</summary>
    public bool IsMyoRep { get; init; }

    /// <summary>Default value: no advanced technique</summary>
    public static AdvanceTrainingTechniques None => new(false, false, false);

    /// <summary>Creates a set of advance training techniques</summary>
    /// <param name="isDropSet">Whether it's a drop set</param>
    /// <param name="isCluster">Whether it's a cluster set</param>
    /// <param name="isMyoRep">Whether it's a myo-rep set</param>
    /// <exception cref="AdvanceTrainingTechniquesExceptions">Thrown when more than one technique is active</exception>
    public static AdvanceTrainingTechniques Create
    (
        bool isDropSet,
        bool isCluster,
        bool isMyoRep
    )
    {
        int activeCount = (isDropSet ? 1 : 0)
                        + (isCluster ? 1 : 0)
                        + (isMyoRep ? 1 : 0);

        if (activeCount > 1)
            throw new AdvanceTrainingTechniquesExceptions();

        return new AdvanceTrainingTechniques(isDropSet, isCluster, isMyoRep);
    }
}