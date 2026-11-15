using System.ComponentModel.DataAnnotations;

namespace CampusRooms.Models
{
    // One college building. A building holds many rooms, and each room
    // points back at the building it is in (see Room.BuildingId).
    public class Building
    {
        public int Id { get; set; }

        [Display(Name = "Building name")]
        [Required(ErrorMessage = "Give the building a name")]
        [StringLength(40, ErrorMessage = "The name is at most 40 characters")]
        public string Name { get; set; } = "";

        // The rooms in this building. The other half of the link:
        // a building's name is stored once here, and every room that
        // points at this building appears in this list.
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}
