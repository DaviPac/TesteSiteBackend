using backend.Models;
using backend.Data;

namespace backend.Utils
{
    public static class LoginFn
    {
        public static bool Validate(UserRequest login, AppDbContext dbContext)
        {
            var usuarios = dbContext.Usuarios;
            //Checa se existe usuario com esse nome e senha
            if (usuarios.Any(u => u.Username == login.Username && u.Password == login.Password))
            {
                return true;
            }
            return false;
        }
    }
}