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
            if (_repository == null)
            {
                _repository = repository;
                // Load static achievement rules once on startup
                _templatesCache = _repository.GetAllAchievementTemplates(); 
                
                // Load all existing earned logs to populate the cache
                LoadEarnedAchievementsCache(); 
                
                Console.WriteLine($"\nAchievement Manager Initialized. Loaded {_templatesCache.Count} templates.");
            }
        }
        private static void LoadEarnedAchievementsCache()
        {
            // Initialize empty for the sake of compiling and moving forward.
            // This is mandatory to prevent re-logging achievements on startup.
            _earnedAchievementsCache = new HashSet<(int GoalID, int AchievementID)>();
            
            /* Example of what the logic would look like:
            var allLogs = _repository.GetAllAchievementLogs(); 
            foreach(var log in allLogs)
            {
                _earnedAchievementsCache.Add((log.GoalID, log.AchievementID));
            }
            */
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

                // Reflection: Get Property Info from the Goal's runtime type
                PropertyInfo? propInfo = goal.GetType().GetProperty(propertyName);
                // Check base class if not found in derived
                if (propInfo == null)
                {
                    propInfo = typeof(Goal).GetProperty(propertyName);
                }
                
                // Add null check for propInfo before using it in the list property check
                if (propInfo == null) return false; 
                
                // Also handle the list count case, like ProgressEntries.Count
                if (propertyName.EndsWith(".Count") && typeof(IList).IsAssignableFrom(propInfo.PropertyType))
                {
                    PropertyInfo? listPropInfo = typeof(Goal).GetProperty(propertyName.Replace(".Count", ""));
                    
                    // Use conditional access (?.) on listPropInfo and cast to nullable IList? to prevent null dereference
                    var list = (IList?)listPropInfo?.GetValue(goal);
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
                
                // Declare goalValue as nullable object to prevent null dereference
                object? goalValue = propInfo.GetValue(goal); 
                
                // Defensive check to prevent further null dereference
                if (goalValue == null) return false;

                if (goalValue is decimal goalDecimal)
                {
                    if (decimal.TryParse(valueString, out decimal targetDecimal))
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
    }
}