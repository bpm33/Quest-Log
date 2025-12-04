/*
Benjamin Mather
20251129
3.8 Course Project
Class Implementation

Main application class
*/
using System.Data.SQLite;

namespace GoalTrackingApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("\n--- Goal Tracking Application End-to-End Test ---\n");

            // --- 1. SETUP DATABASE & REPOSITORY ---
            string dbPath = "Data Source=GoalTrackingDB.sqlite;Version=3;";
            
            // Note: SQLiteConnection and GoalRepository must be disposed of (or left to run for the app duration)
            using (SQLiteConnection connection = new SQLiteConnection(dbPath))
            {
                try
                {
                    connection.Open();
                    GoalRepository repository = new GoalRepository(connection);

                    // Create the database tables if they don't exist
                    repository.CreateSchema();

                    // --- 2. INITIALIZE THE ACHIEVEMENT SYSTEM ---
                    Console.WriteLine("\n--- Initializing Achievement Manager ---");
                    AchievementManager.Initialize(repository);
                    
                    // Insert an achievement template to test the system, admin only
                    AchievementManager.InsertAchievementTemplate(new AchievementTemplateModel(
                        "First 15",
                        "Log 15 total miles toward any Quantitative Goal.",
                        "CurrentValue >= 15",
                        false
                    ));
                    
                    Console.WriteLine("\n--- Starting Execution (CRUD) ---");

                    // --- 3. EXECUTION: Quantitative Goal Test ---
                    QuantitativeGoal runningGoal = new QuantitativeGoal(
                        title: "Run 50 Miles",
                        description: "Complete 50 miles of running by the end of the year.",
                        startDate: new DateTime(2025, 11, 29),
                        endDate: new DateTime(2025, 12, 31),
                        targetValue: 50.0M,
                        unitOfMeasure: "Miles");

                    // A. ADD Goal (The repository assigns the GoalID)
                    int runningGoalId = repository.AddGoal(runningGoal);
                    runningGoal.GoalID = runningGoalId;
                    
                    // B. ADD Progress Entry 1 (5.0M)
                    ProgressEntry entry1 = new ProgressEntry(valueLogged: 5.0M, notes: "Jogged 5 miles today.");
                    Console.WriteLine($"\n[2] Logging Progress Entry 1 ({entry1.ValueLogged} {runningGoal.UnitOfMeasure})...");
                    // This call will ADD the progress entry and trigger AchievementManager.CheckAndUnlock()
                    repository.AddProgressEntry(runningGoal.GoalID, entry1);

                    // C. ADD Progress Entry 2 (10.0M)
                    ProgressEntry entry2 = new ProgressEntry(valueLogged: 10.0M, notes: "Long run today.");
                    Console.WriteLine($"\n[3] Logging Progress Entry 2 ({entry2.ValueLogged} {runningGoal.UnitOfMeasure})...");
                    // This call will trigger AchievementManager.CheckAndUnlock(), which should unlock "First 15"
                    repository.AddProgressEntry(runningGoal.GoalID, entry2);
                    
                    // D. VERIFY Goal Status
                    // Re-fetch the goal from the database to ensure hydration and progress calculation work
                    Goal? fetchedGoal = repository.GetGoalById(runningGoal.GoalID); 
                    
                    Console.WriteLine("\n[4] Quantitative Goal Status (Fetched from DB):");
                    Console.WriteLine(fetchedGoal?.ToString() ?? "Goal not found.");

                    // --- 4. EXECUTION: Time-Based Goal Test ---
                    
                    TimeBasedGoal meditationGoal = new TimeBasedGoal(
                        title: "Daily Meditation",
                        description: "Meditate once every day.",
                        startDate: DateTime.Today.AddDays(-3), //start 3 days ago for a streak test
                        endDate: DateTime.Today.AddMonths(1),
                        requiredFrequency: FrequencyUnit.Daily);
                        
                    // E. ADD Goal (The repository assigns the GoalID and saves the goal)
                    int meditationGoalId = repository.AddGoal(meditationGoal);
                    meditationGoal.GoalID = meditationGoalId;

                    Console.WriteLine($"\n{new string('-', 50)}");
                    Console.WriteLine("\n[5] Time-Based Goal Created and Saved to DB. ID: {meditationGoal.GoalID}");

                    Console.WriteLine("\n[6] Simulating Daily Progress Logs to achieve a 3-Day Streak:");

                    // F. ADD Progress Entries using the Repository
                    ProgressEntry tEntry1 = new ProgressEntry(1M) { DateLogged = DateTime.Today.AddDays(-3), Notes = "Day 1 Meditation" };
                    Console.WriteLine($"  > Logging {meditationGoal.RequiredFrequency} entry for {tEntry1.DateLogged.ToShortDateString()} via Repository...");
                    repository.AddProgressEntry(meditationGoal.GoalID, tEntry1); 

                    ProgressEntry tEntry2 = new ProgressEntry(1M) { DateLogged = DateTime.Today.AddDays(-2), Notes = "Day 2 Meditation" };
                    Console.WriteLine($"  > Logging {meditationGoal.RequiredFrequency} entry for {tEntry2.DateLogged.ToShortDateString()} via Repository...");
                    repository.AddProgressEntry(meditationGoal.GoalID, tEntry2);

                    ProgressEntry tEntry3 = new ProgressEntry(1M) { DateLogged = DateTime.Today.AddDays(-1), Notes = "Day 3 Meditation" };
                    Console.WriteLine($"  > Logging {meditationGoal.RequiredFrequency} entry for {tEntry3.DateLogged.ToShortDateString()} via Repository...");
                    repository.AddProgressEntry(meditationGoal.GoalID, tEntry3);

                    // G. VERIFY Goal Status
                    // Re-fetch the goal to calculate and display the current streak from the persisted data
                    Goal? fetchedMeditationGoal = repository.GetGoalById(meditationGoal.GoalID); 

                    Console.WriteLine("\n[7] Time-Based Goal Status (Fetched from DB):");
                    Console.WriteLine(fetchedMeditationGoal?.ToString() ?? "Goal not found.");
                    
                    Console.WriteLine("\n--- TEST COMPLETE ---");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nFATAL ERROR: {ex.Message}");
                }
            }
        }
    }
}