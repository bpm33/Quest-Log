/*
Benjamin Mather
20251129
3.8 Course Project
Class Implementation

Abstract base class that implements the interface
*/
using System.Text;
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

        //implement interface
        public string GenerateSummaryReport(int goalID)
        {
            return $"Goal ID: {goalID} | Title: {Title} | Status: {Status} | Progress: {CalculateProgress()}";
        }

        //basic string representation of goal's state for testing
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("--- Goal Details ---");
            sb.AppendLine($"Goal ID: {GoalID}");
            sb.AppendLine($"Title: {Title}");
            sb.AppendLine($"Description: {Description}");
            sb.AppendLine($"Start Date: {StartDate.ToShortDateString()}");
            sb.AppendLine($"End Date: {EndDate.ToShortDateString()}");
            sb.AppendLine($"Status: {Status}");
            sb.AppendLine($"Progress: {CalculateProgress()}");
            
            sb.AppendLine($"\n--- Progress Log ({ProgressEntries.Count} entries) ---");

            if (ProgressEntries.Any())
            {
                // Entries are loaded in descending date order from the repository
                foreach (var entry in ProgressEntries)
                {
                    string valueDisplay = entry.ValueLogged.ToString("G29"); // "G29" removes trailing zeros from decimal
                    if (this is QuantitativeGoal qGoal)
                    {
                        valueDisplay += $" {qGoal.UnitOfMeasure}";
                    }

                    sb.Append($"  - {entry.DateLogged:yyyy-MM-dd}: {valueDisplay}");
                    if (!string.IsNullOrWhiteSpace(entry.Notes))
                    {
                        sb.Append($" | Notes: {entry.Notes}");
                    }
                    sb.AppendLine();
                }
            }
            else
            {
                sb.AppendLine("No progress has been logged for this goal yet.");
            }

            return sb.ToString();
        }
    }
}