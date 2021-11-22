using DbModels;
using Microsoft.EntityFrameworkCore;
using Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

string connString = builder.Configuration["ConnectionStrings:Data"];
builder.Services.AddDbContext<UserDbContext>(options => options.UseMySql(connString, ServerVersion.AutoDetect(connString)));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapPost("/login", async (Models.User User, [FromServices] UserDbContext ctx) =>
{
    var user = await ctx.Users.SingleOrDefaultAsync(x => x.Login == User.Login && x.PasswordHash.SequenceEqual(User.PasswordHash));
    if (user == null) return Results.Unauthorized();
    return Results.Ok(user.Id);
})
.WithName("Login");

app.Run();

namespace Models
{
    public record User(string Login, string Password)
    {
        public byte[] PasswordHash => SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(Password));
    }
}

namespace DbModels
{
    public class User
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public byte[] PasswordHash { get; set; }
    }

    public class UserDbContext : DbContext
    {
        public UserDbContext()
        {
        }

        public UserDbContext(DbContextOptions<UserDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseMySql("Name=Data", ServerVersion.AutoDetect("Name=Data"));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbModels.User>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Login)
                .IsRequired();
            });
        }

        public DbSet<DbModels.User> Users { get; set; }
    }
}