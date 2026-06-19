namespace El1teSpr1ntTrack.Core.Entities;

public sealed class Coach : CmsEntityBase
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Bio { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public string? Email { get; set; }

    public bool IsEmailPublic { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
