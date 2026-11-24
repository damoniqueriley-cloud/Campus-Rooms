using CampusRooms.Data;
using CampusRooms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusRooms.Controllers.Api
{
    // The JSON API the timetabling program talks to (Tasks 8 and 9).
    // It sends and receives JSON, never HTML pages, so it is marked as an
    // API controller and inherits from ControllerBase: no views.
    [ApiController]
    [Route("api/rooms")]
    public class RoomsApiController : ControllerBase
    {
        private readonly AppDbContext _db;

        public RoomsApiController(AppDbContext db)
        {
            _db = db;
        }

        // Turn a Room into the flat RoomDto the outside world sees: no
        // navigation properties, so the JSON cannot go round in a circle
        // through Room -> Building -> Rooms.
        private static RoomDto ToDto(Room room) =>
            new RoomDto(room.Id, room.Code, room.Name, room.Seats, room.HasProjector,
                        room.BuildingId, room.Building != null ? room.Building.Name : null);

        // GET /api/rooms: 200 and every room, sorted by code
        [HttpGet]
        public async Task<ActionResult<List<RoomDto>>> GetRooms()
        {
            List<RoomDto> rooms = await _db.Rooms.AsNoTracking()
                .OrderBy(room => room.Code)
                .Select(room => new RoomDto(room.Id, room.Code, room.Name, room.Seats, room.HasProjector,
                                            room.BuildingId, room.Building != null ? room.Building.Name : null))
                .ToListAsync();
            return Ok(rooms);
        }

        // GET /api/rooms/3: 200 and one room, or 404 if there is no such id
        [HttpGet("{id:int}")]
        public async Task<ActionResult<RoomDto>> GetRoom(int id)
        {
            RoomDto? dto = await _db.Rooms.AsNoTracking()
                .Where(room => room.Id == id)
                .Select(room => new RoomDto(room.Id, room.Code, room.Name, room.Seats, room.HasProjector,
                                            room.BuildingId, room.Building != null ? room.Building.Name : null))
                .FirstOrDefaultAsync();
            if (dto == null)
            {
                return NotFound();
            }
            return Ok(dto);
        }

        // POST /api/rooms: add a room. 201 with a Location header and the room
        // as JSON; a broken rule is 400 with the messages; a code another room
        // already has is 409, not a 500. Open without a login: the login cookie
        // is built for browsers, so a program without one would only get a 302
        // to the login page. Locking an API properly needs tokens, which are
        // not part of this lab.
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<RoomDto>> Create(RoomInput input)
        {
            if (!await _db.Buildings.AnyAsync(building => building.Id == input.BuildingId))
            {
                return BadRequest(new { message = "That building does not exist" });
            }
            if (await _db.Rooms.AnyAsync(room => room.Code == input.Code))
            {
                return Conflict(new { message = $"A room with code {input.Code} already exists" });
            }

            Room room = new Room
            {
                Code = input.Code,
                Name = input.Name,
                Seats = input.Seats,
                HasProjector = input.HasProjector,
                BuildingId = input.BuildingId
            };
            _db.Rooms.Add(room);
            await _db.SaveChangesAsync();

            string? building_name = await _db.Buildings
                .Where(building => building.Id == room.BuildingId)
                .Select(building => building.Name)
                .FirstOrDefaultAsync();

            RoomDto dto = new RoomDto(room.Id, room.Code, room.Name, room.Seats, room.HasProjector, room.BuildingId, building_name);

            // 201, with the address of the new room in the Location header
            return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, dto);
        }

        // PUT /api/rooms/2: save the new details. 204 when saved, 404 if there
        // is no such id, 409 for another room's code, 400 for a broken rule.
        [HttpPut("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> Update(int id, RoomInput input)
        {
            Room? room = await _db.Rooms.FindAsync(id);
            if (room == null)
            {
                return NotFound();
            }
            if (await _db.Rooms.AnyAsync(other_room => other_room.Code == input.Code && other_room.Id != id))
            {
                return Conflict(new { message = $"A room with code {input.Code} already exists" });
            }
            if (!await _db.Buildings.AnyAsync(building => building.Id == input.BuildingId))
            {
                return BadRequest(new { message = "That building does not exist" });
            }

            room.Code = input.Code;
            room.Name = input.Name;
            room.Seats = input.Seats;
            room.HasProjector = input.HasProjector;
            room.BuildingId = input.BuildingId;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE /api/rooms/5: 204 when the room goes, 404 if it is already
        // gone. There is no "are you sure?" page: the program that sends
        // DELETE asks its own user.
        [HttpDelete("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> Delete(int id)
        {
            Room? room = await _db.Rooms.FindAsync(id);
            if (room == null)
            {
                return NotFound();
            }
            _db.Rooms.Remove(room);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
