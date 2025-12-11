/*
Benjamin Mather
Quest Log
The Goal Tracking App

Main application class
*/
using System.Data.SQLite;

namespace GoalTrackingApp
{
    class Program
    {
        private static GoalRepository _repository = null!;

        static void Main(string[] args)
        {
            Console.WriteLine("\n--- Welcome to Quest Log! The Goal Tracking Application ---\n");

            // --- 1. SETUP DATABASE & REPOSITORY ---
            // Build an absolute path to the database file in the same directory as the executable.
            // This prevents confusion about where the file is created.
            string dbFileName = "GoalTrackingDB.sqlite";
            string dbFilePath = Path.Combine(AppContext.BaseDirectory, dbFileName);
            string dbPath = $"Data Source={dbFilePath};Version=3;";
            Console.WriteLine($"Database file located at: {dbFilePath}\n");
            
            using (SQLiteConnection connection = new SQLiteConnection(dbPath))
            {
                try
                {
                    connection.Open();
                    _repository = new GoalRepository(connection);

                    // Create the database tables if they don't exist
                    _repository.CreateSchema();

                    // --- 2. INITIALIZE THE ACHIEVEMENT SYSTEM ---
                    AchievementManager.Initialize(_repository);
                    
                    // Seed achievement templates if they don't exist
                    AchievementManager.SeedInitialTemplates();

                    // --- 3. START THE MAIN APPLICATION LOOP ---
                    RunMainMenu();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nFATAL ERROR: An unexpected error occurred. Details: {ex.Message}");
                }
            }
            
            Console.WriteLine("\n--- Thank you for using Quest Log! ---");
        }

        private static void RunMainMenu()
        {
            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("\n--- Main Menu ---");
                Console.WriteLine("1. View All Goals");
                Console.WriteLine("2. Add a New Goal");
                Console.WriteLine("3. Log Progress for a Goal");
                Console.WriteLine("4. View Goal Details");
                Console.WriteLine("5. Delete a Goal");
                Console.WriteLine("6. View Achievements");
                Console.WriteLine("7. Edit a Goal");
                Console.WriteLine("9. Reset Database (for testing)");
                Console.WriteLine("0. Exit");

                int? input = ConsoleHelper.GetInt("Please select an option: ");
                if (input == null)
                {
                    // User pressed enter on main menu, just show it again.
                    continue;
                }

                switch (input.Value)
                {
                    case 1:
                        ViewAllGoals();
                        break;
                    case 2:
                        AddNewGoal();
                        break;
                    case 3:
                        LogProgress();
                        break;
                    case 4:
                        ViewGoalDetails();
                        break;
                    case 5:
                        DeleteGoal();
                        break;
                    case 6:
                        ViewAchievements();
                        break;
                    case 7:
                        EditGoal();
                        break;
                    case 9:
                        ResetDatabase();
                        break;
                    case 0:
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }
            }
        }

        private static void ViewAllGoals()
        {
            Console.WriteLine("\n--- All Goals ---");
            var goals = _repository.GetAllGoals();
            if (!goals.Any())
            {
                Console.WriteLine("No goals found. Try adding one!");
                return;
            }

            foreach (var goal in goals)
            {
                Console.WriteLine(goal.GenerateSummaryReport(goal.GoalID));
            }
        }

        private static void AddNewGoal()
        {
            Console.WriteLine("\n--- Add a New Goal ---");
            Console.WriteLine("Select Goal Type:");
            Console.WriteLine("1. Quantitative (e.g., Run 50 miles)");
            Console.WriteLine("2. Time-Based (e.g., Meditate daily)");
            int? typeChoice = ConsoleHelper.GetInt("Enter choice (or press Enter to cancel): ", 1, 2);
            if (typeChoice == null)
            {
                Console.WriteLine("Action cancelled.");
                return;
            }

            // Common properties
            string title = ConsoleHelper.GetString("Enter Title: ");
            string description = ConsoleHelper.GetString("Enter Description: ", allowEmpty: true);
            DateTime startDate = ConsoleHelper.GetDate("Enter Start Date (yyyy-mm-dd): ");
            DateTime endDate = ConsoleHelper.GetDate("Enter End Date (yyyy-mm-dd): ");

            Goal? newGoal = null;
            if (typeChoice.Value == 1)
            {
                // Quantitative Goal
                decimal targetValue = ConsoleHelper.GetDecimal("Enter Target Value (must be > 0): ", 0);
                string unit = ConsoleHelper.GetString("Enter Unit of Measure (e.g., Miles): ");
                
                newGoal = new QuantitativeGoal(title, description, startDate, endDate, targetValue, unit);
            }
            else if (typeChoice.Value == 2)
            {
                // Time-Based Goal
                Console.WriteLine("Select Frequency: (1) Daily, (2) Weekly, (3) Monthly");
                int? freqChoiceNullable = ConsoleHelper.GetInt("Enter choice (or press Enter to cancel): ", 1, 3);
                if (freqChoiceNullable == null)
                {
                    Console.WriteLine("Action cancelled.");
                    return;
                }
                int freqChoice = freqChoiceNullable.Value;
                FrequencyUnit frequency = freqChoice switch
                {
                    2 => FrequencyUnit.Weekly,
                    3 => FrequencyUnit.Monthly,
                    _ => FrequencyUnit.Daily
                };

                newGoal = new TimeBasedGoal(title, description, startDate, endDate, frequency);
            }

            if (newGoal != null)
            {
                _repository.AddGoal(newGoal);
            }
        }

        private static void LogProgress()
        {
            Console.WriteLine("\n--- Log Progress ---");
            var goals = _repository.GetAllGoals();
            if (!goals.Any())
            {
                Console.WriteLine("No goals exist to log progress against. Please add a goal first.");
                return;
            }
            ViewAllGoals();
            
            int? goalId = ConsoleHelper.GetInt("\nEnter the Goal ID to log progress for (or press Enter to cancel): ");
            if (goalId == null)
            {
                Console.WriteLine("Action cancelled.");
                return;
            }

            var goal = _repository.GetGoalById(goalId.Value);
            if (goal == null)
            {
                Console.WriteLine($"Goal with ID {goalId.Value} not found.");
                return;
            }

            DateTime? entryDate = null;
            if (goal is TimeBasedGoal)
            {
                entryDate = ConsoleHelper.GetOptionalDate("Enter the date for this log (yyyy-mm-dd, or press Enter for today): ");
            }

            decimal valueLogged = ConsoleHelper.GetDecimal("Enter Value Logged (e.g., 5.5 for miles, 1 for a daily task): ");
            string notes = ConsoleHelper.GetString("Enter Notes (optional): ", allowEmpty: true);
            
            var entry = new ProgressEntry(valueLogged, notes, entryDate);
            _repository.AddProgressEntry(goalId.Value, entry);
        }

        private static void ViewGoalDetails()
        {
            Console.WriteLine("\n--- View Goal Details ---");
            var goals = _repository.GetAllGoals();
            if (!goals.Any())
            {
                Console.WriteLine("No goals found.");
                return;
            }
            ViewAllGoals();
            
            int? goalId = ConsoleHelper.GetInt("\nEnter the Goal ID to view details for (or press Enter to cancel): ");
            if (goalId == null)
            {
                Console.WriteLine("Action cancelled.");
                return;
            }

            var goal = _repository.GetGoalById(goalId.Value);
            if (goal == null)
            {
                Console.WriteLine($"Goal with ID {goalId.Value} not found.");
                return;
            }
            
            Console.WriteLine(goal.ToString());
        }

        private static void DeleteGoal()
        {
            Console.WriteLine("\n--- Delete a Goal ---");
            var goals = _repository.GetAllGoals();
            if (!goals.Any())
            {
                Console.WriteLine("No goals found to delete.");
                return;
            }
            ViewAllGoals();

            int? goalId = ConsoleHelper.GetInt("\nEnter the Goal ID to DELETE (or press Enter to cancel): ");
            if (goalId == null)
            {
                Console.WriteLine("Deletion cancelled.");
                return;
            }

            // Add a confirmation step
            if (ConsoleHelper.GetConfirmation($"Are you sure you want to permanently delete Goal ID {goalId.Value}? (y/n): "))
            {
                _repository.DeleteGoal(goalId.Value);
            }
            else
            {
                Console.WriteLine("Deletion cancelled.");
            }
        }

        private static void EditGoal()
        {
            Console.WriteLine("\n--- Edit a Goal ---");
            var goals = _repository.GetAllGoals();
            if (!goals.Any())
            {
                Console.WriteLine("No goals found to edit.");
                return;
            }
            ViewAllGoals();

            int? goalId = ConsoleHelper.GetInt("\nEnter the Goal ID to edit (or press Enter to cancel): ");
            if (goalId == null)
            {
                Console.WriteLine("Edit cancelled.");
                return;
            }

            var goalToEdit = _repository.GetGoalById(goalId.Value);
            if (goalToEdit == null)
            {
                Console.WriteLine($"Goal with ID {goalId.Value} not found.");
                return;
            }

            Console.WriteLine("\nEditing Goal: " + goalToEdit.Title);

            bool doneEditing = false;
            while (!doneEditing)
            {
                Console.WriteLine("\nWhat would you like to edit?");
                Console.WriteLine("1. Title");
                Console.WriteLine("2. Description");
                Console.WriteLine("3. Start Date");
                Console.WriteLine("4. End Date");

                if (goalToEdit is QuantitativeGoal)
                {
                    Console.WriteLine("5. Target Value");
                    Console.WriteLine("6. Unit of Measure");
                }
                else if (goalToEdit is TimeBasedGoal)
                {
                    Console.WriteLine("5. Required Frequency");
                }
                Console.WriteLine("0. Finish Editing and Save");

                int? choice = ConsoleHelper.GetInt("Enter choice: ");
                if (choice == null) continue;

                switch (choice.Value)
                {
                    case 1:
                        goalToEdit.Title = ConsoleHelper.GetString($"Enter new Title (current: {goalToEdit.Title}): ");
                        break;
                    case 2:
                        goalToEdit.Description = ConsoleHelper.GetString($"Enter new Description (current: {goalToEdit.Description}): ", allowEmpty: true);
                        break;
                    case 3:
                        goalToEdit.StartDate = ConsoleHelper.GetDate($"Enter new Start Date (current: {goalToEdit.StartDate.ToShortDateString()}): ");
                        break;
                    case 4:
                        goalToEdit.EndDate = ConsoleHelper.GetDate($"Enter new End Date (current: {goalToEdit.EndDate.ToShortDateString()}): ");
                        break;
                    case 5:
                        if (goalToEdit is QuantitativeGoal qGoal)
                        {
                            qGoal.TargetValue = ConsoleHelper.GetDecimal($"Enter new Target Value (current: {qGoal.TargetValue}, must be > 0): ", 0);
                        }
                        else if (goalToEdit is TimeBasedGoal tGoal)
                        {
                            Console.WriteLine($"Select new Frequency (current: {tGoal.RequiredFrequency}): (1) Daily, (2) Weekly, (3) Monthly");
                            int? freqChoice = ConsoleHelper.GetInt("Enter choice: ", 1, 3);
                            if (freqChoice.HasValue) { tGoal.RequiredFrequency = (FrequencyUnit)(freqChoice.Value - 1); }
                        }
                        break;
                    case 6:
                        if (goalToEdit is QuantitativeGoal qGoalUnit)
                        {
                            qGoalUnit.UnitOfMeasure = ConsoleHelper.GetString($"Enter new Unit of Measure (current: {qGoalUnit.UnitOfMeasure}): ");
                        }
                        break;
                    case 0:
                        doneEditing = true;
                        break;
                    default: Console.WriteLine("Invalid option."); break;
                }
            }
            // Recalculate progress and status before saving, as properties like TargetValue may have changed.
            goalToEdit.CalculateProgress();
            
            _repository.UpdateGoal(goalToEdit);
        }

        private static void ViewAchievements()
        {
            Console.WriteLine("\n--- Achievement Status ---");

            var (unlocked, locked) = AchievementManager.GetAchievementStatus();

            Console.WriteLine("\n--- Unlocked Achievements ---");
            if (unlocked.Any())
            {
                foreach (var achievement in unlocked)
                {
                    Console.WriteLine($"[X] {achievement.Name}: {achievement.Description}");
                }
            }
            else
            {
                Console.WriteLine("No achievements unlocked yet. Keep working on your goals!");
            }

            Console.WriteLine("\n--- Locked Achievements ---");
            if (locked.Any())
            {
                foreach (var achievement in locked)
                {
                    Console.WriteLine($"[ ] {achievement.Name}: {achievement.Description}");
                }
            }
            else
            {
                Console.WriteLine("Congratulations! You have unlocked all achievements!");
            }
        }

        private static void ResetDatabase()
        {
            Console.WriteLine("\n--- Reset Database ---");
            if (ConsoleHelper.GetConfirmation("WARNING: This will delete all goals, progress, and achievements. Are you sure? (y/n): "))
            {
                _repository.ResetDatabaseForTesting();
                // Re-initialize the achievement manager to clear its caches
                AchievementManager.Initialize(_repository); // This reloads the caches
                // Re-seed the achievement templates into the fresh database
                AchievementManager.SeedInitialTemplates();
            }
            else
            {
                Console.WriteLine("Database reset cancelled.");
            }
        }
    }
}