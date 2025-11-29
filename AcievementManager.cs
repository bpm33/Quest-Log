/*
Benjamin Mather
20251129
3.8 Course Project
Class Implementation

Static class that orchestrates the logic for achievement checking and logging
*/
namespace GoalTrackingApp
{
    public static class AchievementManager
    {
        //class will interact with the database to load AchievementTemplateModlels and save AchievementLogModels
        public static void CheckAndUnlock(Goal goal, ProgressEntry progressEntry)
        {
            //load all templates placeholder logc
            List<AchievementTemplateModel> templates = GetAchievementTemplates();

            foreach (var template in templates)
            {
                //simplified until it can check actual rules
                if (EvaluateCondition(template.UnlockCondition, goal))
                {
                    //log the achievement in the database placeholder logic
                    LogAchievement(template, goal.GoalID);
                }
            }
        }

        //place holder method to simulate retrieving all achievement templates
        private static List<AchievementTemplateModel> GetAchievementTemplates()
        {
            //example templates for testing purposes
            var firstLog = new AchievementTemplateModel(
                name: "First Step",
                description: "Log first progress entry",
                unlockCondition: "ProgressEntries.Count == 1",
                isRepeatable: false);
            firstLog.AchievementID = 1; //simulate database ID

            var threeDayStreak = new AchievementTemplateModel(
                name: "7 Day Streak",
                description: "Maintain a 7-day streak on a time-based goal.",
                unlockCondition: "CurrentStreak >= 7",
                isRepeatable: false);
            threeDayStreak.AchievementID = 2; //simulate database ID

            return new List<AchievementTemplateModel> { firstLog, threeDayStreak };
        }

        //place holder method for rule evaluation
        private static bool EvaluateCondition(string condition, Goal goal)
        {
            //simplified logic for testing purposes
            return condition == "ProgressEntries.Count == 1" && goal.ProgressEntries.Count == 1;
        }
        private static void LogAchievement(AchievementTemplateModel template, int goalID)
        {
            //create the log before being saved to the database
            var log = new AchievementLogModel(goalID, template.AchievementID);
            Console.WriteLine($"\n*** ACHIEVEMENT UNLOCKED: {template.Name} - {template.Description} ***"); 
            Console.WriteLine($"Saved Achievement Log in Database [simulated]: {log.AchievementLogID} (Goal {log.GoalID})");
        }
    }
}