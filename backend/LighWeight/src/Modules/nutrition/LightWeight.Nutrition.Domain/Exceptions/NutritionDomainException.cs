using LightWeight.shared.Exceptions;

namespace LightWeight.Nutrition.Domain.Exceptions;

public abstract class NutritionDomainException : BusinessException
{
    protected NutritionDomainException(string message) : base(message)
    {

    }
}
