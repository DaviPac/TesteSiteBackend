using backend.Data;

namespace backend.Models
{

    public class rank {
        public int position { get; set; }
        public required string username { get; set; }
        public required int pontuacao { get; set; }
    }

    public class Ranking {
        public List<rank> ranks { get; set; }

        public void sort() {
            ranks.Sort((a, b) => b.pontuacao.CompareTo(a.pontuacao));
            for (int i = 0; i < ranks.Count; i++)
            {
                ranks[i].position = i + 1;
            }
        }

        public Ranking(AppDbContext dbContext) {
            ranks = new List<rank>();
            var usuarios = dbContext.Usuarios.OrderByDescending(u => u.Pontuacao).ToList();
            foreach (var usuario in usuarios)
            {
                ranks.Add(new rank { username = usuario.Username, pontuacao = usuario.Pontuacao });
            }
            sort();
        }

    }
}