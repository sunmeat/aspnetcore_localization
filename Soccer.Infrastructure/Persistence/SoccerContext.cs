using Microsoft.EntityFrameworkCore;
using Soccer.Domain.Entities; // підключення простору імен з сутностями моделі даних (team та player)

namespace Soccer.Infrastructure.Persistence
{
    public class SoccerContext : DbContext
    {
        public SoccerContext(DbContextOptions<SoccerContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<Player> Player { get; set; }
        public DbSet<Team> Team { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Player>()
                .HasIndex(p => p.Name)
                .IsUnique();

            modelBuilder.Entity<Team>()
                .HasIndex(t => t.Name)
                .IsUnique();

            modelBuilder.Entity<Team>().HasData(
                new Team { Id = 2, Name = "Ліверпуль", Coach = "Арне Слот" },
                new Team { Id = 3, Name = "Динамо Київ", Coach = "Ігор Костюк" },
                new Team { Id = 5, Name = "Барселона", Coach = "Гансі Флік" },
                new Team { Id = 6, Name = "Баварія Мюнхен", Coach = "Вінсент Компані" },
                new Team { Id = 8, Name = "Фенербахче", Coach = "Доменіко Тедеско" },
                new Team { Id = 9, Name = "Парі Сен-Жермен", Coach = "Луїс Енріке" },
                new Team { Id = 10, Name = "Манчестер Сіті", Coach = "Пеп Гвардіола" },
                new Team { Id = 11, Name = "Евертон", Coach = "Девід Моєс" },
                new Team { Id = 12, Name = "Реал Мадрид", Coach = "Сабі Алонсо" },
                new Team { Id = 46, Name = "Борнмут", Coach = "Андоні Іраола" },
                new Team { Id = 1014, Name = "Наполі", Coach = "Антоніо Конте" },
                new Team { Id = 1016, Name = "Інтер Мілан", Coach = "Крістіан Ківу" },
                new Team { Id = 1200, Name = "Аль-Іттіхад", Coach = "Сержіу Консейсау" },
                new Team { Id = 1201, Name = "Аль-Гіляль", Coach = "Сімоне Інзагі" },
                new Team { Id = 1202, Name = "Інтер Маямі", Coach = "Хавьєр Маскерано" }
            );

            modelBuilder.Entity<Player>().HasData(
                new Player { Id = 3, Name = "Роберт Левандовські", Age = 37, Position = "Форвард", TeamId = 5 },
                new Player { Id = 5, Name = "Мохамед Салах", Age = 33, Position = "Форвард", TeamId = 2 },
                new Player { Id = 7, Name = "Ліонель Мессі", Age = 38, Position = "Форвард", TeamId = 9 },
                new Player { Id = 11, Name = "Ерлінг Голанд", Age = 25, Position = "Форвард", TeamId = 10 },
                new Player { Id = 15, Name = "Віталій Миколенко", Age = 26, Position = "Лівий захисник", TeamId = 11 },
                new Player { Id = 20, Name = "Віктор Осімхен", Age = 26, Position = "Форвард", TeamId = 1014 },
                new Player { Id = 21, Name = "Хвіча Кварацхелія", Age = 24, Position = "Лівий вінгер", TeamId = 1014 },
                new Player { Id = 22, Name = "Віталій Буяльський", Age = 32, Position = "Центральний півзахисник", TeamId = 3 },
                new Player { Id = 23, Name = "Карім Бензема", Age = 37, Position = "Форвард", TeamId = 1200 },
                new Player { Id = 24, Name = "Серж Гнабрі", Age = 30, Position = "Правий вінгер", TeamId = 6 },
                new Player { Id = 26, Name = "Ромелу Лукаку", Age = 32, Position = "Форвард", TeamId = 1014 },
                new Player { Id = 27, Name = "Едін Джеко", Age = 39, Position = "Форвард", TeamId = 8 },
                new Player { Id = 28, Name = "Хакан Чалханоглу", Age = 31, Position = "Центральний півзахисник", TeamId = 1016 },
                new Player { Id = 29, Name = "Бенжамен Павар", Age = 29, Position = "Центральний захисник", TeamId = 1016 },
                new Player { Id = 30, Name = "Леон Горецка", Age = 30, Position = "Центральний півзахисник", TeamId = 6 },
                new Player { Id = 32, Name = "Усман Дембеле", Age = 28, Position = "Правий вінгер", TeamId = 9 },
                new Player { Id = 33, Name = "Кіліан Мбаппе", Age = 26, Position = "Форвард", TeamId = 12 },
                new Player { Id = 37, Name = "Кевін Де Брюйне", Age = 34, Position = "Атакувальний півзахисник", TeamId = 10 },
                new Player { Id = 38, Name = "Едер Мілітао", Age = 27, Position = "Центральний захисник", TeamId = 12 },
                new Player { Id = 39, Name = "Вінісіус Жуніор", Age = 25, Position = "Лівий вінгер", TeamId = 12 },
                new Player { Id = 40, Name = "Пьотр Зелінський", Age = 31, Position = "Центральний півзахисник", TeamId = 1016 },
                new Player { Id = 41, Name = "Джуд Беллінгем", Age = 22, Position = "Центральний півзахисник", TeamId = 12 },
                new Player { Id = 42, Name = "Ламін Ямаль", Age = 18, Position = "Правий вінгер", TeamId = 5 },
                new Player { Id = 43, Name = "Філ Фоден", Age = 25, Position = "Атакувальний півзахисник", TeamId = 10 },
                new Player { Id = 45, Name = "Ілля Забарний", Age = 23, Position = "Центральний захисник", TeamId = 46 },
                new Player { Id = 46, Name = "Джамал Мусіяла", Age = 22, Position = "Атакувальний півзахисник", TeamId = 6 }
            );
        }
    }
}
