/*
Benjamin Mather
20251129
3.8 Course Project
Class Implementation

Represents a single progress log entry, used for composition in the Goal class.
*/
namespace GoalTrackingApp
{
    public class ProgressEntry
    {
        //properties
        public int GoalID { get; set; }//foreign key set by database
        public int EntryID { get; set; }//primary key set by database
        public DateTime DateLogged { get; set; }
        public decimal ValueLogged { get; set; }
        public string Notes { get; set; }

        //constructor
        public ProgressEntry(decimal valueLogged, string notes = "")
        {
            this.DateLogged = DateTime.Now;
            this.ValueLogged = valueLogged;
            this.Notes = notes;
        }

        //methods
        public override string ToString()
        {
            return $"--- Progress Entry Details ---\n" +
                //$"Goal ID: {GoalID}\n" +
                //$"Entry ID: {EntryID}\n" +
                $"Date Logged: {DateLogged}\n" +
                $"Value Logged: {ValueLogged}\n" +
                $"Notes: {Notes}";
        }
    }
}