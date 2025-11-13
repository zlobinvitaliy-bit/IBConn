using PostgresDataAccessExample.Models;
using PostgresDataAccessExample.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PostgresDataAccessExample.ViewModels
{
    public class UserViewModel : IViewModel
    {
        private readonly UserRepository _userRepository;

        public UserViewModel(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task CreateUsersTableAsync()
        {
            await _userRepository.CreateUsersTableAsync();
        }

        public async Task InsertTestDataAsync()
        {
            await _userRepository.InsertTestDataAsync();
        }

        public async Task<List<UserModel>> GetAllUsersAsync()
        {
            return await _userRepository.GetAllUsersAsync();
        }

        public async Task<List<UserModel>> GetUserByNameAsync(string name)
        {
            return await _userRepository.GetUserByNameAsync(name);
        }

        public async Task<long> GetUserCountAsync()
        {
            return await _userRepository.GetUserCountAsync();
        }

        public async Task<int> UpdateUserEmailAsync(string userName, string newEmail)
        {
            return await _userRepository.UpdateUserEmailAsync(userName, newEmail);
        }
    }
}
