namespace LightWeight.Nutrition.Domain.Exceptions;

public sealed class UserIdEmptyDomainException : NutritionDomainException
{
    public UserIdEmptyDomainException() : base("The diary must belong to a user")
    {

    }
}
public sealed class NegativeIntakeDomainException : NutritionDomainException
{
    public NegativeIntakeDomainException(string nutrient) : base($"{nutrient} cannot be negative")
    {

    }
}
public sealed class DiaryDayInTheFutureDomainException : NutritionDomainException
{
    public DiaryDayInTheFutureDomainException(DateOnly date)
        : base($"The diary of {date:yyyy-MM-dd} cannot be recorded yet")
    {

    }
}
public sealed class InvalidDiaryPeriodDomainException : NutritionDomainException
{
    public InvalidDiaryPeriodDomainException(DateOnly from, DateOnly to)
        : base($"The period cannot start ({from:yyyy-MM-dd}) after it ends ({to:yyyy-MM-dd})")
    {

    }
}
public sealed class DiaryDayOutOfPeriodDomainException : NutritionDomainException
{
    public DiaryDayOutOfPeriodDomainException(DateOnly date)
        : base($"The day {date:yyyy-MM-dd} is out of the synchronized period")
    {

    }
}
public sealed class DuplicatedDiaryDayDomainException : NutritionDomainException
{
    public DuplicatedDiaryDayDomainException(DateOnly date) : base($"The day {date:yyyy-MM-dd} is repeated")
    {

    }
}
public sealed class DiaryDayOfAnotherUserDomainException : NutritionDomainException
{
    public DiaryDayOfAnotherUserDomainException() : base("Only the days of the synchronized user can be synchronized")
    {

    }
}
