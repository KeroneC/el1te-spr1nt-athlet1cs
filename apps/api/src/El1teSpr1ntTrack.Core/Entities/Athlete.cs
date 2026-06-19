namespace El1teSpr1ntTrack.Core.Entities;

public sealed class Athlete : EntityBase
{
    public Guid ParentUserId { get; set; }

    public User? ParentUser { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public ICollection<ConsentRecord> ConsentRecords { get; set; } = new List<ConsentRecord>();

    public ICollection<AthleteDocument> AthleteDocuments { get; set; } = new List<AthleteDocument>();
}
