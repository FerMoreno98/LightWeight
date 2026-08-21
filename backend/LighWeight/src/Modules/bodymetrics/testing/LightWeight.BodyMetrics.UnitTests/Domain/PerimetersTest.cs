using System.Runtime.CompilerServices;
using LightWeight.bodymetrics.Domain.Aggregates;
using LightWeight.bodymetrics.Domain.Events;
using LightWeight.bodymetrics.Domain.Exceptions;
using Microsoft.Identity.Client;

namespace LightWeight.bodymetrics.UnitTests.Domain;

public class PerimeterTests
{
    public static IEnumerable<Object[]> ValidValues()
    {
        yield return new Object[]
        {
            10.2m, 13.2m, 13.5m, 13.1m, 13.3m, 30.4m, 35.6m,
            18.2m, 16.7m, 14.6m, 13.4m, 12.9m, 9.9m, 9.9m
        };
        yield return new Object[]
        {
            10.2m, null, 13.5m, 13.1m, 13.3m, 30.4m, null,
            18.2m, 16.7m, 14.6m, null, 12.9m, 9.9m, 9.9m
        };
        yield return new Object[]
        {
            null, null, null, null, null, null, null,
            null, null, null, null, null, null, 9.9m
        };
    }
    [Theory]
    [MemberData(nameof(ValidValues))]
    public void Create_WithValidData_RaisesDomainEventAndCreatesPerimeter
    (
        decimal? neck,
        decimal? rightArmRelaxed,
        decimal? rightArmTensioned, 
        decimal? leftArmRelaxed, 
        decimal? leftArmTensioned, 
        decimal? chest, 
        decimal? shoulder, 
        decimal? waist, 
        decimal? hip, 
        decimal? abs, 
        decimal? rightThigh, 
        decimal? leftThigh, 
        decimal? rightCalf, 
        decimal? leftCalf
    )
    {
        // Arrange
        Perimeters perimeters = Perimeters.Create
        (
        Guid.NewGuid(),
         neck,
         rightArmRelaxed,
         rightArmTensioned, 
         leftArmRelaxed, 
         leftArmTensioned, 
         chest, 
         shoulder, 
         waist, 
         hip, 
         abs, 
         rightThigh, 
         leftThigh, 
         rightCalf, 
         leftCalf,
         DateTime.UtcNow
        );
        // Act
        // Assert
        Assert.Equal(neck, perimeters.Neck);
        Assert.Equal(rightArmRelaxed, perimeters.RightArmRelaxed);
        Assert.Equal(rightArmTensioned, perimeters.RightArmTensioned);
        Assert.Equal(leftArmRelaxed, perimeters.LeftArmRelaxed);
        Assert.Equal(leftArmTensioned, perimeters.LeftArmTensioned);
        Assert.Equal(chest, perimeters.Chest);
        Assert.Equal(shoulder, perimeters.Shoulder);
        Assert.Equal(waist, perimeters.Waist);
        Assert.Equal(hip, perimeters.Hip);
        Assert.Equal(abs, perimeters.Abs);
        Assert.Equal(rightThigh, perimeters.RightThigh);
        Assert.Equal(leftThigh, perimeters.LeftThigh);
        Assert.Equal(rightCalf, perimeters.RightCalf);
        Assert.Equal(leftCalf, perimeters.LeftCalf);

        Assert.Single(perimeters.DomainEvents);
        var domainEvent = Assert.IsType<RegisteredPerimetersDomainEvent>(perimeters.DomainEvents.Single());
        Assert.Equal(perimeters.Id, domainEvent.PerimeterId);
        Assert.Equal(perimeters.UserId, domainEvent.UserId);
        
    }

    [Theory]
    [InlineData(-13.5)]
    [InlineData(0)]
    public void Create_NegativeValue_ThrowsDomainException(double ValorInvalido)
    {
        // Arrange
        decimal? valorInvalido = (decimal) ValorInvalido;
        // Act
        // Assert
        Assert.Throws<NegativePerimeterException>(()=>Perimeters.Create
        (
            Guid.NewGuid(),
            10.2m,
            13.2m, 
            valorInvalido, 
            13.1m, 
            13.3m, 
            30.4m, 
            35.6m,
            18.2m, 
            16.7m, 
            14.6m, 
            13.4m, 
            12.9m, 
            9.9m, 
            9.9m,
            DateTime.UtcNow
        ));
                Assert.Throws<NegativePerimeterException>(()=>Perimeters.Create
        (
            Guid.NewGuid(),
            10.2m,
            13.2m, 
            10.5m, 
            13.1m, 
            13.3m, 
            30.4m, 
            35.6m,
            18.2m, 
            16.7m, 
            14.6m, 
            13.4m, 
            12.9m, 
            valorInvalido, 
            9.9m,
            DateTime.UtcNow
        ));
                Assert.Throws<NegativePerimeterException>(()=>Perimeters.Create
        (
            Guid.NewGuid(),
            10.2m,
            13.2m, 
            valorInvalido, 
            13.1m, 
            13.3m, 
            30.4m, 
            35.6m,
            valorInvalido, 
            16.7m, 
            14.6m, 
            13.4m, 
            12.9m, 
            9.9m, 
            9.9m,
            DateTime.UtcNow
        ));
    }
    [Fact]
    public void Create_AllValuesNull_ThrowsDomainException()
    {
        // Arrange
        // Act
        //Assert
        Assert.Throws<EveryPerimeterNullException>(()=>Perimeters.Create
        (
            Guid.NewGuid(),
            null,
            null, 
            null, 
            null, 
            null, 
            null, 
            null,
            null, 
            null, 
            null, 
            null, 
            null, 
            null,
            null,
            DateTime.UtcNow
        ));
    }
}