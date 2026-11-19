using LightWeight.Nutrition.Domain.Exceptions;
using LightWeight.Nutrition.Domain.ValueObjects;

namespace LightWeight.Nutrition.UnitTests.Domain;

public class ValueObjectsTests
{
    [Fact]
    public void DailyIntake_WithZeros_IsValid()
    {
        DailyIntake intake = DailyIntake.Create(0m, 0m, 0m, 0m);

        Assert.Equal(0m, intake.Calories);
    }

    [Theory]
    [InlineData(-1, 0, 0, 0, "Calories")]
    [InlineData(0, -1, 0, 0, "Protein")]
    [InlineData(0, 0, -1, 0, "Carbohydrate")]
    [InlineData(0, 0, 0, -1, "Fat")]
    public void DailyIntake_WithANegativeValue_ThrowsDomainException(double calories, double protein, double carbohydrate, double fat, string nutrient)
    {
        var exception = Assert.Throws<NegativeIntakeDomainException>
        (
            () => DailyIntake.Create((decimal)calories, (decimal)protein, (decimal)carbohydrate, (decimal)fat)
        );
        Assert.Contains(nutrient, exception.Message);
    }

    [Fact]
    public void DailyIntake_WithTheSameValues_AreEqual()
    {
        Assert.Equal(DailyIntake.Create(2000m, 150m, 200m, 70m), DailyIntake.Create(2000m, 150m, 200m, 70m));
    }

    [Fact]
    public void DiaryPeriod_ContainsBothEnds()
    {
        DiaryPeriod period = DiaryPeriod.Create(new DateOnly(2026, 11, 13), new DateOnly(2026, 11, 19));

        Assert.True(period.Contains(new DateOnly(2026, 11, 13)));
        Assert.True(period.Contains(new DateOnly(2026, 11, 19)));
        Assert.False(period.Contains(new DateOnly(2026, 11, 12)));
        Assert.False(period.Contains(new DateOnly(2026, 11, 20)));
    }

    [Fact]
    public void DiaryPeriod_OfOneDay_IsValid()
    {
        var day = new DateOnly(2026, 11, 19);

        Assert.True(DiaryPeriod.Create(day, day).Contains(day));
    }

    [Fact]
    public void DiaryPeriod_StartingAfterItEnds_ThrowsDomainException()
    {
        Assert.Throws<InvalidDiaryPeriodDomainException>
        (
            () => DiaryPeriod.Create(new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 19))
        );
    }
}
