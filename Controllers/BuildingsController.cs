using CampusRooms.Data;
using CampusRooms.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusRooms.Controllers
{
    // The building pages (Task 2): every building, and one building with
    // the rooms in it. Looking at buildings is open to everyone.
    public class BuildingsController : Controller
    {
        private readonly AppDbContext _db;

        public BuildingsController(AppDbContext db)
        {
            _db = db;
        }

        // GET /buildings: every building, A to Z, with its rooms counted
        [HttpGet("buildings")]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Buildings";
            List<Building> buildings = await _db.Buildings
                .Include(building => building.Rooms)
                .OrderBy(building => building.Name)
                .ToListAsync();
            return View(buildings);
        }

        // GET /buildings/2: one building and the rooms in it, in one query.
        // {id:int} means the route only matches a whole number, so
        // /buildings/99 is a 404 here and /buildings/abc is a 404 too.
        [HttpGet("buildings/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            Building? building = await _db.Buildings
                .Include(each_building => each_building.Rooms)
                .FirstOrDefaultAsync(each_building => each_building.Id == id);
            if (building == null)
            {
                return NotFound();
            }
            ViewData["Title"] = building.Name;
            return View(building);
        }
    }
}
