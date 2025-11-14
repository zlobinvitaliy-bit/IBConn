using System.Threading.Tasks;

namespace PostgresDataAccessExample.ViewModels
{
    public interface IViewModel
    {
        Task InitializeAsync();
    }
}
