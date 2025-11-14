using PostgresDataAccessExample.Models;
using PostgresDataAccessExample.Repositories;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace PostgresDataAccessExample.ViewModels
{
    public class TaskViewModel
    {
        private readonly TaskRepository _taskRepository;

        public ObservableCollection<TaskModel> Tasks { get; } = new ObservableCollection<TaskModel>();

        public TaskViewModel(TaskRepository taskRepository)
        {
            _taskRepository = taskRepository;
        }

        public async Task LoadInitialTasksAsync()
        {
            Console.WriteLine("Fetching initial tasks...");
            var initialTasks = await _taskRepository.GetTasksAsync();
            
            Tasks.Clear();
            foreach (var task in initialTasks)
            {
                Tasks.Add(task);
            }
            
            Console.WriteLine($"Loaded {initialTasks.Count} initial tasks.");
        }

        public async void HandleNewJobNotification(int job64)
        {
            Console.WriteLine($"Received new job ID: {job64}. Fetching full data...");
            var newTask = await _taskRepository.GetTaskByIdAsync(job64);
            if (newTask != null)
            {
                Console.WriteLine($"Data fetched for task {job64}. Adding to collection.");

                if (!Tasks.Any(t => t.Job64 == newTask.Job64))
                {
                    Tasks.Insert(0, newTask);
                }
            }
            else
            {
                Console.WriteLine($"Warning: Could not find data for new job ID {job64}.");
            }
        }
    }
}
