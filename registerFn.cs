using backend.Models;
using Microsoft.EntityFrameworkCore;
using backend.Data;


namespace backend.Utils
{
    public static class RegisterFn
    {
        public static bool Register(UserRequest user, AppDbContext dbContext)
        {
            var usuarios = dbContext.Usuarios;
            //Checa se existe usuario com esse nome
            if (usuarios.Any(u => u.Username == user.Username))
            {
                return false;
            }
            //Adiciona usuario
            usuarios.Add(new Usuario
            {
                Username = user.Username,
                Password = user.Password
            });
            dbContext.SaveChanges();
            return true;
        }
    }
}