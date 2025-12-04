/*
Benjamin Mather
20251129
3.8 Course Project
Class Implementation

Data model representing the static definition of an achievement.
*/
namespace GoalTrackingApp
{
    public class AchievementTemplateModel
    {
        //properties
        public int AchievementID { get; set; } //primary key
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string UnlockCondition { get; set; } = null!; //rules for unlocking
        public bool IsRepeatable { get; set; }

        //constructor
        public AchievementTemplateModel(string name, string description, string unlockCondition, bool isRepeatable)
        {
            this.Name = name;
            this.Description = description;
            this.UnlockCondition = unlockCondition;
            this.IsRepeatable = isRepeatable;
        }

        //parameterless constructor for data retrieval
        public AchievementTemplateModel() { }

        //methods
        public override string ToString()
        {
            return $"--- Achievement Template Details ---\n" +
                //$"Achievement ID: {AchievementID}\n" +
                $"Name: {Name}\n" +
                $"Description: {Description}\n" +
                $"Unlock Condition: {UnlockCondition}\n" +
                $"Is Repeatable: {IsRepeatable}";
        }
    }
}
