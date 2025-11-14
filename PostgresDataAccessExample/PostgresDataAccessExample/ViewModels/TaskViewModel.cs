using PostgresDataAccessExample.Models;
using PostgresDataAccessExample.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PostgresDataAccessExample.ViewModels
{
    public class TaskViewModel : IViewModel
    {
        private readonly TaskRepository _taskRepository;

        public TaskViewModel(TaskRepository taskRepository)
        {
            _taskRepository = taskRepository;
        }

        public Task<List<TaskModel>> GetTasksAsync()
        {
            return _taskRepository.GetTasksAsync();
        }

        public Task InitializeAsync()
        {
            // Здесь может быть логика для асинхронной инициализации,
            // например, первоначальная загрузка данных.
            return Task.CompletedTask;
        }
    }
}
