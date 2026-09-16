using AutoMapper;
using Soccer.Application.DTO;
using Soccer.Application.Interfaces;
using Soccer.Common.Exceptions;
using Soccer.Domain.Entities;
using Soccer.Domain.Interfaces;

namespace Soccer.Application.Services
{
    public class TeamService : IEntityService<TeamDTO>
    {
        private IUnitOfWork Database { get; set; } // юніт оф ворк для доступу до репозиторіїв
        private readonly IMapper mapper;

        public TeamService(IUnitOfWork uow, IMapper mapper)
        {
            Database = uow;
            this.mapper = mapper;
        }

        public async Task Create(TeamDTO teamDto)
        {
            var team = new Team
            {
                Id = teamDto.Id,
                Name = teamDto.Name,
                Coach = teamDto.Coach
            };

            await Database.Teams.Create(team); // створюємо сутність команди
            await Database.Save(); // зберігаємо зміни
        }

        public async Task Update(TeamDTO teamDto)
        {
            var team = new Team
            {
                Id = teamDto.Id,
                Name = teamDto.Name,
                Coach = teamDto.Coach
            };

            Database.Teams.Update(team); // оновлюємо сутність
            await Database.Save(); // зберігаємо зміни
        }

        public async Task Delete(int id)
        {
            await Database.Teams.Delete(id); // видаляємо команду за ідентифікатором
            await Database.Save(); // зберігаємо зміни
        }

        public async Task<TeamDTO> Get(int id)
        {
            var team = await Database.Teams.Get(id);
            if (team == null)
                throw new ValidationException("Немає такого клуба!"); // викидаємо виключення, якщо команду не знайдено

            return new TeamDTO
            {
                Id = team.Id,
                Name = team.Name,
                Coach = team.Coach
            };
        }

        public async Task<IEnumerable<TeamDTO>> GetAll()
        {
            return mapper.Map<IEnumerable<TeamDTO>>(await Database.Teams.GetAll()); // мапимо всі сутності команд на dto
        }
    }
}