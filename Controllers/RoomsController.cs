using CampusRooms.Data;
using CampusRooms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CampusRooms.Controllers
{
    // The rooms pages: list, details, add, edit, delete.
    // Looking at rooms is open to everyone; changing rooms needs the
    // CanManageRooms policy (Admin or Staff), and deleting needs Admin.
    public class RoomsController : Controller
    {
        private readonly AppDbContext _db;

        public RoomsController(AppDbContext db)
        {
            _db = db;
        }

        // The building choices for the Add and Edit dropdowns, A to Z (Task 3)
        private async Task FillBuildingChoicesAsync()
        {
            ViewBag.Buildings = new SelectList(
                await _db.Buildings.OrderBy(building => building.Name).ToListAsync(),
                "Id", "Name");
        }

        // GET /rooms: every room, sorted by code, each with its building.
        // Include brings each building in the same query, not one query per room.
        [HttpGet("rooms")]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Rooms";
            List<Room> rooms = await _db.Rooms
                .Include(room => room.Building)
                .OrderBy(room => room.Code)
                .ToListAsync();
            ViewBag.Count = rooms.Count;
            return View(rooms);
        }

        // GET /rooms/2: one room. /rooms/99 is a 404.
        [HttpGet("rooms/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            Room? room = await _db.Rooms
                .Include(each_room => each_room.Building)
                .FirstOrDefaultAsync(each_room => each_room.Id == id);
            if (room == null)
            {
                return NotFound();
            }
            ViewData["Title"] = room.Code;
            return View(room);
        }

        // GET /rooms/seats/30: rooms with at least 30 seats, largest first (Task 4).
        // The SELECT is written by hand and EF Core runs it as it is; the {min}
        // goes into the SQL as a parameter, never pasted into the text. EF Core
        // then turns the rows back into Room objects, and the Include below
        // joins the building in the same query.
        [HttpGet("rooms/seats/{min:int}")]
        public async Task<IActionResult> Seats(int min)
        {
            ViewData["Title"] = $"Rooms with at least {min} seats";
            List<Room> rooms = await _db.Rooms
                .FromSql($"SELECT * FROM Rooms WHERE Seats >= {min}")
                .Include(room => room.Building)
                .OrderByDescending(room => room.Seats)
                .ToListAsync();
            ViewData["Min"] = min;
            return View(rooms);
        }

        // GET /rooms/add: an empty form. Admin or Staff only.
        [HttpGet("rooms/add")]
        [Authorize(Policy = "CanManageRooms")]
        public async Task<IActionResult> Add()
        {
            ViewData["Title"] = "Add a room";
            await FillBuildingChoicesAsync();
            return View(new Room());
        }

        // POST /rooms/add: save the new room, or show the form again with the messages
        [HttpPost("rooms/add")]
        [Authorize(Policy = "CanManageRooms")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Room new_room)
        {
            // "Pick a building" is the dropdown's first option, so nothing
            // picked arrives as a null BuildingId
            if (new_room.BuildingId == null)
            {
                ModelState.AddModelError("BuildingId", "Pick a building");
            }
            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Add a room";
                await FillBuildingChoicesAsync();
                return View(new_room);
            }

            _db.Rooms.Add(new_room);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // The unique index on Code said no
                ModelState.AddModelError("Code", "Another room already has that code");
                ViewData["Title"] = "Add a room";
                await FillBuildingChoicesAsync();
                return View(new_room);
            }

            TempData["Message"] = $"{new_room.Code} {new_room.Name} was added";
            return RedirectToAction("Index");
        }

        // GET /rooms/edit/2: the form, filled in. /rooms/edit/99 is a 404.
        [HttpGet("rooms/edit/{id:int}")]
        [Authorize(Policy = "CanManageRooms")]
        public async Task<IActionResult> Edit(int id)
        {
            Room? room = await _db.Rooms.FindAsync(id);
            if (room == null)
            {
                return NotFound();
            }
            ViewData["Title"] = "Edit a room";
            await FillBuildingChoicesAsync();
            return View(room);
        }

        // POST /rooms/edit/2: save the changes to the same row
        [HttpPost("rooms/edit/{id:int}")]
        [Authorize(Policy = "CanManageRooms")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Room changed_room)
        {
            if (changed_room.BuildingId == null)
            {
                ModelState.AddModelError("BuildingId", "Pick a building");
            }
            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Edit a room";
                await FillBuildingChoicesAsync();
                return View(changed_room);
            }

            Room? room = await _db.Rooms.FindAsync(id);
            if (room == null)
            {
                TempData["Message"] = "That room was deleted before you saved";
                return RedirectToAction("Index");
            }
            room.Code = changed_room.Code;
            room.Name = changed_room.Name;
            room.Seats = changed_room.Seats;
            room.HasProjector = changed_room.HasProjector;
            room.BuildingId = changed_room.BuildingId;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("Code", "Another room already has that code");
                ViewData["Title"] = "Edit a room";
                await FillBuildingChoicesAsync();
                return View(changed_room);
            }

            TempData["Message"] = $"Saved {room.Code}";
            return RedirectToAction("Index");
        }

        // GET /rooms/delete/2: ask first. Nothing is deleted yet. Admin only.
        [HttpGet("rooms/delete/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            Room? room = await _db.Rooms.FindAsync(id);
            if (room == null)
            {
                return NotFound();
            }
            ViewData["Title"] = "Delete a room";
            return View(room);
        }

        // POST /rooms/delete/2: the Yes button. Now the row goes. Admin only.
        [HttpPost("rooms/delete/{id:int}")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Room? room = await _db.Rooms.FindAsync(id);
            if (room == null)
            {
                TempData["Message"] = "That room was already deleted";
                return RedirectToAction("Index");
            }
            _db.Rooms.Remove(room);
            await _db.SaveChangesAsync();
            TempData["Message"] = $"{room.Code} {room.Name} was deleted";
            return RedirectToAction("Index");
        }
    }
}
