namespace backend.Models
{
    public class MudarTorneioRequest
    {
        public required int TorneioId { get; set; }
        public required string Nome { get; set; }
        public required string Data { get; set; }
    }
}