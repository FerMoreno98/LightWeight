using LightWeight.Dashboard.Application.Calculations;

namespace LightWeight.Dashboard.UnitTests.Application;

public class WeeklyAveragesTests
{
    // 2026-10-12 is a Monday
    private static readonly DateTime Monday = new(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc);

    private static DateTime At(int day, int hour = 8) => Monday.AddDays(day).AddHours(hour);

    [Fact]
    public void ValuesOfTheSameWeek_AreAveraged()
    {
        // Act
        var weeks = WeeklyAverages.Calculate([(At(0), 82m), (At(2), 81m), (At(4), 80m)]);
        // Assert
        WeeklyAverage week = Assert.Single(weeks);
        Assert.Equal(Monday, week.WeekStart);
        Assert.Equal(81m, week.Average);
        Assert.Equal(3, week.Count);
    }

    [Fact]
    public void Sunday_BelongsToTheWeekOfThePreviousMonday_AndMondayStartsANewOne()
    {
        // Act: Sunday 23:00 and the next Monday 00:30
        var weeks = WeeklyAverages.Calculate([(At(6, 23), 82m), (At(7, 0).AddMinutes(30), 80m)]);
        // Assert
        Assert.Equal(2, weeks.Count);
        Assert.Equal(Monday, weeks[0].WeekStart);
        Assert.Equal(82m, weeks[0].Average);
        Assert.Equal(Monday.AddDays(7), weeks[1].WeekStart);
        Assert.Equal(80m, weeks[1].Average);
    }

    [Fact]
    public void WeeksWithoutValues_DoNotAppear_AndTheResultIsSortedByWeek()
    {
        // Act: unordered input, with an empty week in between
        var weeks = WeeklyAverages.Calculate([(At(14), 79m), (At(0), 82m)]);
        // Assert
        Assert.Equal([Monday, Monday.AddDays(14)], weeks.Select(w => w.WeekStart));
    }

    [Fact]
    public void WithoutValues_ReturnsAnEmptyList()
    {
        Assert.Empty(WeeklyAverages.Calculate([]));
    }

    [Fact]
    public void Average_IsRoundedToTwoDecimals()
    {
        // Act: (82.4 + 82.1 + 81.9) / 3 = 82.1333...
        var weeks = WeeklyAverages.Calculate([(At(0), 82.4m), (At(1), 82.1m), (At(2), 81.9m)]);
        // Assert
        Assert.Equal(82.13m, Assert.Single(weeks).Average);
    }

    [Fact]
    public void DateWithoutZone_IsTakenAsUtc()
    {
        // Sunday 23:00 without zone must stay on Sunday, whatever the time zone of the machine
        DateTime unspecified = DateTime.SpecifyKind(At(6, 23), DateTimeKind.Unspecified);

        Assert.Equal(Monday, WeeklyAverages.WeekStartOf(unspecified));
    }
}
