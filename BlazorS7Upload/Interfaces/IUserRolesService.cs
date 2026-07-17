using BlazorS7Upload.Authentication;

namespace BlazorS7Upload.Interfaces
{
    public interface IUserRolesService
    {
        Task<List<string>> GetListRoles();
        Task<List<string>> GetListRolesByUser(string email);
        Task<List<UserRolesModel>> GetListRolesAndUser();
        Task<int> GetRol(string rol);
        Task<long> InsertRol(string rol);
        Task<long> DeleteRol(string rol);
        Task<long> InsertRolByUser(string email, string rol);
        Task<long> DeleteRolByUser(string email, string rol);
    }
}