namespace Travio.Core.Helpers;


public record SurveyOption(int Id, string Label, string ImageUrl);
public record SurveyQuestion(int CategoryId, string Title, List<SurveyOption> Options);
public static class SurveyConfiguration
{
    public static readonly List<SurveyQuestion> OnboardingSurvey = new()
    {
        new SurveyQuestion(1, "What type of travel do you prefer?", new List<SurveyOption>
        {
            new (1, "Beaches", "https://your-storage.com/images/beaches.jpg"),
            new (2, "City Life", "https://your-storage.com/images/city.jpg"),
            new (3, "Hotels", "https://your-storage.com/images/safari.jpg"),
            new (4, "SafariDesert", "https://your-storage.com/images/safari.jpg"),
            new (5, "Nature", "https://your-storage.com/images/safari.jpg"),
            new (6, "ShoppingMalls", "https://your-storage.com/images/safari.jpg"),
               // need the photos
        }),
        new SurveyQuestion(2, "What is your preferred travel companion?", new List<SurveyOption>
        {
            new (1, "Solo Travel", "https://your-storage.com/images/solo.jpg"),
            new (2, "With Partner", "https://your-storage.com/images/partner.jpg"),
            new (3, "Family Trip", "https://your-storage.com/images/family.jpg"),
            new (4, "With Friends", "https://your-storage.com/images/friends.jpg"),
               // need the photos
        }),
        new SurveyQuestion(3, "What type of activities do you enjoy during your travels?", new List<SurveyOption>
        {
            new (1, "Relaxed", "https://your-storage.com/images/relaxed.jpg"),
            new (2, "Diving", "https://your-storage.com/images/diving.jpg"),
            new (3, "Adventurous", "https://your-storage.com/images/adventurous.jpg"),
            new (4, "Photography", "https://your-storage.com/images/photography.jpg"),
               // need the photos
        }),
        new SurveyQuestion(4, "What is your typical travel budget?", new List<SurveyOption>
        {
            new (1, "Budget Traveler", "https://your-storage.com/images/budget.jpg"),
            new (2, "Mid-Range", "https://your-storage.com/images/midrange.jpg"),
            new (3, "Premium", "https://your-storage.com/images/premium.jpg"),
            new (4, "Ultra Luxury", "https://your-storage.com/images/ultraluxury.jpg"),
               // need the photos
        })
    };
}
