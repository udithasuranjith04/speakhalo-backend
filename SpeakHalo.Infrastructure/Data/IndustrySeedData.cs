using Microsoft.EntityFrameworkCore;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using System.Text.Json;

namespace SpeakHalo.Infrastructure.Data;

/// <summary>
/// Seeds comprehensive cleaning industry data with AI training metadata
/// </summary>
public static class IndustrySeedData
{
    public static async Task SeedIndustriesAsync(ApplicationDbContext context)
    {
        if (await context.Industries.AnyAsync())
            return; // Already seeded

        var industries = new List<Industry>
        {
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
                    // Residential
                    "regular house cleaning", "deep cleaning", "move-in/move-out cleaning",
                    "spring cleaning", "recurring maintenance cleaning",
                    // Specialized
                    "carpet steam cleaning", "carpet dry cleaning", "carpet encapsulation",
                    "window washing (interior)", "window washing (exterior)", "window track cleaning",
                    "post-construction cleanup (rough)", "post-construction cleanup (detail)", "post-construction cleanup (final)",
                    "pressure washing (driveway)", "pressure washing (deck)", "pressure washing (siding)", "soft washing",
                    "tile and grout cleaning", "grout sealing", "natural stone care",
                    "upholstery cleaning", "leather cleaning and conditioning", "mattress sanitizing",
                    // Commercial
                    "office cleaning (daily)", "office cleaning (weekly)", "office deep clean",
                    "retail/storefront cleaning", "medical/dental office cleaning",
                    "restaurant kitchen deep clean", "restaurant dining area cleaning",
                    "gym/fitness center cleaning", "warehouse/industrial cleaning",
                    "day porter service", "floor stripping and waxing",
                    // Emergency/Special
                    "hoarding cleanup", "biohazard/trauma cleanup", "flood cleanup",
                    "fire/smoke damage cleanup", "odor removal/ozone treatment",
                    "disinfection/sanitization service", "COVID/illness deep sanitize"
                }),
                CommonEquipment = JsonSerializer.Serialize(new[]
                {
                    // Vacuums
                    "commercial backpack vacuum (ProTeam, Windsor)", "HEPA filter vacuum",
                    "wet/dry shop vacuum", "upright commercial vacuum",
                    // Carpet
                    "truck-mount carpet extractor", "portable carpet extractor (Rug Doctor, Bissell BigGreen)",
                    "carpet bonnet machine", "carpet encapsulation machine",
                    // Floor
                    "auto scrubber (for large commercial)", "floor buffer/burnisher",
                    "floor stripping pads", "microfiber mop system (flat mop)",
                    // Pressure washing
                    "gas pressure washer (3000-4000 PSI)", "electric pressure washer (1500-2000 PSI)",
                    "surface cleaner attachment", "soft wash pump system",
                    // Window
                    "professional squeegee set (Ettore, Unger)", "extension poles (telescoping)",
                    "water-fed pole system", "window scrubber sleeves",
                    // Cleaning tools
                    "microfiber cloths (color-coded)", "grout brush set",
                    "caddy/cleaning tote", "spray bottles (labeled)",
                    "scrub pads (white/green/blue)", "pumice stone (toilet ring)",
                    "razor blade scraper (glass)", "steam cleaner (handheld and upright)",
                    // Safety
                    "PPE kit (gloves, masks, goggles)", "biohazard PPE (Tyvek suit, respirator)",
                    "step ladder (3-step)", "extension ladder",
                    // Products
                    "commercial all-purpose cleaner", "hospital-grade disinfectant",
                    "enzyme cleaner (pet/bio)", "degreaser (kitchen/industrial)",
                    "glass cleaner", "stainless steel polish",
                    "grout sealer", "wood floor cleaner (Bona)",
                    "eco-friendly product line (Green Seal certified)",
                    "carpet pre-spray", "carpet spotter kit"
                }),
                TypicalIssues = JsonSerializer.Serialize(new[]
                {
                    // Stains
                    "red wine stains", "pet urine stains and odor", "blood stains",
                    "grease and oil stains", "coffee/tea stains", "ink stains",
                    "rust stains", "candle wax on carpet/fabric", "paint spills",
                    "marker/crayon on walls", "food stains on upholstery",
                    // Surface issues
                    "hard water deposits (shower glass, faucets)", "soap scum buildup",
                    "mineral deposits on fixtures", "water rings on wood furniture",
                    "scratches on hardwood floors", "etching on marble/granite",
                    "dull/hazy laminate floors", "yellowed vinyl flooring",
                    // Biological
                    "mold and mildew (shower grout, windowsills)", "black mold on caulking",
                    "mildew smell in closets/basements", "dust mite buildup in mattresses",
                    "pet hair embedded in upholstery", "allergen accumulation",
                    // Buildup
                    "grease buildup (kitchen hood, stove, backsplash)",
                    "calcium/lime buildup on faucets",
                    "soap residue on shower walls",
                    "wax buildup on floors (over-waxed)",
                    "dust accumulation in air vents and ducts",
                    "cobwebs in high corners and ceiling fans",
                    // Damage assessment
                    "grout cracking/missing", "caulk deterioration around tub/shower",
                    "discolored grout lines", "carpet wear in high-traffic paths",
                    "burned/melted carpet spots", "water damage stains on ceiling/walls"
                }),
                TechnicalTerms = JsonSerializer.Serialize(new[]
                {
                    // Methods
                    "hot water extraction (HWE)", "encapsulation cleaning",
                    "bonnet cleaning", "dry compound cleaning",
                    "soft washing", "pressure washing (PSI ratings)",
                    "stripping and refinishing (floors)", "burnishing",
                    // Products/chemistry
                    "HEPA filtration", "pH-neutral cleaners",
                    "alkaline cleaners (degreasers)", "acidic cleaners (mineral removal)",
                    "enzyme-based cleaners (biological)", "oxygen bleach (sodium percarbonate)",
                    "quaternary ammonium (quat) disinfectants", "hydrogen peroxide-based cleaners",
                    "contact time (disinfectant dwell time)", "EPA-registered disinfectant",
                    // Certifications
                    "Green Seal certified", "EPA Safer Choice",
                    "ISSA CIMS certification", "OSHA bloodborne pathogen standard",
                    // Fabric/surface
                    "colorfast testing", "W/S/WS/X fabric codes",
                    "grout sealing/impregnating", "stone honing vs polishing",
                    // Industry
                    "square footage rate", "per-room pricing",
                    "first clean vs recurring rate", "turnover clean",
                    "day porter", "zone cleaning",
                    "color-coded microfiber system (prevent cross-contamination)",
                    "top-to-bottom cleaning method", "clockwise room method"
                }),
                SeasonalConsiderations = JsonSerializer.Serialize(new[]
                {
                    "spring (Mar-May): peak deep cleaning season — spring cleaning demand surge, book 2-3 weeks ahead. Window washing, screen cleaning, patio/deck pressure washing, mattress sanitizing, closet organization",
                    "summer (Jun-Aug): move-in/move-out peak season (lease turnover). Vacation home prep/post cleaning. Outdoor services in demand: pressure washing, deck cleaning, exterior windows. Humidity increases mold/mildew risk in bathrooms and basements",
                    "fall (Sep-Nov): pre-holiday deep cleans (Thanksgiving, Christmas prep). Window cleaning before storm season. Gutter-related exterior requests. Back-to-school office cleaning contracts start. Commercial year-end cleaning budgets spent",
                    "winter (Dec-Feb): slowest period — indoor focus. Carpet cleaning ideal (low humidity = faster drying). Post-holiday cleanup demand spike (first 2 weeks January). Salt/mud tracked in = more frequent floor service. Good time for commercial deep cleans during slower business periods"
                }),
                EmergencyIndicators = JsonSerializer.Serialize(new[]
                {
                    "biohazard cleanup (blood, bodily fluids, needles) — PPE required, OSHA protocols",
                    "unattended death/decomposition — specialized team, full PPE, insurance documentation",
                    "flood/water damage cleanup — extract water fast, mold starts in 24-48 hours",
                    "fire/smoke damage — soot cleanup, odor removal, content cleaning",
                    "sewage backup — contaminated water extraction, sanitization, health hazard",
                    "hoarding situation (severe) — multi-day, compassionate approach, possible pest/mold",
                    "pest infestation cleanup (after exterminator) — droppings, nests, contaminated surfaces",
                    "chemical spill — ventilation, neutralization, proper disposal",
                    "mold outbreak (large area >10 sq ft) — refer to certified mold remediation specialist",
                    "infectious disease decontamination — hospital-grade disinfectants, full PPE, contact time critical"
                }),
                AverageServiceDuration = 180,
                TypicalPriceRange = "Residential: $120-$400 regular, $250-$800 deep, $300-$900 move-out. Commercial: $0.05-$0.35/sq ft. Specialized: $75-$1,500+ depending on service.",
                DisplayOrder = 1,
                IsActive = true
            }
        };

        await context.Industries.AddRangeAsync(industries);
        await context.SaveChangesAsync();
    }
}
