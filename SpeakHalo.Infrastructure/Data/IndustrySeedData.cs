using Microsoft.EntityFrameworkCore;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using System.Text.Json;

namespace SpeakHalo.Infrastructure.Data;

/// <summary>
/// Seeds comprehensive industry data with AI training metadata for all 9 home service verticals.
/// Upserts by ServiceType so new industries are added without duplicating existing ones.
/// </summary>
public static class IndustrySeedData
{
    public static async Task SeedIndustriesAsync(ApplicationDbContext context)
    {
        var existingTypes = await context.Industries
            .Select(i => i.ServiceType)
            .ToListAsync();

        var industries = BuildAllIndustries();

        var toAdd = industries.Where(i => !existingTypes.Contains(i.ServiceType)).ToList();

        if (toAdd.Count == 0)
            return; // All industries already seeded

        await context.Industries.AddRangeAsync(toAdd);
        await context.SaveChangesAsync();
    }

    private static List<Industry> BuildAllIndustries()
    {
        return new List<Industry>
        {
            // ═══════════════════════════════════════════════════════════════
            // 1. HVAC (ServiceType.HVAC)
            // ═══════════════════════════════════════════════════════════════
            new Industry
            {
                Id = Guid.NewGuid().ToString(),
                ServiceType = ServiceType.HVAC,
                Name = "HVAC Services",
                Code = "hvac",
                Description = "Heating, ventilation, and air conditioning installation, repair, and maintenance services",
                Keywords = JsonSerializer.Serialize(new[]
                {
                    "hvac", "heating", "ventilation", "air conditioning", "furnace", "heat pump",
                    "ductwork", "thermostat", "ac repair", "ac installation", "ac maintenance",
                    "furnace repair", "furnace installation", "boiler", "boiler repair",
                    "heat pump installation", "heat pump repair", "mini split", "ductless",
                    "central air", "forced air", "radiant heating", "geothermal",
                    "air handler", "condenser", "evaporator coil", "compressor",
                    "refrigerant", "freon", "coolant", "R-410A", "R-22",
                    "air filter", "air quality", "humidity control", "dehumidifier",
                    "humidifier", "zone system", "smart thermostat", "programmable thermostat",
                    "energy efficiency", "SEER rating", "AFUE rating", "BTU",
                    "tonnage", "load calculation", "duct sealing", "duct cleaning",
                    "ventilation fan", "exhaust fan", "ERV", "HRV"
                }),
                CommonServices = JsonSerializer.Serialize(new[]
                {
                    "AC installation (central, ductless mini-split)", "AC repair and diagnostics",
                    "AC tune-up and seasonal maintenance", "furnace installation (gas, electric, oil)",
                    "furnace repair and diagnostics", "furnace tune-up and safety inspection",
                    "heat pump installation", "heat pump repair",
                    "boiler installation and repair", "radiant floor heating installation",
                    "ductwork installation", "duct repair and sealing", "duct cleaning",
                    "thermostat installation (smart, programmable)", "zone system installation",
                    "indoor air quality assessment", "air purifier installation",
                    "humidifier/dehumidifier installation", "ventilation system design",
                    "refrigerant recharge and leak repair", "compressor replacement",
                    "evaporator coil cleaning/replacement", "condenser cleaning/replacement",
                    "blower motor replacement", "igniter and flame sensor replacement",
                    "heat exchanger inspection and replacement", "ductless mini-split installation",
                    "geothermal system installation", "commercial HVAC maintenance contracts",
                    "energy audit and efficiency consultation", "load calculation (Manual J)"
                }),
                CommonEquipment = JsonSerializer.Serialize(new[]
                {
                    "manifold gauge set (refrigerant pressures)", "digital vacuum pump",
                    "refrigerant recovery machine", "refrigerant scale",
                    "leak detector (electronic, UV dye)", "nitrogen pressure test kit",
                    "combustion analyzer (furnace efficiency)", "manometer (gas pressure)",
                    "multimeter (electrical diagnostics)", "clamp meter (amp draw)",
                    "infrared thermometer (non-contact)", "psychrometer (wet/dry bulb)",
                    "anemometer (airflow measurement)", "duct blaster (duct leakage test)",
                    "blower door (building envelope test)", "flue gas analyzer",
                    "pipe cutter and flaring tool set", "brazing/soldering torch (MAP, oxy-acetylene)",
                    "tubing bender", "swaging tool set",
                    "sheet metal tools (crimper, seamer, snips)", "duct board knife",
                    "drill and impact driver set", "reciprocating saw",
                    "pipe wrench set", "adjustable wrench set",
                    "vacuum gauge (micron gauge)", "capacitor tester",
                    "borescope (inspect heat exchangers, ducts)", "CO detector (carbon monoxide)",
                    "smoke pencil (airflow visualization)", "condensate pump",
                    "fin comb (condenser/evaporator coils)", "coil cleaner (acid/alkaline)",
                    "recovery cylinder set", "EPA Section 608 certification card"
                }),
                TypicalIssues = JsonSerializer.Serialize(new[]
                {
                    "AC not cooling or blowing warm air", "AC unit running but not reaching set temperature",
                    "uneven cooling/heating between rooms", "AC short cycling (frequent on/off)",
                    "frozen evaporator coil", "refrigerant leak (low charge)",
                    "noisy outdoor unit (grinding, squealing, banging)", "condenser fan not spinning",
                    "thermostat not responding or reading incorrectly", "high energy bills (sudden increase)",
                    "furnace not igniting or producing heat", "furnace blowing cold air",
                    "pilot light keeps going out", "yellow or flickering furnace flame (CO risk)",
                    "cracked heat exchanger (carbon monoxide danger)", "furnace short cycling",
                    "blower motor running constantly", "strange burning smell from furnace",
                    "ductwork leaking air (hot/cold spots)", "dusty or musty air from vents",
                    "condensate drain clogged (water leak)", "compressor not starting",
                    "heat pump stuck in defrost mode", "heat pump not switching modes (heat/cool)",
                    "boiler no heat or low pressure", "boiler leaking water",
                    "radiator not heating evenly", "zone damper stuck open/closed",
                    "smart thermostat connectivity issues", "carbon monoxide alarm triggered",
                    "humidity too high or too low", "ice buildup on outdoor unit",
                    "capacitor failure (humming but not starting)", "contactor failure (clicking noise)",
                    "expansion valve malfunction", "dirty or clogged air filter (restricted airflow)"
                }),
                TechnicalTerms = JsonSerializer.Serialize(new[]
                {
                    "SEER (Seasonal Energy Efficiency Ratio)", "SEER2 (updated 2023 standard)",
                    "AFUE (Annual Fuel Utilization Efficiency)", "HSPF (Heating Seasonal Performance Factor)",
                    "EER (Energy Efficiency Ratio)", "BTU (British Thermal Unit)",
                    "tonnage (cooling capacity, 1 ton = 12,000 BTU)", "Manual J load calculation",
                    "Manual D duct design", "superheat and subcooling",
                    "static pressure (duct system)", "CFM (cubic feet per minute)",
                    "delta T (temperature differential)", "R-410A refrigerant",
                    "R-22 (Freon, phased out)", "TXV (thermostatic expansion valve)",
                    "ECM motor (electronically commutated)", "PSC motor (permanent split capacitor)",
                    "variable speed compressor (inverter)", "two-stage vs single-stage",
                    "modulating furnace", "heat exchanger",
                    "ERV (energy recovery ventilator)", "HRV (heat recovery ventilator)",
                    "MERV rating (filter efficiency)", "HEPA filtration",
                    "zoning system (dampers and thermostats)", "ductless mini-split (wall-mounted head)",
                    "line set (refrigerant piping)", "condensate line (drain)"
                }),
                SeasonalConsiderations = JsonSerializer.Serialize(new[]
                {
                    "spring (Mar-May): AC tune-up season begins. Schedule early to avoid summer rush. Transition from heating to cooling mode. Check refrigerant levels, clean coils, replace filters. Heat pump switchover inspections.",
                    "summer (Jun-Aug): peak AC repair and emergency season. Longest wait times for service. Compressor failures, refrigerant leaks, frozen coils common. New installation demand highest. Indoor air quality concerns with windows closed.",
                    "fall (Sep-Nov): furnace tune-up and heating prep season. Carbon monoxide detector checks critical. Heat exchanger inspections before first cold snap. Duct sealing before winter. Thermostat programming for heating season.",
                    "winter (Dec-Feb): peak heating emergency season. Furnace breakdowns, no-heat calls, pilot light issues. Frozen pipes near outdoor units. Boiler pressure drops. Space heater safety concerns. Good time to book spring AC installations at lower prices."
                }),
                EmergencyIndicators = JsonSerializer.Serialize(new[]
                {
                    "no heat in freezing temperatures (pipe freeze risk, elderly/infant safety)",
                    "carbon monoxide alarm sounding (evacuate immediately, call fire department first)",
                    "gas smell near furnace (shut off gas, evacuate, call gas company)",
                    "cracked heat exchanger suspected (CO poisoning risk, shut down furnace)",
                    "AC completely out during extreme heat (heat stroke risk for vulnerable occupants)",
                    "electrical burning smell from HVAC equipment (fire risk, shut off power)",
                    "water flooding from boiler or condensate overflow",
                    "sparking or arcing at electrical connections",
                    "refrigerant leak in enclosed space (displacement of oxygen)",
                    "furnace making loud banging/explosion sounds on ignition (delayed ignition)"
                }),
                AverageServiceDuration = 120,
                TypicalPriceRange = "Diagnostics: $75-$150. Repairs: $150-$1,500. Tune-ups: $80-$200. AC install: $3,500-$12,000. Furnace install: $2,500-$8,000. Mini-split: $3,000-$10,000. Duct work: $1,500-$5,000.",
                DisplayOrder = 1,
                IsActive = true
            },

            // ═══════════════════════════════════════════════════════════════
            // 2. HomeServices (ServiceType.HomeServices)
            // ═══════════════════════════════════════════════════════════════
            new Industry
            {
                Id = Guid.NewGuid().ToString(),
                ServiceType = ServiceType.HomeServices,
                Name = "Home Services",
                Code = "home-services",
                Description = "General home maintenance, remodeling, renovation, and comprehensive property care services",
                Keywords = JsonSerializer.Serialize(new[]
                {
                    "home services", "home maintenance", "home repair", "home improvement",
                    "renovation", "remodeling", "property maintenance", "home inspection",
                    "general contractor", "home renovation", "kitchen remodel", "bathroom remodel",
                    "basement finishing", "addition", "room conversion", "aging in place",
                    "accessibility modifications", "home automation", "smart home",
                    "energy efficiency", "weatherization", "insulation", "siding",
                    "deck building", "fence installation", "garage door", "driveway",
                    "concrete work", "masonry", "foundation repair", "waterproofing",
                    "crawl space", "attic insulation", "window replacement", "door installation",
                    "flooring installation", "painting", "drywall", "trim work",
                    "cabinet installation", "countertop installation", "tile work",
                    "home warranty", "preventive maintenance", "seasonal maintenance"
                }),
                CommonServices = JsonSerializer.Serialize(new[]
                {
                    "kitchen remodel (full, partial, cosmetic)", "bathroom remodel and renovation",
                    "basement finishing and waterproofing", "room additions and conversions",
                    "deck and patio construction", "fence installation and repair",
                    "garage door installation and repair", "driveway repair and resurfacing",
                    "concrete flatwork (sidewalks, patios)", "foundation inspection and repair",
                    "window replacement and installation", "door installation (entry, interior, sliding)",
                    "siding installation and repair (vinyl, fiber cement, wood)",
                    "insulation installation (attic, wall, crawl space)",
                    "flooring installation (hardwood, LVP, tile, carpet)",
                    "interior and exterior painting", "drywall repair and installation",
                    "trim and molding installation", "cabinet refacing and installation",
                    "countertop installation (granite, quartz, laminate)",
                    "tile installation (backsplash, floor, shower)",
                    "aging-in-place modifications (grab bars, ramps, walk-in tubs)",
                    "home energy audit", "weatherization and air sealing",
                    "crawl space encapsulation", "home maintenance inspection",
                    "seasonal maintenance packages", "smart home installation (locks, cameras, thermostats)",
                    "permit coordination and project management"
                }),
                CommonEquipment = JsonSerializer.Serialize(new[]
                {
                    "table saw", "miter saw (compound, sliding)", "circular saw",
                    "reciprocating saw", "jigsaw", "oscillating multi-tool",
                    "drill/driver set (cordless)", "impact driver",
                    "hammer drill/rotary hammer", "nail gun (framing, finish, brad)",
                    "air compressor", "router (trim, plunge)",
                    "sander (orbital, belt, detail)", "planer (hand, benchtop)",
                    "level set (torpedo, 4-foot, laser)", "tape measure and laser measure",
                    "stud finder", "moisture meter",
                    "drywall tools (knife set, mud pan, stilts)", "texture sprayer",
                    "paint sprayer (HVLP, airless)", "roller and brush set",
                    "tile saw (wet saw)", "tile cutter and nippers",
                    "grout float and sponge set", "caulking gun (manual, powered)",
                    "pry bar set", "demolition tools (sledge, crowbar)",
                    "concrete mixer (portable)", "concrete finishing tools (float, trowel, edger)",
                    "scaffolding and ladder set", "work lights and portable power",
                    "shop vacuum (HEPA)", "dust collection system",
                    "safety equipment (hard hat, safety glasses, ear protection)",
                    "generator (portable)", "trailer/truck for hauling"
                }),
                TypicalIssues = JsonSerializer.Serialize(new[]
                {
                    "kitchen cabinets outdated or falling apart", "countertops cracked or stained",
                    "bathroom tile cracked or grout failing", "bathtub/shower surround deteriorating",
                    "basement water intrusion or dampness", "foundation cracks (structural vs cosmetic)",
                    "windows drafty or difficult to operate", "doors sticking, not closing properly",
                    "siding damaged, warped, or rotting", "deck boards rotting or deck structurally unsafe",
                    "fence leaning, broken panels, or rotting posts", "garage door not opening/closing properly",
                    "driveway cracking or heaving", "concrete settling or uneven walkways",
                    "hardwood floors scratched, cupping, or squeaking", "carpet worn, stained, or smelling",
                    "drywall holes, cracks, or water damage", "interior paint peeling or fading",
                    "exterior paint peeling, chalking, or mildew", "trim and molding damaged or missing",
                    "insulation inadequate (high energy bills, drafts)", "attic moisture or ventilation issues",
                    "crawl space moisture, mold, or pest activity", "aging-in-place safety concerns",
                    "outdated electrical or plumbing during remodel", "permit and code compliance issues",
                    "structural concerns during renovation", "load-bearing wall identification",
                    "asbestos or lead paint in older homes", "mold discovered during renovation"
                }),
                TechnicalTerms = JsonSerializer.Serialize(new[]
                {
                    "load-bearing wall", "non-load-bearing partition",
                    "shear wall", "header beam",
                    "subfloor (plywood, OSB)", "underlayment",
                    "R-value (insulation rating)", "vapor barrier",
                    "flashing (roof, window, wall)", "housewrap (Tyvek, etc.)",
                    "soffit and fascia", "frieze board",
                    "LVL (laminated veneer lumber)", "engineered beam",
                    "footing and foundation wall", "frost line",
                    "rough-in (plumbing, electrical, HVAC)", "finish work (trim, paint, fixtures)",
                    "punch list", "change order",
                    "building permit", "certificate of occupancy",
                    "ADA compliance (accessibility)", "universal design",
                    "GC (general contractor)", "subcontractor",
                    "scope of work (SOW)", "cost-plus vs fixed-bid"
                }),
                SeasonalConsiderations = JsonSerializer.Serialize(new[]
                {
                    "spring (Mar-May): busiest booking season. Exterior projects resume (siding, decks, painting, concrete). Spring home inspections. Deck staining and sealing. Foundation checks after frost heave. Plan summer renovations now.",
                    "summer (Jun-Aug): peak renovation and construction season. Longest daylight for outdoor work. Window and door replacements ideal. Exterior painting optimal weather. Interior remodels in full swing. Book fall projects now as schedules fill fast.",
                    "fall (Sep-Nov): winterization and weatherization priority. Insulation upgrades before winter. Gutter and roof inspections. Caulking and air sealing. Interior projects (painting, flooring, bathrooms) ramp up as outdoor work winds down.",
                    "winter (Dec-Feb): interior renovation season. Kitchen and bathroom remodels ideal. Basement finishing. Flooring installation. Lower demand means better scheduling availability and sometimes better pricing. Plan spring exterior projects."
                }),
                EmergencyIndicators = JsonSerializer.Serialize(new[]
                {
                    "foundation wall bowing or cracking significantly (structural failure risk)",
                    "water pouring into basement during rain (immediate waterproofing needed)",
                    "deck or balcony structurally unsafe (collapse risk, restrict access)",
                    "tree fallen onto house or fence (secure area, check structural damage)",
                    "garage door off track or spring broken (safety hazard, do not operate)",
                    "major water damage from burst pipe or roof leak (mitigate before mold starts in 24-48 hours)",
                    "fire damage requiring immediate board-up and securing",
                    "gas line hit during renovation (evacuate, call gas company)",
                    "staircase or railing failure (fall risk, restrict access)",
                    "asbestos or lead paint disturbed during work (stop work, contain area)"
                }),
                AverageServiceDuration = 240,
                TypicalPriceRange = "Minor repairs: $150-$500. Painting (interior room): $300-$800. Deck build: $4,000-$15,000. Kitchen remodel: $15,000-$75,000. Bathroom remodel: $8,000-$30,000. Basement finish: $20,000-$60,000.",
                DisplayOrder = 2,
                IsActive = true
            },

            // ═══════════════════════════════════════════════════════════════
            // 3. Cleaning (ServiceType.Cleaning) — existing, kept as-is
            // ═══════════════════════════════════════════════════════════════
            new Industry
            {
                Id = Guid.NewGuid().ToString(),
                ServiceType = ServiceType.Cleaning,
                Name = "Cleaning Services",
                Code = "cleaning",
                Description = "Professional residential, commercial, and specialized cleaning services",
                Keywords = JsonSerializer.Serialize(new[]
                {
                    "cleaning", "housekeeping", "janitorial", "deep clean", "move-out cleaning",
                    "carpet cleaning", "window washing", "disinfection", "sanitization", "maid service",
                    "house cleaning", "office cleaning", "commercial cleaning", "post-construction cleanup",
                    "pressure washing", "tile and grout", "upholstery cleaning", "spring cleaning",
                    "recurring cleaning", "one-time clean", "move-in cleaning", "turnover cleaning",
                    "green cleaning", "eco-friendly cleaning", "hoarding cleanup", "biohazard cleanup",
                    "medical office cleaning", "restaurant cleaning", "gym cleaning", "retail cleaning",
                    "floor waxing", "floor stripping", "stain removal", "odor removal",
                    "mattress cleaning", "blind cleaning", "vent cleaning", "baseboard cleaning"
                }),
                CommonServices = JsonSerializer.Serialize(new[]
                {
                    "regular house cleaning", "deep cleaning", "move-in/move-out cleaning",
                    "spring cleaning", "recurring maintenance cleaning",
                    "carpet steam cleaning", "carpet dry cleaning", "carpet encapsulation",
                    "window washing (interior)", "window washing (exterior)", "window track cleaning",
                    "post-construction cleanup (rough)", "post-construction cleanup (detail)", "post-construction cleanup (final)",
                    "pressure washing (driveway)", "pressure washing (deck)", "pressure washing (siding)", "soft washing",
                    "tile and grout cleaning", "grout sealing", "natural stone care",
                    "upholstery cleaning", "leather cleaning and conditioning", "mattress sanitizing",
                    "office cleaning (daily)", "office cleaning (weekly)", "office deep clean",
                    "retail/storefront cleaning", "medical/dental office cleaning",
                    "restaurant kitchen deep clean", "restaurant dining area cleaning",
                    "gym/fitness center cleaning", "warehouse/industrial cleaning",
                    "day porter service", "floor stripping and waxing",
                    "hoarding cleanup", "biohazard/trauma cleanup", "flood cleanup",
                    "fire/smoke damage cleanup", "odor removal/ozone treatment",
                    "disinfection/sanitization service", "COVID/illness deep sanitize"
                }),
                CommonEquipment = JsonSerializer.Serialize(new[]
                {
                    "commercial backpack vacuum (ProTeam, Windsor)", "HEPA filter vacuum",
                    "wet/dry shop vacuum", "upright commercial vacuum",
                    "truck-mount carpet extractor", "portable carpet extractor (Rug Doctor, Bissell BigGreen)",
                    "carpet bonnet machine", "carpet encapsulation machine",
                    "auto scrubber (for large commercial)", "floor buffer/burnisher",
                    "floor stripping pads", "microfiber mop system (flat mop)",
                    "gas pressure washer (3000-4000 PSI)", "electric pressure washer (1500-2000 PSI)",
                    "surface cleaner attachment", "soft wash pump system",
                    "professional squeegee set (Ettore, Unger)", "extension poles (telescoping)",
                    "water-fed pole system", "window scrubber sleeves",
                    "microfiber cloths (color-coded)", "grout brush set",
                    "caddy/cleaning tote", "spray bottles (labeled)",
                    "scrub pads (white/green/blue)", "pumice stone (toilet ring)",
                    "razor blade scraper (glass)", "steam cleaner (handheld and upright)",
                    "PPE kit (gloves, masks, goggles)", "biohazard PPE (Tyvek suit, respirator)",
                    "step ladder (3-step)", "extension ladder",
                    "commercial all-purpose cleaner", "hospital-grade disinfectant",
                    "enzyme cleaner (pet/bio)", "degreaser (kitchen/industrial)",
                    "glass cleaner", "stainless steel polish",
                    "grout sealer", "wood floor cleaner (Bona)",
                    "eco-friendly product line (Green Seal certified)",
                    "carpet pre-spray", "carpet spotter kit"
                }),
                TypicalIssues = JsonSerializer.Serialize(new[]
                {
                    "red wine stains", "pet urine stains and odor", "blood stains",
                    "grease and oil stains", "coffee/tea stains", "ink stains",
                    "rust stains", "candle wax on carpet/fabric", "paint spills",
                    "marker/crayon on walls", "food stains on upholstery",
                    "hard water deposits (shower glass, faucets)", "soap scum buildup",
                    "mineral deposits on fixtures", "water rings on wood furniture",
                    "scratches on hardwood floors", "etching on marble/granite",
                    "dull/hazy laminate floors", "yellowed vinyl flooring",
                    "mold and mildew (shower grout, windowsills)", "black mold on caulking",
                    "mildew smell in closets/basements", "dust mite buildup in mattresses",
                    "pet hair embedded in upholstery", "allergen accumulation",
                    "grease buildup (kitchen hood, stove, backsplash)",
                    "calcium/lime buildup on faucets",
                    "soap residue on shower walls",
                    "wax buildup on floors (over-waxed)",
                    "dust accumulation in air vents and ducts",
                    "cobwebs in high corners and ceiling fans",
                    "grout cracking/missing", "caulk deterioration around tub/shower",
                    "discolored grout lines", "carpet wear in high-traffic paths",
                    "burned/melted carpet spots", "water damage stains on ceiling/walls"
                }),
                TechnicalTerms = JsonSerializer.Serialize(new[]
                {
                    "hot water extraction (HWE)", "encapsulation cleaning",
                    "bonnet cleaning", "dry compound cleaning",
                    "soft washing", "pressure washing (PSI ratings)",
                    "stripping and refinishing (floors)", "burnishing",
                    "HEPA filtration", "pH-neutral cleaners",
                    "alkaline cleaners (degreasers)", "acidic cleaners (mineral removal)",
                    "enzyme-based cleaners (biological)", "oxygen bleach (sodium percarbonate)",
                    "quaternary ammonium (quat) disinfectants", "hydrogen peroxide-based cleaners",
                    "contact time (disinfectant dwell time)", "EPA-registered disinfectant",
                    "Green Seal certified", "EPA Safer Choice",
                    "ISSA CIMS certification", "OSHA bloodborne pathogen standard",
                    "colorfast testing", "W/S/WS/X fabric codes",
                    "grout sealing/impregnating", "stone honing vs polishing",
                    "square footage rate", "per-room pricing",
                    "first clean vs recurring rate", "turnover clean",
                    "day porter", "zone cleaning",
                    "color-coded microfiber system (prevent cross-contamination)",
                    "top-to-bottom cleaning method", "clockwise room method"
                }),
                SeasonalConsiderations = JsonSerializer.Serialize(new[]
                {
                    "spring (Mar-May): peak deep cleaning season. Spring cleaning demand surge, book 2-3 weeks ahead. Window washing, screen cleaning, patio/deck pressure washing, mattress sanitizing, closet organization.",
                    "summer (Jun-Aug): move-in/move-out peak season (lease turnover). Vacation home prep/post cleaning. Outdoor services in demand: pressure washing, deck cleaning, exterior windows. Humidity increases mold/mildew risk in bathrooms and basements.",
                    "fall (Sep-Nov): pre-holiday deep cleans (Thanksgiving, Christmas prep). Window cleaning before storm season. Gutter-related exterior requests. Back-to-school office cleaning contracts start. Commercial year-end cleaning budgets spent.",
                    "winter (Dec-Feb): slowest period for residential. Indoor focus. Carpet cleaning ideal (low humidity = faster drying). Post-holiday cleanup demand spike (first 2 weeks January). Salt/mud tracked in means more frequent floor service. Good time for commercial deep cleans during slower business periods."
                }),
                EmergencyIndicators = JsonSerializer.Serialize(new[]
                {
                    "biohazard cleanup (blood, bodily fluids, needles) requires PPE, OSHA protocols",
                    "unattended death/decomposition requires specialized team, full PPE, insurance documentation",
                    "flood/water damage cleanup: extract water fast, mold starts in 24-48 hours",
                    "fire/smoke damage: soot cleanup, odor removal, content cleaning",
                    "sewage backup: contaminated water extraction, sanitization, health hazard",
                    "hoarding situation (severe): multi-day, compassionate approach, possible pest/mold",
                    "pest infestation cleanup (after exterminator): droppings, nests, contaminated surfaces",
                    "chemical spill: ventilation, neutralization, proper disposal",
                    "mold outbreak (large area >10 sq ft): refer to certified mold remediation specialist",
                    "infectious disease decontamination: hospital-grade disinfectants, full PPE, contact time critical"
                }),
                AverageServiceDuration = 180,
                TypicalPriceRange = "Residential: $120-$400 regular, $250-$800 deep, $300-$900 move-out. Commercial: $0.05-$0.35/sq ft. Specialized: $75-$1,500+ depending on service.",
                DisplayOrder = 3,
                IsActive = true
            },

            // ═══════════════════════════════════════════════════════════════
            // 4. Plumbing (ServiceType.Plumbing)
            // ═══════════════════════════════════════════════════════════════
            new Industry
            {
                Id = Guid.NewGuid().ToString(),
                ServiceType = ServiceType.Plumbing,
                Name = "Plumbing Services",
                Code = "plumbing",
                Description = "Residential and commercial plumbing installation, repair, drain cleaning, and water heater services",
                Keywords = JsonSerializer.Serialize(new[]
                {
                    "plumbing", "plumber", "drain", "pipe", "leak", "faucet", "toilet",
                    "water heater", "tankless water heater", "sewer", "sewer line",
                    "drain cleaning", "clogged drain", "pipe repair", "pipe replacement",
                    "water line", "water main", "shut-off valve", "supply line",
                    "garbage disposal", "sump pump", "ejector pump", "backflow preventer",
                    "water softener", "water filtration", "water pressure", "PRV",
                    "gas line", "gas piping", "re-pipe", "PEX", "copper pipe",
                    "fixture installation", "shower valve", "bathtub", "sink",
                    "hydro jetting", "camera inspection", "trenchless repair",
                    "septic system", "slab leak", "frozen pipe", "burst pipe",
                    "water damage", "sewage backup", "running toilet", "dripping faucet"
                }),
                CommonServices = JsonSerializer.Serialize(new[]
                {
                    "drain cleaning (snake, auger)", "hydro jetting (high-pressure drain clearing)",
                    "sewer camera inspection", "sewer line repair and replacement",
                    "trenchless sewer repair (pipe lining, pipe bursting)",
                    "water heater installation (tank, tankless, hybrid)",
                    "water heater repair and maintenance", "water heater flushing",
                    "faucet installation and repair", "toilet installation and repair",
                    "garbage disposal installation and repair", "sink installation",
                    "shower/tub installation and valve replacement", "bathtub-to-shower conversion",
                    "pipe leak repair (copper, PEX, PVC, galvanized)",
                    "whole-house re-pipe (PEX, copper)", "gas line installation and repair",
                    "water main repair and replacement", "slab leak detection and repair",
                    "sump pump installation and replacement", "ejector pump service",
                    "backflow preventer installation and testing",
                    "water softener installation", "water filtration system installation",
                    "pressure reducing valve (PRV) installation",
                    "fixture upgrades (ADA compliant)", "outdoor faucet/hose bib repair",
                    "washing machine hookup", "dishwasher installation",
                    "commercial plumbing maintenance", "septic system inspection and pumping"
                }),
                CommonEquipment = JsonSerializer.Serialize(new[]
                {
                    "drain snake/auger (hand and electric)", "hydro jetter (3000-4000 PSI)",
                    "sewer camera (push rod, 200ft+)", "sewer locator transmitter",
                    "pipe cutter set (copper, PVC, PEX)", "pipe wrench set (various sizes)",
                    "basin wrench", "adjustable wrench set",
                    "tube bender (copper)", "PEX crimp/cinch tool",
                    "PEX expansion tool (ProPEX)", "soldering torch and kit",
                    "ProPress tool (copper press fittings)", "pipe threader",
                    "channel locks/tongue-and-groove pliers", "needle nose pliers",
                    "hacksaw and reciprocating saw", "oscillating multi-tool",
                    "torpedo level", "tape measure",
                    "multimeter (for water heater diagnostics)", "manometer (gas pressure testing)",
                    "leak detection equipment (electronic, acoustic)", "thermal imaging camera",
                    "pressure test gauge (water lines)", "nitrogen test kit (gas lines)",
                    "closet auger (toilet snake)", "plunger set (flange, cup)",
                    "tubing cutter", "deburring tool",
                    "Teflon tape and pipe dope", "PVC primer and cement",
                    "torch tip cleaner set", "flux and solder (lead-free)",
                    "shop vacuum (wet/dry)", "bucket and tarp set (containment)",
                    "PPE (safety glasses, gloves, knee pads)"
                }),
                TypicalIssues = JsonSerializer.Serialize(new[]
                {
                    "clogged kitchen sink drain (grease, food buildup)",
                    "clogged bathroom drain (hair, soap scum)",
                    "clogged main sewer line (tree roots, debris, bellied pipe)",
                    "toilet running constantly (flapper, fill valve, overflow)",
                    "toilet clogged or overflowing", "toilet rocking or leaking at base (wax ring)",
                    "faucet dripping or leaking (cartridge, washer, O-ring)",
                    "low water pressure throughout house", "low water pressure at single fixture",
                    "no hot water (water heater failure)", "not enough hot water",
                    "water heater leaking from tank (likely replacement needed)",
                    "water heater making popping/rumbling noises (sediment buildup)",
                    "pipe leaking (pinhole leak, joint failure, corrosion)",
                    "frozen pipes in winter", "burst pipe from freezing",
                    "slab leak (water under foundation)", "sewer gas smell in house",
                    "sewage backup through floor drain or toilet",
                    "garbage disposal jammed or not working",
                    "water discoloration (rusty, brown, cloudy)",
                    "galvanized pipe corrosion (old homes)", "polybutylene pipe failure",
                    "water hammer (banging pipes)", "whistling or humming pipes",
                    "outdoor faucet leaking or frozen", "sump pump failure during heavy rain",
                    "backflow contamination concern", "gas leak near water heater or stove",
                    "expansion tank waterlogged", "PRV failure (high water pressure)"
                }),
                TechnicalTerms = JsonSerializer.Serialize(new[]
                {
                    "DWV (drain-waste-vent) system", "trap (P-trap, S-trap)",
                    "vent stack (plumbing vent)", "cleanout (access point)",
                    "ABS vs PVC (drain piping)", "PEX (cross-linked polyethylene)",
                    "copper Type M, L, K (wall thickness)", "galvanized steel pipe",
                    "CPVC (chlorinated PVC)", "SharkBite (push-fit fittings)",
                    "ProPress (pressed copper fittings)", "sweating (soldering copper)",
                    "PRV (pressure reducing valve)", "expansion tank",
                    "backflow preventer (RPZ, DCVA)", "check valve",
                    "ball valve vs gate valve", "angle stop (fixture shut-off)",
                    "GPM (gallons per minute)", "water pressure (PSI)",
                    "static vs dynamic pressure", "fixture unit (drainage calculation)",
                    "grade/slope (drain pitch, 1/4 inch per foot)", "belly (sag in sewer line)",
                    "offset (pipe direction change)", "wye and sanitary tee",
                    "closet flange (toilet floor connection)", "wax ring vs wax-free seal",
                    "anode rod (water heater corrosion protection)", "dip tube"
                }),
                SeasonalConsiderations = JsonSerializer.Serialize(new[]
                {
                    "spring (Mar-May): sewer line issues from root growth (spring root surge). Sump pump testing before rain season. Outdoor faucet/hose bib inspection after winter. Water heater flush recommended. Irrigation system startup.",
                    "summer (Jun-Aug): highest water usage season. Garbage disposal clogs increase (entertaining, cooking). Sewer line backups from heavy rain/storms. Sprinkler system repairs. Water heater demand increases. Good time for re-pipe projects (dry weather).",
                    "fall (Sep-Nov): winterization critical. Disconnect hoses, insulate outdoor faucets. Drain sprinkler systems. Water heater inspection before high-demand winter. Sump pump maintenance. Check for slow drains before holiday cooking season.",
                    "winter (Dec-Feb): frozen and burst pipe emergencies peak. Water heater failures increase (working harder in cold). Holiday drain clogs (grease, food waste). No-hot-water calls spike. Pipe insulation urgent for exposed pipes. Slab leak risk in freeze/thaw areas."
                }),
                EmergencyIndicators = JsonSerializer.Serialize(new[]
                {
                    "burst pipe flooding home (shut off main water valve immediately)",
                    "sewage backing up into home (health hazard, do not use fixtures)",
                    "gas leak smell near water heater or gas line (evacuate, call gas company)",
                    "slab leak causing foundation damage (water pooling, hot spots on floor)",
                    "no water to entire house (main line break or shut-off issue)",
                    "water heater leaking significantly (risk of flooding, potential scalding)",
                    "frozen pipes about to burst (apply gentle heat, do not use torch)",
                    "toilet overflowing and not stopping (shut off supply valve at wall)",
                    "sump pump failure during heavy rain (basement flooding imminent)"
                }),
                AverageServiceDuration = 120,
                TypicalPriceRange = "Service call: $75-$150. Drain cleaning: $150-$500. Faucet/toilet repair: $150-$350. Water heater install: $1,200-$3,500. Tankless install: $3,000-$6,000. Re-pipe: $4,000-$15,000. Sewer line: $3,000-$25,000.",
                DisplayOrder = 4,
                IsActive = true
            },

            // ═══════════════════════════════════════════════════════════════
            // 5. Electrical (ServiceType.Electrical)
            // ═══════════════════════════════════════════════════════════════
            new Industry
            {
                Id = Guid.NewGuid().ToString(),
                ServiceType = ServiceType.Electrical,
                Name = "Electrical Services",
                Code = "electrical",
                Description = "Residential and commercial electrical installation, repair, panel upgrades, and safety inspections",
                Keywords = JsonSerializer.Serialize(new[]
                {
                    "electrician", "electrical", "wiring", "panel", "breaker", "outlet",
                    "switch", "lighting", "generator", "surge protector", "grounding",
                    "circuit breaker", "fuse box", "electrical panel", "panel upgrade",
                    "rewiring", "knob and tube", "aluminum wiring", "copper wiring",
                    "GFCI", "AFCI", "arc fault", "ground fault", "short circuit",
                    "power outage", "flickering lights", "tripping breaker",
                    "ceiling fan", "recessed lighting", "LED", "dimmer switch",
                    "EV charger", "electric vehicle charging", "smart home wiring",
                    "smoke detector", "carbon monoxide detector", "whole house fan",
                    "backup generator", "standby generator", "transfer switch",
                    "landscape lighting", "security lighting", "motion sensor",
                    "dedicated circuit", "240V outlet", "sub-panel"
                }),
                CommonServices = JsonSerializer.Serialize(new[]
                {
                    "electrical panel upgrade (100A to 200A, 200A to 400A)",
                    "breaker replacement", "fuse box to breaker panel conversion",
                    "whole-house rewiring", "knob-and-tube replacement",
                    "aluminum wiring remediation (COPALUM, AlumiConn)",
                    "outlet installation and replacement", "GFCI outlet installation",
                    "AFCI breaker installation", "dedicated circuit installation (appliances)",
                    "240V outlet installation (dryer, range, EV charger, hot tub)",
                    "switch installation (single, 3-way, dimmer, smart)",
                    "ceiling fan installation and replacement",
                    "recessed/can light installation", "LED retrofit and conversion",
                    "under-cabinet lighting", "pendant and chandelier installation",
                    "landscape and outdoor lighting", "security and motion sensor lighting",
                    "EV charger installation (Level 2)", "generator installation (standby, portable interlock)",
                    "transfer switch installation", "whole-house surge protector installation",
                    "smoke and CO detector installation (hardwired)",
                    "electrical safety inspection", "code compliance inspection and remediation",
                    "troubleshooting (circuit tracing, fault finding)",
                    "commercial lighting installation", "commercial wiring and tenant build-out",
                    "data/low-voltage wiring (Ethernet, coax, phone)",
                    "hot tub/spa wiring", "pool and fountain wiring"
                }),
                CommonEquipment = JsonSerializer.Serialize(new[]
                {
                    "multimeter (Fluke, Klein)", "clamp meter (amp measurement)",
                    "voltage tester (non-contact, solenoid)", "circuit tracer/finder",
                    "wire strippers (manual, automatic)", "wire crimpers",
                    "fish tape (steel, fiberglass)", "fish sticks/glow rods",
                    "conduit bender (EMT, rigid)", "PVC conduit heater",
                    "cable puller", "wire pulling lubricant",
                    "drill and impact driver set", "hammer drill/rotary hammer",
                    "hole saw set", "auger bit set (flex bit for walls)",
                    "reciprocating saw", "oscillating multi-tool",
                    "knockout punch set", "cable cutter (large gauge)",
                    "torque screwdriver (for lugs and connections)", "insulated tool set",
                    "label maker (circuit identification)", "wire markers",
                    "infrared thermometer", "thermal imaging camera (hot spots)",
                    "megger/insulation resistance tester", "ground resistance tester",
                    "receptacle tester (outlet checker)", "GFCI tester",
                    "level (torpedo, laser)", "stud finder",
                    "headlamp and work lights", "voltage rated gloves and mat",
                    "lockout/tagout kit", "fire extinguisher (Class C)",
                    "conduit reamer", "tap and die set (for grounding)"
                }),
                TypicalIssues = JsonSerializer.Serialize(new[]
                {
                    "circuit breaker tripping repeatedly", "fuse blowing frequently",
                    "lights flickering or dimming", "outlets not working (dead outlet)",
                    "outlet sparking when plugging in", "outlet feels warm or hot",
                    "burning smell from outlet or switch", "buzzing from panel or outlet",
                    "power outage in part of house (partial)", "total power loss to home",
                    "GFCI outlet tripping and not resetting", "AFCI breaker nuisance tripping",
                    "light switch not working or intermittent", "dimmer switch buzzing or humming",
                    "ceiling fan wobbling or not working", "recessed light overheating and shutting off",
                    "outdoor lights not working", "landscape lighting transformer failed",
                    "aluminum wiring concerns (fire risk in older homes)",
                    "knob-and-tube wiring (insurance concerns, fire risk)",
                    "two-prong outlets (no ground, cannot use 3-prong)",
                    "insufficient outlets in rooms", "extension cord overuse (fire hazard)",
                    "panel at capacity (need more circuits)", "corroded or damaged panel bus bar",
                    "ground rod missing or inadequate", "bonding issue (CSST gas line)",
                    "code violations found during home sale inspection",
                    "EV charger circuit requirements", "generator sizing and connection",
                    "surge damage after lightning or power event",
                    "smart home device compatibility issues", "low voltage wiring needs (Ethernet, security)"
                }),
                TechnicalTerms = JsonSerializer.Serialize(new[]
                {
                    "amperage (amps) and voltage (volts)", "wattage (watts) and VA (volt-amps)",
                    "single-phase vs three-phase power", "120V vs 240V circuits",
                    "NEC (National Electrical Code)", "local amendments to NEC",
                    "GFCI (ground fault circuit interrupter)", "AFCI (arc fault circuit interrupter)",
                    "grounding and bonding", "equipment grounding conductor (EGC)",
                    "neutral vs ground", "hot/live wire",
                    "Romex (NM-B cable)", "MC cable (metal clad)",
                    "EMT conduit (electrical metallic tubing)", "rigid conduit",
                    "wire gauge (AWG: 14, 12, 10, 8, 6, etc.)", "ampacity (wire current capacity)",
                    "breaker sizing (15A, 20A, 30A, 50A, etc.)", "double-pole vs single-pole breaker",
                    "tandem/slim breaker", "main breaker vs main lug panel",
                    "sub-panel", "load center",
                    "dedicated circuit", "shared/general circuit",
                    "junction box", "device box (single, double gang)",
                    "wire nut vs Wago lever connector", "lug connection (panel)"
                }),
                SeasonalConsiderations = JsonSerializer.Serialize(new[]
                {
                    "spring (Mar-May): outdoor lighting and landscape lighting projects. Pool/spa wiring for summer. Panel upgrades before summer AC load. Storm damage repair from spring storms. Generator interest after late winter outages.",
                    "summer (Jun-Aug): peak AC load exposes undersized panels (tripping breakers). Ceiling fan installations surge. Outdoor entertaining lighting. EV charger demand increases. Lightning and storm surge damage. Summer construction/renovation electrical rough-ins.",
                    "fall (Sep-Nov): holiday lighting prep and outdoor outlet installation. Generator installations before winter storm season. Panel upgrades before winter heating load. Smoke and CO detector checks (daylight saving reminder). Weatherproof exterior outlet covers.",
                    "winter (Dec-Feb): generator emergencies during ice/snow storms. Power outage response. Space heater overload on circuits (fire risk). Holiday lighting circuit issues. Indoor electrical projects (remodels). Good time for rewiring projects (lower demand, faster scheduling)."
                }),
                EmergencyIndicators = JsonSerializer.Serialize(new[]
                {
                    "burning smell from electrical panel, outlet, or switch (fire risk, cut power)",
                    "sparking or arcing visible at outlet, panel, or wiring",
                    "electrical fire (call 911 first, then electrician for assessment)",
                    "total power loss to home (check utility first, then panel)",
                    "exposed live wires (keep away, do not touch, cut power at panel)",
                    "electric shock received from outlet or appliance (check wiring immediately)",
                    "water intrusion into electrical panel or outlets (electrocution risk)",
                    "downed power line on property (stay away, call utility, call 911)",
                    "generator back-feeding without transfer switch (electrocution risk to utility workers)",
                    "aluminum wiring overheating at connections (house fire risk, immediate inspection needed)"
                }),
                AverageServiceDuration = 120,
                TypicalPriceRange = "Service call/diagnostic: $75-$150. Outlet/switch install: $150-$300. GFCI install: $150-$250. Ceiling fan: $200-$500. Panel upgrade: $1,500-$4,000. Rewire: $8,000-$25,000. EV charger: $800-$2,500. Generator: $5,000-$15,000.",
                DisplayOrder = 5,
                IsActive = true
            },

            // ═══════════════════════════════════════════════════════════════
            // 6. Landscaping (ServiceType.Landscaping)
            // ═══════════════════════════════════════════════════════════════
            new Industry
            {
                Id = Guid.NewGuid().ToString(),
                ServiceType = ServiceType.Landscaping,
                Name = "Landscaping Services",
                Code = "landscaping",
                Description = "Lawn care, landscape design, hardscaping, irrigation, tree service, and outdoor living spaces",
                Keywords = JsonSerializer.Serialize(new[]
                {
                    "landscaping", "lawn care", "lawn mowing", "lawn maintenance",
                    "landscape design", "garden", "gardening", "planting", "mulching",
                    "tree trimming", "tree removal", "tree service", "stump grinding",
                    "hedge trimming", "shrub pruning", "bush removal",
                    "irrigation", "sprinkler system", "drip irrigation",
                    "hardscape", "paver", "patio", "retaining wall", "walkway",
                    "outdoor living", "fire pit", "outdoor kitchen",
                    "sod installation", "seeding", "overseeding", "aeration",
                    "fertilization", "weed control", "grub treatment",
                    "leaf removal", "fall cleanup", "spring cleanup",
                    "drainage", "French drain", "grading", "erosion control",
                    "landscape lighting", "water feature", "pond", "fountain",
                    "xeriscaping", "native plants", "drought tolerant"
                }),
                CommonServices = JsonSerializer.Serialize(new[]
                {
                    "lawn mowing (weekly, bi-weekly)", "lawn edging and trimming",
                    "leaf removal and fall cleanup", "spring cleanup and bed preparation",
                    "lawn aeration (core aeration)", "overseeding and dethatching",
                    "fertilization program (4-6 applications/year)", "weed control and pre-emergent",
                    "grub and pest treatment", "sod installation",
                    "landscape design and consultation", "planting (trees, shrubs, perennials, annuals)",
                    "mulch installation and bed maintenance", "garden bed creation and edging",
                    "tree trimming and pruning", "tree removal (including crane work)",
                    "stump grinding and removal", "hedge and shrub trimming",
                    "irrigation system installation", "sprinkler repair and winterization",
                    "drip irrigation installation", "smart irrigation controller installation",
                    "paver patio installation", "natural stone patio",
                    "retaining wall construction (block, stone, timber)",
                    "walkway and path installation", "driveway paver installation",
                    "outdoor fire pit and fireplace", "outdoor kitchen construction",
                    "French drain and drainage solutions", "grading and soil work",
                    "erosion control (rip rap, blankets, plantings)",
                    "landscape lighting (LED, low-voltage)", "water feature installation"
                }),
                CommonEquipment = JsonSerializer.Serialize(new[]
                {
                    "commercial zero-turn mower (Scag, Exmark, Hustler)",
                    "walk-behind mower (36-inch, 48-inch)", "push mower (21-inch)",
                    "string trimmer/weed whacker", "edger (stick, walk-behind)",
                    "backpack blower", "handheld blower",
                    "hedge trimmer (gas, battery)", "pole saw",
                    "chainsaw (gas, battery)", "stump grinder",
                    "core aerator (walk-behind, tow-behind)", "dethatcher/power rake",
                    "sod cutter", "seed spreader (broadcast, drop)",
                    "skid steer/compact loader (Bobcat)", "mini excavator",
                    "compact utility tractor", "wheelbarrow and utility cart",
                    "plate compactor (for pavers)", "jumping jack compactor",
                    "wet saw (for pavers/stone)", "masonry saw",
                    "hand tamper", "rubber mallet",
                    "rake set (landscape, leaf, bow)", "shovel set (flat, round, trenching)",
                    "trencher (walk-behind)", "sod roller",
                    "irrigation tools (pipe cutter, swing joint wrench)", "backflow test kit",
                    "soil test kit", "pH meter",
                    "tape measure and marking paint", "laser level",
                    "truck and trailer (dump trailer, enclosed)", "chipper/shredder",
                    "PPE (chaps, ear protection, eye protection, gloves)"
                }),
                TypicalIssues = JsonSerializer.Serialize(new[]
                {
                    "lawn brown spots or dead patches", "lawn thinning or bare areas",
                    "excessive weeds (broadleaf, crabgrass, clover)", "grub damage (spongy lawn, animals digging)",
                    "fungal disease (brown patch, dollar spot, fairy ring)",
                    "lawn not growing well (soil compaction, pH imbalance)",
                    "moss growing in lawn (shade, drainage, acidity)",
                    "tree limbs dead, hanging, or dangerous", "tree leaning or roots damaging property",
                    "stumps needing removal after tree cutting",
                    "shrubs overgrown, leggy, or misshapen", "hedges sparse or dying",
                    "mulch faded, thin, or washed away", "garden beds full of weeds",
                    "plants dying (overwatering, underwatering, wrong location)",
                    "irrigation system not covering evenly", "sprinkler head broken or misdirected",
                    "irrigation leak (high water bill)", "sprinkler system won't turn on",
                    "paver patio settling or uneven", "pavers shifting or gaps widening",
                    "retaining wall leaning, bulging, or failing",
                    "drainage issues (standing water, soggy yard, erosion)",
                    "downspout discharge causing erosion", "French drain clogged",
                    "slope erosion (washouts after rain)", "soil compaction in heavy traffic areas",
                    "landscape lighting not working", "deer or rabbit damage to plantings",
                    "invasive plant species spreading", "poor curb appeal"
                }),
                TechnicalTerms = JsonSerializer.Serialize(new[]
                {
                    "hardscape vs softscape", "grading (positive drainage away from structure)",
                    "compaction (sub-base for pavers)", "geotextile fabric (weed barrier, stabilization)",
                    "joint sand (polymeric sand)", "base material (class 5, crushed limestone)",
                    "retaining wall: batter, setback, geogrid", "surcharge load (wall design)",
                    "French drain (perforated pipe, gravel, fabric)", "catch basin",
                    "drip zone (tree canopy)", "root flare (proper tree planting depth)",
                    "crown (lawn grading for drainage)", "thatch layer",
                    "soil amendment (comite, peat, gypsum)", "NPK (fertilizer ratio)",
                    "pre-emergent vs post-emergent herbicide", "selective vs non-selective herbicide",
                    "irrigation zone (station)", "GPM (gallons per minute) and PSI",
                    "head-to-head coverage (sprinkler design)", "precipitation rate (inches per hour)",
                    "smart controller (weather-based, soil moisture)", "backflow preventer (irrigation)",
                    "USDA hardiness zone", "native vs non-native species",
                    "xeriscaping (water-wise landscaping)", "rain garden",
                    "bed edging (metal, stone, plastic)", "low-voltage lighting (12V transformer)"
                }),
                SeasonalConsiderations = JsonSerializer.Serialize(new[]
                {
                    "spring (Mar-May): busiest season begins. Spring cleanup, mulching, bed prep. Lawn aeration and overseeding. Pre-emergent herbicide application (timing critical). Irrigation system startup and inspection. Tree/shrub planting (ideal time). Hardscape project bookings fill fast.",
                    "summer (Jun-Aug): peak mowing season (weekly service). Irrigation management critical. Fertilization applications. Weed and grub treatments. Drought stress management. Tree trimming (after spring growth). Hardscape construction season. Landscape lighting installations.",
                    "fall (Sep-Nov): fall cleanup and leaf removal (high demand). Fall aeration and overseeding (cool-season grasses). Final fertilization (winterizer). Irrigation winterization (blow-out). Fall planting (trees, shrubs, bulbs). Retaining wall and drainage projects before freeze. Mulch top-up.",
                    "winter (Dec-Feb): planning and design season. Dormant tree pruning (ideal for most species). Snow and ice management (if offered). Equipment maintenance and sharpening. Quote and schedule spring projects. Hardscape design finalization. Holiday lighting installation/removal."
                }),
                EmergencyIndicators = JsonSerializer.Serialize(new[]
                {
                    "large tree fallen on house, car, or power line (call 911 if power line involved)",
                    "large limb hanging dangerously over house or walkway (restrict area, call arborist)",
                    "irrigation main line break (flooding, shut off water supply to system)",
                    "retaining wall actively failing or collapsing (safety risk, restrict access)",
                    "severe erosion threatening foundation or structure",
                    "sinkhole forming in yard (possible utility line break)",
                    "bee/wasp nest discovered in landscape area (allergic reaction risk)",
                    "gas line struck during digging (evacuate area, call 811/gas company)",
                    "storm damage cleanup (debris blocking access, hazardous limbs)"
                }),
                AverageServiceDuration = 180,
                TypicalPriceRange = "Mowing: $30-$80/visit. Mulching: $50-$100/yard. Tree trimming: $300-$1,500. Tree removal: $500-$5,000+. Paver patio: $15-$30/sq ft. Retaining wall: $20-$50/sq ft face. Irrigation install: $2,500-$6,000. Landscape design+install: $5,000-$50,000+.",
                DisplayOrder = 6,
                IsActive = true
            },

            // ═══════════════════════════════════════════════════════════════
            // 7. PestControl (ServiceType.PestControl)
            // ═══════════════════════════════════════════════════════════════
            new Industry
            {
                Id = Guid.NewGuid().ToString(),
                ServiceType = ServiceType.PestControl,
                Name = "Pest Control Services",
                Code = "pest-control",
                Description = "Residential and commercial pest management, termite treatment, wildlife removal, and prevention services",
                Keywords = JsonSerializer.Serialize(new[]
                {
                    "pest control", "exterminator", "pest management", "IPM",
                    "termite", "termite treatment", "termite inspection", "wood-destroying insects",
                    "ant", "carpenter ant", "fire ant", "ant colony",
                    "cockroach", "roach", "German cockroach", "American cockroach",
                    "bed bug", "bed bug treatment", "bed bug heat treatment",
                    "rodent", "mouse", "rat", "rodent control", "rodent exclusion",
                    "mosquito", "mosquito treatment", "tick", "flea",
                    "spider", "brown recluse", "black widow", "wolf spider",
                    "wasp", "hornet", "yellow jacket", "bee removal",
                    "wildlife removal", "raccoon", "squirrel", "bat",
                    "fumigation", "tent fumigation", "spot treatment",
                    "pest prevention", "quarterly service", "monthly service",
                    "organic pest control", "green pest control", "eco-friendly"
                }),
                CommonServices = JsonSerializer.Serialize(new[]
                {
                    "general pest treatment (ants, spiders, roaches, silverfish)",
                    "quarterly pest prevention program", "monthly pest management program",
                    "termite inspection (WDI/WDO report)", "termite liquid treatment (Termidor, etc.)",
                    "termite bait station installation and monitoring (Sentricon, Trelona)",
                    "termite damage assessment", "wood-destroying insect report (real estate)",
                    "bed bug inspection (visual, canine)", "bed bug heat treatment (whole room)",
                    "bed bug chemical treatment", "bed bug follow-up monitoring",
                    "rodent trapping and removal", "rodent exclusion (sealing entry points)",
                    "rodent bait station program", "rodent damage assessment",
                    "mosquito yard treatment (spray, misting system)", "tick and flea yard treatment",
                    "wasp/hornet/yellow jacket nest removal", "bee relocation (live removal)",
                    "carpenter ant treatment", "fire ant treatment (mound, broadcast)",
                    "cockroach treatment (German, American, Oriental)", "cockroach gel bait program",
                    "spider treatment (interior, exterior web removal)",
                    "wildlife removal (raccoon, squirrel, opossum, bat)",
                    "wildlife exclusion and habitat modification", "dead animal removal",
                    "fumigation (whole structure, drywood termites)", "crawl space treatment",
                    "commercial pest management (restaurants, offices, warehouses)"
                }),
                CommonEquipment = JsonSerializer.Serialize(new[]
                {
                    "backpack sprayer (manual, electric)", "compressed air sprayer (B&G)",
                    "power sprayer (truck-mounted, gas)", "mist blower (ULV fogger)",
                    "bait gun (gel applicator)", "dust applicator (hand bellows, electric)",
                    "termite treatment rig (drill, pump, sub-slab injection)",
                    "termiticide injection rod (soil rod)", "foam generator (expansion foam)",
                    "rodent bait stations (tamper-resistant)", "snap traps (various sizes)",
                    "glue boards", "live catch traps (cage traps)",
                    "exclusion materials (copper mesh, steel wool, caulk, hardware cloth)",
                    "moisture meter (termite inspection)", "thermal imaging camera (pest detection)",
                    "borescope/inspection camera (wall voids)", "flashlight (high-lumen)",
                    "bed bug heat treatment equipment (propane/electric heaters, fans, thermometers)",
                    "bed bug mattress encasements", "bed bug interceptors (monitors)",
                    "canine bed bug detection (trained K-9)",
                    "ladder set (extension, step)", "crawl space suit and PPE",
                    "respirator (half-face, full-face) for fumigation", "SCBA (self-contained breathing for fumigation)",
                    "fumigation tarps and clamps", "warning signs and tape (fumigation)",
                    "Vikane gas (sulfuryl fluoride) monitoring device", "Fumiscope (gas concentration)",
                    "granular spreader", "bait stations (ant, cockroach)",
                    "probing tool (termite damage assessment)", "sounding device (tap for termite hollows)"
                }),
                TypicalIssues = JsonSerializer.Serialize(new[]
                {
                    "ants trailing inside home (kitchen, bathroom, foundation cracks)",
                    "carpenter ants (sawdust frass, hollow wood sounds)",
                    "fire ant mounds in yard (painful stings, allergic reactions possible)",
                    "cockroaches in kitchen/bathroom (droppings, egg cases, musty smell)",
                    "German cockroach infestation (rapid reproduction, hard to eliminate)",
                    "termite swarmers found indoors (spring, discarded wings near windows)",
                    "termite mud tubes on foundation (subterranean termite activity)",
                    "termite damage discovered during renovation (structural concern)",
                    "bed bugs (bites in line pattern, blood spots on sheets, live bugs)",
                    "mice in walls or attic (scratching sounds, droppings, gnaw marks)",
                    "rats under house or in attic (larger droppings, gnaw damage, grease marks)",
                    "rodent smell (dead rodent in wall or crawl space)",
                    "spiders (webs, concern about venomous species)", "brown recluse concern",
                    "wasp nest near entry door or under eaves", "yellow jacket ground nest in yard",
                    "mosquitoes in yard (standing water, breeding habitat)",
                    "fleas in home (pets, biting ankles)", "ticks in yard (Lyme disease concern)",
                    "raccoon in attic (noise at night, entry damage, droppings)",
                    "squirrels in attic (chewing wires, fire risk)", "bats in attic (guano, rabies concern)",
                    "birds nesting in vents or eaves", "wildlife under deck or shed",
                    "pantry moths (webbing in food products)", "carpet beetles (damage to fabrics)",
                    "stored product pests (weevils, Indian meal moths)",
                    "wood-boring beetles (exit holes, frass in furniture or structure)"
                }),
                TechnicalTerms = JsonSerializer.Serialize(new[]
                {
                    "IPM (Integrated Pest Management)", "exclusion (sealing entry points)",
                    "harborage (pest hiding/nesting location)", "conducive conditions",
                    "WDI/WDO report (wood-destroying insect/organism)", "Section 1 vs Section 2 (CA termite report)",
                    "subterranean vs drywood vs dampwood termite", "mud tube (shelter tube)",
                    "frass (insect excrement/debris)", "swarmers (alates, reproductive termites/ants)",
                    "bait matrix (termite bait active ingredient)", "non-repellent termiticide (transfer effect)",
                    "pre-treatment (new construction termite barrier)", "post-treatment (existing structure)",
                    "active ingredient (AI)", "mode of action (how pesticide works)",
                    "residual (how long product remains effective)", "knockdown (immediate kill effect)",
                    "resistance (pest tolerance to pesticides)", "rotation (alternating AI classes)",
                    "bait vs barrier (treatment approaches)", "crack and crevice application",
                    "void treatment (wall injection)", "perimeter treatment (exterior spray)",
                    "fumigation (tenting, whole-structure gas)", "Vikane (sulfuryl fluoride)",
                    "EPA registration number", "SDS (Safety Data Sheet)",
                    "re-entry interval (REI)", "signal word (Caution, Warning, Danger)",
                    "structural fumigation clearance reading", "secondary inspection (QA check)"
                }),
                SeasonalConsiderations = JsonSerializer.Serialize(new[]
                {
                    "spring (Mar-May): termite swarming season peaks (subterranean). Ant activity surges as colonies expand. Overwintering pests emerge (stink bugs, ladybugs, cluster flies). Mosquito breeding begins with rain. Pre-season mosquito/tick yard treatment. Busiest termite inspection period (real estate).",
                    "summer (Jun-Aug): peak pest activity across all species. Wasp/hornet nests reach maximum size. Mosquito and tick season at peak. Flea infestations spike (pets outdoors more). Ant trailing at highest. Spider activity increases. Drywood termite swarmers in southern regions. Highest service call volume.",
                    "fall (Sep-Nov): rodent invasion season begins (seeking warmth). Overwintering pests enter homes (stink bugs, Asian beetles, boxelder bugs). Wasp nests decline but yellow jackets aggressive near food. Fall pest prevention treatment ideal. Wildlife exclusion before winter. Bed bug awareness (travel season aftermath).",
                    "winter (Dec-Feb): rodent calls peak (mice and rats indoors). Wildlife in attics (raccoons, squirrels seeking warmth). Indoor pest focus (cockroaches, spiders). Pantry pest complaints increase (holiday baking/stored foods). Slower season for outdoor pests. Good time for structural fumigation (lower temps in some regions)."
                }),
                EmergencyIndicators = JsonSerializer.Serialize(new[]
                {
                    "severe allergic reaction to stings (call 911 for anaphylaxis, then pest control)",
                    "large wasp/hornet nest near door, play area, or high-traffic zone",
                    "bed bug infestation spreading to multiple rooms (rapid treatment needed)",
                    "termite structural damage discovered (beam, floor, load-bearing member)",
                    "rodents chewing electrical wiring (fire hazard, electrician + pest control)",
                    "bat colony in living space (rabies exposure risk, do not handle)",
                    "raccoon or wildlife acting erratically (possible rabies, call animal control)",
                    "severe cockroach infestation (health code risk for businesses)",
                    "venomous snake on property (do not approach, call wildlife removal)"
                }),
                AverageServiceDuration = 90,
                TypicalPriceRange = "Initial treatment: $150-$400. Quarterly service: $100-$200/visit. Termite inspection: $75-$150. Termite treatment: $1,500-$5,000. Bed bug heat: $1,500-$4,000/room. Rodent exclusion: $500-$3,000. Wildlife removal: $300-$1,500. Fumigation: $5,000-$15,000.",
                DisplayOrder = 7,
                IsActive = true
            },

            // ═══════════════════════════════════════════════════════════════
            // 8. Handyman (ServiceType.Handyman)
            // ═══════════════════════════════════════════════════════════════
            new Industry
            {
                Id = Guid.NewGuid().ToString(),
                ServiceType = ServiceType.Handyman,
                Name = "Handyman Services",
                Code = "handyman",
                Description = "General repairs, installations, maintenance tasks, furniture assembly, and small home improvement projects",
                Keywords = JsonSerializer.Serialize(new[]
                {
                    "handyman", "handy man", "home repair", "fix it", "odd jobs",
                    "minor repair", "small jobs", "honey-do list", "maintenance",
                    "installation", "mounting", "hanging", "assembly",
                    "furniture assembly", "TV mounting", "shelf installation",
                    "door repair", "lock installation", "weather stripping",
                    "drywall repair", "patching", "spackling", "caulking",
                    "painting", "touch-up painting", "staining",
                    "fixture replacement", "faucet replacement", "toilet repair",
                    "light fixture", "ceiling fan", "outlet cover",
                    "tile repair", "grout repair", "flooring repair",
                    "fence repair", "deck repair", "gate repair",
                    "gutter cleaning", "power washing", "screen repair",
                    "garage organization", "shelving", "closet organization",
                    "childproofing", "aging-in-place", "grab bar installation"
                }),
                CommonServices = JsonSerializer.Serialize(new[]
                {
                    "drywall patching and repair (holes, cracks, water damage)",
                    "interior painting (room, accent wall, trim, doors)",
                    "exterior painting (touch-up, shutters, trim, small areas)",
                    "caulking (bathrooms, kitchens, windows, exterior)",
                    "weather stripping and door sweep replacement",
                    "door repair (sticking, hinge, latch, deadbolt)",
                    "door installation (interior, closet, pet door)",
                    "lock and deadbolt installation/replacement",
                    "window repair (hardware, balance, screen, caulk)",
                    "screen door/window screen repair or replacement",
                    "light fixture installation and replacement",
                    "ceiling fan installation and replacement",
                    "outlet and switch cover replacement",
                    "TV wall mounting (with cable concealment)",
                    "shelf and shelving installation (floating, bracket, closet)",
                    "furniture assembly (IKEA, office, outdoor)",
                    "picture/mirror/art hanging", "curtain and blind installation",
                    "faucet replacement (kitchen, bathroom)",
                    "toilet repair (flapper, handle, fill valve, wax ring)",
                    "garbage disposal replacement",
                    "tile and grout repair (cracked tiles, re-grouting)",
                    "flooring repair (squeaky boards, transition strips, thresholds)",
                    "fence repair (boards, posts, gate alignment)",
                    "deck board replacement and minor repair",
                    "gutter cleaning and minor gutter repair",
                    "pressure washing (small areas, walkways, patios)",
                    "grab bar and safety rail installation",
                    "smoke and CO detector installation",
                    "mailbox installation or repair", "garage organization and storage solutions"
                }),
                CommonEquipment = JsonSerializer.Serialize(new[]
                {
                    "cordless drill/driver", "impact driver",
                    "circular saw", "jigsaw", "reciprocating saw",
                    "oscillating multi-tool", "miter saw (portable)",
                    "sander (orbital, detail)", "hand plane",
                    "level set (torpedo, 2-foot, 4-foot)", "stud finder",
                    "tape measure and speed square", "laser level",
                    "hammer (framing, finish)", "mallet (rubber, dead-blow)",
                    "pry bar and flat bar", "utility knife and razor blades",
                    "screwdriver set (Phillips, flat, Torx, hex)", "nut driver set",
                    "pliers set (needle nose, channel lock, linesman)", "adjustable wrench set",
                    "pipe wrench (small)", "basin wrench",
                    "Allen key/hex key set (metric and SAE)", "socket set",
                    "drywall knife set (4, 6, 10 inch)", "mud pan and hawk",
                    "caulking gun", "putty knife set",
                    "paint supplies (roller, brush, tray, tape, drop cloth)",
                    "tile cutter (manual)", "grout float and sponge",
                    "wire stripper and voltage tester", "fish tape (short)",
                    "ladder (step, extension)", "work light",
                    "shop vacuum", "tool bag/toolbox organized system",
                    "fastener assortment (screws, nails, anchors, bolts)"
                }),
                TypicalIssues = JsonSerializer.Serialize(new[]
                {
                    "drywall hole from door knob or accident", "nail pops in drywall",
                    "drywall cracks along seams or corners", "water-damaged drywall (small area)",
                    "door sticking or not closing properly", "door knob loose or broken",
                    "cabinet door or drawer not closing", "cabinet hinge broken or misaligned",
                    "faucet dripping or handle loose", "toilet running or handle broken",
                    "shower head leaking or clogged", "under-sink leak (minor)",
                    "light fixture not working (after bulb change)", "ceiling fan wobbling",
                    "outlet or switch not working (basic)", "doorbell not working",
                    "window won't stay open or won't lock", "broken window screen",
                    "tile cracked or grout missing in bathroom/kitchen",
                    "squeaky floor or stairs", "baseboard or trim damaged or loose",
                    "fence board broken or gate sagging", "deck board warped or loose",
                    "gutter overflowing or pulling away", "downspout disconnected",
                    "weather stripping worn (door drafts)", "caulk cracked around tub/windows",
                    "smoke detector chirping (beyond battery change)", "garage door opener issues (minor)",
                    "mailbox post leaning", "exterior handrail loose",
                    "picture fell (anchor pulled out of wall)", "closet rod or shelf fallen",
                    "appliance installation (dishwasher, microwave)", "furniture assembly needed"
                }),
                TechnicalTerms = JsonSerializer.Serialize(new[]
                {
                    "toggle bolt (hollow wall anchor)", "molly bolt",
                    "drywall anchor (plastic, self-drilling, toggle)", "tapcon (concrete screw)",
                    "lag bolt/lag screw", "carriage bolt",
                    "stud (16-inch on center, 24-inch on center)", "blocking (added framing support)",
                    "shim (leveling material)", "plumb and level",
                    "miter joint (45-degree)", "butt joint",
                    "coped joint (crown molding)", "scarf joint",
                    "joint compound (mud, spackle)", "mesh tape vs paper tape (drywall)",
                    "primer (drywall, stain-blocking, adhesion)", "latex vs oil-based paint",
                    "P-trap", "supply line (braided, compression)",
                    "wax ring (toilet)", "flapper valve (toilet)",
                    "wire nut (twist connector)", "Romex (NM-B cable)",
                    "GFCI outlet", "15A vs 20A circuit",
                    "shiplap", "beadboard", "wainscoting",
                    "LVP (luxury vinyl plank)", "transition strip",
                    "quarter round (shoe molding)", "base cap"
                }),
                SeasonalConsiderations = JsonSerializer.Serialize(new[]
                {
                    "spring (Mar-May): busiest booking season. Outdoor repair demand (fence, deck, pressure wash). Interior touch-up painting after winter. Screen repair for windows and doors. Gutter cleaning after winter debris. Caulking and weather seal refresh.",
                    "summer (Jun-Aug): outdoor project focus (deck repair, fence, gate, exterior painting). Ceiling fan installations peak. TV mounting and organization projects. Vacation home prep. High demand, book 1-2 weeks ahead. Good weather for exterior caulking and painting.",
                    "fall (Sep-Nov): winterization tasks (weather stripping, caulking, storm doors). Gutter cleaning before winter. Interior projects ramp up (painting, shelving, organization). Holiday prep (lighting installation, grab bars for visiting family). Smoke/CO detector check.",
                    "winter (Dec-Feb): interior project season. Furniture assembly (post-holiday). Painting, drywall repair, shelving. Lower demand means better availability. Home organization and closet systems. Small bathroom updates. Draft-proofing urgent for comfort and energy savings."
                }),
                EmergencyIndicators = JsonSerializer.Serialize(new[]
                {
                    "water leak causing active damage (refer to plumber for major, handyman for minor mitigation)",
                    "broken exterior door lock (security risk, immediate replacement needed)",
                    "broken window (security and weather exposure, board up or replace)",
                    "handrail or railing broken on stairs (fall risk, especially elderly)",
                    "smoke detector not working and no backup (fire safety risk)",
                    "gas smell (do NOT attempt to fix, evacuate, call gas company)",
                    "exposed electrical wires (do NOT touch, refer to licensed electrician)",
                    "ceiling drywall sagging or falling (water damage, possible structural issue)",
                    "toilet overflow that won't stop (shut off valve, basic repair or refer to plumber)"
                }),
                AverageServiceDuration = 120,
                TypicalPriceRange = "Hourly rate: $50-$100/hour (2-4 hour minimum typical). Drywall patch: $75-$250. Door repair: $100-$300. Faucet replace: $150-$350. TV mount: $100-$300. Furniture assembly: $100-$400. Painting (room): $200-$600. Fence repair: $150-$500.",
                DisplayOrder = 8,
                IsActive = true
            },

            // ═══════════════════════════════════════════════════════════════
            // 9. Roofing (ServiceType.Roofing)
            // ═══════════════════════════════════════════════════════════════
            new Industry
            {
                Id = Guid.NewGuid().ToString(),
                ServiceType = ServiceType.Roofing,
                Name = "Roofing Services",
                Code = "roofing",
                Description = "Roof installation, repair, replacement, inspection, gutter services, and weatherproofing",
                Keywords = JsonSerializer.Serialize(new[]
                {
                    "roofing", "roof", "roof repair", "roof replacement", "reroof",
                    "shingles", "asphalt shingles", "architectural shingles", "3-tab shingles",
                    "metal roof", "standing seam", "metal roofing",
                    "tile roof", "clay tile", "concrete tile", "slate roof",
                    "flat roof", "low-slope roof", "TPO", "EPDM", "modified bitumen",
                    "roof leak", "roof damage", "storm damage", "wind damage", "hail damage",
                    "flashing", "chimney flashing", "valley flashing", "step flashing",
                    "gutter", "gutter installation", "gutter repair", "gutter guard",
                    "downspout", "soffit", "fascia", "drip edge",
                    "roof inspection", "roof certification", "roof warranty",
                    "ice dam", "ventilation", "ridge vent", "soffit vent",
                    "skylight", "roof coating", "waterproofing", "underlayment"
                }),
                CommonServices = JsonSerializer.Serialize(new[]
                {
                    "roof inspection (annual, storm, buyer/seller)",
                    "roof certification letter (real estate transaction)",
                    "asphalt shingle roof installation (new construction)",
                    "asphalt shingle roof replacement (tear-off and reroof)",
                    "asphalt shingle repair (missing, damaged, blown-off)",
                    "architectural/dimensional shingle upgrade",
                    "metal roof installation (standing seam, corrugated, panels)",
                    "metal roof repair", "tile roof installation (clay, concrete)",
                    "tile roof repair (cracked, broken, slipped tiles)",
                    "slate roof repair and restoration",
                    "flat/low-slope roof installation (TPO, EPDM, modified bitumen)",
                    "flat roof repair and patching", "flat roof coating and maintenance",
                    "flashing repair and replacement (chimney, wall, valley, pipe boot)",
                    "chimney cap and cricket installation", "skylight installation and repair",
                    "ridge vent installation", "soffit and ridge vent system",
                    "attic ventilation assessment and improvement",
                    "gutter installation (seamless aluminum, copper)", "gutter repair",
                    "gutter guard/leaf protection installation", "gutter cleaning",
                    "downspout installation and rerouting", "fascia and soffit repair/replacement",
                    "drip edge installation", "ice and water shield installation",
                    "storm damage assessment and insurance claim assistance",
                    "emergency tarp and board-up (storm/tree damage)",
                    "roof coating application (silicone, acrylic, elastomeric)",
                    "commercial roof maintenance program"
                }),
                CommonEquipment = JsonSerializer.Serialize(new[]
                {
                    "roofing nail gun (pneumatic, coil)", "cap nail gun (underlayment)",
                    "air compressor (roofing grade, high CFM)", "roofing shovel/shingle ripper",
                    "pry bar and flat bar", "utility knife and hook blade knife",
                    "chalk line and reel", "roofing hatchet (for wood shakes)",
                    "tin snips (aviation, straight, left, right)", "seamer (standing seam)",
                    "metal brake (portable for flashing)", "pop rivet gun",
                    "caulking gun and roofing sealant", "roof cement (trowel grade, brushable)",
                    "propane torch (modified bitumen/torch-down)", "hot kettle (built-up roofing)",
                    "roller (flat roof membrane)", "seam probe and test cut tools",
                    "tape measure (100-foot)", "speed square and framing square",
                    "level (2-foot, 4-foot)", "laser level",
                    "ladder set (extension 28-40ft, step)", "ladder stabilizer/standoff",
                    "roof brackets and scaffold planks", "safety harness and rope system",
                    "roof anchor (temporary, permanent)", "warning line system",
                    "dump trailer (shingle disposal)", "conveyor/hoist (material loading)",
                    "magnetic nail sweeper", "broom and blower (cleanup)",
                    "moisture meter (deck inspection)", "infrared camera (leak detection, flat roof)",
                    "core cutter (roof sample)", "drone (roof inspection, large commercial)",
                    "gutter machine (seamless gutter fabrication)", "gutter crimper and outlet cutter"
                }),
                TypicalIssues = JsonSerializer.Serialize(new[]
                {
                    "roof leak (active water intrusion during rain)", "water stains on ceiling or walls",
                    "missing shingles after wind storm", "shingles curling, buckling, or lifting",
                    "shingle granule loss (bald spots, granules in gutters)",
                    "cracked or broken shingles", "nail pops (exposed nail heads)",
                    "flashing deteriorated or lifted (chimney, wall, valley)",
                    "pipe boot/vent collar cracked or deteriorated",
                    "valley leaking (improper flashing or debris buildup)",
                    "ridge cap shingles blown off or damaged",
                    "ice dam forming at eaves (water backing up under shingles)",
                    "attic too hot in summer (inadequate ventilation)",
                    "attic moisture/condensation (ventilation imbalance)",
                    "sagging roof deck (structural concern, water damage)",
                    "moss, algae, or lichen growth on shingles",
                    "flat roof ponding water (standing water after rain)",
                    "flat roof membrane bubbling, splitting, or shrinking",
                    "metal roof panel loose, lifted, or dented",
                    "metal roof fastener backing out", "tile cracked or slipped out of place",
                    "gutters overflowing, leaking at seams, or pulling away",
                    "downspout clogged or disconnected", "fascia board rotting",
                    "soffit damaged or falling (pest entry point)",
                    "skylight leaking or condensation forming",
                    "hail damage (dents, bruising, cracked shingles)",
                    "tree branch damage to roof", "animal damage (raccoon, squirrel entry)",
                    "insurance claim needed for storm damage",
                    "roof age approaching end of life (15-20 year shingles showing wear)"
                }),
                TechnicalTerms = JsonSerializer.Serialize(new[]
                {
                    "square (100 sq ft of roof area)", "pitch/slope (rise over run, e.g. 6/12)",
                    "deck/sheathing (plywood, OSB substrate)", "underlayment (felt, synthetic)",
                    "ice and water shield (self-adhering membrane)", "drip edge (eave and rake)",
                    "starter strip (first course at eave)", "exposure (visible portion of shingle)",
                    "tab and laminate/dimensional (shingle types)", "wind rating (mph, class D, G, H)",
                    "impact resistance (Class 1-4, hail rating)", "algae resistance (AR shingles)",
                    "cool roof (reflective, Energy Star rated)", "SRI (Solar Reflectance Index)",
                    "flashing (step, counter, apron, valley, chimney cricket)",
                    "pipe boot/vent collar (roof penetration seal)",
                    "ridge vent (shingle-over, aluminum)", "soffit vent (continuous, individual)",
                    "NFA (net free area, ventilation calculation)", "balanced ventilation (intake/exhaust)",
                    "TPO (thermoplastic polyolefin)", "EPDM (ethylene propylene diene monomer)",
                    "modified bitumen (SBS, APP)", "built-up roof (BUR, tar and gravel)",
                    "standing seam (concealed fastener metal)", "exposed fastener metal panel",
                    "Galvalume (aluminum-zinc coated steel)", "gauge (metal thickness, 24-26 typical)",
                    "warranty: manufacturer vs workmanship", "GAF, Owens Corning, CertainTeed (major brands)"
                }),
                SeasonalConsiderations = JsonSerializer.Serialize(new[]
                {
                    "spring (Mar-May): peak booking season begins. Storm damage repair from winter/spring storms. Insurance claim season ramps up. Ideal weather for shingle installation (adhesive activation needs warmth). Gutter cleaning after winter. Roof inspection recommended. Book summer projects now.",
                    "summer (Jun-Aug): busiest installation season. Best weather for all roof types. Longest work days (daylight). Shingle adhesive seals best in heat. Metal and flat roof installs ideal. Highest demand, longest wait times (book 2-4 weeks ahead). Heat safety for crews is priority.",
                    "fall (Sep-Nov): final push before winter. Last chance for full replacements before cold. Storm prep and gutter work. Pre-winter inspections. Shingle install still viable until temps drop below 40F consistently. Gutter guard installation popular. Schedule before holiday season.",
                    "winter (Dec-Feb): emergency repair season. Ice dam issues in cold climates. Emergency tarps for leaks. Limited full replacements (cold temperature restrictions for shingles). Interior leak investigation. Good time for estimates and planning spring projects. Metal roof can install in cold (no adhesive dependency)."
                }),
                EmergencyIndicators = JsonSerializer.Serialize(new[]
                {
                    "active roof leak during rainstorm (immediate tarp needed, interior damage mitigation)",
                    "large section of roof blown off by wind (structure exposed to weather, emergency tarp)",
                    "tree fallen onto roof (structural damage assessment, tree removal coordination)",
                    "fire damage to roof section (structural assessment after fire department clears)",
                    "ice dam causing interior water damage (careful removal, do not damage roof)",
                    "sagging roof deck visible from inside attic (possible structural failure, evacuate room below)",
                    "hail storm damage requiring immediate documentation for insurance claim",
                    "animal breach through roof into living space (temporary seal, wildlife removal)",
                    "chimney or vent cap blown off (water entry and animal entry risk)",
                    "flat roof membrane torn in storm (ponding water will cause rapid interior damage)"
                }),
                AverageServiceDuration = 240,
                TypicalPriceRange = "Inspection: $100-$300. Minor repair: $200-$600. Leak repair: $300-$1,000. Shingle roof replacement: $8,000-$20,000 (avg home). Metal roof: $15,000-$35,000. Flat roof: $5,000-$15,000. Gutters: $1,000-$3,000. Gutter guards: $1,500-$4,000.",
                DisplayOrder = 9,
                IsActive = true
            }
        };
    }
}
