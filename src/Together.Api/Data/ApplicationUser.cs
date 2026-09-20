using Microsoft.AspNetCore.Identity;

namespace Together.Api.Data;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public ICollection<TripEntity> Trips { get; } = [];
}
