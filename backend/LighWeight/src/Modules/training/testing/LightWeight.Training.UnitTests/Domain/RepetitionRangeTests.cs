using LightWeight.Training.Domain.Exceptions;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.UnitTests.Domain;

public class RepetitionRangeTests
{
    [Theory]
    [InlineData(-6,8)]
    [InlineData(16,-12)]
    [InlineData(0,15)]
    [InlineData(5,0)]
    public void Create_WithInValidData_throwDomainException
    (
        int min,
        int max
    )
    {
        // Arrange
        // Act
        // Assert
        Assert.Throws<ReptitionRangeLessThanZeroDomainException>
        (
            ()=>
            RepetitionRange.Create(max,min)
        );
    }
}