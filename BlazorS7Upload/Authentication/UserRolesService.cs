using BlazorS7Upload.Interfaces;
using Dapper;
//using Microsoft.Data.SqlClient;
using Npgsql;
namespace BlazorS7Upload.Authentication
{
    public class UserRolesService : IUserRolesService
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        public UserRolesService(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("db_contenido");
        }

        public async Task<int> GetRol(string rol)
        {
            IEnumerable<string> results;

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                var parametros = new { nombre_rol = rol };
                string query = "SELECT nombre_rol FROM dbo.roles WHERE nombre_rol = @nombre_rol";

                results = await connection.QueryAsync<string>(query, parametros);
            }

            return results.Count();
        }

        public async Task<List<string>> GetListRoles()
        {
            IEnumerable<string> results;
            
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = "SELECT nombre_rol FROM dbo.roles";
                results = await connection.QueryAsync<string>(query);
            }

            return results.ToList();
        }

        public async Task<List<string>> GetListRolesByUser(string email)
        {
            IEnumerable<string> results;
            
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = @"SELECT nombre_rol 
              FROM dbo.usuarios AS a
              LEFT JOIN dbo.usuarios_roles AS b ON b.fk_usuarios = a.id_usuarios
              LEFT JOIN dbo.roles AS c ON c.id_roles = b.fk_roles
              WHERE email = @email";
                var parametros = new { email = email };
                results = await connection.QueryAsync<string>(query, parametros);
            }

            return results.ToList();
        }

        public async Task<List<UserRolesModel>> GetListRolesAndUser()
        {
            IEnumerable<UserRolesModel> results;

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = @"SELECT email, nombre_rol 
              FROM  dbo.usuarios AS a
              LEFT JOIN dbo.usuarios_roles AS b ON b.fk_usuarios = a.id_usuarios
              LEFT JOIN dbo.roles AS c ON c.id_roles = b.fk_roles";
                results = await connection.QueryAsync<UserRolesModel>(query);
            }

            return results.ToList();
        }

        public async Task<long> InsertRol(string rol)
        {
            int results = await GetRol(rol);
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                long id = 0;
                if (results == 0)
                {
                    string queryInsert = "INSERT INTO dbo.roles (nombre_rol) VALUES (@nombre_rol)";
                    var parametrosInsert = new { nombre_rol = rol };
                    id = await connection.ExecuteAsync(queryInsert, parametrosInsert);
                }

                return id;
            }

        }

        public async Task<long> DeleteRol(string rol)
        {
            int results = await GetRol(rol);
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                long id = 0;
                if (results > 0)
                {
                    string queryInsert = "DELETE FROM dbo.roles WHERE nombre_rol = @nombre_rol";
                    var parametrosInsert = new { nombre_rol = rol };
                    id = await connection.ExecuteAsync(queryInsert, parametrosInsert);
                }

                return id;
            }

        }

        public async Task<long> InsertRolByUser(string email, string rol)
        {
            
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                long id = 0;
                string queryInsert = @"
                WITH user_id AS (SELECT id_usuarios FROM dbo.usuarios WHERE email = @email),
                     rol_id AS (SELECT id_roles FROM dbo.roles WHERE nombre_rol = @nombre_rol)
                INSERT INTO dbo.usuarios_roles (fk_usuarios, fk_roles)
                SELECT user_id.id_usuarios, rol_id.id_roles FROM user_id, rol_id";
                var parametrosInsert = new { email = email, nombre_rol = rol };
                id = await connection.ExecuteAsync(queryInsert, parametrosInsert);                

                return id;
            }

        }

        public async Task<long> DeleteRolByUser(string email, string rol)
        {

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                long id = 0;
                string queryInsert = @"
                DELETE FROM dbo.usuarios_roles
                WHERE fk_usuarios = (SELECT id_usuarios FROM dbo.usuarios WHERE email = @email)
                  AND fk_roles = (SELECT id_roles FROM dbo.roles WHERE nombre_rol = @nombre_rol)";
                var parametrosInsert = new { email = email, nombre_rol = rol };
                id = await connection.ExecuteAsync(queryInsert, parametrosInsert);

                return id;
            }

        }

    }
}
