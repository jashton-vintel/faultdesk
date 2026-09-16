using FaultDesk.Domain.Common;
using FaultDesk.Domain.Vehicles;

namespace FaultDesk.Domain.Tests.Vehicles;

public class VehicleRegistrationTests
{
    [Theory]
    [InlineData("ab12 cde", "AB12CDE")]
    [InlineData(" AB12-CDE ", "AB12CDE")]
    [InlineData("AB12CDE", "AB12CDE")]
    [InlineData("a123 bcd", "A123BCD")]
    public void Parse_normalises_case_spaces_and_hyphens(string input, string expected)
    {
        var registration = VehicleRegistration.Parse(input);

        Assert.Equal(expected, registration.Value);
    }

    [Theory]
    [InlineData("AB12CDE", "AB12 CDE")]
    [InlineData("A123BCD", "A123 BCD")]
    [InlineData("ABC123D", "ABC 123D")]
    [InlineData("1234AB", "1234AB")]
    public void Display_inserts_the_conventional_space(string input, string expected)
    {
        Assert.Equal(expected, VehicleRegistration.Parse(input).Display);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("AB12CDEF")]
    [InlineData("AB!2CDE")]
    [InlineData("ABCDEFG")]
    [InlineData("1234567")]
    [InlineData("A")]
    public void Parse_rejects_invalid_input(string input)
    {
        Assert.Throws<DomainException>(() => VehicleRegistration.Parse(input));
        Assert.False(VehicleRegistration.TryParse(input, out _, out var error));
        Assert.NotEmpty(error);
    }

    [Fact]
    public void Registrations_are_compared_by_normalised_value()
    {
        Assert.Equal(VehicleRegistration.Parse("ab12 cde"), VehicleRegistration.Parse("AB12CDE"));
        Assert.NotEqual(VehicleRegistration.Parse("AB12CDE"), VehicleRegistration.Parse("AB12CDF"));
    }
}
