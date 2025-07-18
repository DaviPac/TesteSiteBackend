using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Data;

public class AppDbContext : DbContext
{
    public DbSet<Usuario> Usuarios { get; set; }

    public DbSet<Torneio> Torneios { get; set; }

    public DbSet<Time> Times { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

}
