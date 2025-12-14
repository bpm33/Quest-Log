# Goal Tracking C# Console Application (Quest Log)
 [Watch the Video Demonstration] https://youtu.be/_UXveoV0jJk
 
## 1. Overview

This project is a C# console application designed to demonstrate core object-oriented programming (OOP) principles and database integration. It provides a simple yet robust system for users to define, track, and manage personal goals. The application features a flexible goal system, data persistence using SQLite, and a dynamic achievement system.

The application runs as an interactive console menu, allowing users to manage their goals in real-time. It showcases all major features, from database creation to goal tracking and achievement unlocking.

## 2. Features

- **Polymorphic Goal Types**:
    - **Quantitative Goals**: Track progress towards a specific numerical target (e.g., "Run 50 miles").
    - **Time-Based Goals**: Track consistency and streaks for recurring activities (e.g., "Meditate daily").
- **Interactive Console UI**: A menu-driven interface for viewing, adding, editing, deleting, and logging progress on goals.
- **Data Persistence with SQLite**:
    - All goals, progress entries, and achievements are saved to a local `GoalTrackingDB.sqlite` database.
    - The `GoalRepository` class encapsulates all database operations (CRUD).
    - The database schema is automatically created on the first run.
- **Dynamic Achievement System**:
    - The `AchievementManager` checks for and unlocks achievements based on user progress.
    - Achievements are defined by templates with simple, string-based unlock conditions (e.g., `"CurrentStreak >= 7"`).
    - The condition evaluator uses C# Reflection to dynamically check goal properties and also supports global conditions (e.g., total goals completed).
- **Clear Separation of Concerns**:
    - **Models**: `Goal`, `ProgressEntry`, `AchievementTemplateModel`, etc., represent the core data structures.
    - **Repository**: `GoalRepository` handles the data access layer, isolating database logic.
    - **Business Logic**: `AchievementManager` and methods within the goal classes contain the application's rules.

## 3. Technologies & Concepts

- **Language**: C# (.NET 8)
- **Database**: SQLite
- **Key OOP Concepts Demonstrated**:
    - **Inheritance**: `QuantitativeGoal` and `TimeBasedGoal` inherit from an abstract `Goal` base class.
    - **Polymorphism**: The repository can save and load different goal types using the base `Goal` reference.
    - **Encapsulation**: Data and related logic are contained within classes (e.g., `CurrentValue` is a private set property calculated within `QuantitativeGoal`).
    - **Composition**: The `Goal` class is composed of a `List<ProgressEntry>`.
    - **Interfaces**: The `IProgressReporter` interface defines a contract for generating progress summaries.
- **Design Patterns**:
    - **Repository Pattern**: Decouples business logic from the data access layer.
- **Other Concepts**:
    - **Reflection**: Used in the `AchievementManager` to dynamically evaluate unlock conditions.
    - **Exception Handling**: Used throughout the repository and program to manage potential errors gracefully.

## 4. Getting Started

### Prerequisites

1.  .NET 8 SDK or later.
2.  The `System.Data.SQLite` NuGet package. If it's not already part of the project, you can add it via the command line:
    ```bash
    dotnet add package System.Data.SQLite.Core
    ```

### How to Run

1.  Clone or download the project repository.
2.  Open a terminal or command prompt in the project's root directory (`GoalTrackingApp`).
3.  Run the application using the .NET CLI:
    ```bash
    dotnet run
    ```

### Expected Output

When you run the application, you will be greeted with a main menu. From here, you can interact with the application to:
- View all existing goals.
- Add new quantitative or time-based goals.
- Log progress for any goal.
- View the detailed status of a specific goal.
- Edit the properties of an existing goal.
- Delete goals.

A `GoalTrackingDB.sqlite` file will be created in the output directory (e.g., `bin/Debug/net8.0`). You can inspect this file with a SQLite browser to see the persisted data.

## 5. Project Structure

- `Program.cs`: The main application entry point, containing the interactive menu loop and user interface logic.
- `Goal.cs`: The abstract base class for all goal types.
- `QuantitativeGoal.cs`: A concrete goal class for tracking numerical progress.
- `TimeBaseGoal.cs`: A concrete goal class for tracking streaks and frequency.
- `ProgressEntry.cs`: Represents a single log of progress made towards a goal.
- `GoalRepository.cs`: The data access layer responsible for all SQLite database interactions.
- `Helper.cs`: A static utility class for handling and validating user console input.
- `AchievementManager.cs`: A static class that orchestrates the achievement unlocking logic.
- `AchievementTemplateModel.cs`: A model for defining an achievement's properties and unlock rules.
- `AchievementLogModel.cs`: A model representing an instance of an earned achievement.
- `IProgressReporter.cs`: An interface ensuring all goals can generate a summary report.

## 6. Project Summary

### Project Description

The Goal Tracking Application is a C# console-based tool designed to help users define, manage, and track progress toward personal goals. It demonstrates advanced object-oriented programming by utilizing polymorphic goal types (Quantitative and Time-Based) and ensures data persistence through a local SQLite database. The project includes a dynamic achievement system that evaluates user progress against specific rules using C# Reflection.

### Project Tasks

- Task 1: Environment Setup
  - Configured .NET 8 SDK and development environment.
  - Installed System.Data.SQLite packages for database connectivity.
- Task 2: Architecture Design
  - Designed the abstract Goal base class and derived QuantitativeGoal and TimeBasedGoal classes.
  - Planned the database schema for relational data integrity (Goals, Progress, Achievements).
- Task 3: Core Implementation
  - Implemented the GoalRepository using the Repository Pattern to handle CRUD operations.
  - Built the AchievementManager to handle logic separation.
- Task 4: Interface Development
  - Created an interactive console menu loop for user interaction.
  - Implemented the ConsoleHelper class to separate I/O logic from business logic.
- Task 5: Refactoring and Polish
  - Refactored Program.cs to utilize switch expressions and robust error handling.
  - Conducted end-to-end testing of data persistence and achievement unlocking.

### Project Skills Learned

- Advanced OOP: Practical application of Polymorphism, Inheritance, Composition, and Interfaces.
- Database Management: Writing raw SQL queries and managing SQLite transactions within C#.
- Software Architecture: Implementing the Repository Pattern and Separation of Concerns.
- Advanced C# Features: Utilizing Reflection for dynamic property evaluation and Switch Expressions for cleaner logic.
- Input Validation: Creating robust helper methods to ensure data integrity from user input.

### Language Used

- C# (.NET 8): Used for core application logic and console interface.
- SQL: Used for SQLite database schema creation and data manipulation.

### Development Process Used

- Iterative Development: The project started with hardcoded test scripts and evolved into a fully interactive, menu-driven application through successive refactoring cycles.




