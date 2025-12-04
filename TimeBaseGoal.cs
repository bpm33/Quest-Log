/*
Benjamin Mather
20251129
3.8 Course Project
Class Implementation

Represents a goal tracked by frequency and consistency (streak count). 
Inherits from Goal.
*/
namespace GoalTrackingApp
{
    public enum FrequencyUnit
    {
        Daily,
        Weekly,
        Monthly
    }
    public class TimeBasedGoal : Goal
    {
        //properties
        public FrequencyUnit RequiredFrequency { get; set; }
        public int CurrentStreak { get; private set; } = 0;
        
        //constructor
        public TimeBasedGoal(string title, string description, DateTime startDate, DateTime endDate, FrequencyUnit requiredFrequency) : base(title, description, startDate, endDate)
        {
            this.RequiredFrequency = requiredFrequency;
        }

        //parameterless constructor for database loading
        public TimeBasedGoal() : base() {}

        //streak calculation helper method
        private int CalculateCurrentStreak()
        {
            var loggedDates = ProgressEntries
            .Select(e => e.DateLogged.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

            if (loggedDates.Count == 0)
            {
                return 0;
            }
            int streak = 0;
            DateTime checkDate = DateTime.Today;
            if (RequiredFrequency == FrequencyUnit.Daily)
            {
                if (!loggedDates.Contains(checkDate))
                {
                    checkDate = checkDate.AddDays(-1);
                }
                foreach (var date in loggedDates)
                {
                    if (date == checkDate)
                    {
                        streak++;
                        checkDate = checkDate.AddDays(-1);
                    }
                    else if (date < checkDate)
                    {
                        break;
                    }
                }
            }
            return streak;
        }
        //methods
        public override string CalculateProgress()
        {
            CurrentStreak = CalculateCurrentStreak();
            return $"{CurrentStreak} Day Streak! (Required Frequency: {RequiredFrequency})";
        }
    }
}