using Maranny.Core.Entities;
using Maranny.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Maranny.Api.Controllers
{
    [ApiController]
    [Route("api/sports")]
    public class SportsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public SportsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var sports = await _db.Sports
                .OrderBy(s => s.Name)
                .Select(s => new { s.Id, s.Name })
                .ToListAsync();
            return Ok(sports);
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateSportDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { error = "Sport name is required" });
            {
                var sport = new Sport
                {
                    Name = dto.Name
                };
                _db.Sports.Add(sport);
                await _db.SaveChangesAsync();
                return Ok(new { sport.Id, sport.Name });
            }
        }

        // DTO defined here temporarily
        public class CreateSportDto
        {
            public string Name { get; set; } = string.Empty;
        }
    }
}