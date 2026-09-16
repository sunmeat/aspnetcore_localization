using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Soccer.Domain.Interfaces;
using Soccer.Infrastructure.Persistence;
using Soccer.Infrastructure.Repositories;

namespace Soccer.Infrastructure.DependencyInjection
{
    /// <summary>
    /// У початковому тришаровому прикладі реєстрація SoccerContext і UnitOfWork
    /// лежала у BLL, бо DAL (бібліотека класів) не мала доступу до IServiceCollection.
    /// У Clean Architecture для цього є окремий шар Infrastructure — саме тут і
    /// живе вся "технічна" реєстрація: DbContext, підключення до SQL Server,
    /// репозиторії, Unit of Work. Presentation (композиційний корінь) лише
    /// викликає AddInfrastructure(...) у Program.cs.
    /// </summary>
    public static class InfrastructureServiceExtensions
    {
        public static void AddInfrastructure(this IServiceCollection services, string? connection)
        {
            services.AddDbContext<SoccerContext>(options =>
                options.UseSqlServer(connection)); // реєструємо контекст для роботи з sql server

            services.AddScoped<IUnitOfWork, EFUnitOfWork>(); // реєструємо unit of work з реалізацією на entity framework
        }
    }
}