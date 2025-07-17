namespace backend.Models
{
    public class TorneioRequest
    {
        public required string Nome { get; set; }
        public required string Data { get; set; }
        public required string Type { get; set; }
    }
}