/*
Benjamin Mather
20251129
3.8 Course Project
Class Implementation

Represents a goal tracked by a numerical target value. Inherits from Goal
*/
namespace GoalTrackingApp
{
    public class QuantitativeGoal : Goal
    {
        //properties
        public decimal TargetValue { get; set; }
        public decimal CurrentValue { get; private set; } = 0; //calculated value set to private and initialized to 0 in constructor
        public string UnitOfMeasure { get; set; } = null!;

        //constructor
        public QuantitativeGoal(string title, string description, DateTime startDate, DateTime endDate, decimal targetValue, string unitOfMeasure) : base(title, description, startDate, endDate)
        {
            this.TargetValue = targetValue;
            this.UnitOfMeasure = unitOfMeasure;
        }

        //parameterless constructor for database loading
        public QuantitativeGoal() : base() {}

        //methods
        public override string CalculateProgress()
        {
            //recalculate CurrentValue from the composed list of ProgressEntries to ensure it is always up to date after any LogProgress call
            CurrentValue = ProgressEntries.Sum(entry => entry.ValueLogged);
            if (TargetValue <= 0)
            {
                return "Target Invalid (0%)";
            }
            decimal percentage = (CurrentValue / TargetValue) * 100;

            //update the GoalStatus if complete
            if (CurrentValue >= TargetValue)
            {
                this.Status = GoalStatus.Complete;
                percentage = 100; //cap percentage at 100%
            }
            return $"{percentage:N1}% Complete ({CurrentValue:N1} of {TargetValue:N1} {UnitOfMeasure})";
        }
    }
}