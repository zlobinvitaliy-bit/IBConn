using PostgresDataAccessExample.Models;
using PostgresDataAccessExample.Repositories;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading; // Необходимо для Dispatcher

namespace PostgresDataAccessExample.ViewModels
{
    public class TaskViewModel
    {
        private readonly TaskRepository _taskRepository;
        private readonly Dispatcher _dispatcher; // Диспетчер для обновления UI-потока

        // Коллекция, которая будет связана с DataGrid. Она будет уведомлять UI об изменениях.
        public ObservableCollection<TaskModel> Tasks { get; } = new ObservableCollection<TaskModel>();

        public TaskViewModel(TaskRepository taskRepository, Dispatcher dispatcher)
        {
            _taskRepository = taskRepository;
            _dispatcher = dispatcher;
        }

        // Загружает первоначальный список задач
        public async Task LoadInitialTasksAsync()
        {
            Console.WriteLine("Fetching initial tasks...");
            var initialTasks = await _taskRepository.GetTasksAsync();
            _dispatcher.Invoke(() =>
            {
                Tasks.Clear();
                foreach (var task in initialTasks)
                {
                    Tasks.Add(task);
                }
            });
            Console.WriteLine($"Loaded {initialTasks.Count} initial tasks.");
        }

        // Метод, который будет вызываться из NotificationService
        public async void HandleNewJobNotification(int jobId)
        {
            Console.WriteLine($"Received new job ID: {jobId}. Fetching full data...");
            var newTask = await _taskRepository.GetTaskByIdAsync(jobId);
            if (newTask != null)
            {
                Console.WriteLine($"Data fetched for task {jobId}. Adding to collection.");

                // Используем Dispatcher для безопасного обновления ObservableCollection из любого потока
                _dispatcher.Invoke(() =>
                {
                    // Проверяем, нет ли уже такой задачи (на всякий случай)
                    if (!Tasks.Any(t => t.Id == newTask.Id))
                    {
                        // Добавляем в начало списка для наглядности
                        Tasks.Insert(0, newTask);
                    }
                });
            }
            else
            {
                Console.WriteLine($"Warning: Could not find data for new job ID {jobId}.");
            }
        }
    }
}
