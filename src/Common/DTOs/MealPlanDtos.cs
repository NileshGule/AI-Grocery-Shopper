namespace Common.DTOs
{
    public record MealPlanRequestDto(string Preferences, string Constraints);

    public record MealDay(string Day, string[] Meals);

    public record MealPlanResponseDto(string RawText, MealDay[]? Days = null);
}
