namespace LightWeight.Training.Domain.Enum;

/// <summary>How an exercise is performed regarding the limbs. For now it is only shown to the user</summary>
public enum Laterality
{
    /// <summary>Both limbs at the same time</summary>
    Bilateral,
    /// <summary>One limb at a time</summary>
    Unilateral,
    /// <summary>It can be performed either way</summary>
    Both
}
