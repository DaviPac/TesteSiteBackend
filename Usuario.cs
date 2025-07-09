namespace backend.Models;

public class Usuario
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public string Role { get; set; } = "User"; // ou "Admin"
    public int Pontuacao { get; set; } = 0;
}
