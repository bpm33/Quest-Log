/*
Benjamin Mather
20251205
5.2 Course Project
Refactoring

Static helper class for handling console input and parsing.
*/
using System.Globalization;

namespace GoalTrackingApp
{
    public static class ConsoleHelper
    {
        // Prompts the user for a string and ensures it's not empty unless allowed.
        public static string GetString(string prompt, bool allowEmpty = false)
        {
            string? input;
            do
            {
                Console.Write(prompt);
                input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input) && !allowEmpty)
                {
                    Console.WriteLine("Input cannot be empty. Please try again.");
                }
            } while (string.IsNullOrWhiteSpace(input) && !allowEmpty);
            
            return input ?? string.Empty;
        }

        // Prompts the user for an integer within an optional range.
        public static int? GetInt(string prompt, int? min = null, int? max = null)
        {
            int value;
            while (true)
            {
                Console.Write(prompt);
                string? input = Console.ReadLine();

                // Allow user to cancel by pressing Enter
                if (string.IsNullOrWhiteSpace(input))
                {
                    return null;
                }

                if (int.TryParse(input, out value))
                {
                    if (min.HasValue && value < min.Value)
                    {
                        Console.WriteLine($"Value must be at least {min.Value}. Please try again.");
                        continue;
                    }
                    if (max.HasValue && value > max.Value)
                    {
                        Console.WriteLine($"Value must be no more than {max.Value}. Please try again.");
                        continue;
                    }
                    return value;
                }
                Console.WriteLine("Invalid number. Please enter a whole number.");
            }
        }

        // Prompts the user for a decimal value.
        public static decimal GetDecimal(string prompt, decimal? min = null)
        {
            decimal value;
            while (true)
            {
                Console.Write(prompt);
                if (decimal.TryParse(Console.ReadLine(), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                {
                    if (min.HasValue && value <= min.Value)
                    {
                        Console.WriteLine($"Value must be greater than {min.Value}. Please try again.");
                        continue;
                    }
                    return value;
                }
                Console.WriteLine("Invalid decimal number. Please try again.");
            }
        }

        // Prompts the user for a date.
        public static DateTime GetDate(string prompt)
        {
            DateTime date;
            while (true)
            {
                Console.Write(prompt);
                if (DateTime.TryParse(Console.ReadLine(), out date))
                {
                    return date;
                }
                Console.WriteLine("Invalid date format. Please use yyyy-mm-dd.");
            }
        }

        // Prompts the user for an optional date. Returns null if user enters nothing.
        public static DateTime? GetOptionalDate(string prompt)
        {
            DateTime date;
            while (true)
            {
                Console.Write(prompt);
                string? input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    return null;
                }
                if (DateTime.TryParse(input, out date))
                {
                    return date;
                }
                Console.WriteLine("Invalid date format. Please use yyyy-mm-dd.");
            }
        }

        // Prompts the user for a yes/no confirmation.
        public static bool GetConfirmation(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                string? input = Console.ReadLine()?.ToLower();
                if (input == "y")
                {
                    return true;
                }
                if (input == "n")
                {
                    return false;
                }
                Console.WriteLine("Invalid input. Please enter 'y' for yes or 'n' for no.");
            }
        }
    }
}