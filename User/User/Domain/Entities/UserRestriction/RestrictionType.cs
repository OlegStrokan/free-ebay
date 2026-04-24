namespace Domain.Entities.UserRestriction;

public enum RestrictionType
{
    Restricted = 0, // read-only: no orders or listings
    Banned = 1,     // cannot use the service at all
}
