using FaultDesk.Domain.Tickets;
using FaultDesk.Domain.Vehicles;

namespace FaultDesk.Infrastructure.Persistence.Seed;

/// <summary>
/// Twenty realistic historical tickets so the garage portal has history and similar-ticket suggestions from the
/// first run. Identities are fixed so seeding is idempotent. Several use the mock lookup's registrations so a
/// customer reporting on one of those cars sees that vehicle's history.
/// </summary>
internal static class SeedTickets
{
    internal sealed record SeedTicket(
        int Number,
        string Registration,
        string Make,
        string Model,
        string? Variant,
        int Year,
        int? EngineCc,
        FuelType Fuel,
        string? CustomerName,
        int DaysAgo,
        string Description,
        string Title,
        FaultCategory Category,
        Severity Severity,
        SafeToDrive SafeToDrive,
        string[] Symptoms,
        string[] Systems,
        TicketStatus Status,
        string? Resolution)
    {
        public TicketId Id => new(new Guid($"00000000-0000-4000-8000-{Number:000000000000}"));

        public FaultTicket Build(DateTimeOffset now)
        {
            var createdAt = now.AddDays(-DaysAgo).AddHours(-(Number % 7)).AddMinutes(-(Number * 11 % 60));
            var ticket = FaultTicket.Import(
                Id,
                $"FD-{createdAt:yyMMdd}-S{Number:000}",
                VehicleRegistration.Parse(Registration),
                VehicleDetails.Create(Make, Model, Variant, Year, EngineCc, Fuel),
                Description,
                CustomerName,
                null,
                createdAt);

            ticket.ApplyTriage(
                TriageSummary.Create(Title, Category, Severity, SafeToDrive, Symptoms, Systems, QuestionsFor(Category), CustomerSummary(Description)),
                createdAt);

            if (Status is TicketStatus.InProgress or TicketStatus.Resolved)
            {
                ticket.Start(createdAt.AddHours(20));
            }

            if (Status is TicketStatus.Resolved)
            {
                ticket.Resolve(Resolution, createdAt.AddDays(2));
            }

            return ticket;
        }

        private static string CustomerSummary(string description)
        {
            var firstSentence = description.Split(['.', '!', '?'], 2)[0].Trim();
            return $"You reported that {char.ToLowerInvariant(firstSentence[0])}{firstSentence[1..]}.";
        }

        private static string[] QuestionsFor(FaultCategory category) => category switch
        {
            FaultCategory.Brakes => ["Does it happen every time you brake or only when the brakes are hot or cold?", "Does the car pull to one side?", "Any warning lights?"],
            FaultCategory.SteeringSuspension => ["Does the noise change when turning or braking?", "Is it worse on rough roads?", "Any recent work on the suspension or tyres?"],
            FaultCategory.Electrical => ["Does it happen after the car has been standing?", "Any warning lights or flickering?", "How old is the battery?"],
            FaultCategory.Cooling => ["Has the temperature gauge gone into the red?", "Any coolant on the ground or a sweet smell?", "How often are you topping up?"],
            FaultCategory.Engine => ["Does it happen from cold or when warm?", "Any warning lights, and are they flashing?", "When was it last serviced?"],
            FaultCategory.Transmission => ["Does it happen in every gear or specific ones?", "Any burning smell?", "Manual or automatic?"],
            FaultCategory.ExhaustEmissions => ["Mostly short journeys or longer runs?", "Any warning lights?", "Any loss of power?"],
            FaultCategory.Tyres => ["Does the vibration change with speed?", "When were the tyres last checked?", "Any recent kerb strikes or potholes?"],
            _ => ["When did it start?", "Does anything make it better or worse?", "Any recent work on the car?"],
        };
    }

    public static readonly IReadOnlyList<SeedTicket> All =
    [
        new(1, "AB12 CDE", "Ford", "Focus", "1.6 TDCi Zetec", 2012, 1560, FuelType.Diesel, "Priya Shah", 400,
            "Grinding noise from the front when braking, worse when coming down hills. Brake pedal feels a bit soft as well.",
            "Grinding from front brakes, soft pedal", FaultCategory.Brakes, Severity.High, SafeToDrive.Caution,
            ["grinding noise when braking", "soft brake pedal"], ["front brake pads", "front brake discs"],
            TicketStatus.Resolved,
            "Front discs badly lipped and pads worn to the metal. Replaced front discs and pads, cleaned and lubricated the caliper sliders, bled the brakes. Road tested, pedal firm."),

        new(2, "AB12 CDE", "Ford", "Focus", "1.6 TDCi Zetec", 2012, 1560, FuelType.Diesel, "Priya Shah", 120,
            "Engine management light is on, the car feels sluggish and it sometimes goes into limp mode on the motorway.",
            "Engine light on, limp mode on motorway", FaultCategory.ExhaustEmissions, Severity.Medium, SafeToDrive.Caution,
            ["engine management light", "loss of power", "limp mode"], ["diesel particulate filter", "DPF pressure sensor"],
            TicketStatus.Resolved,
            "P2002 DPF efficiency below threshold. Forced regeneration completed, codes cleared, differential pressure sensor readings normal afterwards. Advised customer on regular longer runs."),

        new(3, "KX19 HWL", "Volkswagen", "Golf", "1.5 TSI Evo Match", 2019, 1498, FuelType.Petrol, "Tom Reid", 200,
            "Rattle from under the car at idle that goes away as soon as you rev it a little.",
            "Rattle from underneath at idle", FaultCategory.ExhaustEmissions, Severity.Low, SafeToDrive.Yes,
            ["rattle at idle", "stops when revved"], ["exhaust heat shield"],
            TicketStatus.Resolved,
            "Corroded fixing on the mid-section exhaust heat shield. Replaced the fixing and secured the shield. No rattle on test."),

        new(4, "LM68 RTO", "Vauxhall", "Corsa", "1.4i SRi", 2018, 1398, FuelType.Petrol, "Aisha Khan", 300,
            "Struggles to start on cold mornings and the dashboard lights flicker while it is cranking.",
            "Slow cold start, dash lights flicker when cranking", FaultCategory.Electrical, Severity.Medium, SafeToDrive.Yes,
            ["slow cranking when cold", "dashboard lights flicker"], ["12V battery", "alternator"],
            TicketStatus.Resolved,
            "Battery tested at 45% state of health; alternator charging at 14.2V. Replaced battery, cleaned and protected terminals."),

        new(5, "BN17 PLX", "BMW", "320d", "M Sport", 2017, 1995, FuelType.Diesel, "Marcus Bell", 90,
            "Knocking noise from the front over speed bumps and rough roads, and sometimes a creak when turning at low speed.",
            "Front knock over bumps, creak on low-speed turns", FaultCategory.SteeringSuspension, Severity.Medium, SafeToDrive.Caution,
            ["knocking over bumps", "creak when turning"], ["anti-roll bar drop links", "lower control arm bushes"],
            TicketStatus.Resolved,
            "Both front anti-roll bar drop links worn and play in the nearside lower control arm rear bush. Replaced drop links and control arm, four-wheel alignment carried out."),

        new(6, "WR21 ZKD", "Toyota", "Yaris", "1.5 Hybrid Icon", 2021, 1490, FuelType.Hybrid, "Helen Osei", 60,
            "Squeal from the brakes at low speed first thing in the morning; it disappears after a few stops.",
            "Morning brake squeal at low speed", FaultCategory.Brakes, Severity.Low, SafeToDrive.Yes,
            ["brake squeal when cold", "clears after a few stops"], ["rear brake discs"],
            TicketStatus.Resolved,
            "Light surface corrosion on the rear discs, common on hybrids because regenerative braking does most of the work. Cleaned discs and pads, no fault found. Advised customer."),

        new(7, "YF15 VNP", "Nissan", "Qashqai", "1.5 dCi Acenta", 2015, 1461, FuelType.Diesel, "Dan Walsh", 250,
            "Temperature gauge climbs high in traffic and I can smell coolant after a drive. I have had to top it up twice this month.",
            "Overheating in traffic, coolant smell, losing coolant", FaultCategory.Cooling, Severity.High, SafeToDrive.No,
            ["temperature gauge high in traffic", "coolant smell", "coolant loss"], ["thermostat housing", "cooling system"],
            TicketStatus.Resolved,
            "Coolant leak from a cracked plastic thermostat housing. Replaced housing and thermostat, pressure tested the system, refilled and bled. No further loss over a 20-mile test."),

        new(8, "DG66 UJT", "Mercedes-Benz", "A180d", "Sport", 2016, 1461, FuelType.Diesel, "Sofia Marin", 180,
            "Juddering when pulling away from a standstill, especially uphill, and a burning smell after slow traffic.",
            "Clutch judder pulling away, burning smell", FaultCategory.Transmission, Severity.Medium, SafeToDrive.Caution,
            ["judder pulling away", "burning smell in traffic"], ["clutch", "dual-mass flywheel"],
            TicketStatus.Resolved,
            "Clutch worn and oil-contaminated; dual-mass flywheel within tolerance. Replaced clutch kit and release bearing, replaced leaking crankshaft rear seal."),

        new(9, "PO70 EVQ", "Tesla", "Model 3", "Standard Range Plus", 2020, null, FuelType.Electric, "Lee Chambers", 45,
            "Humming noise from the rear at motorway speeds and a slight vibration through the seat.",
            "Rear hum and vibration at motorway speed", FaultCategory.Tyres, Severity.Medium, SafeToDrive.Caution,
            ["humming from the rear at speed", "vibration through the seat"], ["rear tyres", "rear wheel alignment"],
            TicketStatus.Resolved,
            "Both rear tyres cupped on the inner edge. Replaced rear tyres; rear toe was out of specification, alignment adjusted. Noise gone on road test."),

        new(10, "SA09 MKW", "Honda", "Jazz", "1.4 i-VTEC ES", 2009, 1339, FuelType.Petrol, "Margaret Hill", 30,
            "The air conditioning only blows warm air and there is a clicking noise from the engine bay when it is switched on.",
            "Air con blowing warm, clicking from engine bay", FaultCategory.Electrical, Severity.Low, SafeToDrive.Yes,
            ["air con blows warm", "clicking when air con switched on"], ["air conditioning compressor clutch", "refrigerant"],
            TicketStatus.InProgress, null),

        new(11, "HJ13 XRC", "Land Rover", "Freelander 2", "2.2 SD4 HSE", 2013, 2179, FuelType.Diesel, "Owen Price", 15,
            "Steering wheel shakes at 60 to 70 mph and the car wanders on the motorway.",
            "Steering shake at 60-70 mph, wandering", FaultCategory.SteeringSuspension, Severity.Medium, SafeToDrive.Caution,
            ["steering wheel shake at speed", "wandering on motorway"], ["wheel balance", "track rod ends", "front tyres"],
            TicketStatus.New, null),

        new(12, "MK15 HLW", "Skoda", "Octavia", "2.0 TDI SE", 2015, 1968, FuelType.Diesel, "Jakub Nowak", 330,
            "Loss of power under acceleration and black smoke from the exhaust when I put my foot down.",
            "Power loss and black smoke under acceleration", FaultCategory.ExhaustEmissions, Severity.Medium, SafeToDrive.Caution,
            ["loss of power", "black exhaust smoke"], ["EGR valve", "inlet manifold"],
            TicketStatus.Resolved,
            "EGR valve heavily carboned and sticking open. Cleaned inlet manifold, replaced EGR valve, reset adaptations. Boost and smoke normal on test."),

        new(13, "RJ64 TFD", "Peugeot", "208", "1.2 PureTech Active", 2014, 1199, FuelType.Petrol, "Chloe Barnes", 280,
            "Rattling noise from the engine for a couple of seconds on start-up, then it goes quiet.",
            "Engine rattle for a few seconds on start-up", FaultCategory.Engine, Severity.High, SafeToDrive.Caution,
            ["rattle on start-up", "quiet after a few seconds"], ["timing chain", "chain tensioner"],
            TicketStatus.Resolved,
            "Timing chain stretched and tensioner worn, a known issue on this engine. Replaced timing chain kit and tensioner, oil and filter changed."),

        new(14, "EY18 NKP", "Kia", "Sportage", "1.6 GDi 2", 2018, 1591, FuelType.Petrol, "Ravi Menon", 210,
            "Whining noise that gets louder with road speed rather than engine speed. It is louder when turning right.",
            "Speed-related whine, louder turning right", FaultCategory.SteeringSuspension, Severity.Medium, SafeToDrive.Caution,
            ["whine increases with road speed", "louder turning right"], ["nearside front wheel bearing"],
            TicketStatus.Resolved,
            "Nearside front wheel bearing worn (noise loads up on right-hand turns). Replaced bearing and hub assembly."),

        new(15, "GX12 OVR", "Audi", "A3", "2.0 TDI Sport", 2012, 1968, FuelType.Diesel, "Nina Fox", 150,
            "Car cuts out at junctions and idles rough. The engine light flashes sometimes.",
            "Cutting out at junctions, rough idle, flashing engine light", FaultCategory.Engine, Severity.High, SafeToDrive.No,
            ["cuts out at idle", "rough idle", "flashing engine light"], ["fuel injectors", "ignition"],
            TicketStatus.Resolved,
            "Misfire on cylinder 3, injector failed on the leak-off test. Replaced injector and coded it to the ECU. Idle smooth, no codes after 30 miles."),

        new(16, "NA19 WUB", "Ford", "Fiesta", "1.0 EcoBoost Titanium", 2019, 999, FuelType.Petrol, "Ben Carter", 100,
            "Coolant warning came on and there was steam from the bonnet on the motorway.",
            "Coolant warning and steam from bonnet", FaultCategory.Cooling, Severity.High, SafeToDrive.No,
            ["coolant warning light", "steam from bonnet"], ["degas hose", "cooling system", "head gasket"],
            TicketStatus.Resolved,
            "Degas hose split near the thermostat housing. Pressure tested, replaced hose, refilled and bled. Combustion gas test negative, head gasket OK."),

        new(17, "LB09 ZRT", "Vauxhall", "Astra", "1.7 CDTi Life", 2009, 1686, FuelType.Diesel, "Gary Holt", 380,
            "The handbrake will not hold on hills and there is a scraping noise from the back wheels.",
            "Handbrake not holding, scraping from rear", FaultCategory.Brakes, Severity.High, SafeToDrive.No,
            ["handbrake not holding", "scraping from rear wheels"], ["rear brake shoes", "rear drums", "handbrake cable"],
            TicketStatus.Resolved,
            "Rear brake shoes worn through and offside drum scored. Replaced rear shoes and drums, adjusted the handbrake cable. Holds on the ramp and on a hill test."),

        new(18, "WP66 ADX", "Volkswagen", "Polo", "1.2 TSI Match", 2016, 1197, FuelType.Petrol, "Imogen Ward", 20,
            "Central locking has stopped working on the driver's door and the window on that door is slow.",
            "Driver's door central locking and slow window", FaultCategory.Electrical, Severity.Low, SafeToDrive.Yes,
            ["driver's door not locking", "slow window"], ["door wiring loom", "door lock module"],
            TicketStatus.InProgress, null),

        new(19, "FD17 TYK", "Renault", "Clio", "1.5 dCi Dynamique", 2017, 1461, FuelType.Diesel, "Amelia Grant", 70,
            "Clunk from the gearbox when changing from first to second and reverse is hard to select.",
            "Clunk on 1st to 2nd, reverse hard to select", FaultCategory.Transmission, Severity.Medium, SafeToDrive.Caution,
            ["clunk on gear change", "reverse hard to select"], ["gear linkage", "gear cables"],
            TicketStatus.Resolved,
            "Gear linkage bushes worn. Replaced linkage bushes and adjusted the selector cables. All gears select cleanly."),

        new(20, "KX19 HWL", "Volkswagen", "Golf", "1.5 TSI Evo Match", 2019, 1498, FuelType.Petrol, "Tom Reid", 5,
            "The tyre pressure warning keeps coming on even after I have inflated all the tyres to the right pressure.",
            "Tyre pressure warning keeps returning", FaultCategory.Tyres, Severity.Low, SafeToDrive.Yes,
            ["tyre pressure warning light", "returns after inflating"], ["tyre pressure monitoring system", "slow puncture"],
            TicketStatus.New, null),
    ];
}
