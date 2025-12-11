/*
Benjamin Mather
Quest Log
The Goal Tracking App

Repository class implementing the data access layer (CRUD) for all Goal-related tables.
*/
using System.Data.SQLite;

namespace GoalTrackingApp
{
    public class GoalRepository
    {
        // Private constant strings for table names, aiding in code readability and maintenance
        private const string GoalTable = "Goal";
        private const string QuantitativeTable = "QuantitativeGoal";
        private const string TimeBasedTable = "TimeBaseGoal";
        private const string ProgressEntryTable = "ProgressEntry";
        private const string AchievementTemplateTable = "AchievementTemplate";
        private const string AchievementLogTable = "AchievementLog";

        private SQLiteConnection _connection;

        // Constructor requires an active connection instance
        public GoalRepository(SQLiteConnection conn)
        {
            _connection = conn;
        }

        // Creates all necessary tables for the Goal Tracking App database schema.
        public void CreateSchema()
        {
            Console.WriteLine("--- Creating Database Schema ---");
            
            // Goal Table (Base Table)
            string sqlGoal = $@"CREATE TABLE IF NOT EXISTS {GoalTable} (
                 GoalID INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
                ,Title TEXT NOT NULL
                ,Description TEXT
                ,GoalType TEXT NOT NULL
                ,Status INTEGER NOT NULL
                ,StartDate TEXT NOT NULL
                ,EndDate TEXT NOT NULL
            );";
            ExecuteNonQuery(sqlGoal);
            
            // QuantitativeGoal Table (Child Table)
            string sqlQuant = $@"CREATE TABLE IF NOT EXISTS {QuantitativeTable} (
                 GoalID INTEGER PRIMARY KEY NOT NULL
                ,TargetValue REAL NOT NULL
                ,UnitOfMeasure TEXT
                ,FOREIGN KEY(GoalID) REFERENCES {GoalTable}(GoalID) ON DELETE CASCADE
            );";
            ExecuteNonQuery(sqlQuant);

            // TimeBaseGoal Table (Child Table)
            string sqlTimeBase = $@"CREATE TABLE IF NOT EXISTS {TimeBasedTable} (
                 GoalID INTEGER PRIMARY KEY NOT NULL
                ,RequiredFrequency INTEGER NOT NULL
                ,FOREIGN KEY(GoalID) REFERENCES {GoalTable}(GoalID) ON DELETE CASCADE
            );";
            ExecuteNonQuery(sqlTimeBase);

            // ProgressEntry Table (Transactional Ledger)
            string sqlProgress = $@"CREATE TABLE IF NOT EXISTS {ProgressEntryTable} (
                 EntryID INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
                ,GoalID INTEGER NOT NULL
                ,DateLogged TEXT NOT NULL
                ,ValueLogged REAL NOT NULL
                ,Notes TEXT
                ,FOREIGN KEY(GoalID) REFERENCES {GoalTable}(GoalID) ON DELETE CASCADE
            );";
            ExecuteNonQuery(sqlProgress);

            // AchievementTemplate Table (Static Rules)
            string sqlTemplate = $@"CREATE TABLE IF NOT EXISTS {AchievementTemplateTable} (
                 AchievementID INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
                ,Name TEXT NOT NULL
                ,Description TEXT
                ,UnlockCondition TEXT NOT NULL
                ,IsRepeatable INTEGER NOT NULL 
            );";
            ExecuteNonQuery(sqlTemplate);

            // AchievementLog Table (Earned Achievements)
            string sqlLog = $@"CREATE TABLE IF NOT EXISTS {AchievementLogTable} (
                 LogID INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
                ,GoalID INTEGER NOT NULL
                ,AchievementID INTEGER NOT NULL
                ,DateEarned TEXT NOT NULL
                ,FOREIGN KEY(GoalID) REFERENCES {GoalTable}(GoalID) ON DELETE CASCADE
                ,FOREIGN KEY(AchievementID) REFERENCES {AchievementTemplateTable}(AchievementID) ON DELETE CASCADE
            );";
            ExecuteNonQuery(sqlLog);

            Console.WriteLine("--- Schema Creation Complete ---");
        }

        // Private utility method to execute non-query SQL statements safely
        private void ExecuteNonQuery(string sql)
        {
            using (SQLiteCommand cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.ExecuteNonQuery();
            }
        }
        
        //Adds a new Goal object (and its derived type data) to the database. This method handles the two-table insert required for class table inheritance.
        public int AddGoal(Goal goal)
        {
            // Insert into the base Goal table
            string sqlBaseInsert = $@"
                INSERT INTO {GoalTable} (Title, Description, GoalType, Status, StartDate, EndDate)
                VALUES (@Title, @Description, @GoalType, @Status, @StartDate, @EndDate);";

            using (SQLiteCommand cmd = new SQLiteCommand(sqlBaseInsert, _connection))
            {
                // Parameter binding for security and type safety
                cmd.Parameters.AddWithValue("@Title", goal.Title);
                cmd.Parameters.AddWithValue("@Description", goal.Description);
                cmd.Parameters.AddWithValue("@GoalType", goal.GetType().Name); // Stores "QuantitativeGoal" or "TimeBasedGoal"
                cmd.Parameters.AddWithValue("@Status", (int)goal.Status); // Stores the enum's integer value
                cmd.Parameters.AddWithValue("@StartDate", goal.StartDate.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@EndDate", goal.EndDate.ToString("yyyy-MM-dd HH:mm:ss"));

                cmd.ExecuteNonQuery();

                // Retrieve the newly created GoalID (the Primary Key)
                long lastId = _connection.LastInsertRowId;
                goal.GoalID = (int)lastId; // Update the C# object's ID

                // Insert into the specific child table based on the GoalType
                if (goal is QuantitativeGoal quantitativeGoal)
                {
                    InsertQuantitativeGoalData(quantitativeGoal, goal.GoalID);
                }
                else if (goal is TimeBasedGoal timeBasedGoal)
                {
                    InsertTimeBasedGoalData(timeBasedGoal, goal.GoalID);
                }
                else
                {
                    throw new InvalidOperationException("Goal type not supported for database insert.");
                }

                Console.WriteLine($"\nSUCCESS: New Goal '{goal.Title}' added with GoalID: {goal.GoalID}");
                return goal.GoalID;
            }
        }

        // Private helper methods for inserting child data
        private void InsertQuantitativeGoalData(QuantitativeGoal goal, int goalId)
        {
            string sqlQuantInsert = $@"
                INSERT INTO {QuantitativeTable} (GoalID, TargetValue, UnitOfMeasure)
                VALUES (@GoalID, @TargetValue, @UnitOfMeasure);";

            using (SQLiteCommand cmd = new SQLiteCommand(sqlQuantInsert, _connection))
            {
                cmd.Parameters.AddWithValue("@GoalID", goalId);
                cmd.Parameters.AddWithValue("@TargetValue", goal.TargetValue);
                cmd.Parameters.AddWithValue("@UnitOfMeasure", goal.UnitOfMeasure);
                cmd.ExecuteNonQuery();
            }
        }

        private void InsertTimeBasedGoalData(TimeBasedGoal goal, int goalId)
        {
            string sqlTimeBaseInsert = $@"
                INSERT INTO {TimeBasedTable} (GoalID, RequiredFrequency)
                VALUES (@GoalID, @RequiredFrequency);";

            using (SQLiteCommand cmd = new SQLiteCommand(sqlTimeBaseInsert, _connection))
            {
                cmd.Parameters.AddWithValue("@GoalID", goalId);
                cmd.Parameters.AddWithValue("@RequiredFrequency", (int)goal.RequiredFrequency);
                cmd.ExecuteNonQuery();
            }
        }

        // Loads all ProgressEntry records associated with a specific GoalID.
        private List<ProgressEntry> LoadProgressEntries(int goalId)
        {
            string sql = $"SELECT EntryID, DateLogged, ValueLogged, Notes FROM {ProgressEntryTable} WHERE GoalID = @GoalID ORDER BY DateLogged DESC";
            List<ProgressEntry> entries = new List<ProgressEntry>();

            using (SQLiteCommand cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.Parameters.AddWithValue("@GoalID", goalId);
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Instantiate using the new parameterless constructor for hydration
                        ProgressEntry entry = new ProgressEntry(); 
                        
                        // Hydrate the properties from the database
                        entry.EntryID = reader.GetInt32(0);
                        entry.GoalID = goalId; // Set FK
                        entry.DateLogged = DateTime.Parse(reader.GetString(1));
                        
                        // SQLite's REAL type maps to C# double, which we cast to decimal
                        entry.ValueLogged = (decimal)reader.GetDouble(2); 
                        entry.Notes = reader.IsDBNull(3) ? "" : reader.GetString(3);
                        
                        entries.Add(entry);
                    }
                }
            }
            return entries;
        }

        // Loads all ProgressEntry records from the database and groups them by GoalID for efficient lookup.
        private Dictionary<int, List<ProgressEntry>> LoadAllProgressEntriesGroupedByGoal()
        {
            var allEntries = new Dictionary<int, List<ProgressEntry>>();
            string sql = $"SELECT EntryID, GoalID, DateLogged, ValueLogged, Notes FROM {ProgressEntryTable} ORDER BY GoalID, DateLogged DESC";

            using (SQLiteCommand cmd = new SQLiteCommand(sql, _connection))
            {
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int goalId = reader.GetInt32(1);
                        if (!allEntries.ContainsKey(goalId))
                        {
                            allEntries[goalId] = new List<ProgressEntry>();
                        }

                        ProgressEntry entry = new ProgressEntry
                        {
                            EntryID = reader.GetInt32(0),
                            GoalID = goalId,
                            DateLogged = DateTime.Parse(reader.GetString(2)),
                            ValueLogged = (decimal)reader.GetDouble(3),
                            Notes = reader.IsDBNull(4) ? "" : reader.GetString(4)
                        };
                        allEntries[goalId].Add(entry);
                    }
                }
            }
            return allEntries;
        }

        // Reads a list of all goals, reconstituting them as the correct derived type
        public List<Goal> GetAllGoals()
        {
            List<Goal> goals = new List<Goal>();
            
            // Load all progress entries in one go and group them by GoalID to solve the N+1 query problem.
            var allProgressEntries = LoadAllProgressEntriesGroupedByGoal();
            
            // Load all base and derived goal data in a single query using LEFT JOINs.
            string sql = $@"
                SELECT 
                    g.GoalID, g.Title, g.Description, g.GoalType, g.Status, g.StartDate, g.EndDate,
                    q.TargetValue, q.UnitOfMeasure,
                    t.RequiredFrequency
                FROM {GoalTable} g
                LEFT JOIN {QuantitativeTable} q ON g.GoalID = q.GoalID
                LEFT JOIN {TimeBasedTable} t ON g.GoalID = t.GoalID";
            
            using (SQLiteCommand cmd = new SQLiteCommand(sql, _connection))
            {
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int goalId = reader.GetInt32(0);
                        string? goalType = reader.IsDBNull(3) ? null : reader.GetString(3);
                        Goal? goal = null;

                        // Polymorphic Hydration
                        if (goalType == "QuantitativeGoal")
                        {
                            var qGoal = new QuantitativeGoal
                            {
                                // Use IsDBNull checks for safety on LEFT JOIN results
                                TargetValue = reader.IsDBNull(7) ? 0 : (decimal)reader.GetDouble(7),
                                UnitOfMeasure = reader.IsDBNull(8) ? "" : reader.GetString(8)
                            };
                            goal = qGoal;
                        }
                        else if (goalType == "TimeBasedGoal")
                        {
                            var tGoal = new TimeBasedGoal
                            {
                                RequiredFrequency = reader.IsDBNull(9) ? FrequencyUnit.Daily : (FrequencyUnit)reader.GetInt32(9)
                            };
                            goal = tGoal;
                        }
                        
                        if (goal != null)
                        {
                            // Populate common properties
                            goal.GoalID = goalId;
                            goal.Title = reader.GetString(1);
                            goal.Description = reader.GetString(2);
                            goal.Status = (GoalStatus)reader.GetInt32(4);
                            goal.StartDate = DateTime.Parse(reader.GetString(5));
                            goal.EndDate = DateTime.Parse(reader.GetString(6));

                            // Assign the pre-loaded progress entries from the dictionary.
                            if (allProgressEntries.TryGetValue(goalId, out var entries))
                            {
                                goal.ProgressEntries = entries;
                            }

                            goal.CalculateProgress();
                            goals.Add(goal);
                        }
                    }
                }
            }
            Console.WriteLine($"\nSUCCESS: Loaded {goals.Count} goals from the database.");
            return goals;
        }

        // Updates an existing Goal object and its derived type data in the database.
        public void UpdateGoal(Goal goal)
        {
            // Start a transaction for atomicity
            using (var transaction = _connection.BeginTransaction())
            {
                try
                {
                    // Update the base Goal table
                    string sqlBaseUpdate = $@"
                        UPDATE {GoalTable} SET 
                            Title = @Title, 
                            Description = @Description, 
                            Status = @Status, 
                            StartDate = @StartDate, 
                            EndDate = @EndDate
                        WHERE GoalID = @GoalID;";

                    using (SQLiteCommand cmd = new SQLiteCommand(sqlBaseUpdate, _connection))
                    {
                        cmd.Parameters.AddWithValue("@GoalID", goal.GoalID);
                        cmd.Parameters.AddWithValue("@Title", goal.Title);
                        cmd.Parameters.AddWithValue("@Description", goal.Description);
                        cmd.Parameters.AddWithValue("@Status", (int)goal.Status);
                        cmd.Parameters.AddWithValue("@StartDate", goal.StartDate.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@EndDate", goal.EndDate.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.ExecuteNonQuery();
                    }

                    // Update the specific child table based on the GoalType
                    if (goal is QuantitativeGoal quantitativeGoal)
                    {
                        UpdateQuantitativeGoalData(quantitativeGoal);
                    }
                    else if (goal is TimeBasedGoal timeBasedGoal)
                    {
                        UpdateTimeBasedGoalData(timeBasedGoal);
                    }
                    
                    transaction.Commit();
                    Console.WriteLine($"\nSUCCESS: Goal '{goal.Title}' (ID: {goal.GoalID}) updated successfully.");
                    
                    // Trigger Achievement Check after successful update
                    AchievementManager.CheckAndUnlock(goal);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"\nERROR: Failed to update goal. Details: {ex.Message}");
                    throw; // Re-throw to allow application-level handling
                }
            }
        }

        // Private helper methods for updating child data
        private void UpdateQuantitativeGoalData(QuantitativeGoal goal)
        {
            string sqlQuantUpdate = $@"
                UPDATE {QuantitativeTable} SET 
                    TargetValue = @TargetValue, 
                    UnitOfMeasure = @UnitOfMeasure
                WHERE GoalID = @GoalID;";

            using (SQLiteCommand cmd = new SQLiteCommand(sqlQuantUpdate, _connection))
            {
                cmd.Parameters.AddWithValue("@GoalID", goal.GoalID);
                cmd.Parameters.AddWithValue("@TargetValue", goal.TargetValue);
                cmd.Parameters.AddWithValue("@UnitOfMeasure", goal.UnitOfMeasure);
                cmd.ExecuteNonQuery();
            }
        }

        private void UpdateTimeBasedGoalData(TimeBasedGoal goal)
        {
            string sqlTimeBaseUpdate = $@"
                UPDATE {TimeBasedTable} SET 
                    RequiredFrequency = @RequiredFrequency
                WHERE GoalID = @GoalID;";

            using (SQLiteCommand cmd = new SQLiteCommand(sqlTimeBaseUpdate, _connection))
            {
                cmd.Parameters.AddWithValue("@GoalID", goal.GoalID);
                cmd.Parameters.AddWithValue("@RequiredFrequency", (int)goal.RequiredFrequency);
                cmd.ExecuteNonQuery();
            }
        }

        // Deletes a Goal and all associated records across three tables.
        public void DeleteGoal(int goalId)
        {
            // Start a transaction to ensure all or none of the deletes happen
            using (var transaction = _connection.BeginTransaction())
            {
                try
                {
                    // Delete from the ProgressEntry table
                    string sqlDeleteProgress = $"DELETE FROM {ProgressEntryTable} WHERE GoalID = @GoalID";
                    using (SQLiteCommand cmd = new SQLiteCommand(sqlDeleteProgress, _connection))
                    {
                        cmd.Parameters.AddWithValue("@GoalID", goalId);
                        cmd.ExecuteNonQuery();
                    }

                    // Delete from the derived tables (QuantitativeGoal and TimeBasedGoal)
                    string sqlDeleteChildren = $@"
                        DELETE FROM {QuantitativeTable} WHERE GoalID = @GoalID;
                        DELETE FROM {TimeBasedTable} WHERE GoalID = @GoalID;";
                    using (SQLiteCommand cmd = new SQLiteCommand(sqlDeleteChildren, _connection))
                    {
                        cmd.Parameters.AddWithValue("@GoalID", goalId);
                        cmd.ExecuteNonQuery();
                    }

                    // Delete the base Goal record (The root)
                    string sqlDeleteBase = $"DELETE FROM {GoalTable} WHERE GoalID = @GoalID";
                    using (SQLiteCommand cmd = new SQLiteCommand(sqlDeleteBase, _connection))
                    {
                        cmd.Parameters.AddWithValue("@GoalID", goalId);
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    Console.WriteLine($"\nSUCCESS: Goal ID {goalId} and all associated records deleted.");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"\nERROR: Failed to delete goal. Details: {ex.Message}");
                    throw;
                }
            }
        }

        // Logs a new progress entry for an existing goal.
        public void AddProgressEntry(int goalId, ProgressEntry entry)
        {
            string sql = $@"
                INSERT INTO {ProgressEntryTable} (GoalID, DateLogged, ValueLogged, Notes)
                VALUES (@GoalID, @DateLogged, @ValueLogged, @Notes);";

            using (SQLiteCommand cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.Parameters.AddWithValue("@GoalID", goalId);
                cmd.Parameters.AddWithValue("@DateLogged", entry.DateLogged.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@ValueLogged", entry.ValueLogged);
                cmd.Parameters.AddWithValue("@Notes", entry.Notes);

                cmd.ExecuteNonQuery();
                
                // Retrieve the EntryID (The PK for the log)
                long lastId = _connection.LastInsertRowId;
                entry.EntryID = (int)lastId; 

                Console.WriteLine($"\nSUCCESS: Progress entry added to Goal ID {goalId}. Entry ID: {entry.EntryID}");

                // Get the Goal object with its updated ProgressEntries list for evaluation
                Goal? updatedGoal = GetGoalById(goalId);
                
                // Trigger Achievement Check (Event-Driven Logic)
                if (updatedGoal != null)
                {
                    // If the goal's status changed (e.g., became 'Complete'), persist this change to the DB
                    // before checking for achievements that depend on database state.
                    string sqlUpdateStatus = $"UPDATE {GoalTable} SET Status = @Status WHERE GoalID = @GoalID";
                    using (SQLiteCommand updateCmd = new SQLiteCommand(sqlUpdateStatus, _connection))
                    {
                        updateCmd.Parameters.AddWithValue("@Status", (int)updatedGoal.Status);
                        updateCmd.Parameters.AddWithValue("@GoalID", goalId);
                        updateCmd.ExecuteNonQuery();
                    }

                    AchievementManager.CheckAndUnlock(updatedGoal, entry);
                }
            }
        }

        // Loads all static achievement definitions (templates) from the database.
        public List<AchievementTemplateModel> GetAllAchievementTemplates()
        {
            List<AchievementTemplateModel> templates = new List<AchievementTemplateModel>();
            // Ensure all columns are selected.
            string sql = $"SELECT AchievementID, Name, Description, UnlockCondition, IsRepeatable FROM {AchievementTemplateTable}";

            using (SQLiteCommand cmd = new SQLiteCommand(sql, _connection))
            {
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Instantiate using the new parameterless constructor
                        AchievementTemplateModel template = new AchievementTemplateModel
                        {
                            AchievementID = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Description = reader.GetString(2),
                            UnlockCondition = reader.GetString(3),
                            IsRepeatable = reader.GetBoolean(4) 
                        };
                        templates.Add(template);
                    }
                }
            }
            return templates;
        }

        // Logs an achievement being earned by a specific goal.
        public void AddAchievementLogEntry(AchievementLogModel logEntry)
        {
            string sql = $@"
                INSERT INTO {AchievementLogTable} (AchievementID, GoalID, DateEarned)
                VALUES (@AchievementID, @GoalID, @DateEarned);";

            using (SQLiteCommand cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.Parameters.AddWithValue("@AchievementID", logEntry.AchievementID);
                cmd.Parameters.AddWithValue("@GoalID", logEntry.GoalID);
                cmd.Parameters.AddWithValue("@DateEarned", logEntry.DateEarned.ToString("yyyy-MM-dd HH:mm:ss"));
                
                cmd.ExecuteNonQuery();
                
                // Retrieve the LogID (The PK) and update the model object
                logEntry.AchievementLogID = (int)_connection.LastInsertRowId; 
            }
        }

        // Retrieves a single Goal object by its ID, reconstituting it as the correct derived type.
        public Goal? GetGoalById(int goalId)
        {
            Goal? goal = null;
            // Use the same LEFT JOIN pattern as GetAllGoals, but for a single ID.
            string sql = $@"
                SELECT 
                    g.GoalID, g.Title, g.Description, g.GoalType, g.Status, g.StartDate, g.EndDate,
                    q.TargetValue, q.UnitOfMeasure,
                    t.RequiredFrequency
                FROM {GoalTable} g
                LEFT JOIN {QuantitativeTable} q ON g.GoalID = q.GoalID
                LEFT JOIN {TimeBasedTable} t ON g.GoalID = t.GoalID
                WHERE g.GoalID = @GoalID";

            using (SQLiteCommand cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.Parameters.AddWithValue("@GoalID", goalId);
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string? goalType = reader.IsDBNull(3) ? null : reader.GetString(3);
                        
                        if (goalType == "QuantitativeGoal")
                        {
                            var qGoal = new QuantitativeGoal
                            {
                                TargetValue = reader.IsDBNull(7) ? 0 : (decimal)reader.GetDouble(7),
                                UnitOfMeasure = reader.IsDBNull(8) ? "" : reader.GetString(8)
                            };
                            goal = qGoal;
                        }
                        else if (goalType == "TimeBasedGoal")
                        {
                            var tGoal = new TimeBasedGoal
                            {
                                RequiredFrequency = reader.IsDBNull(9) ? FrequencyUnit.Daily : (FrequencyUnit)reader.GetInt32(9)
                            };
                            goal = tGoal;
                        }
                        
                        if (goal != null)
                        {
                            goal.GoalID = goalId;
                            goal.Title = reader.GetString(1);
                            goal.Description = reader.GetString(2);
                            goal.Status = (GoalStatus)reader.GetInt32(4);
                            goal.StartDate = DateTime.Parse(reader.GetString(5));
                            goal.EndDate = DateTime.Parse(reader.GetString(6));
                            
                            goal.ProgressEntries = LoadProgressEntries(goalId);
                            goal.CalculateProgress();
                        }
                    }
                }
            }
            return goal; // Returns the fully hydrated goal or null if not found
        }


        // Loads all earned achievement log entries from the database.
        public List<AchievementLogModel> GetAllAchievementLogs()
        {
            List<AchievementLogModel> logs = new List<AchievementLogModel>();
            string sql = $"SELECT LogID, GoalID, AchievementID, DateEarned FROM {AchievementLogTable}";

            using (SQLiteCommand cmd = new SQLiteCommand(sql, _connection))
            {
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        AchievementLogModel log = new AchievementLogModel
                        {
                            AchievementLogID = reader.GetInt32(0),
                            GoalID = reader.GetInt32(1),
                            AchievementID = reader.GetInt32(2),
                            DateEarned = DateTime.Parse(reader.GetString(3))
                        };
                        logs.Add(log);
                    }
                }
            }
            return logs;
        }

        // Retrieves a count of all goals marked as 'Complete'.
        public int GetCompletedGoalCount()
        {
            string sql = $"SELECT COUNT(*) FROM {GoalTable} WHERE Status = @Status";
            using (SQLiteCommand cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.Parameters.AddWithValue("@Status", (int)GoalStatus.Complete);
                
                object? result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result);
                }
            }
            return 0;
        }

        // Adds a new Achievement Template definition to the database.
        public int AddAchievementTemplate(AchievementTemplateModel template)
        {
            string sql = $@"
                INSERT INTO AchievementTemplate (Name, Description, UnlockCondition, IsRepeatable)
                VALUES (@Name, @Description, @UnlockCondition, @IsRepeatable);";

            using (SQLiteCommand cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.Parameters.AddWithValue("@Name", template.Name);
                cmd.Parameters.AddWithValue("@Description", template.Description);
                cmd.Parameters.AddWithValue("@UnlockCondition", template.UnlockCondition);
                // SQLite stores booleans as 1/0
                cmd.Parameters.AddWithValue("@IsRepeatable", template.IsRepeatable ? 1 : 0); 

                cmd.ExecuteNonQuery();

                // Retrieve the ID of the last inserted row
                long lastId = _connection.LastInsertRowId;
                return (int)lastId;
            }
        }

        // Deletes all data from all tables and resets auto-increment counters. For testing purposes.
        public void ResetDatabaseForTesting()
        {
            Console.WriteLine("\n--- RESETTING DATABASE ---");
            using (var transaction = _connection.BeginTransaction())
            {
                try
                {
                    // List of all tables to clear
                    var tables = new[] 
                    { 
                        GoalTable, 
                        QuantitativeTable, 
                        TimeBasedTable, 
                        ProgressEntryTable, 
                        AchievementLogTable, 
                        AchievementTemplateTable 
                    };

                    foreach (var table in tables)
                    {
                        // Clear all data from the table
                        ExecuteNonQuery($"DELETE FROM {table};");
                        // Reset the auto-increment counter for the table
                        ExecuteNonQuery($"DELETE FROM sqlite_sequence WHERE name = '{table}';");
                    }

                    transaction.Commit();
                    Console.WriteLine("SUCCESS: Database has been cleared and reset.");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"ERROR: Failed to reset database. Details: {ex.Message}");
                    throw;
                }
            }
        }
    }
}