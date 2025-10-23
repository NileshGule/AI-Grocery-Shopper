using System.Collections.Generic;

namespace UI.Models
{
    public class UserInput
    {
        public string Description { get; set; }
        public int NumberOfMeals { get; set; }
        public List<string> DietaryPreferences { get; set; } = new();
        public decimal Budget { get; set; }
    }
}
