using backend.Models;

namespace backend.Utils
{
    public static class LoginFn
    {
        public static bool Validate(LoginRequest login)
        {
            return login.Username == "DaviPac" && login.Password == "Pires2001";
        }
    }
}