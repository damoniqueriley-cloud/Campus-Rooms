using System.ComponentModel.DataAnnotations;

namespace CampusRooms.Models
{
    // What a program sends to POST or PUT /api/rooms (Task 9):
    // the same rules as Room, a required building, and no Id.
    public class RoomInput
    {
        [Required(ErrorMessage = "Give the room code, e.g. MB-101")]
        [StringLength(10, ErrorMessage = "The room code is at most 10 characters")]
        public string Code { get; set; } = "";

        [Required(ErrorMessage = "Give the room a name")]
        [StringLength(40, ErrorMessage = "The name is at most 40 characters")]
        public string Name { get; set; } = "";

        [Range(1, 300, ErrorMessage = "Seats must be from 1 to 300")]
        public int Seats { get; set; }

        public bool HasProjector { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A building is required")]
        public int BuildingId { get; set; }
    }
}
