using El1teSpr1ntTrack.Infrastructure.Commerce;

namespace El1teSpr1ntTrack.UnitTests;

public sealed class UsPhoneNumberTests
{
    [Theory]
    [InlineData("(412) 555-0100", "+14125550100")]
    [InlineData("412.555.0100", "+14125550100")]
    [InlineData("1-412-555-0100", "+14125550100")]
    [InlineData("+1 412 555 0100", "+14125550100")]
    public void TryNormalize_FormatsValidUsNumbersAsE164(string input, string expected)
    {
        Assert.True(UsPhoneNumber.TryNormalize(input, out var normalized));
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("555-0100")]
    [InlineData("012-555-0100")]
    [InlineData("412-155-0100")]
    [InlineData("+44 20 7946 0958")]
    [InlineData("412-CALL-NOW")]
    public void TryNormalize_RejectsMalformedOrNonUsNumbers(string input)
    {
        Assert.False(UsPhoneNumber.TryNormalize(input, out _));
    }
}
