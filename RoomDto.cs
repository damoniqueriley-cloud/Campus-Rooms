namespace CampusRooms.Models
{
    // One room as the API sends it (Task 8): flat JSON fields, no navigation
    // properties. A Room points to its Building, which points back at its
    // rooms, so sending the entity as it is would go round in a circle and
    // the request would fail with a 500.
    public record RoomDto(int Id, string Code, string Name, int Seats, bool HasProjector, int? BuildingId, string? BuildingName);
}
