using LightWeight.bodymetrics.Domain.Aggregates;
using LightWeight.bodymetrics.Domain.Events;
using LightWeight.bodymetrics.Domain.Exceptions;

namespace LightWeight.bodymetrics.UnitTests.Domain;

public class SkinFoldsTests
{
    public static IEnumerable<Object[]> ValidValues()
    {
        yield return new Object[] 
        {
            10.2m,8.6m,null,
            null,3.4m,null
        };
        yield return new Object[] 
        {
            10.2m,8.6m,6.4m,
            9.2m,3.4m,4.2m
        };
        yield return new Object[] 
        {
            null,null,6.4m,
            9.2m,3.4m,4.2m
        };
    }

    [Theory]
    [MemberData(nameof(ValidValues))]
    public void Create_WithValidData_RaisesDomainEvent
    (
        decimal? abdominal, 
        decimal? suprailiac, 
        decimal? tricipital, 
        decimal? subscapular, 
        decimal? thigh, 
        decimal? calf
    )
    {
        // Arrange
        // Act
        SkinFolds skinFolds = SkinFolds.Create
        (
            Guid.NewGuid(),
            abdominal, 
            suprailiac, 
            tricipital, 
            subscapular, 
            thigh, 
            calf,
            DateTime.UtcNow
        );
        // Assert
        Assert.Equal(abdominal,skinFolds.Abdominal);
        Assert.Equal(suprailiac,skinFolds.Suprailiac);
        Assert.Equal(tricipital,skinFolds.Tricipital);
        Assert.Equal(subscapular,skinFolds.Subscapular);
        Assert.Equal(thigh,skinFolds.Thigh);
        Assert.Equal(calf,skinFolds.Calf);

        Assert.Single(skinFolds.DomainEvents);
        var domainEvent = Assert.IsType<RegisteredSkinFoldsDomainEvent>(skinFolds.DomainEvents.Single());
        Assert.Equal(skinFolds.Id,domainEvent.SkinFoldId);
        Assert.Equal(skinFolds.UserId,domainEvent.UserId);
        
    }

    [Fact]
    public void Create_WithEveryValueNull_ThrowDomainException()
    {
        // Arrange

        // Act
        // Assert
        Assert.Throws<EverySkinFoldNullException>(()=>SkinFolds.Create
        (
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            null,
            null,
            DateTime.UtcNow
        ));
    }
    [Theory]
    [InlineData(-2.3)]
    [InlineData(0)]
    public void Create_WithInvalidValues_ThrowsDomainException(double abdominal)
    {
        // Arrange
        decimal? ConvertedSkinFold = (decimal)abdominal;
        // Act
        // Assert
        Assert.Throws<NegativeSkinFoldException>(()=>       
        SkinFolds.Create
        (
            Guid.NewGuid(),
            ConvertedSkinFold,
            null,
            null,
            null,
            23.3m,
            3.3m,
            DateTime.UtcNow
        ));

    }
}