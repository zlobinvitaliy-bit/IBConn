using Microsoft.Extensions.Configuration;
using PostgresDataAccessExample.Data;
using PostgresDataAccessExample.Models;
using PostgresDataAccessExample.Repositories;
using PostgresDataAccessExample.Services;
using PostgresDataAccessExample.Setup;
using PostgresDataAccessExample.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PostgresDataAccessExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                var dbConnectionFactory = new DbConnectionFactory(configuration);
                using var dbContext = new DbContext(dbConnectionFactory);

                // Setup database triggers and functions
                Console.WriteLine("\nEnsuring database notification triggers are up to date...");
                var databaseSetup = new DatabaseSetup(dbContext);
                await databaseSetup.EnsureNewJobNotificationTriggerExistsAsync();
                Console.WriteLine("✅ Database notification setup complete!");

                // --- User related operations ---
                var userRepository = new UserRepository(dbContext);
                var userViewModel = new UserViewModel(userRepository);

                Console.WriteLine("\n--- Running User Examples ---");
                Console.WriteLine("Creating test table 'users' if it doesn't exist...");
                await userViewModel.CreateUsersTableAsync();
                await userViewModel.InsertTestDataAsync();
                var allUsers = await userViewModel.GetAllUsersAsync();
                DisplayUsers(allUsers);

                // --- Task related operations ---
                var taskRepository = new TaskRepository(dbContext);
                var taskViewModel = new TaskViewModel(taskRepository);
                Console.WriteLine("\n--- Fetching Initial Tasks ---");
                var tasks = await taskViewModel.GetTasksAsync();
                DisplayTasks(tasks); // A new display method for tasks

                // --- Notification Listeners ---
                var notificationService = new NotificationService(dbConnectionFactory);
                var cts = new CancellationTokenSource();

                // Start listening for both user and job notifications
                var userListenTask = notificationService.ListenForNewUsers(cts.Token);
                var jobListenTask = notificationService.ListenForNewJobs(cts.Token);

                Console.WriteLine("\n👂 Listening for new user and new job notifications...");
                Console.WriteLine("A new record in 'JobsN' table in the database will be displayed here.");
                Console.WriteLine("Press any key to stop listening and exit.");
                Console.ReadKey();

                Console.WriteLine("\nCancelling listeners...");
                cts.Cancel();
                await Task.WhenAll(userListenTask, jobListenTask);
                Console.WriteLine("✅ Listeners stopped.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ An unexpected error occurred: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static void DisplayUsers(List<UserModel> users)
        {
            if (users.Count == 0)
            {
                Console.WriteLine("No user data found.");
                return;
            }
            Console.WriteLine($"{'Id',-5} {'Name',-20} {'Email',-30} {'Created At',-25}");
            Console.WriteLine(new string('-', 80));
            foreach (var user in users)
            {
                Console.WriteLine($"{user.Id,-5} {user.Name,-20} {user.Email,-30} {user.CreatedAt,-25:yyyy-MM-dd HH:mm:ss}");
            }
            Console.WriteLine();
        }

        static void DisplayTasks(List<TaskModel> tasks)
        {
            if (tasks.Count == 0)
            {
                Console.WriteLine("No task data found.");
                return;
            }
            Console.WriteLine($"{'Time',-22} {'Document',-15} {'Product',-10} {'Machine',-15}");
            Console.WriteLine(new string('-', 80));
            foreach (var task in tasks)
            {
                Console.WriteLine($"{task.Time,-22} {task.TDoc,-15} {task.Product,-10} {task.Car,-15}");
            }
            Console.WriteLine();
        }
    }
}
