namespace backend.Models
{
    public class RegistrarTorneioRequest
    {
        public int TorneioId { get; set; }
        public required string Nome { get; set; }
        public required List<string> Usernames { get; set; }
    }
}