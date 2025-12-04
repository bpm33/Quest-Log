/*
Benjamin Mather
20251204
4.6 Course Project
Database Implementation

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
                        entry.Notes = reader.GetString(3);
                        
                        entries.Add(entry);
                    }
                }
            }
            return entries;
        }

        // Reads a list of all goals, reconstituting them as the correct derived type
        public List<Goal> GetAllGoals()
        {
            List<Goal> goals = new List<Goal>();
            
            // Select all base goal data
            string sql = $"SELECT GoalID, Title, Description, GoalType, Status, StartDate, EndDate FROM {GoalTable}";
            
            using (SQLiteCommand cmd = new SQLiteCommand(sql, _connection))
            {
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int goalId = reader.GetInt32(0);
                        string? goalType = reader.IsDBNull(3) ? null : reader.GetString(3);
                        Goal? goal = null;

                        // Polymorphic Hydration - Instantiate the correct derived class
                        if (goalType == "QuantitativeGoal")
                        {
                            QuantitativeGoal qGoal = new QuantitativeGoal();
                            
                            // Load Quantitative-specific data
                            string sqlQuant = $"SELECT TargetValue, UnitOfMeasure FROM {QuantitativeTable} WHERE GoalID = @GoalID";
                            using (SQLiteCommand qCmd = new SQLiteCommand(sqlQuant, _connection))
                            {
                                qCmd.Parameters.AddWithValue("@GoalID", goalId);
                                using (SQLiteDataReader qReader = qCmd.ExecuteReader())
                                {
                                    if (qReader.Read())
                                    {
                                        qGoal.TargetValue = (decimal)qReader.GetDouble(0);
                                        qGoal.UnitOfMeasure = qReader.GetString(1);
                                    }
                                }
                            }
                            goal = qGoal;
                        }
                        else if (goalType == "TimeBasedGoal")
                        {
                            TimeBasedGoal tGoal = new TimeBasedGoal(); 

                            // Load TimeBased-specific data
                            string sqlTime = $"SELECT RequiredFrequency FROM {TimeBasedTable} WHERE GoalID = @GoalID";
                            using (SQLiteCommand tCmd = new SQLiteCommand(sqlTime, _connection))
                            {
                                tCmd.Parameters.AddWithValue("@GoalID", goalId);
                                using (SQLiteDataReader tReader = tCmd.ExecuteReader())
                                {
                                    if (tReader.Read())
                                    {
                                        // Cast the integer from the database back to the C# enum
                                        tGoal.RequiredFrequency = (FrequencyUnit)tReader.GetInt32(0);
                                    }
                                }
                            }
                            goal = tGoal;
                        }
                        
                        // Populate all common properties
                        if (goal != null)
                        {
                            goal.GoalID = goalId;
                            goal.Title = reader.GetString(1);
                            goal.Description = reader.GetString(2);
                            goal.Status = (GoalStatus)reader.GetInt32(4);
                            goal.StartDate = DateTime.Parse(reader.GetString(5));
                            goal.EndDate = DateTime.Parse(reader.GetString(6));
                            
                            // Load Composed Progress Entries
                            goal.ProgressEntries = LoadProgressEntries(goalId);
                            
                            // Call CalculateProgress to update CurrentValue/CurrentStreak based on loaded entries
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
            // Select base goal data with a WHERE clause
            string sql = $"SELECT GoalID, Title, Description, GoalType, Status, StartDate, EndDate FROM {GoalTable} WHERE GoalID = @GoalID";
            Goal? goal = null;

            using (SQLiteCommand cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.Parameters.AddWithValue("@GoalID", goalId); // Ensure goalId is correctly used in the query
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read()) // Check if a record was found
                    {
                        string? goalType = reader.IsDBNull(3) ? null : reader.GetString(3);
                        
                        // Instantiate the correct derived class
                        if (goalType == "QuantitativeGoal")
                        {
                            QuantitativeGoal qGoal = new QuantitativeGoal();
                            
                            // Load Quantitative-specific data
                            string sqlQuant = $"SELECT TargetValue, UnitOfMeasure FROM {QuantitativeTable} WHERE GoalID = @GoalID";
                            using (SQLiteCommand qCmd = new SQLiteCommand(sqlQuant, _connection))
                            {
                                qCmd.Parameters.AddWithValue("@GoalID", goalId);
                                using (SQLiteDataReader qReader = qCmd.ExecuteReader())
                                {
                                    if (qReader.Read())
                                    {
                                        qGoal.TargetValue = (decimal)qReader.GetDouble(0);
                                        qGoal.UnitOfMeasure = qReader.GetString(1);
                                    }
                                }
                            }
                            goal = qGoal;
                        }
                        else if (goalType == "TimeBasedGoal")
                        {
                            TimeBasedGoal tGoal = new TimeBasedGoal(); 

                            // Load TimeBased-specific data
                            string sqlTime = $"SELECT RequiredFrequency FROM {TimeBasedTable} WHERE GoalID = @GoalID";
                            using (SQLiteCommand tCmd = new SQLiteCommand(sqlTime, _connection))
                            {
                                tCmd.Parameters.AddWithValue("@GoalID", goalId);
                                using (SQLiteDataReader tReader = tCmd.ExecuteReader())
                                {
                                    if (tReader.Read())
                                    {
                                        // Cast the integer from the database back to the C# enum
                                        tGoal.RequiredFrequency = (FrequencyUnit)tReader.GetInt32(0);
                                    }
                                }
                            }
                            goal = tGoal;
                        }
                        
                        // Populate all common properties
                        if (goal != null)
                        {
                            goal.GoalID = goalId;
                            goal.Title = reader.GetString(1);
                            goal.Description = reader.GetString(2);
                            goal.Status = (GoalStatus)reader.GetInt32(4);
                            goal.StartDate = DateTime.Parse(reader.GetString(5));
                            goal.EndDate = DateTime.Parse(reader.GetString(6));
                            
                            // Load Composed Progress Entries
                            goal.ProgressEntries = LoadProgressEntries(goalId);
                            
                            // Call CalculateProgress to update CurrentValue/CurrentStreak based on loaded entries
                            goal.CalculateProgress();
                        }
                    }
                }
            }
            return goal; // Returns the fully hydrated goal or null if not found
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

    }
}