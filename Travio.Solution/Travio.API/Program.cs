
using Microsoft.EntityFrameworkCore;
using Travio.API.Middleware;
using Travio.Core.Domain.Entities.Account_Mangement;
using Travio.Infrastructure;
namespace Travio.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
            builder.Services.AddDbContext<ApplicationDbContext>(options => // add the ApplicationDbContext to the DI container
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")); // configure the context to use SQL Server with the connection string from appsettings.json
            });
            builder.Services.AddIdentity<ApplicationUser, ApplicationRole>().AddEntityFrameworkStores<ApplicationDbContext>();
            var app = builder.Build();
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }


            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
