/*
Benjamin Mather
20251129
3.8 Course Project
Class Implementation

Main application class
*/
namespace GoalTrackingApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("\n--- Goal Tracking Application Test ---\n");

            //instantiate a quantitative goal with simulated ID
            QuantitativeGoal runningGoal = new QuantitativeGoal(
                title: "Run 50 Miles",
                description: "Complete 50 miles of running by the end of the year.",
                startDate: new DateTime(2025, 11, 29),
                endDate: new DateTime(2025, 12, 31),
                targetValue: 50.0M,
                unitOfMeasure: "Miles");
            runningGoal.GoalID = 1;

            Console.WriteLine($"[1] Quantitative Goal Created:");
            Console.WriteLine(runningGoal.ToString());

            //log first progress entry
            ProgressEntry entry1 = new ProgressEntry(valueLogged: 5.0M, notes: "Jogged 5 miles today.");
            Console.WriteLine($"\n[2] Logging Progress Entry 1 ({entry1.ValueLogged} {runningGoal.UnitOfMeasure})...");
            runningGoal.LogProgress(entry1); //trigger CalculateProgress and AchievementManager check

            //log second progress entry
            ProgressEntry entry2 = new ProgressEntry(valueLogged: 10.0M, notes: "Long run today.");
            Console.WriteLine($"\n[3] Logging Progress Entry 2 ({entry2.ValueLogged} {runningGoal.UnitOfMeasure})...");
            runningGoal.LogProgress(entry2);
            
            //show the result after logging
            Console.WriteLine("\n[4] Quantitative Goal Status:");
            Console.WriteLine(runningGoal.ToString()); 
            
            //interface check
            Console.WriteLine("\n[5] Interface Report Check:");
            Console.WriteLine(runningGoal.GenerateSummaryReport(runningGoal.GoalID));

            //instantiate a time-based goal with simulated ID
            TimeBasedGoal meditationGoal = new TimeBasedGoal(
                title: "Daily Meditation",
                description: "Meditate once every day.",
                startDate: DateTime.Today.AddDays(-3), //start 3 days ago for a streak test
                endDate: DateTime.Today.AddMonths(1),
                requiredFrequency: FrequencyUnit.Daily);
            meditationGoal.GoalID = 2;
            
            Console.WriteLine($"\n{new string('-', 50)}");
            Console.WriteLine("\n[6] Time-Based Goal Created:");

            Console.WriteLine("\n[7] Simulating Daily Progress Logs to achieve a 3-Day Streak:");

            ProgressEntry tEntry1 = new ProgressEntry(1M) { DateLogged = DateTime.Today.AddDays(-3), Notes = "Day 1 Meditation" };
            Console.WriteLine($"  > Logging {meditationGoal.RequiredFrequency} entry for {tEntry1.DateLogged.ToShortDateString()}.");
            meditationGoal.LogProgress(tEntry1); 
            
            ProgressEntry tEntry2 = new ProgressEntry(1M) { DateLogged = DateTime.Today.AddDays(-2), Notes = "Day 2 Meditation" };
            Console.WriteLine($"  > Logging {meditationGoal.RequiredFrequency} entry for {tEntry2.DateLogged.ToShortDateString()}.");
            meditationGoal.LogProgress(tEntry2);
            
            ProgressEntry tEntry3 = new ProgressEntry(1M) { DateLogged = DateTime.Today.AddDays(-1), Notes = "Day 3 Meditation" };
            Console.WriteLine($"  > Logging {meditationGoal.RequiredFrequency} entry for {tEntry3.DateLogged.ToShortDateString()}.");
            meditationGoal.LogProgress(tEntry3);
            
            //show the result
            Console.WriteLine("\n[8] Time-Based Goal Status:");
            Console.WriteLine(meditationGoal.ToString());
            
            Console.WriteLine("\n--- TEST COMPLETE ---");
        }
    }
}