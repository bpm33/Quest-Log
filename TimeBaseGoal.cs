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
            if (!ProgressEntries.Any())
            {
                return 0;
            }

            var loggedDates = ProgressEntries
                .Select(e => e.DateLogged.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();

            int streak = 1;
            
            switch (RequiredFrequency)
            {
                case FrequencyUnit.Daily:
                {
                    var lastDate = loggedDates[0];
                    for (int i = 1; i < loggedDates.Count; i++)
                    {
                        if (loggedDates[i] == lastDate.AddDays(-1))
                        {
                            streak++;
                            lastDate = loggedDates[i];
                        }
                        else
                        {
                            break; // Streak is broken
                        }
                    }
                    break;
                }
                case FrequencyUnit.Weekly:
                {
                    var loggedWeeks = loggedDates.Select(d => d.AddDays(-(int)d.DayOfWeek)).Distinct().ToList();
                    var lastWeek = loggedWeeks[0];
                    for (int i = 1; i < loggedWeeks.Count; i++)
                    {
                        if (loggedWeeks[i] == lastWeek.AddDays(-7))
                        {
                            streak++;
                            lastWeek = loggedWeeks[i];
                        }
                        else
                        {
                            break; // Streak is broken
                        }
                    }
                    break;
                }
                case FrequencyUnit.Monthly:
                {
                    var loggedMonths = loggedDates.Select(d => new DateTime(d.Year, d.Month, 1)).Distinct().ToList();
                    var lastMonth = loggedMonths[0];
                    for (int i = 1; i < loggedMonths.Count; i++)
                    {
                        if (loggedMonths[i] == lastMonth.AddMonths(-1))
                        {
                            streak++;
                            lastMonth = loggedMonths[i];
                        }
                        else
                        {
                            break; // Streak is broken
                        }
                    }
                    break;
                }
            }
            return streak;
        }

        private string GetFrequencyUnitString()
        {
            return RequiredFrequency switch
            {
                FrequencyUnit.Daily => "Day",
                FrequencyUnit.Weekly => "Week",
                FrequencyUnit.Monthly => "Month",
                _ => ""
            };
        }

        //methods
        public override string CalculateProgress()
        {
            CurrentStreak = CalculateCurrentStreak();

            // If the goal's end date has passed, mark it as complete.
            if (DateTime.Today > EndDate && Status == GoalStatus.InProgress)
            {
                Status = GoalStatus.Complete;
            }
            return $"{CurrentStreak} {GetFrequencyUnitString()} Streak! (Required Frequency: {RequiredFrequency})";
        }
    }
}