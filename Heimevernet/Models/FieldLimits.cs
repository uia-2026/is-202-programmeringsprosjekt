namespace Heimevernet.Models;

/// <summary>
/// Maximum lengths shared by validation attributes and database columns, so the two cannot drift apart.
/// </summary>
public static class FieldLimits
{
    public const int Username = 50;
    public const int Email = 254;
    public const int PasswordHash = 256;
    public const int ActorType = 100;
    public const int TwoFactorSecret = 128;

    public const int Title = 150;
    public const int Description = 1000;
    public const int Region = 100;
    public const int ContactPoint = 200;

    public const int CategoryName = 100;
    public const int StorageKey = 512;
    public const int EnumText = 32;
}