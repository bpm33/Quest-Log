/*
Benjamin Mather
Quest Log
The Goal Tracking App

The interface defines a contract for any classes that provide progress reports, ensuring all goal types can generate a summary in a standardized way.
*/
namespace GoalTrackingApp
{
    public interface IProgressReporter
    {
        string GenerateSummaryReport(int goalID);
    }

}