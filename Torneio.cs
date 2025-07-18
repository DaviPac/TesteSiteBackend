using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace backend.Models
{
    public class Time
    {
        public int Id { get; set; }
        public required List<int> IdsDeUsuario { get; set; }
        public required string Nome { get; set; }

        public int OrdemNoTorneio { get; set; }

        public int TorneioId { get; set; }
        [JsonIgnore]
        public Torneio Torneio { get; set; } = null!;
    }

    public class Jogo
    {
        public string Id { get; set; } = null!;
        public int? Vencedor { get; set; }

        public Time? Time1 { get; set; }
        public Time? Time2 { get; set; }
    }
    public class Torneio
    {
        public int Id { get; set; }
        public required string Nome { get; set; }
        public required List<Time> Times { get; set; }
        public required DateTime Data { get; set; }

        public required string Type { get; set; }

        public string? ChaveamentoJson { get; set; } // Armazena resultados como JSON
        public DateTime CriadoEm { get; set; } = DateTime.Now;

        public void Iniciar()
        {
            var random = new Random();
            Times = Times.OrderBy(x => random.Next()).ToList();
            for (int i = 0; i < Times.Count; i++)
            {
                Times[i].OrdemNoTorneio = i;
            }

            var rodadas = CriarChaveamentoInicial();
            ChaveamentoJson = JsonSerializer.Serialize(rodadas);
        }

        public List<List<Jogo>> CriarChaveamentoInicial()
        {
            // Suporte apenas para potências de 2
            int n = Times.Count;
            if ((n & (n - 1)) != 0)
                throw new InvalidOperationException("O número de times deve ser potência de 2");

            var rodada0 = new List<Jogo>();
            for (int i = 0; i < n; i += 2)
            {
                rodada0.Add(new Jogo
                {
                    Id = Guid.NewGuid().ToString(),
                    Time1 = Times[i],
                    Time2 = Times[i + 1],
                    Vencedor = null
                });
            }

            return new List<List<Jogo>> { rodada0 }; // Primeira rodada
        }

        public void SetVencedor(string jogoId, int timeVencedorId)
        {
            if (string.IsNullOrWhiteSpace(ChaveamentoJson)) return;

            var rodadas = JsonSerializer.Deserialize<List<List<Jogo>>>(ChaveamentoJson!)!;
            foreach (var rodada in rodadas)
            {
                foreach (var jogo in rodada)
                {
                    if (jogo.Id == jogoId)
                    {
                        jogo.Vencedor = timeVencedorId;
                        AtualizarProximaRodada(rodadas, jogo);
                        ChaveamentoJson = JsonSerializer.Serialize(rodadas);
                        return;
                    }
                }
            }
        }

        private void AtualizarProximaRodada(List<List<Jogo>> rodadas, Jogo jogoAtual)
        {
            int rodadaIndex = rodadas.FindIndex(r => r.Contains(jogoAtual));
            int posicao = rodadas[rodadaIndex].IndexOf(jogoAtual);

            // Criar próxima rodada se ainda não existe
            if (rodadas.Count == rodadaIndex + 1)
            {
                int jogosNaProximaRodada = rodadas[rodadaIndex].Count / 2;
                var novaRodada = new List<Jogo>();
                for (int i = 0; i < jogosNaProximaRodada; i++)
                {
                    novaRodada.Add(new Jogo
                    {
                        Id = Guid.NewGuid().ToString()
                    });
                }
                rodadas.Add(novaRodada);
            }

            var proximoJogo = rodadas[rodadaIndex + 1][posicao / 2];
            if (posicao % 2 == 0)
                proximoJogo.Time1 = GetTimeById(jogoAtual.Vencedor!.Value);
            else
                proximoJogo.Time2 = GetTimeById(jogoAtual.Vencedor!.Value);
        }

        private Time GetTimeById(int id)
        {
            return Times.First(t => t.Id == id);
        }

        public object ExportarParaFront()
        {
            if (string.IsNullOrWhiteSpace(ChaveamentoJson))
                return new { teams = new string[][] { }, results = new object[] { } };

            var rodadas = JsonSerializer.Deserialize<List<List<Jogo>>>(ChaveamentoJson!)!;

            var orderedTimes = Times
                .OrderBy(t => t.OrdemNoTorneio)
                .ToList();
            var teams = new List<List<string>>();
            for (int i = 0; i < orderedTimes.Count; i += 2)
            {
                teams.Add(new List<string> { orderedTimes[i].Nome, orderedTimes[i + 1].Nome });
            }

            var results = rodadas
                .Select(r => r.Select(j => new List<int?> {
                    j.Vencedor == j.Time1?.Id ? 1 : j.Vencedor == j.Time2?.Id ? 0 : null,
                    j.Vencedor == j.Time2?.Id ? 1 : j.Vencedor == j.Time1?.Id ? 0 : null
                }).ToArray()).ToArray();

            return new { teams, results };
        }
    }
}