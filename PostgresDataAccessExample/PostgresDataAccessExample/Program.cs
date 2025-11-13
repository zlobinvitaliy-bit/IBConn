using Microsoft.Extensions.Configuration;
using PostgresDataAccessExample.Data;
using PostgresDataAccessExample.Models;
using PostgresDataAccessExample.Services;
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
                var userViewModel = new UserViewModel(dbConnectionFactory);
                var taskViewModel = new TaskViewModel(dbConnectionFactory);

                Console.WriteLine("=== PostgreSQL Data Access Example ===");

                Console.WriteLine("\nCreating test table 'users' if it doesn't exist...");
                await userViewModel.CreateUsersTableAsync();
                Console.WriteLine("✅ Test table checked/created!");

                Console.WriteLine("\nInserting test data into 'users' table...");
                await userViewModel.InsertTestDataAsync();
                Console.WriteLine("✅ Test data inserted!");

                Console.WriteLine("\nExample 1: Getting all users");
                var allUsers = await userViewModel.GetAllUsersAsync();
                DisplayUsers(allUsers);

                Console.WriteLine("\nExample 2: Getting user by name (with parameters)");
                var filteredUsers = await userViewModel.GetUserByNameAsync("Иван");
                DisplayUsers(filteredUsers);

                Console.WriteLine("\nExample 3: Getting user count (scalar query)");
                var userCount = await userViewModel.GetUserCountAsync();
                Console.WriteLine($"Total users: {userCount}");

                Console.WriteLine("\nExample 4: Updating user email");
                var rowsAffected = await userViewModel.UpdateUserEmailAsync("Иван Иванов", "ivan.updated@example.com");
                Console.WriteLine($"Rows updated: {rowsAffected}");

                Console.WriteLine("\nUpdated data:");
                var updatedUsers = await userViewModel.GetAllUsersAsync();
                DisplayUsers(updatedUsers);
                
                var notificationService = new NotificationService(dbConnectionFactory);
                var cts = new CancellationTokenSource();
                var listenTask = notificationService.ListenForNewUsers(cts.Token);

                Console.WriteLine("\nListening for new user notifications. Press any key to stop.");
                Console.ReadKey();

                cts.Cancel();
                await listenTask;
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
                Console.WriteLine("No data found.");
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
    }
}
