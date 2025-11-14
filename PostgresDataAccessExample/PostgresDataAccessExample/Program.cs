using Microsoft.Extensions.Configuration;
using PostgresDataAccessExample.Data;
using PostgresDataAccessExample.Models;
using PostgresDataAccessExample.Repositories;
using PostgresDataAccessExample.Services;
using PostgresDataAccessExample.Setup;
using PostgresDataAccessExample.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PostgresDataAccessExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var cts = new CancellationTokenSource();

            try
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory) // Set the base path to the application's base directory
                    .AddJsonFile("appsettings.json", optional: false)
                    .Build();

                var dbConnectionFactory = new DbConnectionFactory(configuration);
                using var dbContext = new DbContext(dbConnectionFactory);

                Console.WriteLine("Setting up database...");
                var databaseSetup = new DatabaseSetup(dbContext);
                await databaseSetup.EnsureDatabaseSetupAsync();
                
                var userRepository = new UserRepository(dbContext);
                await userRepository.CreateTableAsync();
                await userRepository.InsertTestDataAsync();
                Console.WriteLine("✅ Database setup complete.");

                var taskRepository = new TaskRepository(dbContext);
                var taskViewModel = new TaskViewModel(taskRepository);
                var userViewModel = new UserViewModel(userRepository);

                taskViewModel.Tasks.CollectionChanged += (s, e) => OnCollectionChanged(s, e, "Tasks");
                userViewModel.Users.CollectionChanged += (s, e) => OnCollectionChanged(s, e, "Users");

                await taskViewModel.LoadInitialTasksAsync();
                await userViewModel.LoadInitialUsersAsync();
                Console.WriteLine("\n--- Initial Data ---");
                DisplayTasks(taskViewModel.Tasks);
                DisplayUsers(userViewModel.Users);

                var notificationService = new NotificationService(dbConnectionFactory);
                var jobsListener = notificationService.ListenForNewJobs(taskViewModel.HandleNewJobNotification, cts.Token);
                var usersListener = notificationService.ListenForNewUsers(userViewModel.HandleNewUserNotification, cts.Token);

                Console.WriteLine("\n👂 Listening for new notifications for JobsN and Users...");
                Console.WriteLine("Press any key to stop.");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ An unexpected error occurred: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                Console.WriteLine("\nShutting down...");
                cts.Cancel();
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e, string collectionName)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                Console.WriteLine($"\n--- Collection {collectionName} Updated ---");
                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems)
                    {
                        if (item is TaskModel task) {
                            Console.WriteLine($"  -> New Task: Job64: {task.Job64}, Doc: {task.TDoc}, Product: {task.Product}");
                        } else if (item is UserModel user) {
                            Console.WriteLine($"  -> New User: ID: {user.Id}, Name: {user.Name}, Email: {user.Email}");
                        }
                    }
                    Console.WriteLine("-----------------------------------------\n");
                }
            }
        }

        static void DisplayTasks(ICollection<TaskModel> tasks)
        {
            Console.WriteLine("\n--- Tasks ---");
            if (tasks.Count == 0) { Console.WriteLine("No tasks found."); return; }
            Console.WriteLine($"{"Job64",-8} {"Time",-22} {"TDoc",-12} {"Product",-15} {"Car",-15} {"Driver",-15}");
            Console.WriteLine(new string('-', 100));
            foreach (var task in tasks) 
            {
                Console.WriteLine($"{task.Job64,-8} {task.Time,-22} {task.TDoc,-12} {task.Product,-15} {task.Car,-15} {task.CarDriver,-15}"); 
            }
        }

        static void DisplayUsers(ICollection<UserModel> users)
        {
            Console.WriteLine("\n--- Users ---");
            if (users.Count == 0) { Console.WriteLine("No users found."); return; }
            Console.WriteLine($"{"Id",-5} {"Name",-20} {"Email",-25} {"CreatedAt",-22}");
            Console.WriteLine(new string('-', 80));
            foreach (var user in users) { Console.WriteLine($"{user.Id,-5} {user.Name,-20} {user.Email,-25} {user.CreatedAt,-22:yyyy-MM-dd HH:mm:ss}"); }
        }
    }
}
