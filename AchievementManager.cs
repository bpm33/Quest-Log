/*
Benjamin Mather
20251129
3.8 Course Project
Class Implementation

Static class that orchestrates the logic for achievement checking and logging
*/
using System.Reflection;
using System.Collections;

namespace GoalTrackingApp
{
    // The static class ensures a single, globally accessible set of logic
    public static class AchievementManager
    {
        // Data Access and Caching (Manager State)
        private static GoalRepository _repository = null!; 
        private static List<AchievementTemplateModel> _templatesCache = null!;
        
        // Use a HashSet to efficiently check if an AchievementID has been earned by a GoalID
        private static HashSet<(int GoalID, int AchievementID)> _earnedAchievementsCache = null!;

        // Initializes the AchievementManager by providing the data repository and loading all necessary data into memory.
        public static void Initialize(GoalRepository repository)
        {
            _repository = repository;
            // Load static achievement rules once on startup
            _templatesCache = _repository.GetAllAchievementTemplates(); 
            
            // Load all existing earned logs to populate the cache
            LoadEarnedAchievementsCache(); 
            
            Console.WriteLine($"\nAchievement Manager Initialized. Loaded {_templatesCache.Count} templates and {_earnedAchievementsCache.Count} earned achievements.");
        }
        private static void LoadEarnedAchievementsCache()
        {
            _earnedAchievementsCache = new HashSet<(int GoalID, int AchievementID)>();
            var allLogs = _repository.GetAllAchievementLogs();

            foreach (var log in allLogs)
            {
                // Populate the cache with existing earned achievements
                _earnedAchievementsCache.Add((log.GoalID, log.AchievementID));
            }
        }

        // Checks a Goal object against all templates for unlocked conditions and logs any new achievement. Should be called after adding progress or updating a goal.
        public static void CheckAndUnlock(Goal goal, ProgressEntry? progressEntry = null)
        {
            // Ensure goal has a valid ID (it must be saved to the database first)
            if (goal.GoalID <= 0) return; 

            foreach (var template in _templatesCache)
            {
                // Check for extensibility: Skip if already earned and template is not repeatable.
                if (!template.IsRepeatable && _earnedAchievementsCache.Contains((goal.GoalID, template.AchievementID)))
                {
                    continue;
                }

                if (EvaluateCondition(template.UnlockCondition, goal))
                {
                    // ACHIEVEMENT UNLOCKED!
                    
                    // Create Log Model
                    var newLog = new AchievementLogModel(goal.GoalID, template.AchievementID);
                    
                    // Log the achievement in the database using the repository
                    _repository.AddAchievementLogEntry(newLog); 
                    
                    // Update the in-memory cache to prevent immediate re-logging
                    _earnedAchievementsCache.Add((goal.GoalID, template.AchievementID)); 
                    
                    Console.WriteLine($"\n*** ACHIEVEMENT UNLOCKED: {template.Name} - {template.Description} ***"); 
                }
            }
        }

        // Attempts to evaluate a simple condition string against the Goal object's properties using Reflection.
        private static bool EvaluateCondition(string condition, Goal goal)
        {
            try
            {
                // Example Rule: "CurrentValue >= 100" or "ProgressEntries.Count == 1"
                string[] parts = condition.Split(' ');
                if (parts.Length != 3) return false;

                string propertyName = parts[0];
                string op = parts[1];
                string valueString = parts[2];

                // Handle special "Global" properties that are not on the Goal object itself.
                // This allows for achievements based on overall user progress.
                if (propertyName == "GlobalCompletedGoalCount")
                {
                    // This check is only relevant when a goal's status has just changed to Complete.
                    if (goal.Status != GoalStatus.Complete) return false;

                    int completedCount = _repository.GetCompletedGoalCount();
                    if (int.TryParse(valueString, out int targetCount))
                    {
                        return op switch
                        {
                            ">=" => completedCount >= targetCount,
                            "==" => completedCount == targetCount,
                            _ => false,
                        };
                    }
                    return false;
                }

                // Handle list.Count properties, e.g., "ProgressEntries.Count"
                if (propertyName.EndsWith(".Count"))
                {
                    string listPropertyName = propertyName.Replace(".Count", "");
                    // Check derived class first, then base class
                    PropertyInfo? listPropInfo = goal.GetType().GetProperty(listPropertyName) ?? typeof(Goal).GetProperty(listPropertyName);

                    if (listPropInfo != null && typeof(IList).IsAssignableFrom(listPropInfo.PropertyType))
                    {
                        var list = (IList?)listPropInfo.GetValue(goal);
                        int listCount = list?.Count ?? 0;

                        if (int.TryParse(valueString, out int targetInt))
                        {
                            return op switch
                            {
                                ">=" => listCount >= targetInt,
                                "==" => listCount == targetInt,
                                _ => false,
                            };
                        }
                    }
                    return false; // Property not found or not a list
                }
                
                // Handle direct properties like CurrentValue or CurrentStreak
                // Check derived class first, then base class
                PropertyInfo? propInfo = goal.GetType().GetProperty(propertyName) ?? typeof(Goal).GetProperty(propertyName);

                // Declare goalValue as nullable object to prevent null dereference
                if (propInfo == null) return false;
                object? goalValue = propInfo.GetValue(goal);
                
                // Defensive check to prevent further null dereference
                if (goalValue == null) return false;

                if (goalValue is decimal goalDecimal)
                {
                    // Use InvariantCulture to handle '.' as decimal separator consistently
                    if (decimal.TryParse(valueString, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal targetDecimal))
                    {
                        return op switch
                        {
                            ">=" => goalDecimal >= targetDecimal,
                            "==" => goalDecimal == targetDecimal,
                            _ => false,
                        };
                    }
                }
                
                if (goalValue is int goalInt)
                {
                    if (int.TryParse(valueString, out int targetInt))
                    {
                        return op switch
                        {
                            ">=" => goalInt >= targetInt,
                            "==" => goalInt == targetInt,
                            _ => false,
                        };
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\t[ERROR] Failed to evaluate achievement condition: {condition}. Details: {ex.Message}");
                return false;
            }
        }

        // Adds a new Achievement Template definition to the database and loads it into the runtime cache, for admin only
        public static void InsertAchievementTemplate(AchievementTemplateModel template)
        {
            if (_repository == null)
            {
                throw new InvalidOperationException("AchievementManager must be initialized before inserting templates.");
            }
            
            // Save the template to the database using the repository. 
            int templateId = _repository.AddAchievementTemplate(template);
            template.AchievementID = templateId; // Update the model with the new ID

            // Add the newly saved template to the runtime cache allowing CheckAndUnlock to use it immediately
            _templatesCache.Add(template);

            Console.WriteLine($"[Manager] New Achievement Template '{template.Name}' inserted and loaded. ID: {templateId}");
        }

        // Returns a tuple of lists containing all unlocked and locked achievement templates.
        public static (List<AchievementTemplateModel> Unlocked, List<AchievementTemplateModel> Locked) GetAchievementStatus()
        {
            var unlocked = new List<AchievementTemplateModel>();
            var locked = new List<AchievementTemplateModel>();

            // Create a set of just the unique achievement IDs that have been earned for efficient lookup.
            var earnedAchievementIds = _earnedAchievementsCache.Select(e => e.AchievementID).ToHashSet();

            foreach (var template in _templatesCache)
            {
                if (earnedAchievementIds.Contains(template.AchievementID))
                {
                    unlocked.Add(template);
                }
                else
                {
                    locked.Add(template);
                }
            }

            return (unlocked, locked);
        }

        // Seeds the database with the initial set of achievement templates if they don't already exist.
        public static void SeedInitialTemplates()
        {
            Console.WriteLine("Seeding achievement templates...");
            var templates = _repository.GetAllAchievementTemplates();
            
            // --- Streak / Frequency Achievements ---

            // 1. First Progress Log
            if (!templates.Any(t => t.Name == "Off the Starting Blocks"))
            {
                InsertAchievementTemplate(new AchievementTemplateModel(
                    "Off the Starting Blocks",
                    "Log your very first progress entry for any goal.",
                    "ProgressEntries.Count == 1",
                    false
                ));
            }
            
            // 2. Bronze Streak (3 days)
            if (!templates.Any(t => t.Name == "Getting Consistent"))
            {
                InsertAchievementTemplate(new AchievementTemplateModel(
                    "Getting Consistent",
                    "Achieve a 3-day streak on a Time-Based Goal.",
                    "CurrentStreak >= 3",
                    false
                ));
            }

            // 3. Silver Streak (7 days)
            if (!templates.Any(t => t.Name == "Weekly Warrior"))
            {
                InsertAchievementTemplate(new AchievementTemplateModel(
                    "Weekly Warrior",
                    "Achieve a 7-day streak on a Time-Based Goal.",
                    "CurrentStreak >= 7",
                    false
                ));
            }

            // 4. Gold Streak (30 days)
            if (!templates.Any(t => t.Name == "Habit Master"))
            {
                InsertAchievementTemplate(new AchievementTemplateModel(
                    "Habit Master",
                    "Achieve a 30-day streak on a Time-Based Goal.",
                    "CurrentStreak >= 30",
                    false
                ));
            }

            // --- Goal Completion Achievements ---

            // 5. First Goal Completed
            if (!templates.Any(t => t.Name == "One Down!"))
            {
                InsertAchievementTemplate(new AchievementTemplateModel(
                    "One Down!",
                    "Complete your first goal.",
                    "GlobalCompletedGoalCount == 1",
                    false
                ));
            }

            // 6. Bronze Completion (5 goals)
            if (!templates.Any(t => t.Name == "Five-Star Finisher"))
            {
                InsertAchievementTemplate(new AchievementTemplateModel(
                    "Five-Star Finisher",
                    "Complete 5 goals.",
                    "GlobalCompletedGoalCount >= 5",
                    false
                ));
            }

            // 7. Silver Completion (10 goals)
            if (!templates.Any(t => t.Name == "Goal Getter"))
            {
                InsertAchievementTemplate(new AchievementTemplateModel(
                    "Goal Getter",
                    "Complete 10 goals.",
                    "GlobalCompletedGoalCount >= 10",
                    false
                ));
            }

            // 8. Gold Completion (20 goals)
            if (!templates.Any(t => t.Name == "Goal Hoarder"))
            {
                InsertAchievementTemplate(new AchievementTemplateModel(
                    "Goal Hoarder",
                    "Complete 20 goals.",
                    "GlobalCompletedGoalCount >= 20",
                    false
                ));
            }
        }
    }
}