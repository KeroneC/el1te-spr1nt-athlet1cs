namespace El1teSpr1ntTrack.IntegrationTests;

public class ApiProjectReferenceTests
{
    [Fact]
    public void ProgramType_IsAvailableToIntegrationTests()
    {
        Assert.Equal("Program", typeof(Program).Name);
    }
}
