using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class Time
    {
        public int Id { get; set; }
        public required string Nome { get; set; }
        public required List<Usuario> Jogadores { get; set; }
    }
    public class Partida
    {
        public int Id { get; set; }
        public required Time? TimeA { get; set; }
        public required Time? TimeB { get; set; }
        public Time? Vencedor { get; set; } = null;
        public Time? Perdedor { get; set; } = null;
    }
    public class Rodada
    {
        public required List<Partida> Partidas { get; set; }
    }

    public class Torneio
    {
        private class Chaveamento
        {
            public int Rodada { get; set; } = 0;
            int NumRodadas { get; set; }
            public List<Rodada> Rodadas { get; set; } = [];
            public static void FimPartida(Partida partida, Time vencedor)
            {
                partida.Vencedor = vencedor;
                partida.Perdedor = vencedor == partida.TimeA ? partida.TimeB : partida.TimeA;
            }
            public void Update()
            {
                // Atualiza as rodadas
                for (int i = 0; i < Rodadas.Count; i++)
                {
                    // Atualiza as partidas
                    for (int j = 0; j < Rodadas[i].Partidas.Count; j++)
                    {
                        // Verifica se a partida ja foi finalizada
                        if (Rodadas[i].Partidas[j].Vencedor != null)
                        {
                            if (j % 2 != 0 ) Rodadas[i + 1].Partidas[j / 2].TimeA = Rodadas[i].Partidas[j].Vencedor;
                            else Rodadas[i + 1].Partidas[j / 2].TimeB = Rodadas[i].Partidas[j].Vencedor;
                        }
                        var partida = Rodadas[i].Partidas[j];
                        string nomePerdedor = $"Perdedor {partida.TimeA?.Nome} x {partida.TimeB?.Nome}";
                        // Procurar partida com esse nome
                        foreach (var rodada in Rodadas)
                        {
                            foreach (var perdedorPartida in rodada.Partidas)
                            {
                                if (perdedorPartida.TimeA?.Nome == nomePerdedor)
                                {
                                    perdedorPartida.TimeA = partida.Perdedor;
                                }
                                else if (perdedorPartida.TimeB?.Nome == nomePerdedor)
                                {
                                    perdedorPartida.TimeB = partida.Perdedor;
                                }
                            }
                        }
                    }
                }
            }
            public Chaveamento(List<Time> times)
            {
                // Embaralhar times
                Random random = new();
                var timesEmbaralhados = times.OrderBy(t => random.Next()).ToList();
                // Calcula numero de "times" vazios para que tenha o numero de times necessario
                NumRodadas = 1;
                while (NumRodadas < times.Count)
                    NumRodadas *= 2;
                int nulos = NumRodadas - times.Count;
                for (int i = 0; i < nulos; i++)
                {
                    timesEmbaralhados.Add(new Time()
                    {
                        Nome = $"Perdedor {timesEmbaralhados[i * 2].Nome} x {timesEmbaralhados[(i * 2) + 1].Nome}",
                        Jogadores = []
                    });
                }
                Rodada PrimeiraRodada = new()
                {
                    Partidas = []
                };
                for (int i = 0; i < timesEmbaralhados.Count; i++)
                {
                    Partida partida = new()
                    {
                        TimeA = timesEmbaralhados[i],
                        TimeB = timesEmbaralhados[i + 1]
                    };
                    PrimeiraRodada.Partidas.Add(partida);
                }
                Rodadas.Add(PrimeiraRodada);
                int numTimes = PrimeiraRodada.Partidas.Count * 2;
                while (numTimes > 1)
                {
                    Rodada rodada = new()
                    {
                        Partidas = []
                    };
                    for (int i = 0; i < numTimes; i += 2)
                    {
                        rodada.Partidas.Add(new()
                        {
                            TimeA = Rodadas[^1].Partidas[i].Vencedor,
                            TimeB = Rodadas[^1].Partidas[i + 1].Vencedor
                        });
                    }
                    Rodadas.Add(rodada);
                    numTimes /= 2;
                }
            }
        }
        public int Id { get; set; }
        public string Type { get; set; } = "single";
        public required string Nome { get; set; }
        public required DateTime Data { get; set; }
        public List<Time> Times { get; set; } = [];

        [NotMapped]
        private Chaveamento? Chave { get; set; } = null;
        public bool HasStarted { get; set; } = false;
        public void SoftReset()
        {
            Rodada primeiraRodada = GetRodadas()[0];
            GerarChaveamento();
            if (Chave != null)
            {
                Chave.Rodadas[0] = primeiraRodada;
                foreach (Partida partida in Chave.Rodadas[0].Partidas)
                {
                    partida.Vencedor = null;
                    partida.Perdedor = null;
                }
                UpdateChaveamento();
            }
        }
        public void Reset()
        {
            HasStarted = false;
            Chave = null;
        }
        public void FullReset()
        {
            Times.Clear();
            HasStarted = false;
            Chave = null;
        }
        public bool IsRegistered(Usuario user)
        {
            return Times.Any(t => t.Jogadores.Any(j => j == user));
        }
        public int RegistrarTime(string nome, List<Usuario> jogadores)
        {
            if (HasStarted)
                return -1;
            if (Times.Any(t => t.Nome == nome))
                return -2;
            if (Times.Any(t => t.Jogadores.Any(j => jogadores.Contains(j))))
                return -3;
            Times.Add(new()
            {
                Nome = nome,
                Jogadores = jogadores
            });
            return 0;
        }
        public void FimPartida(Partida partida, Time time)
        {
            Chaveamento.FimPartida(partida, time);
            UpdateChaveamento();
        }
        public List<Rodada> GetRodadas()
        {
            return Chave?.Rodadas ?? [];
        }
        public List<Partida> GetPartidas()
        {
            List<Partida> partidas = [];
            foreach (Rodada rodada in GetRodadas())
            {
                partidas.AddRange(rodada.Partidas);
            }
            return partidas;
        }
        public List<Partida> GetPendingPartidas()
        {
            List<Partida> pendingPartidas = [];
            foreach (Partida partida in GetPartidas())
            {
                if (partida.Vencedor == null && partida.TimeA != null && partida.TimeB != null && partida.TimeA.Jogadores.Count > 0 && partida.TimeB.Jogadores.Count > 0)
                {
                    pendingPartidas.Add(partida);
                }
            }
            return pendingPartidas;
        }


        public void UpdateChaveamento()
        {
            if (Chave == null)
            {
                Chave = new Chaveamento(Times);
                HasStarted = true;;
            }
            else
            {
                Chave.Update();
            }
        }
        public void GerarChaveamento()
        {
            Chave = new Chaveamento(Times);
            HasStarted = true;
        }
        public object ToJqueryBracketFormat()
        {
            if (Chave == null || Chave.Rodadas.Count == 0)
                return new { teams = new List<List<string>>(), results = new List<List<List<int?>>>() };

            var teams = new List<List<string>>();
            var results = new List<List<List<int?>>>();

            // Primeira rodada - monta os times
            var primeiraRodada = Chave.Rodadas[0];
            foreach (var partida in primeiraRodada.Partidas)
            {
                teams.Add(new List<string> {
                    partida.TimeA?.Nome ?? "???",
                    partida.TimeB?.Nome ?? "???"
                });
            }

            // Para cada rodada monta os resultados
            foreach (var rodada in Chave.Rodadas)
            {
                var resultadoRodada = new List<List<int?>>();

                foreach (var partida in rodada.Partidas)
                {
                    if (partida.Vencedor != null && partida.TimeA != null && partida.TimeB != null)
                    {
                        // 1 para vencedor, 0 para perdedor na ordem TimeA, TimeB
                        int a = partida.Vencedor.Nome == partida.TimeA.Nome ? 1 : 0;
                        int b = 1 - a;
                        resultadoRodada.Add(new List<int?> { a, b });
                    }
                    else
                    {
                        resultadoRodada.Add(new List<int?> { null, null });
                    }
                }

                results.Add(resultadoRodada);
            }

            return new
            {
                teams,
                results
            };
        }
    }
}