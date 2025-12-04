/*
Benjamin Mather
20251129
3.8 Course Project
Class Implementation

Abstract base class that implements the interface
*/
namespace GoalTrackingApp
{
    //Replaces the IsActive boolean to provide a clearer status for the application logic
    public enum GoalStatus
    {
        InProgress,
        Complete,
        Cancelled
    }
    public abstract class Goal : IProgressReporter
    {
        //protected access used for properties to allow derived classes access while keeping them private to external code
        public int GoalID { get; set; }//set by database, primary key
        public string Title { get; set;} = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Description { get; set; } = null!;
        //public bool IsActive { get; set; } replaced by GoalStatus
        public GoalStatus Status { get; set; }

        //ledger of all progress entries for a goal
        public List<ProgressEntry> ProgressEntries { get; internal set; }
    
        //constructor
        public Goal(string title, string description, DateTime startDate, DateTime endDate)
        {
            this.Title = title;
            this.Description = description;
            this.StartDate = startDate;
            this.EndDate = endDate;
            //this.IsActive = true; //replaced by GoalStatus
            this.Status = GoalStatus.InProgress;
            this.ProgressEntries = new List<ProgressEntry>();//initialize the composed list
        
        }

        //parameterless constructor for database loading
        protected Goal() 
        {
            this.ProgressEntries = new List<ProgressEntry>(); 
        }

        //abstract method to be implemented by inheriting classes
        public abstract string CalculateProgress();

        //Add a new progress entry to the goal's ledger
        public void LogProgress(ProgressEntry entry)
        {
            entry.GoalID = this.GoalID; //set goal ID before logging
            this.ProgressEntries.Add(entry); //add entry to the list
            CalculateProgress(); //after logging, recalculate and update goal's status
            AchievementManager.CheckAndUnlock(this, entry); //check for achievements
            Console.WriteLine($"Trigger progress entry database save [simulated] for Goal ID: {this.GoalID}");
        }

        //implement interface
        public string GenerateSummaryReport(int goalID)
        {
            return $"Goal ID: {goalID} | Title: {Title} | Status: {Status} | Progress: {CalculateProgress()}";
        }

        //basic string representation of goal's state for testing
        public override string ToString()
        {
            return $"--- Goal Details ---\n" +
                $"Goal ID: {GoalID}\n" +
                $"Title: {Title}\n" +
                $"Description: {Description}\n" +
                $"Start Date: {StartDate}\n" +
                $"End Date: {EndDate}\n" +
                $"Status: {Status}\n" +
                $"Progress: {CalculateProgress()}\n" +
                $"Progress Entries Logged: {ProgressEntries.Count}";
        }
    }
}