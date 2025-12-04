/*
Benjamin Mather
20251129
3.8 Course Project
Class Implementation

Data Model representing a single instance of an achievement being earned.
*/
namespace GoalTrackingApp
{
    public class AchievementLogModel
    {
        //properties
        public int AchievementLogID { get; set; } //primary key set by database
        public int AchievementID { get; set; } //foreign key to the AchievementTemplateModel definition
        public int GoalID { get; set; } //foreign key set by database for the goal that earned it
        public DateTime DateEarned { get; set; }

        //constructor
        public AchievementLogModel(int goalID, int achievementID)
        {
            this.DateEarned = DateTime.Now;
            this.GoalID = goalID;
            this.AchievementID = achievementID;
        }

        //parameterless constructor for data retrieval
        public AchievementLogModel() { }

        //methods
        public override string ToString()
        {
            return $"--- Achievement Log Details ---\n" +
                //$"Achievement Log ID: {AchievementLogID}\n" +
                $"Achievement ID: {AchievementID}\n" +
                $"Goal ID: {GoalID}\n" +
                $"Date Earned: {DateEarned}";
        }
    }
}