using PostgresDataAccessExample.Models;
using PostgresDataAccessExample.Repositories;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading; // Необходимо для Dispatcher

namespace PostgresDataAccessExample.ViewModels
{
    public class UserViewModel
    {
        private readonly UserRepository _userRepository;
        private readonly Dispatcher _dispatcher;

        public ObservableCollection<UserModel> Users { get; } = new ObservableCollection<UserModel>();

        public UserViewModel(UserRepository userRepository, Dispatcher dispatcher)
        {
            _userRepository = userRepository;
            _dispatcher = dispatcher;
        }

        public async Task LoadInitialUsersAsync()
        {
            Console.WriteLine("Fetching initial users...");
            var initialUsers = await _userRepository.GetAllAsync();
            _dispatcher.Invoke(() =>
            {
                Users.Clear();
                foreach (var user in initialUsers)
                {
                    Users.Add(user);
                }
            });
            Console.WriteLine($"Loaded {initialUsers.Count} initial users.");
        }

        public async void HandleNewUserNotification(int userId)
        {
            Console.WriteLine($"Received new user ID: {userId}. Fetching full data...");
            var newUser = await _userRepository.GetUserByIdAsync(userId);
            if (newUser != null)
            {
                Console.WriteLine($"Data fetched for user {userId}. Adding to collection.");
                _dispatcher.Invoke(() =>
                {
                    if (!Users.Any(u => u.Id == newUser.Id))
                    {
                        Users.Insert(0, newUser);
                    }
                });
            }
            else
            {
                Console.WriteLine($"Warning: Could not find data for new user ID {userId}.");
            }
        }
    }
}
