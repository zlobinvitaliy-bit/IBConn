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
using System.Windows.Threading;

namespace PostgresDataAccessExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            var cts = new CancellationTokenSource();

            try
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false)
                    .Build();

                var dbConnectionFactory = new DbConnectionFactory(configuration);
                using var dbContext = new DbContext(dbConnectionFactory);

                // 1. Настройка базы данных (триггеры для JobsN и users)
                Console.WriteLine("Setting up database...");
                var databaseSetup = new DatabaseSetup(dbContext);
                await databaseSetup.EnsureDatabaseSetupAsync();
                
                // Убедимся, что таблица users существует
                var userRepository = new UserRepository(dbContext);
                await userRepository.CreateTableAsync();
                await userRepository.InsertTestDataAsync(); // Вставим начальных пользователей
                Console.WriteLine("✅ Database setup complete.");

                // 2. Инициализация ViewModel'ов
                var taskRepository = new TaskRepository(dbContext);
                var taskViewModel = new TaskViewModel(taskRepository, dispatcher);
                var userViewModel = new UserViewModel(userRepository, dispatcher);

                // Подписываемся на изменения коллекций
                taskViewModel.Tasks.CollectionChanged += (s, e) => OnCollectionChanged(s, e, "Tasks");
                userViewModel.Users.CollectionChanged += (s, e) => OnCollectionChanged(s, e, "Users");

                // 3. Загрузка начальных данных
                await taskViewModel.LoadInitialTasksAsync();
                await userViewModel.LoadInitialUsersAsync();
                Console.WriteLine("\n--- Initial Data ---");
                DisplayTasks(taskViewModel.Tasks);
                DisplayUsers(userViewModel.Users);

                // 4. Запуск сервиса уведомлений для обеих сущностей
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
                dispatcher.InvokeShutdown();
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        // Обобщенный обработчик для изменений в любой коллекции
        private static void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e, string collectionName)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                Console.WriteLine($"\n--- Collection '{collectionName}' Updated ---");
                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems)
                    {
                        if (item is TaskModel task) {
                            Console.WriteLine($"  -> New Task: ID: {task.Id}, Doc: {task.TDoc}, Product: {task.ProductName}");
                        } else if (item is UserModel user) {
                            Console.WriteLine($"  -> New User: ID: {user.Id}, Name: {user.Name}, Email: {user.Email}");
                        }
                    }
                    Console.WriteLine("-----------------------------------------\n");
                }
            }
        }

        // Функции отображения...
        static void DisplayTasks(ICollection<TaskModel> tasks)
        {
            Console.WriteLine("\n--- Tasks ---");
            if (tasks.Count == 0) { Console.WriteLine("No tasks found."); return; }
            Console.WriteLine($"{'Id',-5} {'Time',-22} {'Doc',-15} {'Product',-15} {'Machine',-15}");
            Console.WriteLine(new string('-', 80));
            foreach (var task in tasks) { Console.WriteLine($"{task.Id,-5} {task.Time,-22:yyyy-MM-dd HH:mm:ss} {task.TDoc,-15} {task.ProductName,-15} {task.Car,-15}"); }
        }

        static void DisplayUsers(ICollection<UserModel> users)
        {
            Console.WriteLine("\n--- Users ---");
            if (users.Count == 0) { Console.WriteLine("No users found."); return; }
            Console.WriteLine($"{'Id',-5} {'Name',-20} {'Email',-25} {'CreatedAt',-22}");
            Console.WriteLine(new string('-', 80));
            foreach (var user in users) { Console.WriteLine($"{user.Id,-5} {user.Name,-20} {user.Email,-25} {user.CreatedAt,-22:yyyy-MM-dd HH:mm:ss}"); }
        }
    }
}
