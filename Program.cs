using backend.Models;
using backend.Utils;
using backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

var jwtKey = "minha-chave-super-ultra-secreta-mesmo-veridico-2025-123456"; //adicionar como variavel de ambiente depois

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorizationBuilder().AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

// Adiciona o DbContext ao serviço
var dbPath = Environment.GetEnvironmentVariable("DB_PATH") ?? "app.db";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Adiciona CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://davipac.github.io")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});


// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/login", (UserRequest login, AppDbContext db) =>
{
    var user = db.Usuarios.FirstOrDefault(u => u.Username == login.Username && u.Password == login.Password);
    if (user == null)
        return Results.Unauthorized();

    var claims = new[]
    {
        new Claim(ClaimTypes.Name, user.Username),
        new Claim("role", user.Role)
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: creds);

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(new { token = tokenString });
});

app.MapPost("/Register", (UserRequest user, AppDbContext dbContext) =>
{
    if (RegisterFn.Register(user, dbContext))
    {
        return Results.Ok(new { message = "Usuário registrado com sucesso" });
    }
    return Results.BadRequest(new { message = "Usuário já existe" });
});

app.MapGet("/perfil", (ClaimsPrincipal user) =>
{
    if (user.Identity == null || !user.Identity.IsAuthenticated)
        return Results.Unauthorized();

    var username = user.Identity.Name ?? "";
    var role = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "User";

    return Results.Ok(new { username, role });
}).RequireAuthorization();

app.MapGet("/users", (AppDbContext dbContext) =>
{
    var users = dbContext.Usuarios.ToList();
    return Results.Ok(users);
}).RequireAuthorization("AdminOnly");

app.MapGet("/user", (AppDbContext dbContext, string username) =>
{
    var user = dbContext.Usuarios.FirstOrDefault(u => u.Username == username);
    if (user == null)
        return Results.NotFound();
    return Results.Ok(user);
}).RequireAuthorization("AdminOnly");

app.MapPost("/promote", (AppDbContext dbContext, string username) =>
{
    var user = dbContext.Usuarios.FirstOrDefault(u => u.Username == username);
    if (user == null)
        return Results.NotFound();
    if (user.Role == "Admin")
        return Results.BadRequest(new { message = "Usuário já é Admin" });
    user.Role = "Admin";
    dbContext.SaveChanges();
    return Results.Ok(new { message = "Usuário promovido com sucesso" });
}).RequireAuthorization("AdminOnly");

app.MapPost("/demote", (AppDbContext dbContext, string username) =>
{
    var user = dbContext.Usuarios.FirstOrDefault(u => u.Username == username);
    if (user == null)
        return Results.NotFound();
    if (user.Role == "User")
        return Results.BadRequest(new { message = "Usuário já é User" });
    user.Role = "User";
    dbContext.SaveChanges();
    return Results.Ok(new { message = "Usuário rebaixado com sucesso" });
}).RequireAuthorization("AdminOnly");

app.MapDelete("/user", (AppDbContext dbContext, string username) =>
{
    var user = dbContext.Usuarios.FirstOrDefault(u => u.Username == username);
    if (user == null)
        return Results.NotFound();
    dbContext.Usuarios.Remove(user);
    dbContext.SaveChanges();
    return Results.Ok(new { message = "Usuário excluído com sucesso" });
}).RequireAuthorization("AdminOnly");

app.MapGet("/ranking", (AppDbContext dbContext) =>
{
    var rankings = new Ranking(dbContext);
    return Results.Ok(rankings.ranks);
}
).RequireAuthorization();

app.MapPost("/change-username", (AppDbContext dbContext, string username, string newUsername) =>
{
    var user = dbContext.Usuarios.FirstOrDefault(u => u.Username == username);
    if (user == null)
        return Results.NotFound();
    if (dbContext.Usuarios.Any(u => u.Username == newUsername))
        return Results.BadRequest(new { message = "Nome de usuário já existe" });
    user.Username = newUsername;
    dbContext.SaveChanges();
    return Results.Ok(new { message = "Usuário alterado com sucesso" });
}).RequireAuthorization("AdminOnly");

app.MapPost("/change-password", (AppDbContext dbContext, string username, string newPassword) =>
{
    var user = dbContext.Usuarios.FirstOrDefault(u => u.Username == username);
    if (user == null)
        return Results.NotFound();
    user.Password = newPassword;
    dbContext.SaveChanges();
    return Results.Ok(new { message = "Senha alterada com sucesso" });
}).RequireAuthorization("AdminOnly");

app.MapPost("/set-points", (AppDbContext dbContext, string username, int points) =>
{
    var user = dbContext.Usuarios.FirstOrDefault(u => u.Username == username);
    if (user == null)
        return Results.NotFound();
    user.Pontuacao = points;
    dbContext.SaveChanges();
    return Results.Ok(new { message = "Pontuação alterada com sucesso" });
}).RequireAuthorization("AdminOnly");

app.Urls.Add("http://0.0.0.0:80");

app.Run();