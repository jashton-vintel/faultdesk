using FaultDesk.Application.Tickets;
using FaultDesk.Domain.Common;
using FaultDesk.Domain.Vehicles;
using Microsoft.Extensions.Logging.Abstractions;

namespace FaultDesk.Application.Tests.Tickets;

public class ClarifyingQuestionsTests
{
    private static readonly VehicleInput Focus = new("Ford", "Focus", null, 2018, 999, FuelType.Petrol);

    [Fact]
    public async Task Returns_the_questions_the_service_suggests()
    {
        var service = new FakeClarifyingQuestionService();
        service.Questions.AddRange(["When does it happen?", "Any warning lights?"]);
        var handler = new SuggestClarifyingQuestionsHandler(service, NullLogger<SuggestClarifyingQuestionsHandler>.Instance);

        var questions = await handler.HandleAsync(Focus, "Rattle from the front over bumps.", CancellationToken.None);

        Assert.Equal(["When does it happen?", "Any warning lights?"], questions);
    }

    [Fact]
    public async Task Service_failure_means_no_questions_not_an_error()
    {
        var service = new FakeClarifyingQuestionService { Throw = new HttpRequestException("down") };
        var handler = new SuggestClarifyingQuestionsHandler(service, NullLogger<SuggestClarifyingQuestionsHandler>.Instance);

        var questions = await handler.HandleAsync(Focus, "Rattle from the front over bumps.", CancellationToken.None);

        Assert.Empty(questions);
    }

    [Fact]
    public async Task Invalid_vehicle_details_are_still_rejected()
    {
        var handler = new SuggestClarifyingQuestionsHandler(new FakeClarifyingQuestionService(), NullLogger<SuggestClarifyingQuestionsHandler>.Instance);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(new VehicleInput("", "Focus", null, 2018, null, FuelType.Petrol), "Rattle from the front.", CancellationToken.None));
    }

    [Fact]
    public void Compose_appends_only_the_answered_questions()
    {
        var composed = ClarifiedDescription.Compose(
            "Rattle from the front over bumps.  ",
            ["When does it happen?", "Any warning lights?", "Recent work?"],
            ["Mostly when cold ", null, "  "]);

        Assert.Equal(
            "Rattle from the front over bumps.\n\nFollow-up answers:\n- When does it happen? Mostly when cold",
            composed.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Compose_leaves_the_description_alone_when_nothing_was_answered()
    {
        var composed = ClarifiedDescription.Compose("Rattle from the front over bumps.", ["When does it happen?"], [null]);

        Assert.Equal("Rattle from the front over bumps.", composed);
    }
}
