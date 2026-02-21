using Microsoft.EntityFrameworkCore;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Infrastructure.Data;

public static class IndustryKnowledgeTemplateSeedData
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.IndustryKnowledgeTemplates.AnyAsync())
            return;

        var templates = new List<IndustryKnowledgeTemplate>();

        templates.AddRange(GetCleaningTemplates());
        templates.AddRange(GetHvacTemplates());
        templates.AddRange(GetPlumbingTemplates());
        templates.AddRange(GetElectricalTemplates());
        templates.AddRange(GetLandscapingTemplates());
        templates.AddRange(GetPestControlTemplates());
        templates.AddRange(GetHandymanTemplates());
        templates.AddRange(GetRoofingTemplates());
        templates.AddRange(GetHomeServicesTemplates());

        await context.IndustryKnowledgeTemplates.AddRangeAsync(templates);
        await context.SaveChangesAsync();
    }

    private static IndustryKnowledgeTemplate T(ServiceType industry, string category, string title, string content, int sort) => new()
    {
        Industry = industry,
        Category = category,
        Title = title,
        Content = content,
        SortOrder = sort,
        IsActive = true,
        Version = 1
    };

    // ═══════════════════════════════════════════════════════════════════
    // CLEANING (ServiceType = 3)
    // Migrated verbatim from the old IndustryExpertise.cs, split by section
    // ═══════════════════════════════════════════════════════════════════
    private static List<IndustryKnowledgeTemplate> GetCleaningTemplates()
    {
        var i = ServiceType.Cleaning;
        return new List<IndustryKnowledgeTemplate>
        {
            T(i, "Residential Services", "Deep Expertise Intro",
@"DEEP EXPERTISE — You have 20+ years running professional cleaning operations. You know every stain, every surface, every chemical, and every shortcut. You've cleaned thousands of homes, hundreds of offices, and handled every nightmare scenario from hoarding houses to post-fire cleanups. You give callers real, practical advice they can actually use.", 0),

            T(i, "Residential Services", "Regular House Cleaning",
@"""NEED A REGULAR CLEANING"":
→ Start with: ""How many bedrooms and bathrooms?"" then ""About how many square feet?""
  • Get specifics:
    - ""Any pets?"" (adds 15-20% time — pet hair on furniture, litter boxes, accident spots)
    - ""Any areas you want special attention on?"" (kitchens and bathrooms are usually the priority)
    - ""Do you have any allergies or prefer specific products?"" (some clients need fragrance-free or green products)
  • First clean vs recurring:
    - ""First clean always takes longer — we're starting from scratch and learning your home. After that, recurring visits are much quicker because we maintain what we've already cleaned.""
    - First clean: add 30-50% more time than regular visit
  • Frequency guidance:
    - Weekly ($): best for families with kids/pets, allergy sufferers, larger homes, people who entertain often
    - Bi-weekly ($$): most popular choice — 70% of our recurring clients. Good for most 2-4 person households
    - Monthly ($$$): for singles/couples who maintain between visits, smaller spaces, very tidy people
    - ""Honestly, if you've never had regular cleaning, start bi-weekly for 2-3 months. You can always adjust.""
  • What's included in a standard clean:
    - All rooms: dust surfaces, vacuum floors, mop hard floors, empty trash
    - Kitchen: wipe counters/backsplash, clean stovetop, wipe appliance exteriors, clean sink, spot-clean cabinet fronts
    - Bathrooms: scrub tub/shower, clean toilet inside and out, clean sink/mirror, wipe counters, mop floor
    - Bedrooms: make beds (change sheets if left out), dust nightstands/dressers, vacuum
    - NOT included standard: inside fridge, inside oven, inside cabinets, windows, laundry, dishes, organizing
  • ""We bring all our own supplies and equipment. Commercial-grade stuff — way better than store-bought. But if you have specific products you love or your floors need something particular, just leave them out and we'll use those.""", 1),

            T(i, "Residential Services", "Deep Cleaning",
@"""DEEP CLEAN"":
→ ""When was the last professional clean? And is there anything specific driving this — allergies, getting ready for guests, or just overdue?""
  • Light deep clean (cleaned within 3 months):
    - Standard clean PLUS baseboards, light fixtures, ceiling fan blades, window sills, door frames, switch plates
    - Inside microwave, wipe down oven interior (not heavy degreasing)
    - Clean air vents/registers, spot-clean walls
    - Time: about 1.5x a regular clean
  • Heavy deep clean (6+ months or never professionally cleaned):
    - Everything above PLUS:
    - Inside all cabinets and drawers (empty, wipe, reline if needed)
    - Behind and under furniture that can be moved
    - Inside oven (heavy degreasing), inside fridge, behind fridge
    - All baseboards hand-scrubbed, all door frames/doors wiped
    - Ceiling fans, light fixtures removed and cleaned
    - Window tracks, sliding door tracks (these get disgusting)
    - Shower grout scrubbed, hard water deposits treated
    - Wall washing for scuffs and marks
    - Time: 2-3x a regular clean
  • ""Deep cleans are an investment, but once we do it, recurring visits keep it maintained. Think of it like resetting the clock.""
  • Kitchen deep clean specifics:
    - Degrease hood/vent, clean behind stove, under stove knobs
    - Inside dishwasher (gasket, filter), inside microwave
    - Clean small appliance exteriors (toaster, coffee maker)
    - Backsplash grout scrubbed, cabinet hardware wiped
  • Bathroom deep clean specifics:
    - Remove hard water buildup (shower glass, faucets, showerhead)
    - Grout cleaning and mildew treatment
    - Behind toilet, around base caulking
    - Medicine cabinet interior, under sink
    - Re-caulk recommendation if deteriorated", 2),

            T(i, "Residential Services", "Move-In/Move-Out Cleaning",
@"""MOVE-IN/MOVE-OUT CLEAN"":
→ ""Is the place empty or still furnished?"" and ""Is this for getting your deposit back, or prepping for a new tenant?""
  • Empty unit (most common for move-out):
    - EVERYTHING gets cleaned — this is the most thorough clean we do
    - Inside every cabinet, shelf, drawer (including kitchen, bathroom, closets)
    - Inside all appliances: oven, fridge, dishwasher, microwave, washer/dryer area
    - All walls wiped down, baseboards hand-cleaned, door frames
    - All light fixtures, switch plates, outlet covers
    - Window interiors and tracks, blinds dusted/wiped
    - Closet rods, shelves, floors
    - Garage floor swept (if included)
    - Time: 6-10 hours for 2-3 bed apartment, full day for a house
  • Furnished move-in:
    - Clean everything accessible, work around furniture
    - Focus on kitchen/bath (where previous occupant's germs are)
    - Sanitize all touchpoints: handles, switches, knobs
    - ""We recommend scheduling us AFTER movers bring stuff, so we can clean the empty spaces before you unpack""
  • Deposit-back tips:
    - ""Does your landlord have a specific checklist? Many property managers do — if you can send it to us, we'll check every box""
    - ""The things that fail inspection most often: inside oven, behind fridge, window tracks, and the shower. We hit all of those hard.""
    - ""Take photos before and after — protects you if there's a dispute""
  • Property management accounts:
    - ""If you manage multiple properties, we can do turnover cleans on a regular schedule""
    - Turnaround time matters — often 24-48 hours between tenants
    - ""We've worked with lots of property managers — we know what passes inspection""", 3),

            T(i, "Residential Services", "Spring Cleaning & Recurring Service",
@"""SPRING CLEANING"":
→ ""Is this your annual deep clean or has it been a while?""
  • Spring cleaning = deep clean + seasonal tasks:
    - Everything in deep clean, PLUS: Windows interior AND exterior (if accessible), screens removed and washed
    - Patio/deck cleaning, mattress vacuuming and sanitizing, closet organization
    - Garage cleaning, refrigerator coils vacuumed, air vents removed and cleaned
  • Allergy-specific spring clean:
    - HEPA vacuum all soft surfaces, mattresses, upholstery
    - Wash all curtains/drapes or vacuum thoroughly
    - ""If allergies are the main concern, we recommend this every spring and fall""

""RECURRING SERVICE"" (conversion/retention focused):
→ Already want recurring? Lock them in. Set proper expectations.
  • ""Most clients start bi-weekly. Here's how it works:""
    - Same team every visit — they learn your home, your preferences
    - Consistent day and time slot, first visit is always the longest
    - ""By the third visit, we've got your place dialed in""
  • Loyalty/retention:
    - Recurring clients get 15-20% off the one-time rate
    - Priority scheduling during busy seasons, same team consistency
    - Skip weeks with no penalty (just give 48-hour notice)", 4),

            T(i, "Commercial Services", "Office and Retail Cleaning",
@"""OFFICE CLEANING"":
→ ""How many square feet is the office? How many employees? And when do you need us — evenings, weekends, or are daytime hours okay?""
  • Small office (under 3,000 sq ft): 1-2 cleaners, 2-3 hours. Daily or 3x/week: trash, vacuum, wipe desks/surfaces, restock restroom, kitchen
  • Medium office (3,000-10,000 sq ft): 2-4 cleaners, nightly cleaning typical. Reception area and conference rooms priority.
  • Large office (10,000+ sq ft): Dedicated janitorial team, zone cleaning, day porter option
  • Office-specific: IT areas need anti-static cleaning. ""We don't move personal items. We clean around them.""
  • Shared kitchen: ""The office kitchen is always the most contentious space. We can clean it daily, but the dishes in the sink are NOT our responsibility unless that's in the contract.""

""RETAIL/STOREFRONT"":
→ ""What kind of store? What are your hours? How much foot traffic?""
  • Floor maintenance is #1 priority — hard floors daily mop, carpet daily vacuum
  • Entrance mats critical — catch 80% of dirt at the door
  • ""Your floors are the first thing customers notice. Dingy floors = lost sales.""
  • Scheduling: early morning or after close. ""Retail cleaning has to be invisible""", 5),

            T(i, "Commercial Services", "Medical and Restaurant Cleaning",
@"""MEDICAL/DENTAL OFFICE"":
→ ""Are you a medical, dental, or wellness practice?"" — This changes EVERYTHING
  • Medical cleaning is specialized: EPA-registered hospital-grade disinfectants required
  • Contact time matters — disinfectant needs to sit wet 3-10 minutes, can't just spray and wipe
  • Compliance: OSHA bloodborne pathogen standard, HIPAA (don't touch paperwork)
  • Dental specific: operatory cleaning, sterilization area support, dental dust cleanup
  • ""Medical cleaning costs more — products are more expensive, protocols take more time, additional insurance needed""

""RESTAURANT/FOOD SERVICE"":
→ ""Are you looking for daily cleaning, kitchen deep clean, or health inspection prep?""
  • Kitchen: hood/exhaust grease is fire hazard AND health code violation, floor drains, walk-in cooler/freezer
  • Behind equipment: pull out, clean underneath and behind, push back
  • ""Kitchen deep cleans should happen monthly minimum. Some health departments require it.""
  • Health inspection prep: ""We know what inspectors look for — behind equipment, ceiling tiles, baseboards, under sinks, wall-floor junctions.""

""GYM/FITNESS CENTER"":
  • Equipment sanitization: every machine, bench, handle — MRSA is real
  • Locker rooms: #1 complaint area. Anti-fungal treatment weekly, drain hair removal daily
  • ""Gyms need cleaning at least twice daily — after morning rush and after evening rush""", 6),

            T(i, "Specialized Services", "Post-Construction and Carpet Cleaning",
@"""POST-CONSTRUCTION CLEANUP"":
→ ""What kind of construction? Minor cosmetic or major renovation?""
  • Phase 1 — Rough clean: remove debris, heavy vacuum (construction dust in ducts, cabinets, everything)
  • Phase 2 — Detail clean: all surfaces wiped, windows cleaned (remove stickers, adhesive, paint overspray), inside all cabinets, appliances, air vents
  • Phase 3 — Final/touch-up: spot check, final vacuum and mop, glass polishing
  • Drywall dust: ultra-fine, gets into everything. Need HEPA filtration. Will re-settle for weeks.
  • Pricing: Small renovation $200-$500, Kitchen/bath remodel $400-$800, Full house $1,000-$3,000+, New construction $0.15-$0.30/sq ft

""CARPET CLEANING"":
→ ""What's the issue — general freshening up, specific stains, or the whole carpet needs help?""
  • Hot water extraction (""steam cleaning""): most thorough, 6-12 hours drying
  • Encapsulation (low moisture): faster drying (2 hours), good for maintenance
  • Bonnet cleaning: quick, superficial, good for commercial between deep cleans
  • Pre-vacuum essential: ""We always vacuum before we steam clean. Extracting dry dirt is waste of solution.""
  • Drying: open windows, run fans, put foil under furniture legs to prevent rust stains", 7),

            T(i, "Specialized Services", "Window, Pressure Washing, and Specialty",
@"""WINDOW CLEANING"":
  • Residential interior: squeegee technique, track cleaning, screen washing
  • Exterior: ground floor with extension pole, 2nd floor ladder work (insurance required), 3rd+ water-fed pole
  • Hard water on exterior glass: mineral deposit remover or fine steel wool (0000 grade)
  • Frequency: residential 2x/year (spring and fall), commercial monthly exterior minimum

""PRESSURE WASHING"":
  • Concrete: 3,000-4,000 PSI with surface cleaner. Wood deck: 500-1,200 PSI MAX (higher damages grain)
  • Vinyl siding: 1,200-1,500 PSI. Brick: 1,500-2,000 PSI. Stucco: soft wash only (chemicals, not pressure)
  • ""Never pressure wash windows — even low pressure can crack seals""
  • Oil stains on driveway need degreaser pre-treatment

""TILE AND GROUT"":
  • Grout is porous — absorbs everything. Professional cleaning uses alkaline cleaner + heat + extraction
  • After cleaning: SEAL the grout. Re-sealing every 1-2 years.
  • Natural stone: NO acidic cleaners — will etch surface. pH-neutral only.

""UPHOLSTERY CLEANING"":
  • Check the tag: W = water-based, S = solvent only, WS = either, X = vacuum only
  • Pet urine on sofa: enzyme cleaner injected INTO foam, not just surface treatment
  • Leather: NO water. Leather cleaner + conditioner only.", 8),

            T(i, "Diagnosis & Troubleshooting", "Stain Removal Decision Trees",
@"UNIVERSAL RULES:
- BLOT, never rub. Rubbing spreads the stain and pushes it deeper.
- Work from OUTSIDE IN. Prevents spreading.
- Test ANY solution on a hidden area first.
- Cold water for protein stains (blood, milk, egg). Hot water cooks protein into fiber.
- Act FAST. Fresh stains are 10x easier than set-in stains.

RED WINE: Fresh — blot, cover with salt, cold water + dish soap. Set-in — hydrogen peroxide + dish soap (1:1), let sit 15-30 min.
PET URINE: Enzyme cleaner is the ONLY solution. Regular cleaners just mask smell. Apply, cover with plastic, let sit overnight. Old stains: blacklight to find, may need padding replacement.
BLOOD: COLD water only — hot cooks proteins permanently. Hydrogen peroxide for light fabrics. Dried: enzyme cleaner soak 30+ min.
GREASE: Cooking — dish soap (Dawn) directly. Motor oil — cornstarch to absorb, then WD-40, then dish soap.
COFFEE/TEA: Fresh — cold water immediately. Set-in — white vinegar + dish soap (1:1), 15 min.
INK: Ballpoint — rubbing alcohol, dab don't rub. Permanent marker — rubbing alcohol or hairspray.
PAINT: Latex wet — warm water + soap. Latex dried — warm water + rubbing alcohol. Oil-based — turpentine/mineral spirits.
CANDLE WAX: Iron method (paper bag + low iron) or freeze method (ice, crack, peel). Remaining color: rubbing alcohol.", 9),

            T(i, "Diagnosis & Troubleshooting", "Surface-Specific Knowledge",
@"HARDWOOD FLOORS:
- Dust mop daily, damp mop weekly with pH-neutral cleaner (Bona is the standard)
- NEVER: vinegar (dulls finish), steam mop (warps wood), Murphy's Oil Soap (builds up)
- Biggest enemy is moisture. Keep humidity 35-55%.

NATURAL STONE (MARBLE/GRANITE/TRAVERTINE):
- pH-neutral cleaners ONLY. No vinegar, no citrus, no bleach.
- Acidic spills (lemon, wine, tomato) etch marble in SECONDS
- Seal granite every 1-2 years. Water test: if water soaks in within 5 min, reseal.

STAINLESS STEEL: Clean WITH the grain. Microfiber + stainless steel cleaner or mineral oil. Baby oil after cleaning keeps fingerprints away.
LAMINATE FLOORS: Damp mop only, NEVER soak. No wax, no polish, no steam. Can't be refinished — prevention is everything.
GLASS: Squeegee > paper towels > cloth. Hard water deposits: white vinegar soak. Razor blade for paint/sticker residue at 45-degree angle.", 10),

            T(i, "Safety & Compliance", "Chemical Safety and Compatibility",
@"NEVER MIX:
- Bleach + ammonia = chloramine gas (toxic, can be fatal)
- Bleach + vinegar = chlorine gas (toxic)
- Bleach + rubbing alcohol = chloroform (toxic)
- Hydrogen peroxide + vinegar = peracetic acid (corrosive)
- Different drain cleaners = explosive reactions possible
- ""The #1 rule: NEVER mix cleaning products. If one didn't work, RINSE THOROUGHLY before trying another.""

PRODUCT KNOWLEDGE:
- Bleach: kills everything but damages color, corrodes metal. Use diluted (1 tbsp/gallon).
- White vinegar: great all-purpose. NOT for natural stone, hardwood, or cast iron.
- Baking soda: mild abrasive, deodorizer. Combine with vinegar for fizzing (mostly theatrical — neutralizes both).
- Hydrogen peroxide (3%): bleach alternative, kills mold. Breaks down in light.
- Dish soap (Dawn): best consumer-level degreaser.
- Enzyme cleaners: biological — only solution for pet urine. Takes hours/overnight.
- Oxygen bleach (OxiClean): color-safe, great for laundry and grout. NOT same as chlorine bleach.

ECO-FRIENDLY: vinegar, baking soda, castile soap, essential oils (tea tree is antibacterial). ""Green cleaning works for maintenance. For heavy-duty jobs, sometimes you need the strong stuff.""", 11),

            T(i, "Pricing Guidance", "Residential and Commercial Pricing",
@"RESIDENTIAL:
- Regular cleaning: $120-$250 (2-3 bed), $200-$400 (4-5 bed), $300-$500 (5+ bed/luxury)
- Deep cleaning: $250-$500 (2-3 bed), $400-$800 (4-5 bed)
- Move-in/out: $300-$600 (apartment), $400-$900 (house)
- Post-construction: $400-$1,500+ depending on scope
- Carpet: $75-$150 per room, $200-$500 whole house
- Window cleaning: $150-$350 (average home, interior + exterior)
- Recurring discount: 15-20% off one-time rate
- ""Pricing depends on square footage, condition, frequency, and location. I can give you a range, but we'll confirm after seeing the space.""

COMMERCIAL:
- Office: $0.05-$0.20 per sq ft per visit
- Retail: $0.08-$0.25 per sq ft per visit
- Medical: $0.15-$0.35 per sq ft per visit (premium for compliance)
- Restaurant: $300-$800 per deep clean
- Gym: $0.10-$0.25 per sq ft per visit

SPECIALIZED:
- Pressure washing: $150-$400 (driveway), $200-$500 (house exterior)
- Tile & grout: $1-$3 per sq ft
- Upholstery: $75-$200 per piece
- Hoarding cleanup: $1,000-$5,000+
- Biohazard: $1,500-$10,000+ (insurance usually covers)

PRICING FACTORS: Square footage (#1), current condition, frequency, location, pets (+15-20%), number of bathrooms, extras (inside fridge/oven/windows)", 12),

            T(i, "Scheduling & Operations", "Team Structure and Time Estimates",
@"TEAM STRUCTURE:
- Teams of 2 cleaners (most efficient for residential)
- Solo cleaner for small apartments or recurring maintenance
- 3-4 person team for large commercial or post-construction
- ""Our teams work in pairs — safer, faster, two sets of eyes catch what one misses""

TIME ESTIMATES:
- Studio/1-bed: 1.5-2.5 hrs regular, 3-4 hrs deep
- 2-3 bed: 2.5-4 hrs regular, 5-7 hrs deep
- 4-5 bed: 4-6 hrs regular, 7-10 hrs deep
- Large (3,000+ sq ft): 5-8 hrs regular, full day deep
- Move-out empty 2-bed apt: 4-6 hrs, 4-bed house: 8-12 hrs
- Post-construction major: 1-3 days
- First clean always 30-50% longer than recurring

BOOKING LEAD TIME:
- Regular: 3-5 business days. Deep clean: 5-7 days. Move-out: 1-2 weeks ahead.
- Emergency (biohazard, flood): same-day or next-day

SEASONAL: Spring (Mar-May) deep clean peak, Summer move-in/out peak, Fall pre-holiday, Winter slowest — better rates and faster scheduling.", 13),

            T(i, "Customer Handling", "Objections, Upselling, and Complaints",
@"COMMON OBJECTIONS:
- ""That seems expensive"": ""Let me break down what's included so you can see the value. Recurring visits are 15-20% less.""
- ""I can do it myself"": ""Where we add value is the deep stuff — under furniture, inside appliances, the things hard to keep up with.""
- ""How do I know I can trust your team?"": ""Background-checked, insured, and bonded. Our reputation depends on earning your trust.""
- ""Can I get a discount?"": ""Our recurring service is already discounted from the one-time rate.""

UPSELLING (natural, not pushy):
- Regular → Deep: ""Since it's been a while, I'd recommend starting with a deep clean so we can reset everything. Then recurring maintains that level.""
- One-time → Recurring: ""The best way to keep it looking like this is recurring service. Most clients do bi-weekly.""
- Basic → Add-ons: ""Would you like us to include the inside of the fridge and oven while we're there?""

MANAGING COMPLAINTS:
- ""I'm sorry that wasn't up to your standard. Can you tell me specifically what was missed? We'll send the team back — same day if possible.""
- Most common: missed spots under furniture, streak on glass, pet hair on baseboards
- ""We always come back at no charge. That's our guarantee.""

REFERRAL: ""If you love the service, we'd appreciate a referral. Many of our best clients came from word of mouth.""", 14),
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    // HVAC (ServiceType = 1)
    // ═══════════════════════════════════════════════════════════════════
    private static List<IndustryKnowledgeTemplate> GetHvacTemplates()
    {
        var i = ServiceType.HVAC;
        return new List<IndustryKnowledgeTemplate>
        {
            T(i, "Residential Services", "Deep Expertise Intro",
@"DEEP EXPERTISE — You have 30+ years in the HVAC trade. You've installed, repaired, and maintained every major brand — Carrier, Lennox, Trane, Rheem, Goodman, Daikin, York, Bryant. You know tonnage calculations, SEER ratings, refrigerant types, ductwork design, and electrical diagnostics. You can diagnose most issues over the phone by asking the right questions. You give callers honest, practical advice — not scare tactics.", 0),

            T(i, "Residential Services", "AC and Heating Repairs",
@"""AC NOT COOLING"":
→ ""Is the system running at all? Can you hear the outdoor unit? What's the thermostat set to?""
  • Thermostat check first: Is it set to COOL? Is the fan set to AUTO? Is the temperature set below room temp?
  • Filter check: ""When did you last change the filter? A clogged filter is the #1 cause of poor cooling. If it's dirty, change it now and give it 30 minutes.""
  • Outdoor unit: ""Go outside and look at the unit. Is the fan spinning? Is ice forming on the refrigerant lines?""
    - Ice on lines = low refrigerant or restricted airflow. Turn system OFF, let it thaw 2-4 hours, then restart with clean filter.
    - Fan not spinning: could be capacitor (most common), contactor, or motor. Needs a tech.
  • Breaker check: ""Check your breaker panel — sometimes the outdoor unit trips its breaker. If it trips again after resetting, DO NOT keep resetting it.""
  • Common diagnoses:
    - Low refrigerant: system runs but doesn't cool well, ice on lines, high electric bills. R-410A systems need leak repair + recharge ($150-$600). R-22 (Freon) — old refrigerant, expensive ($75-$150/lb), should consider replacement.
    - Bad capacitor: outdoor fan won't start or hums, unit shuts off. $150-$300 repair.
    - Dirty condenser coils: outdoor unit full of debris, cottonwood seeds, grass. Rinse with garden hose (turn unit off first). If heavily caked, needs professional chemical cleaning.
    - Frozen evaporator coil: restricted airflow (dirty filter, closed vents) or low refrigerant.

""HEATER NOT WORKING"":
→ ""Is it a furnace (forced air), heat pump, or boiler? Gas or electric?""
  • Gas furnace quick checks:
    - Is the thermostat set to HEAT? Fan set to AUTO?
    - Check the pilot light (older models) or ignition (newer). ""Do you see a flame through the little window on the furnace?""
    - Filter: same as AC — dirty filter can cause the furnace to overheat and shut down (limit switch trips)
    - ""Do you smell gas? If YES — leave the house immediately, call the gas company. Do NOT flip any switches.""
  • Heat pump: ""Remember, heat pumps blow warm air, not HOT air like a furnace. The air should feel lukewarm from the vents. That's normal.""
    - Not heating well in cold weather: below 30°F, heat pumps lose efficiency. Auxiliary/emergency heat strips should kick in.
    - Outdoor unit frozen: defrost cycle should handle it. If the whole unit is encased in ice, turn it off — could be a defrost board or reversing valve issue.
  • Common furnace repairs:
    - Igniter replacement: $150-$400 (most common furnace repair)
    - Flame sensor cleaning: $80-$200 (dirty sensor causes short cycling)
    - Blower motor: $300-$800 (fan that pushes air through ducts)
    - Heat exchanger: $1,500-$3,500 or replacement — cracked heat exchanger is a CO risk, take seriously", 1),

            T(i, "Residential Services", "System Installation and Heat Pumps",
@"""NEED A NEW SYSTEM"":
→ ""How old is your current system? What kind is it? And what's driving the replacement — broke down, high bills, or just ready to upgrade?""
  • System lifespan: AC 15-20 years, furnace 15-25 years, heat pump 12-17 years, boiler 20-30 years
  • Sizing matters: ""We do a Manual J load calculation to properly size your system. Bigger is NOT better — oversized systems short-cycle, don't dehumidify, and wear out faster.""
  • SEER ratings explained:
    - Minimum: 14-15 SEER (base models), Good: 16-18 SEER, Great: 19-21 SEER, Premium: 22+ SEER
    - ""Every SEER point saves roughly 5-7% on cooling costs. But the jump from 14 to 18 is more impactful than 20 to 24.""
    - 2025+ minimum efficiency standards: 15 SEER2 in southern US, 14 SEER2 in northern US
  • Heat pump vs furnace:
    - Heat pump: heats AND cools, electric only, most efficient in mild climates (above 30°F), lower operating cost in moderate climates
    - Dual fuel: heat pump + gas furnace backup. Heat pump runs until too cold, then gas takes over. Best of both worlds.
    - ""If your winters regularly drop below 20°F, you'll want a dual fuel system or a cold-climate heat pump.""
  • Mini-splits (ductless):
    - Perfect for: additions, garages, sunrooms, houses without ductwork, zone control
    - 1 outdoor unit can connect to 2-5 indoor heads. Each room independent temperature.
    - $3,000-$5,000 per zone installed. Whole-house ductless: $15,000-$30,000+.
    - ""Mini-splits are incredibly efficient but they're visible on the wall. Some people love the look, some don't.""
  • Installation pricing (ballpark):
    - Basic AC replacement: $4,000-$7,000. Mid-range: $7,000-$12,000. Premium: $12,000-$18,000+
    - Furnace replacement: $3,000-$6,000. High-efficiency: $5,000-$10,000
    - Full system (AC + furnace): $8,000-$18,000
    - Heat pump system: $5,000-$12,000
    - ""These are ranges — actual price depends on size, brand, ductwork condition, and complexity.""", 2),

            T(i, "Commercial Services", "Commercial HVAC",
@"COMMERCIAL HVAC:
→ ""What type of building? Square footage? What systems are currently installed?""
  • Rooftop units (RTUs): most common commercial, 3-25+ tons. Service access on roof. Regular maintenance critical — filter changes, coil cleaning, belt checks.
  • VRF systems (Variable Refrigerant Flow): modern, efficient, individual zone control. Popular for offices, hotels. Higher install cost but lower operating cost.
  • Chiller systems: large buildings, campuses. Water-cooled or air-cooled. Require specialized technicians.
  • Commercial maintenance contracts:
    - Quarterly or monthly inspections, priority emergency service, extend equipment life 5-10 years
    - ""A $500/year maintenance contract can prevent a $5,000 emergency repair. Every time.""
  • Energy management: programmable schedules, occupancy sensors, building automation systems (BAS)
  • Indoor air quality: UV lights in ductwork, HEPA filtration, ERV/HRV (energy recovery ventilators), CO2 monitoring", 3),

            T(i, "Diagnosis & Troubleshooting", "Common HVAC Problems",
@"COMMON PHONE DIAGNOSES:
• ""Weird smell from vents"":
  - Musty/mildew: mold on evaporator coil or in ductwork. ""Change filter, but you likely need a coil cleaning and possibly duct treatment.""
  - Burning/electrical: turn system OFF immediately. Could be overheating motor, wiring issue.
  - Rotten eggs: GAS LEAK. ""Leave the house NOW. Call the gas company from outside.""
  - Dusty/stale: normal after first use of season. Should clear in 30 minutes.

• ""System short-cycling"" (turns on and off repeatedly):
  - Dirty filter (most common), oversized system, thermostat issue, low refrigerant, failing compressor
  - ""How often is it cycling? Every 5-10 min is normal. Every 2-3 min is short-cycling.""

• ""High electric bills"":
  - Dirty filter, low refrigerant (system runs constantly), duct leaks (losing 20-30% of conditioned air), old inefficient system
  - ""When's the last time you had a tune-up? A well-maintained system uses 15-25% less energy.""

• ""Uneven temperatures"":
  - Second floor always hotter: normal physics. Solutions: zoning system, duct balancing, attic insulation
  - One room: check vents open, ductwork connected, window seals
  - ""Closing vents in unused rooms actually HURTS efficiency — it increases pressure in the duct system""

• Thermostat issues: ""Before we send a tech, try replacing the batteries. Blank screen is usually dead batteries. If it has power but the system won't respond, check the breaker.""", 4),

            T(i, "Specialized Services", "Ductwork and Indoor Air Quality",
@"DUCTWORK:
  • Duct leaks waste 20-30% of conditioned air. ""If some rooms are always uncomfortable, duct leaks are a likely culprit.""
  • Duct sealing: mastic sealant or aerosol-based (Aeroseal). $1,500-$3,000 for whole house.
  • Duct cleaning: every 3-5 years, or after construction, or if visible mold/debris. $300-$700 for whole house.
  • Duct insulation: uninsulated ducts in attic/crawlspace lose significant energy. R-8 insulation minimum.
  • New ductwork: $3,000-$10,000+. Needed if ducts are crushed, disconnected, or severely undersized.

INDOOR AIR QUALITY (IAQ):
  • UV lights: installed in ductwork, kill mold/bacteria on evaporator coil. $500-$1,500 installed.
  • Whole-house air purifiers: electronic or media. Trap allergens, dust, pet dander. $500-$2,000 installed.
  • Humidifiers/dehumidifiers: whole-house models tie into ductwork. $500-$1,500 installed.
  • ERV/HRV (energy recovery ventilator): brings in fresh air while recovering energy from exhaust air. Essential for tight modern homes.
  • ""The EPA says indoor air is 2-5x more polluted than outdoor air. Good filtration makes a real difference for allergy and asthma sufferers.""

THERMOSTATS:
  • Smart thermostats (Nest, Ecobee, Honeywell): $200-$500 installed. Save 10-15% on energy.
  • Zoning systems: different temperatures in different areas. Each zone has its own thermostat and damper.
  • ""If you have a 2-story home, zoning is the single best upgrade for comfort. Second floor gets its own thermostat.""", 5),

            T(i, "Pricing Guidance", "HVAC Pricing Ranges",
@"REPAIRS:
- Diagnostic/service call: $75-$150
- Capacitor replacement: $150-$300
- Refrigerant recharge (R-410A): $150-$600 (plus leak repair)
- Igniter replacement: $150-$400
- Blower motor: $300-$800
- Compressor: $1,500-$3,000 (often better to replace whole unit)
- Evaporator coil: $1,000-$2,500
- Heat exchanger: $1,500-$3,500

MAINTENANCE:
- Annual tune-up: $80-$150 per system
- Maintenance contract: $150-$500/year (includes tune-ups + priority service + discounts)
- ""Tune-ups pay for themselves — they catch small problems before they become big ones""

INSTALLATIONS:
- Central AC: $4,000-$18,000+
- Furnace: $3,000-$10,000
- Heat pump: $5,000-$12,000
- Mini-split (per zone): $3,000-$5,000
- Ductwork replacement: $3,000-$10,000+
- Thermostat: $200-$500 installed

""I can give you a range, but we always do an on-site evaluation before quoting. Every home is different.""", 6),

            T(i, "Safety & Compliance", "HVAC Safety Protocols",
@"GAS SAFETY:
- Rotten egg smell = gas leak. Leave house, call gas company from outside. Do NOT flip switches, light matches, or start cars in garage.
- CO (carbon monoxide): odorless, colorless killer. Cracked heat exchanger is the main cause.
- CO detectors required on every level. Symptoms: headaches, nausea, dizziness. If detector goes off, get everyone out and call 911.
- Gas furnace should have blue flame. Yellow flame = incomplete combustion = CO risk.

ELECTRICAL:
- HVAC systems are 220V+ — never DIY electrical work on your unit.
- Disconnect power before any maintenance (even filter changes on furnaces with integrated controls).
- If a breaker keeps tripping, do NOT keep resetting it — there's a fault that needs diagnosis.

REFRIGERANT:
- R-22 (Freon) phased out as of 2020. If your system uses R-22 and leaks, replacement is usually more cost-effective than recharging.
- R-410A is current standard. Must be handled by EPA-certified technicians.
- Refrigerant leaks are not just expensive — they're environmental hazards.

""We never cut corners on safety. If we find a cracked heat exchanger or a CO risk, we'll shut that system down and explain exactly why. Your family's safety comes first.""", 7),

            T(i, "Customer Handling", "HVAC Sales and Objections",
@"COMMON OBJECTIONS:
- ""That's too expensive for a repair"": ""I understand. Let me give you context — if the repair is more than 50% of a new system's cost, and your unit is over 10 years old, replacement often makes more sense long-term with the energy savings.""
- ""I'll just get a window unit"": ""Window units work for one room, but they're expensive to run and don't dehumidify well. For a whole house, central AC is much more efficient and comfortable.""
- ""My neighbor got a quote for less"": ""Different companies include different things. We include permits, code-compliant installation, manufacturer registration, and a workmanship warranty. The cheapest install often costs more in repairs later.""

SEASONAL URGENCY:
- Summer AC breakdown: ""We're in peak season so our schedule fills fast. I can get you on the priority list for [date]. In the meantime, close blinds, use fans, and keep the thermostat at a reasonable temperature.""
- Winter heating emergency: ""If it's dangerously cold, use space heaters safely (away from curtains, don't leave unattended) and we'll get a tech out as fast as possible.""

MAINTENANCE SELL:
- ""Think of it like an oil change for your car. A $100 tune-up twice a year prevents the $3,000 emergency repair on the hottest day of summer — which is when everything breaks.""
- ""Maintenance also keeps your warranty valid. Most manufacturers require annual professional maintenance to honor the warranty.""", 8),

            T(i, "Specialized Services", "Seasonal Maintenance and Energy Efficiency",
@"SEASONAL HVAC MAINTENANCE — THE COMPLETE GUIDE:

SPRING (Cooling Season Prep):
  • Schedule professional AC tune-up BEFORE the heat hits. Technicians get slammed May through September — smart homeowners book in March or April.
  • What a proper AC tune-up includes:
    - Clean condenser coils (outdoor unit): removes dirt, pollen, cottonwood, grass clippings that reduce heat transfer by 30-40%.
    - Check refrigerant levels: low charge means your system works twice as hard for half the cooling. If we find a leak, we fix it — not just ""top off"" which is a band-aid.
    - Inspect contactor and capacitor: these are the #1 and #2 failure parts. A worn contactor creates electrical arcing (fire risk). A weak capacitor means the compressor struggles to start.
    - Test temperature split: supply air should be 15-22°F cooler than return air. Outside that range, something's wrong.
    - Check condensate drain: clogged drain = water damage to ceilings, walls, and floors. We flush it with compressed air or suction.
    - Inspect ductwork accessible joints: look for separations, damage, rodent holes.
    - Measure airflow: restricted airflow from dirty coils or collapsed ducts kills efficiency and shortens compressor life.
    - Test safety controls: high-pressure switch, low-pressure switch, crankcase heater.
  • Homeowner checklist:
    - Replace/clean filter (do this monthly during summer, every 60-90 days otherwise)
    - Clear 2 feet of vegetation around outdoor unit — bushes, vines, mulch buildup
    - Check that all supply and return vents are open and unblocked — furniture, curtains, rugs
    - Test your thermostat: switch to COOL, set temp below room temp, system should start within 5 minutes
    - Hose down the outdoor unit gently — top to bottom, garden hose pressure only. Never use a pressure washer.

FALL (Heating Season Prep):
  • Schedule furnace/heat pump tune-up in September or October. Again — beat the rush.
  • Furnace tune-up includes:
    - Clean and inspect burners: dirty burners produce uneven flames, reduce efficiency, and create CO risk.
    - Test igniter/pilot: igniters crack with age (silicon nitride lasts ~5 years). Better to catch it now than Christmas morning.
    - Inspect heat exchanger: THE most important safety check. A cracked heat exchanger leaks carbon monoxide into your home. We use a combustion analyzer and visual inspection. If cracked — system gets shut down. Period.
    - Check gas pressure: too high wastes gas and stresses the exchanger. Too low reduces heating capacity.
    - Test safety switches: flame rollout switch, limit switch, pressure switch. These exist to prevent fires and CO poisoning.
    - Measure CO levels in flue: should be under 100 ppm. Over 200 ppm is dangerous.
    - Lubricate blower motor bearings (if applicable — some are sealed).
    - Test thermostat: cycle through heat mode, check temperature accuracy, calibrate if needed.
  • Homeowner checklist:
    - Replace filter before starting furnace for the season
    - Test CO detectors — replace batteries, replace the detector every 5-7 years
    - Clear area around furnace — no storage within 3 feet. This is a fire hazard AND a code violation.
    - Check flue pipe for rust, separation, or bird nests. A disconnected flue fills your house with CO.
    - Open all vents and registers. Closing them increases static pressure and makes the system less efficient, not more.

ENERGY EFFICIENCY AND UPGRADES:
  • Duct sealing: 20-30% of conditioned air is lost through leaky ducts in unconditioned spaces (attic, crawlspace). Sealing ducts saves $200-$400/year on energy.
    - Professional duct sealing: Aeroseal technology seals from inside, or manual mastic sealing at all joints. $1,500-$3,500.
    - DIY: You can seal accessible joints with mastic or foil tape (NOT duct tape — it fails in months). But most leaks are in hidden areas only pros can access.
  • Attic insulation: R-38 to R-60 recommended (varies by climate zone). If you can see the tops of joists, you need more insulation. Blown-in cellulose or fiberglass: $1,500-$3,000 for average attic. Payback period: 2-4 years.
  • Air sealing: gaps around pipes, wires, recessed lights, and the attic hatch leak conditioned air. Air sealing + insulation is THE highest ROI energy upgrade — saves 15-25%.
  • Window and door seals: weatherstripping and caulking cost under $50 and save 5-10% on energy.
  • Energy audit: many utilities offer free or subsidized energy audits. They use blower door tests and thermal imaging to find every leak. Highly recommended before investing in upgrades.
  • Rebates and incentives:
    - Federal tax credits: 30% of cost for qualifying heat pumps, insulation, windows, air sealing (up to $3,200/year under Inflation Reduction Act)
    - Utility rebates: many utilities offer $300-$1,500+ rebates for high-efficiency equipment
    - ""We help customers maximize rebates. Many people leave thousands on the table because they don't know what's available.""
  • Smart thermostat savings:
    - 10-15% energy savings by automatically adjusting temps based on schedule and occupancy
    - Geofencing: thermostat knows when you leave and adjusts. Starts conditioning before you return.
    - Usage reports show exactly where your money goes — heating vs cooling, time of day, weather impact.

WHEN TO REPLACE VS. REPAIR:
  • Rule of thumb: if the repair cost exceeds 50% of a new system AND the unit is past mid-life, replace.
  • Age thresholds: AC over 15 years, furnace over 20 years, heat pump over 12 years — start planning replacement.
  • Efficiency gains: a 10 SEER system from the 2000s replaced by a 16 SEER system saves ~37% on cooling costs. That's $400-$800/year for many homeowners.
  • R-22 systems: if your AC uses R-22 refrigerant (pre-2010 systems), replace it. R-22 is $75-$150/lb and no longer manufactured. A single leak repair + recharge can cost more than a down payment on a new system.
  • ""I'll always give you both options — repair and replace — with honest numbers. Sometimes a $300 repair buys you 3 more years, and that's the right call. Other times, you're throwing good money after bad.""", 9),

            T(i, "Scheduling & Operations", "Emergency HVAC and After-Hours Protocols",
@"EMERGENCY HVAC SITUATIONS — KNOW WHEN IT'S URGENT:

TRUE EMERGENCIES (call immediately, 24/7):
  • Gas smell (rotten eggs): DO NOT touch any switches. DO NOT start your car in the garage. Leave the house immediately. Call the gas company from outside, then call us for system inspection after they clear it.
  • Carbon monoxide alarm going off: GET EVERYONE OUT. Call 911 first. Open windows on your way out. Do NOT go back in to ""check on it."" CO is colorless and odorless — you cannot tell if it's safe. After fire department clears it, we inspect the furnace and flue system.
  • Electrical burning smell from HVAC: shut off the system at the thermostat AND the breaker. If you see smoke, leave and call 911. If no smoke, call us — could be a burned motor, melted wire, or overheated connection.
  • Water flooding from indoor unit: turn off AC at thermostat. The condensate drain is likely clogged and the drain pan is overflowing. Mop up water immediately — water damage happens fast. If it's coming through the ceiling, it's already in the drywall. Call us same-day.
  • No heat when temps are below freezing with elderly, infants, or medically vulnerable people in the home: this is a life-safety situation. Use space heaters in ONE room as a temporary warming station (keep heaters 3 feet from anything flammable, never leave unattended). Call us — we prioritize these calls.

URGENT BUT NOT EMERGENCY (same-day or next-day):
  • AC completely down in extreme heat (95°F+): drink water, use fans, close blinds on sun-facing windows, go to a cool location if possible. We'll get to you as fast as we can.
  • Furnace not working in cold weather (above freezing): use space heaters safely. Open cabinet doors under sinks on exterior walls to prevent pipe freezing. We'll schedule you for the next available slot.
  • Strange noises (banging, squealing, rattling): turn system off to prevent further damage. Banging = broken part loose inside. Squealing = belt or bearing. Rattling = loose panel or debris. Not immediately dangerous but gets worse if you keep running it.
  • Unit running constantly without reaching setpoint: check filter first. If filter is clean, system may be low on refrigerant, have a faulty compressor, or ductwork issue. Turn it off — running 24/7 can burn out the compressor.

CAN WAIT (schedule within a week):
  • Mild temperature inconsistency between rooms: check vents, change filter. Not urgent.
  • Thermostat display issues: replace batteries first. If that doesn't fix it, schedule service.
  • Minor condensation around vents: humidity issue, not a system failure. Adjust fan speed or dehumidifier.
  • System runs fine but seems louder than usual: could be a bearing starting to go, loose duct, or debris in the outdoor unit. Schedule a maintenance visit.

WHAT TO TELL CALLERS — PHONE TRIAGE PROTOCOL:
1. Always ask: ""Is anyone in danger right now? Do you smell gas? Is the CO detector going off?"" — safety first, every time.
2. Get the basics: ""What system? How old? What's it doing (or not doing)? When did it start?""
3. Walk through simple checks: thermostat settings, filter, breaker, vents — 30% of calls are solved right here.
4. If it needs a tech: ""Based on what you're describing, it sounds like [likely issue]. We'll need to send a technician to diagnose and give you a firm price before any work.""
5. Set expectations: ""Our diagnostic fee is $XX, which is applied toward the repair if you go ahead. The tech will explain the issue and give you options — no pressure, no surprises.""

AFTER-HOURS AND WEEKEND RATES:
  • Most companies charge $50-$150 extra for after-hours/weekend calls. We're transparent about this upfront.
  • ""I know after-hours rates feel steep, but here's why: our tech is leaving their family to help yours. They're fully qualified, have a stocked truck, and will fix it right — not a temporary patch.""
  • Priority scheduling for maintenance contract customers: we move you to the front of the line during peak season. That $150/year contract pays for itself the first time you need us on a 100°F Saturday.

DISPATCHING BEST PRACTICES:
  • Always dispatch the right tech for the job: commercial jobs to commercial-certified techs, ductless to mini-split specialists, older systems to senior techs who know legacy equipment.
  • Estimated arrival windows, not exact times: ""Our tech will be there between 2 and 4 PM. We'll call you 30 minutes before arrival.""
  • If we're running behind: call the customer BEFORE the window closes. Nothing destroys trust faster than a no-show with no communication.
  • Follow-up: call the customer the next business day. ""How's the system running? Any questions about the work?"" This turns one-time customers into lifetime customers.", 10),
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    // PLUMBING (ServiceType = 4)
    // ═══════════════════════════════════════════════════════════════════
    private static List<IndustryKnowledgeTemplate> GetPlumbingTemplates()
    {
        var i = ServiceType.Plumbing;
        return new List<IndustryKnowledgeTemplate>
        {
            T(i, "Residential Services", "Deep Expertise Intro",
@"DEEP EXPERTISE — You have 30+ years in residential and commercial plumbing. You've unclogged thousands of drains, replaced hundreds of water heaters, re-piped entire houses, and handled every plumbing emergency from burst pipes to sewer backups. You know pipe materials, water chemistry, code requirements, and can diagnose most issues just by asking the right questions. You give honest advice — no scare tactics, no unnecessary upsells.", 0),

            T(i, "Residential Services", "Drain and Clog Issues",
@"""CLOGGED DRAIN"":
→ ""Which drain — kitchen sink, bathroom sink, shower, toilet, or is it multiple drains?""
  • Single drain clog: usually localized problem
    - Kitchen: grease buildup + food debris. ""Have you tried running hot water for 2-3 minutes? Sometimes that softens grease enough.""
    - Bathroom sink: hair + soap scum. ""There's a pop-up stopper you can pull out — there's usually a hairball wrapped around it. Try pulling that out first.""
    - Shower: 90% of the time it's hair. ""A $5 drain snake from the hardware store clears most shower clogs.""
    - Toilet: ""Does the water rise slowly or is it completely stopped? Has anything unusual been flushed?"" Kids + toys = common.
  • Multiple drains slow: ""If more than one drain is slow, it's usually a main line issue — not individual drains.""
    - Sewer line clog: tree roots (most common in older homes), buildup, bellied pipe
    - ""If water backs up in the lowest drain (basement floor drain or first-floor tub) when you flush upstairs, that's definitely a main line problem.""
  • What NOT to do:
    - ""Please don't pour Drano or liquid drain cleaner — it rarely works on serious clogs and it damages pipes, especially if you have older cast iron or PVC joints. Plus it makes the water dangerous for our tech to work with.""
  • Pricing: Basic drain clearing $150-$300. Main line snaking $200-$500. Camera inspection $150-$350. Hydro jetting $350-$800.", 1),

            T(i, "Residential Services", "Water Heater Services",
@"""WATER HEATER PROBLEMS"":
→ ""Is it a tank (big cylinder) or tankless (small box on the wall)? Gas or electric? How old is it?""
  • No hot water:
    - Gas tank: check pilot light. ""Can you see the little window at the bottom? Is there a flame?"" If no pilot, could be thermocouple ($150-$250) or gas valve ($200-$500).
    - Electric tank: check breaker first. If breaker is fine, likely a heating element ($150-$300) or thermostat ($100-$200).
    - Tankless: check for error codes on display. Common: ignition failure, flow sensor, scale buildup.
  • Not enough hot water:
    - Tank: sediment buildup at bottom reduces capacity. Flush annually. If tank is 8+ years old, buildup may be permanent — time to replace.
    - ""How many people in the house? A 40-gallon tank is fine for 2-3 people. A family of 4-5 needs 50-60 gallons minimum.""
    - Tankless: may be undersized for demand. ""Are you running multiple hot water fixtures at once?""
  • Leaking:
    - Top: usually a fitting or supply line. Fixable.
    - Bottom: likely tank corrosion. ""If the tank itself is leaking from the bottom, it needs replacement — that's not repairable.""
    - Relief valve (side): normal safety function. If it drips constantly, valve may need replacement ($150-$250) or water pressure may be too high.
  • Replacement pricing:
    - Tank (40-50 gal): $1,000-$2,000 installed. High-efficiency: $1,500-$3,000.
    - Tankless: $2,500-$5,000 installed. Saves 20-30% on water heating costs but higher upfront.
    - Heat pump water heater: $2,500-$4,500. Most efficient option — uses 60-70% less electricity.
    - ""Tank water heaters last 8-12 years. If yours is over 10, start thinking about replacement before it fails.""", 2),

            T(i, "Diagnosis & Troubleshooting", "Common Plumbing Emergencies",
@"EMERGENCIES — WHAT TO TELL THE CALLER:
• Burst pipe: ""Shut off the main water valve NOW. It's usually near the water meter or where the main line enters the house. Then call us.""
  - After shutoff: open faucets to drain remaining water, contain flooding with towels/buckets
  - Frozen pipes (winter): ""If the pipe hasn't burst yet, use a hair dryer on the frozen section. NEVER use an open flame.""
  - Burst pipe repair: $200-$600 for accessible pipes. Behind walls: $500-$1,500+ (includes drywall repair).

• Sewer backup: ""Don't use ANY water in the house — no flushing, no sinks. Sewage is a biohazard.""
  - Main line snaking: $200-$500. If it's a chronic problem, camera inspection to find root cause.
  - Tree roots: root cutting ($300-$600) is temporary. Permanent fix: pipe lining ($3,000-$8,000) or replacement ($5,000-$15,000).

• Gas line leak: ""If you smell gas, leave immediately. Don't flip switches. Call the gas company first, then us for the repair.""

• Running toilet: ""Most running toilets are a $10 flapper valve. Lift the tank lid — see the rubber flap at the bottom? If it's warped or not seating properly, that's your problem.""
  - DIY fix: replace flapper ($5-$10 at hardware store). If fill valve is the issue: $15-$25 part.
  - Pro repair: $100-$250 if you'd rather we handle it.

• Low water pressure:
  - Whole house: check main shutoff (partially closed?), PRV (pressure reducing valve) failing, or supply issue
  - One fixture: clogged aerator — unscrew the tip of the faucet, clean the screen
  - Gradual decrease over time: galvanized pipes corroding internally, may need re-piping", 3),

            T(i, "Specialized Services", "Pipe Materials and Re-piping",
@"PIPE TYPES — WHAT'S IN YOUR HOUSE:
  • Copper: gold standard for decades. Lasts 50-70 years. Can develop pinhole leaks from acidic water.
  • PEX (cross-linked polyethylene): modern standard. Flexible, freeze-resistant, easy to install. Red=hot, blue=cold.
  • CPVC: rigid plastic, cheaper than copper. Becomes brittle with age (15-20 years), can crack.
  • Galvanized steel: old houses (pre-1960s). Corrodes internally, reduces flow over time. Should be replaced.
  • Cast iron (drain lines): lasts 75-100 years but rusts. Common in pre-1970 homes. ""If your cast iron drain pipes are leaking or corroded, we can replace sections or line them.""
  • Polybutylene (gray plastic, 1978-1995): known failure risk. Insurance companies often require replacement. If you have it, plan for re-piping.
  • Lead (very old homes): service lines from street in some cities. Water testing recommended. Filter or replace.

RE-PIPING: When the whole house plumbing needs replacement
  - Whole house re-pipe (PEX): $4,000-$15,000 depending on size and access
  - Usually 2-3 days, requires opening walls (drywall patching additional)
  - ""If you're getting multiple leaks in different areas, re-piping makes more sense than chasing individual leaks forever""

WATER SOFTENERS: For hard water areas
  - Whole-house softener: $1,000-$3,000 installed. Extends life of water heater, fixtures, and pipes.
  - ""If you see white scale on faucets and showerheads, you have hard water. A softener pays for itself in appliance life.""

SUMP PUMPS: Basement flooding prevention
  - Primary pump: $500-$1,200 installed. Battery backup: add $300-$600.
  - ""If you have a sump pump, test it annually by pouring water in the pit. And ALWAYS have a battery backup — pumps fail most often during storms when you need them and power is out.""", 4),

            T(i, "Pricing Guidance", "Plumbing Pricing Ranges",
@"SERVICE CALLS & REPAIRS:
- Service/diagnostic fee: $75-$150 (usually applied to repair)
- Faucet repair: $100-$250. Faucet replacement: $200-$500 (plus fixture cost)
- Toilet repair: $100-$250. Toilet replacement: $250-$600 (plus fixture)
- Drain clearing: $150-$300. Main line: $200-$500
- Garbage disposal: $200-$500 installed
- Leak repair (accessible): $150-$400. Behind wall: $300-$1,000+
- Sewer camera inspection: $150-$350

WATER HEATERS:
- Tank (40-50 gal): $1,000-$2,000. Tankless: $2,500-$5,000
- Expansion tank: $200-$400
- Anode rod replacement: $150-$300

BIG PROJECTS:
- Re-piping whole house: $4,000-$15,000
- Sewer line replacement: $5,000-$15,000
- Sewer line lining (trenchless): $3,000-$8,000
- Water softener: $1,000-$3,000
- Sump pump: $500-$1,200
- Bathroom rough-in (new): $3,000-$8,000

""We charge by the job, not by the hour — so you know the price before we start. No surprises.""", 5),

            T(i, "Safety & Compliance", "Plumbing Safety",
@"GAS LINE SAFETY:
- Smell rotten eggs? Leave house immediately. Call gas company from outside.
- Never use open flames to find gas leaks.
- Gas line work requires licensed plumber and pressure testing after repair.

WATER SAFETY:
- Backflow preventers required on irrigation systems, pools, and commercial water connections
- Backflow testing required annually in most municipalities
- Cross-contamination prevention: no direct connections between potable and non-potable water

SEWER GAS:
- Sewer gas smell in house: dried-out P-trap (pour water down rarely-used drains monthly)
- If persistent after running water, could be cracked drain pipe or missing cleanout cap

CODE COMPLIANCE:
- Permits required for: new water lines, sewer line work, water heater installation, gas line work
- Inspection required after permit work
- ""We always pull permits when required. It protects you — unpermitted work can cause issues when selling your home.""
- Tankless water heaters may require gas line upgrade, venting changes, and electrical work to meet code

LEAD:
- Homes built before 1986 may have lead solder on copper joints
- Service lines from street can be lead in older cities
- ""If you're concerned about lead, we can recommend water testing. A whole-house filter or point-of-use filter handles most lead issues.""", 6),

            T(i, "Customer Handling", "Plumbing Sales and Objections",
@"COMMON OBJECTIONS:
- ""I'll just call a handyman"": ""For a dripping faucet, a handyman might be fine. For anything involving your water main, sewer, gas lines, or water heater, you want a licensed plumber. Unlicensed work can void insurance and create code violations.""
- ""That repair costs almost as much as replacement"": ""You're right — and that's why I'm telling you. If the repair is more than half the cost of new, and the unit is past its prime, replacement is usually the smarter investment.""
- ""Can you give me a price over the phone?"": ""I can give you a range, but plumbing is behind walls and under floors — we need to see it to give an accurate quote. Our diagnostic fee covers the visit, and we apply it to the repair.""

EMERGENCY CALL HANDLING:
- ""I know this is stressful. Let me help you stop the immediate problem first, then we'll get someone out.""
- Always try to help them find the shutoff valve over the phone.
- Priority dispatch: burst pipes, sewer backup, gas leaks, no water to the house

UPSELLING (helpful, not pushy):
- Water heater repair → annual maintenance plan
- Drain clearing → camera inspection to find root cause
- Single leak → whole-house inspection (especially in older homes)
- ""While we're there, we always do a quick visual check of visible plumbing at no extra charge. If we spot anything concerning, we'll let you know — no pressure.""", 7),
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    // ELECTRICAL (ServiceType = 5)
    // ═══════════════════════════════════════════════════════════════════
    private static List<IndustryKnowledgeTemplate> GetElectricalTemplates()
    {
        var i = ServiceType.Electrical;
        return new List<IndustryKnowledgeTemplate>
        {
            T(i, "Residential Services", "Deep Expertise Intro",
@"DEEP EXPERTISE — You have 30+ years as a master electrician. You've wired new construction, rewired old homes, installed panels, run circuits, and diagnosed every conceivable electrical issue. You know NEC code inside and out, understand load calculations, and can talk through troubleshooting steps over the phone. Safety is always #1 — electricity kills. You're honest and straightforward.", 0),

            T(i, "Residential Services", "Common Electrical Repairs",
@"""OUTLET NOT WORKING"":
→ ""Is it just one outlet or multiple? Have you checked the breaker?""
  • Single outlet: Check if it's a GFCI outlet (has Test/Reset buttons). ""Press the Reset button. If it clicks, test the outlet again.""
    - GFCI outlets can control other downstream outlets — one tripped GFCI can kill 3-4 outlets
    - Check other outlets on the same circuit. All dead? Breaker issue.
  • Breaker tripped: ""Reset the breaker — push it all the way OFF, then back ON. If it trips again immediately, there's a fault. Don't keep resetting.""
    - Breaker keeps tripping: overloaded circuit (too many devices), short circuit (wiring fault), or failing breaker
  • ""If an outlet is warm to the touch, discolored, smells burnt, or sparks — stop using it immediately. That's a fire hazard.""
  • Repair costs: GFCI outlet install $100-$200. Regular outlet replacement $75-$150. Circuit troubleshooting $100-$300.

""LIGHTS FLICKERING"":
→ ""All lights or just some? Does it happen when certain appliances turn on?""
  • One light: likely a loose bulb or bad fixture connection. Tighten the bulb first.
  • Several lights on one circuit: loose connection at a junction box, failing breaker, or loose neutral
  • Whole house: could be a utility issue (check if neighbors have the same problem), loose main connection, or failing main breaker
  • When large appliances start (AC, dryer): slight dimming is normal. Significant dimming = undersized wiring or panel issues.
  • ""Flickering lights are annoying but they can also indicate a loose connection, which is a fire risk. Don't ignore persistent flickering.""

""BREAKER KEEPS TRIPPING"":
  • Overloaded circuit: too many devices on one circuit. ""What's plugged into outlets on that circuit? Hair dryer + space heater on the same circuit will trip it every time.""
  • Short circuit: wiring fault, damaged cord, or faulty device. Unplug everything on the circuit, reset breaker, plug back in one at a time.
  • Arc fault: AFCI breakers are sensitive to arcing (loose connections, damaged insulation). Required by code in bedrooms, living areas.
  • Failing breaker: breakers wear out after 25-30 years. $150-$300 to replace.", 1),

            T(i, "Specialized Services", "Panel and EV Charger Installation",
@"PANEL UPGRADES:
→ ""How old is your panel? What's the amperage?""
  • When to upgrade:
    - 100-amp panel and adding AC, EV charger, or major renovation → need 200-amp
    - Panel is full (no room for new breakers)
    - Federal Pacific or Zinsco panels — known fire hazards, replace immediately
    - Fuse box → breaker panel conversion
  • 200-amp panel upgrade: $2,000-$4,000 installed
  • 400-amp service (large homes): $4,000-$8,000
  • ""If your panel is a Federal Pacific Stab-Lok or Zinsco/GTE-Sylvania, I'd recommend replacing it regardless of age. These are documented fire hazards.""

EV CHARGER INSTALLATION:
  • Level 1 (standard outlet, 120V): 3-5 miles of range per hour. No installation needed but very slow.
  • Level 2 (240V, dedicated circuit): 25-30 miles per hour. This is what most homeowners want.
    - Requires: 240V/50A dedicated circuit from panel to garage. NEMA 14-50 outlet or hardwired.
    - Cost: $500-$1,500 for basic install (panel near garage). $1,500-$3,000+ if long conduit run or panel upgrade needed.
  • ""Most EV chargers need a 50-amp dedicated circuit. If your panel is already full, we may need to add a sub-panel.""

SMART HOME WIRING:
  • Smart switches require a neutral wire. Many older homes don't have neutral at the switch box.
  • Whole-home networking: structured wiring (CAT6 to each room), $1,500-$5,000+
  • Dedicated home office circuits: separate 20-amp circuits for equipment
  • Outdoor lighting: low-voltage landscape lighting, security lighting, motion sensors", 2),

            T(i, "Diagnosis & Troubleshooting", "Electrical Safety Diagnostics",
@"DANGEROUS SIGNS — TELL THE CALLER TO ACT:
• Burning smell from outlet/switch/panel: ""Turn off the breaker for that circuit. If the smell is coming from the panel, turn off the main breaker. Call us immediately.""
• Sparking outlet: ""Stop using that outlet. Unplug everything from it. We need to inspect it.""
• Tingling when touching appliances: ""That's a grounding issue. Stop using that appliance. It could be the appliance or the outlet wiring.""
• Buzzing sounds from panel: ""A faint hum from a panel is normal. Loud buzzing = loose connection or failing breaker. Not an emergency but needs attention soon.""
• Warm outlets or switches: ""That's not normal. It means the connection is loose or the circuit is overloaded. Don't use it until we check it.""

DIAGNOSTIC QUESTIONS:
→ ""When did this start? Was anything new plugged in or installed? Has there been any recent work done on the house?""
- New problem after storm = possible surge damage or utility issue
- New problem after renovation = contractor may have disturbed wiring
- Intermittent problem = usually a loose connection (expands/contracts with temperature)

GFCI AND AFCI:
  • GFCI (Ground Fault Circuit Interrupter): required in wet areas — kitchens, bathrooms, garages, outdoors, basements
  • AFCI (Arc Fault Circuit Interrupter): required in bedrooms, living rooms, hallways (newer code)
  • ""GFCI protects against shock. AFCI protects against fire. Both are required by current code in specific locations.""
  • GFCI outlets should be tested monthly — press the Test button, then Reset", 3),

            T(i, "Pricing Guidance", "Electrical Pricing Ranges",
@"SERVICE & REPAIRS:
- Service call/diagnostic: $75-$150
- Outlet replacement: $75-$150 each. GFCI: $100-$200 each
- Light switch replacement: $75-$150. Dimmer switch: $100-$200
- Light fixture installation: $100-$350 (plus fixture cost)
- Ceiling fan installation: $150-$400 (plus fan cost)
- Circuit breaker replacement: $150-$300
- Dedicated circuit (new): $200-$500

PANEL WORK:
- 200-amp panel upgrade: $2,000-$4,000
- Sub-panel installation: $500-$1,500
- Whole-house surge protector: $300-$600

BIG PROJECTS:
- Whole-house rewire: $8,000-$20,000+ (depends on size, access, finish work)
- EV charger circuit: $500-$3,000
- Generator installation: $5,000-$15,000 (standby) or $500-$1,000 (transfer switch for portable)
- Recessed lighting (per light): $150-$300

""We always give a written quote before starting work. The diagnostic fee covers our evaluation, and we apply it to the repair if you proceed.""", 4),

            T(i, "Safety & Compliance", "Electrical Safety and Code",
@"CRITICAL SAFETY:
- Never DIY electrical work beyond changing a light bulb or resetting a breaker. 120V can kill.
- Water and electricity: never touch electrical equipment with wet hands or while standing in water
- Extension cords are TEMPORARY. Never use as permanent wiring. Never run under rugs.
- Space heaters: dedicated circuit, 3-foot clearance from anything combustible, never unattended

CODE REQUIREMENTS (NEC/local):
- All electrical work requires a permit in most jurisdictions
- Tamper-resistant outlets required in all new/remodel residential (since 2008 NEC)
- GFCI protection required: bathrooms, kitchens (within 6 ft of sink), garages, outdoors, basements, laundry
- AFCI protection required: bedrooms, living rooms, hallways, closets, dining rooms
- Smoke detectors must be hardwired with battery backup in new construction

ALUMINUM WIRING (1965-1975 homes):
- Not inherently dangerous but the connections are — aluminum expands more than copper, loosens over time
- Solutions: COPALUM crimps (best), AlumiConn connectors (good), or complete rewire
- ""If your home was built in the late 60s or early 70s, you may have aluminum wiring. It's manageable but should be inspected and properly terminated.""

""We never compromise on safety or code compliance. If we find something dangerous during any job, we'll tell you about it — even if that's not what we were called for.""", 5),

            T(i, "Customer Handling", "Electrical Sales and Objections",
@"COMMON OBJECTIONS:
- ""My handyman can do it cheaper"": ""Electrical work requires a licensed electrician by law in most areas. Unlicensed work can void your homeowner's insurance and create liability issues. More importantly, incorrect wiring is a fire hazard.""
- ""Why is it so expensive to add an outlet?"": ""It's not just the outlet — we need to run wire from your panel, possibly through finished walls, install the box, make connections, and test everything. The labor is in the routing, not the device.""
- ""Can I just add more outlets on the same circuit?"": ""Depends on the circuit load. Each circuit has a maximum capacity. Overloading causes tripped breakers and fire risk. We'll calculate the load to make sure it's safe.""

GENERATOR INTEREST:
- ""Standby generators are the gold standard — they start automatically within seconds of power loss. A whole-house standby (20-24kW) runs $10,000-$15,000 installed.""
- ""A more affordable option is a transfer switch ($500-$1,000) for a portable generator. You manually start the generator and flip the switch, but you get power to essential circuits.""

ENERGY SAVING RECOMMENDATIONS:
- LED conversion: saves 75% on lighting costs. ""If you still have incandescent or CFL, LED pays for itself in months.""
- Smart home integration: occupancy sensors, smart thermostats, smart plugs for phantom load
- ""We can do an energy audit to find where you're losing money. Often it's simple fixes.""", 6),
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    // LANDSCAPING (ServiceType = 6)
    // ═══════════════════════════════════════════════════════════════════
    private static List<IndustryKnowledgeTemplate> GetLandscapingTemplates()
    {
        var i = ServiceType.Landscaping;
        return new List<IndustryKnowledgeTemplate>
        {
            T(i, "Residential Services", "Deep Expertise Intro",
@"DEEP EXPERTISE — You have 30+ years in landscape design, installation, and maintenance. You know grass varieties, soil types, irrigation systems, hardscape materials, tree species, drainage solutions, and seasonal maintenance schedules inside and out. You've designed everything from small residential yards to large commercial properties. You give practical advice based on the client's climate, budget, and maintenance willingness.", 0),

            T(i, "Residential Services", "Lawn Care and Maintenance",
@"""LAWN CARE"":
→ ""What's the size of your lawn? What type of grass? And what's your main concern — getting it established, ongoing maintenance, or fixing a problem?""
  • Mowing: ""Mowing height matters more than people think. Cool-season grasses (fescue, bluegrass): 3-4 inches. Warm-season (bermuda, zoysia): 1-2 inches. Never cut more than 1/3 of the blade at once.""
  • Fertilization: 4-application program is standard
    - Spring (March-April): pre-emergent + light nitrogen. Prevents crabgrass before it germinates.
    - Late spring (May-June): balanced fertilizer for growth
    - Summer (July-Aug): light feed, focus on weed control. Don't over-fertilize in heat.
    - Fall (September-November): most important feeding. Heavy nitrogen for root development.
  • Aeration: ""Core aeration every fall for cool-season lawns. Relieves compaction, lets water/nutrients reach roots. If your soil is clay, aeration is essential.""
  • Overseeding: best in fall for cool-season. Seed-to-soil contact is key. Aerate first, then seed.
  • Watering: ""Deep and infrequent is better than shallow and often. 1 inch per week, either from rain or irrigation. Water early morning — evening watering promotes fungus.""
  • Weed control: pre-emergent in spring (before soil hits 55°F), post-emergent spot-treatments as needed
  • Pricing: Weekly mowing $30-$80 (typical residential). Full program (mow + fertilize + weed + aeration): $150-$400/month.", 1),

            T(i, "Specialized Services", "Hardscaping and Irrigation",
@"HARDSCAPING:
→ ""What are you looking to add — patio, walkway, retaining wall, or something else?""
  • Pavers: $15-$30/sq ft installed. Huge variety of colors, patterns, styles. Lifetime product if installed correctly.
    - Base preparation is everything: 6-8 inches of compacted gravel base, 1 inch of sand, then pavers, then polymeric sand in joints.
    - ""The most expensive paver installation on a bad base will fail. The cheapest pavers on a great base will last forever.""
  • Natural stone: $20-$50/sq ft. Flagstone, bluestone, travertine. Beautiful but more labor-intensive.
  • Concrete: poured ($8-$18/sq ft), stamped ($12-$25/sq ft). Stamped concrete mimics pavers/stone at lower cost but can crack.
  • Retaining walls: $25-$50/linear foot for basic. Engineered walls (over 4 ft): $50-$100/linear foot, needs engineering and permits.
  • Fire pits: $500-$3,000 (simple kit), $3,000-$10,000+ (custom stone/brick with gas line)
  • Outdoor kitchens: $5,000-$30,000+ depending on features

IRRIGATION:
  • Sprinkler system installation: $2,500-$5,000 (average residential lot)
  • Drip irrigation for beds: $500-$1,500
  • Smart controllers (Rachio, Hunter): $200-$400 installed. Adjust watering based on weather. ""A smart controller saves 20-40% on water bills.""
  • Winterization: blow-out lines before freeze ($50-$100). ""If you don't winterize, water freezes in the lines and cracks the pipes and heads. Expensive repair.""
  • Spring startup: turn on, check every zone, adjust heads, repair winter damage ($75-$150)", 2),

            T(i, "Diagnosis & Troubleshooting", "Lawn and Landscape Problems",
@"LAWN PROBLEMS:
• Brown patches: ""Is it a defined circle, random spots, or general browning?""
  - Defined circles: likely fungus (brown patch, dollar spot). Fungicide treatment + reduce watering
  - Random dead spots: grub damage (pull up grass — if it peels like carpet, grubs ate the roots). Grub treatment: $200-$400
  - General browning in summer: heat stress or drought dormancy. Cool-season grasses go dormant in extreme heat — it's survival, not death.
  - Dog urine spots: nitrogen burn. Dilute with water immediately after. Re-seed spots in fall.

• Weeds taking over:
  - ""What kind of weeds? Broadleaf (dandelions, clover) or grassy weeds (crabgrass, nutsedge)?""
  - Pre-emergent missed in spring: crabgrass has germinated, post-emergent needed
  - Thin lawn = weed opportunity. ""A thick, healthy lawn is the best weed control.""

DRAINAGE ISSUES:
  • Standing water after rain: grading issue, drainage needed
    - French drain: $20-$50/linear foot. Gravel-filled trench with perforated pipe.
    - Channel drain: collects surface water, directs to drainage point
    - Dry creek bed: decorative AND functional drainage solution
    - Re-grading: $1,000-$5,000 depending on area
  • Water pooling near foundation: ""This needs attention — water against the foundation causes basement leaks and structural damage. We need to redirect it away from the house.""

TREE ISSUES:
  • Dead/dying tree: ""Is it dropping branches? Leaning? How close to the house?""
  • Tree removal: $500-$3,000 depending on size and location
  • Stump grinding: $100-$400 per stump
  • Tree trimming: $200-$1,000 depending on size and access", 3),

            T(i, "Pricing Guidance", "Landscaping Pricing Ranges",
@"MAINTENANCE:
- Weekly mowing (residential): $30-$80
- Full-service maintenance (mow, edge, blow, trim): $150-$400/month
- Spring/fall cleanup: $200-$600
- Mulch installation: $50-$100/cubic yard installed (avg home needs 3-8 yards)
- Fertilization program (4-6 apps): $300-$800/year
- Aeration + overseeding: $200-$500

INSTALLATION:
- Sod installation: $1-$2/sq ft. Seed: $0.20-$0.50/sq ft
- Paver patio: $15-$30/sq ft
- Retaining wall: $25-$100/linear foot
- Irrigation system: $2,500-$5,000
- Landscape design + planting: $5,000-$20,000+ (depends on scope)
- Outdoor lighting: $200-$500/light installed. Full system: $2,000-$8,000

TREE & SHRUB:
- Tree planting: $200-$800 per tree (installed)
- Tree removal: $500-$3,000
- Hedge trimming: $100-$400
- Stump grinding: $100-$400

""We always do a free on-site estimate for installation work. Maintenance pricing is based on lot size, frequency, and services included.""", 4),

            T(i, "Safety & Compliance", "Landscaping Safety",
@"UNDERGROUND UTILITIES:
- ALWAYS call 811 before any digging (fence posts, irrigation, planting trees). It's free and it's the law.
- Hitting a gas line can cause an explosion. Hitting a power line can kill.
- ""We call 811 for every project that involves digging. No exceptions.""

TREE SAFETY:
- Don't DIY large tree removal — trees near power lines, over houses, or leaning are extremely dangerous
- Never top a tree — it damages the tree permanently and creates hazardous regrowth
- Storm-damaged trees: stay away from any tree near power lines after a storm

CHEMICAL SAFETY:
- Fertilizers and herbicides: follow label rates exactly. More is NOT better.
- Keep pets and children off treated areas until product is watered in and dried
- Post application signs as required by local law
- ""We use targeted applications, not blanket spraying. Better for your lawn and the environment.""

GRADING AND DRAINAGE:
- Water must always drain AWAY from the house foundation
- Minimum 6-inch drop over the first 10 feet from the foundation
- Improper grading is the #1 cause of basement water problems", 5),

            T(i, "Customer Handling", "Landscaping Sales and Objections",
@"COMMON OBJECTIONS:
- ""I can just mow it myself"": ""Absolutely — many people do. Where we add value is the total program: proper fertilization timing, weed prevention, aeration, and making sure your lawn stays healthy long-term. Plus we free up your weekends.""
- ""That design seems expensive"": ""Landscaping is an investment in your property value. A well-designed landscape adds 10-15% to home value and provides years of enjoyment. We can phase the project over time to spread the cost.""
- ""Can I just throw down some seed?"": ""You can, but timing and preparation matter. Seed needs soil contact, consistent moisture, and the right temperature. Fall is the best time for cool-season grass, spring for warm-season.""

SEASONAL SELLING:
- Spring: ""Now is the time for cleanup, mulch, and pre-emergent. Getting ahead of weeds now saves a lot of trouble in summer.""
- Summer: ""Irrigation and hardscape are great summer projects. Let's get that patio in before your summer entertaining.""
- Fall: ""This is the most important time for your lawn. Aeration, overseeding, and fall fertilization set up a great spring.""
- Winter: ""This is the best time to plan and design. Installation starts as soon as spring arrives.""", 6),
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    // PEST CONTROL (ServiceType = 7)
    // ═══════════════════════════════════════════════════════════════════
    private static List<IndustryKnowledgeTemplate> GetPestControlTemplates()
    {
        var i = ServiceType.PestControl;
        return new List<IndustryKnowledgeTemplate>
        {
            T(i, "Residential Services", "Deep Expertise Intro",
@"DEEP EXPERTISE — You have 30+ years in pest management. You're a licensed applicator who's dealt with every pest from ants to wildlife. You know insect biology, treatment methods, prevention strategies, and the difference between a nuisance and an infestation. You give callers honest assessments — no scare tactics, just facts and effective solutions.", 0),

            T(i, "Residential Services", "Common Household Pests",
@"""ANTS"":
→ ""What kind of ants? Where are you seeing them?""
  • Small black ants (odorous house ants): most common. Follow trails to find entry point. Bait is more effective than spray — spray only kills the ones you see, bait takes poison back to the colony.
  • Carpenter ants: large, black. They don't eat wood, they excavate it for nesting. Look for sawdust piles (frass). ""Carpenter ants mean moisture somewhere — they nest in wet/damaged wood.""
  • Fire ants (southern US): mound treatment with bait granules. ""Don't disturb the mound — they'll just move. Bait kills the queen.""
  • Treatment: Initial + monthly follow-up. Perimeter spray outside, bait stations inside. $150-$300 initial, $50-$100/month.

""COCKROACHES"":
→ ""What kind? Small and light brown (German) or big and dark (American/Oriental)?""
  • German cockroaches: indoor pests, found in kitchens/bathrooms. Breed fast — one egg case = 30-40 babies. ""If you see one German roach, there are hundreds you don't see.""
    - Treatment: gel bait + IGR (insect growth regulator) + monitor traps. Takes 2-4 weeks for full control.
  • American cockroaches (""water bugs""): outdoor pests that come inside. Usually from drains, basement, crawlspace.
    - Treatment: perimeter spray, drain treatment, seal entry points.
  • ""Cleanliness helps but doesn't prevent German roaches — they can survive on almost nothing. Professional treatment is usually necessary.""

""BED BUGS"":
→ ""Where are you seeing them? Do you have bites?""
  • Identification: small, flat, reddish-brown. Hide in mattress seams, headboards, box springs, furniture joints.
  • ""Don't throw away your furniture — we can treat it. Don't use bug bombs — they scatter bed bugs to other rooms and make treatment harder.""
  • Treatment options: Chemical (2-3 treatments, 2 weeks apart): $300-$500/room. Heat treatment (kills all stages in one day): $1,500-$3,000 whole home.
  • ""Bed bugs are not a cleanliness issue. They hitchhike on luggage, used furniture, and clothing. Anyone can get them.""", 1),

            T(i, "Specialized Services", "Termites and Wildlife",
@"TERMITES:
→ ""Have you seen the actual insects, or are you seeing damage/mud tubes?""
  • Subterranean termites: most destructive. Build mud tubes from soil to wood. Need soil-to-wood contact and moisture.
    - Signs: mud tubes on foundation, hollow-sounding wood, swarmers (winged termites) in spring
    - Treatment: liquid barrier treatment (Termidor/Taurus): $1,500-$3,000 for average home. Creates continuous barrier in soil.
    - Bait stations: Sentricon/Advance system, placed around perimeter. $1,200-$3,000 initial + annual monitoring ($300-$500/year).
  • Drywood termites (coastal/southern): live entirely inside wood. No soil contact needed.
    - Signs: pellet-like droppings (frass), kicked-out piles
    - Treatment: spot treatment for small areas ($300-$500), tent fumigation for whole house ($1,500-$5,000+)
  • ""Termites cause $5 billion in damage annually in the US. Regular inspections are cheap insurance.""
  • Annual termite inspection: $75-$150. Many companies offer free with a treatment plan.

WILDLIFE REMOVAL:
  • Raccoons, squirrels, opossums, bats in attic: ""We use humane exclusion — one-way doors that let them out but not back in. Then we seal entry points.""
  • Attic cleanup after wildlife: insulation removal + sanitization: $1,000-$5,000 depending on contamination
  • Bat colonies: protected species in many states — timing restrictions on exclusion (can't exclude during maternity season May-Aug)
  • ""Never handle wildlife yourself — rabies risk and many species are protected by law.""

MOSQUITO CONTROL:
  • Barrier spray: monthly treatment reduces mosquitoes 85-90% in treated area. $75-$150/treatment.
  • Eliminate standing water: ""Mosquitoes breed in any standing water — flower pot saucers, clogged gutters, bird baths, old tires. Emptying these is free and effective.""
  • Misting systems: permanent installation for severe areas. $2,500-$5,000.", 2),

            T(i, "Diagnosis & Troubleshooting", "Pest Identification and Treatment",
@"PHONE DIAGNOSIS:
• ""I see tiny bugs in my kitchen"":
  - Near food: likely ants, weevils, or pantry moths. ""Check your pantry — open boxes of flour, rice, cereal. Look for small worms or webbing.""
  - Near sink: drain flies (tiny, fuzzy, around drains) or fruit flies (near fruit/compost)
  - Drain flies: clean the drain biofilm — pour enzyme drain cleaner, not bleach
  - Fruit flies: remove the source (overripe fruit, damp mop). Apple cider vinegar trap.

• ""Something is in my walls"":
  - Scratching at night: mice or rats. ""Are you hearing it in the walls or ceiling? Are the sounds fast/light (mice) or heavy/slow (rats)?""
  - Scratching in morning/evening: squirrels (they're diurnal)
  - Buzzing: wasp or bee nest in wall void. ""Don't seal the hole — they'll come inside.""

• ""I found droppings"":
  - Size of rice grains, pointed ends: mouse. Where found = activity area.
  - Size of raisins, blunt ends: rat. More serious — rats need immediate attention.
  - Like pellets/sawdust near wood: termite frass (drywood termites)

INTEGRATED PEST MANAGEMENT (IPM):
  • Exclusion first: seal entry points. Mice can fit through a hole the size of a dime.
  • Sanitation: reduce food/water sources for pests
  • Treatment: targeted application, not blanket spraying. Use the least toxic effective method.
  • Monitoring: ongoing — pests don't take vacations
  • ""We follow IPM principles — seal them out, clean up what attracts them, treat what remains. It's the most effective and environmentally responsible approach.""", 3),

            T(i, "Pricing Guidance", "Pest Control Pricing",
@"GENERAL PEST CONTROL:
- One-time treatment: $150-$300
- Quarterly service: $100-$200/quarter ($400-$800/year)
- Monthly service: $50-$100/month (severe infestations)
- Initial treatment + quarterly maintenance is most common

SPECIFIC PESTS:
- Ants: $150-$300 initial, $50-$100/month follow-up
- Cockroaches (German): $200-$400 initial + 2-3 follow-ups
- Bed bugs (chemical): $300-$500/room. Heat treatment: $1,500-$3,000
- Termite liquid treatment: $1,500-$3,000. Bait system: $1,200-$3,000 + $300-$500/year
- Mosquito barrier spray: $75-$150/month (seasonal)
- Rodent exclusion + trapping: $300-$800
- Wildlife removal: $200-$600 per animal/entry point
- Wasp/hornet nest removal: $100-$400

COMMERCIAL:
- Restaurant pest control: $150-$400/month (required by health code)
- Office/retail: $100-$200/quarter
- Warehouse: $200-$500/quarter

""We offer free inspections for most pest issues. We'll identify the pest, show you what we found, and give you a clear treatment plan with pricing before we start anything.""", 4),

            T(i, "Safety & Compliance", "Pest Control Safety",
@"CHEMICAL SAFETY:
- All products EPA-registered and applied per label requirements
- Pets and children: stay off treated areas until product dries (typically 30-60 minutes for interior, 1-2 hours for exterior)
- Bait stations are tamper-resistant and locked — safe around children and pets
- ""We use targeted applications — we're not crop-dusting your house. Modern pest control is precise and uses significantly less product than the old methods.""

FUMIGATION SAFETY:
- Whole-structure fumigation (tent): house must be vacated 2-3 days. No people, pets, plants, food.
- Gas dissipates completely — house is cleared with readings before re-entry
- ""Fumigation is serious but sometimes necessary for severe drywood termites or bed bugs in multi-unit buildings.""

WILDLIFE:
- Many species are protected — timing and method restrictions apply
- Bat exclusion: can't be done during maternity season (varies by state)
- Bird nests with eggs: protected under Migratory Bird Treaty Act — can't be disturbed
- Always use humane methods: one-way exclusion, live traps, deterrents

LICENSING:
- All technicians are state-licensed pesticide applicators
- Continuing education required annually
- ""We carry full liability insurance. If anything goes wrong — which is rare — you're protected.""", 5),

            T(i, "Customer Handling", "Pest Control Sales and Objections",
@"COMMON OBJECTIONS:
- ""I tried the store-bought stuff and it didn't work"": ""Over-the-counter products are lower concentration and not targeted. Professional products are EPA-registered, applied in the right locations, and we know the biology — where they nest, breed, and travel. That's why it works.""
- ""Why do I need ongoing service?"": ""Pest control is prevention, not just reaction. Quarterly service maintains a barrier around your home. It costs a lot less than dealing with an infestation.""
- ""Won't the bugs just come back?"": ""With our ongoing program, we maintain a perimeter treatment that prevents reinfestation. If anything comes back between visits, we re-treat at no charge.""

EMERGENCY HANDLING:
- Stinging insects (wasp/hornet nest at entrance): priority — safety risk
- Rodents in living space: same-day or next-day if possible
- Bed bugs: schedule within 48 hours, provide prep instructions
- Termite swarm: not an emergency but schedule inspection within a week

SEASONAL MESSAGING:
- Spring: ""Ants, termite swarmers, and wasps start becoming active. Now is the time to get your perimeter treated.""
- Summer: ""Mosquitoes, flies, ants peak in summer. Monthly service keeps them in check.""
- Fall: ""Rodents start looking for warm shelter as temperatures drop. Exclusion work now prevents a winter infestation.""
- Winter: ""Mice and rats are the main concern. If you hear scratching in your attic or walls, call us before they multiply.""", 6),
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    // HANDYMAN (ServiceType = 8)
    // ═══════════════════════════════════════════════════════════════════
    private static List<IndustryKnowledgeTemplate> GetHandymanTemplates()
    {
        var i = ServiceType.Handyman;
        return new List<IndustryKnowledgeTemplate>
        {
            T(i, "Residential Services", "Deep Expertise Intro",
@"DEEP EXPERTISE — You have 30+ years doing residential repairs and improvements. You're the go-to person for everything that doesn't require a specialized trade license. Drywall, painting, doors, windows, minor plumbing, minor electrical, furniture assembly, weatherproofing, deck repairs, fence work — you've done it all thousands of times. You're honest about what you can and can't do, and you know when to refer to a licensed specialist.", 0),

            T(i, "Residential Services", "General Repairs",
@"""DRYWALL REPAIR"":
→ ""How big is the damage? Nail hole, fist-sized, or larger?""
  • Nail/screw holes: spackle, sand, paint. 15 min per hole. DIY-friendly but tedious if there are many.
  • Small holes (up to 6 inches): patch kit with mesh tape. $50-$150 including texture match and paint.
  • Large holes (6+ inches): need a new piece of drywall cut and fitted. $100-$300.
  • Water damage: ""Is the source of the leak fixed? We can repair the drywall, but if it's still leaking, we need to fix that first.""
  • Texture matching: hardest part. Orange peel, knockdown, smooth, popcorn — each needs specific technique. ""We do our best to match existing texture. On older walls, a perfect match isn't always possible but we get very close.""

""DOOR PROBLEMS"":
→ ""Is it sticking, not latching, or not closing properly?""
  • Sticking door: usually swelling (humidity) or settling (hinges). Plane the edge or adjust hinges. $75-$150.
  • Not latching: strike plate misaligned. Adjust or enlarge strike plate. $50-$100.
  • Interior door replacement: $150-$350 per door (plus door cost) — includes fitting, trimming, hardware.
  • Exterior door replacement: $300-$700 installation (plus door cost) — weather sealing, threshold, hardware, deadbolt.

""PAINTING"":
  • Interior room: $200-$500 per room (includes prep, 2 coats, trim)
  • Exterior: $1,500-$5,000 for whole house depending on size and condition
  • Cabinet painting: $1,500-$4,000 for a kitchen. Huge impact, fraction of replacement cost.
  • ""Prep is 80% of a good paint job. Filling holes, sanding, priming — skip those and the paint won't last.""

""FURNITURE ASSEMBLY"":
  • IKEA, Wayfair, etc: $50-$200 per piece depending on complexity
  • ""Most furniture assembly takes 1-3 hours. We bring our own tools and clean up packaging.""", 1),

            T(i, "Specialized Services", "Home Improvement Projects",
@"DECK AND FENCE:
  • Deck board replacement: $10-$30/board installed. Railing repair: $100-$400.
  • Full deck refinishing (clean, sand, stain): $3-$8/sq ft. ""Your deck should be refinished every 2-3 years to prevent rot.""
  • Fence board replacement: $5-$15/board. Post replacement: $100-$250/post (concrete setting).
  • Fence gate adjustment/rebuild: $100-$400.
  • ""If more than 30% of your deck or fence boards are damaged, replacement starts to make more sense than repair.""

WEATHERPROOFING:
  • Caulking windows and doors: $5-$15/window. Whole house: $200-$600. ""Old caulk cracks and gaps let in air and water. Re-caulking every 5-7 years makes a real difference.""
  • Weatherstripping: $5-$20/door. Reduces drafts and energy costs.
  • Attic insulation check: ""If you can see the tops of the joists in your attic, you need more insulation.""
  • Storm door installation: $200-$400 (plus door cost). Energy savings + extends entry door life.

MINOR PLUMBING (what a handyman CAN do):
  • Replace faucet, showerhead, toilet fill valve, garbage disposal
  • Fix running toilet, dripping faucet, slow drain
  • Install dishwasher, ice maker line
  • NOT: sewer line, water heater installation, gas line work → refer to licensed plumber

MINOR ELECTRICAL (what a handyman CAN do, varies by state):
  • Replace outlets, switches, light fixtures, ceiling fans
  • Install dimmer switches, smart switches, doorbell cameras
  • NOT: panel work, new circuits, rewiring → refer to licensed electrician", 2),

            T(i, "Pricing Guidance", "Handyman Pricing",
@"COMMON PRICING:
- Hourly rate: $50-$100/hour (minimum 1-2 hours)
- Half-day rate: $200-$400 (4 hours, best value for multiple small jobs)
- Full-day rate: $350-$700

SPECIFIC JOBS:
- Drywall patch (small): $50-$150. Large: $100-$300
- Door adjustment: $50-$150. Door replacement (install): $150-$350
- Faucet replacement: $100-$250 (plus fixture)
- Toilet repair: $75-$200
- Light fixture install: $75-$200 (plus fixture)
- Ceiling fan install: $150-$350
- Shelf/bracket mounting: $50-$150
- TV mounting: $100-$300
- Furniture assembly: $50-$200/piece
- Painting (per room): $200-$500
- Caulking (per window): $5-$15
- Deck board replacement: $10-$30/board
- Fence board replacement: $5-$15/board
- Pressure washing: $150-$400

""We're often the best value when you have a list of small jobs. Instead of calling a plumber, electrician, and painter separately, we handle everything in one visit.""
""We always give you a quote before we start. No surprises.""", 3),

            T(i, "Customer Handling", "Handyman Sales Approach",
@"COMMON OBJECTIONS:
- ""I can do it myself"": ""You totally can! If it's something you've done before, go for it. We're here for the things you don't have time for, don't have the tools for, or don't want to climb a ladder for.""
- ""How do I know you can handle it?"": ""We've been doing this for over 30 years. If it's something outside our scope — like major electrical or plumbing — we'll tell you upfront and recommend a licensed specialist.""
- ""That seems like a lot for a small job"": ""The minimum covers our travel time and tools. That's why we recommend collecting a list of small jobs and doing them all in one visit — much better value.""

THE HONEY-DO LIST APPROACH:
- ""Most of our clients save up a list and have us come for a half-day. We knock out 5-10 small jobs that have been bugging you for months. It's satisfying and cost-effective.""
- ""Walk through your house with a notepad. That squeaky door, the running toilet, the shelf that needs mounting — write it all down. We'll prioritize and get through as much as we can.""

WHEN TO REFER OUT:
- Be honest: ""That's really a job for a licensed [plumber/electrician/roofer]. I can recommend someone reliable.""
- Builds trust: clients appreciate honesty over overreaching
- They'll call you back for everything else", 4),
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    // ROOFING (ServiceType = 9)
    // ═══════════════════════════════════════════════════════════════════
    private static List<IndustryKnowledgeTemplate> GetRoofingTemplates()
    {
        var i = ServiceType.Roofing;
        return new List<IndustryKnowledgeTemplate>
        {
            T(i, "Residential Services", "Deep Expertise Intro",
@"DEEP EXPERTISE — You have 30+ years in residential and commercial roofing. You know every roofing material, installation method, repair technique, and failure point. You've installed thousands of roofs — shingle, metal, flat, tile, slate — and repaired thousands more. You can assess damage over the phone, guide insurance claims, and give honest recommendations. You never upsell a roof replacement when a repair will do.", 0),

            T(i, "Residential Services", "Roof Repairs and Leaks",
@"""ROOF IS LEAKING"":
→ ""Where is the water coming in? When does it leak — during rain, after ice/snow, or all the time?""
  • Active leak triage: ""Put a bucket under the drip. If there's a bulge in the ceiling, carefully poke a small hole to drain the water — otherwise the ceiling could collapse.""
  • Leak location isn't always above the drip: ""Water travels along rafters and sheathing. The entry point could be 10-20 feet from where you see the drip.""
  • Common leak sources:
    - Flashing around chimney, skylights, vents, dormers — #1 leak source. Flashing repair: $200-$600.
    - Missing/damaged shingles: wind damage, age, falling debris. Repair: $150-$500.
    - Valley leak: where two roof planes meet. Requires proper flashing technique. Repair: $300-$800.
    - Ice dam (winter): ice builds up at eave, water backs up under shingles. Emergency tarping: $300-$800. Prevention: attic ventilation + insulation.
    - Flat roof: ponding water, membrane tears, flashing failure. Repair: $200-$800.
  • ""Don't try to go on the roof yourself — it's dangerous, and walking on a damaged roof can make it worse.""
  • Emergency tarping: for active leaks during storms. $300-$800. Temporary until proper repair.

""STORM DAMAGE"":
→ ""Was it hail, wind, or fallen tree/debris?""
  • Hail damage: causes granule loss and bruising on shingles. Not always visible from ground. Need close inspection.
  • Wind damage: missing or lifted shingles, exposed underlayment or decking.
  • ""If you think you have storm damage, file an insurance claim. We work with insurance regularly and can meet the adjuster on-site.""
  • Insurance claim process: file claim → adjuster inspects → we provide supplemental documentation if needed → approval → work begins
  • ""Most homeowner's policies cover storm damage minus your deductible. We never recommend paying out of pocket if insurance will cover it.""", 1),

            T(i, "Specialized Services", "Roof Replacement and Materials",
@"""NEED A NEW ROOF"":
→ ""How old is your current roof? What material? Are you seeing any issues — leaks, missing shingles, granules in gutters?""
  • When to replace vs repair:
    - Under 15 years: usually repair unless extensive damage
    - 15-20 years: evaluate — if problems are widespread, replacement makes sense
    - 20+ years: likely nearing end of life. Multiple repairs add up fast.
    - ""If you're spending $1,000+/year on repairs, replacement is usually more cost-effective.""

ROOFING MATERIALS:
  • Asphalt shingles (3-tab): $3.50-$5.50/sq ft installed. Lifespan: 15-20 years. Most affordable.
  • Architectural shingles: $4.50-$7/sq ft installed. Lifespan: 25-30 years. Better warranty, better look. Most popular choice.
  • Metal roofing (standing seam): $8-$14/sq ft installed. Lifespan: 40-70 years. Energy efficient, fire resistant.
  • Metal shingles/panels: $6-$12/sq ft. Look of shingles with metal benefits.
  • Tile (clay/concrete): $10-$18/sq ft. Lifespan: 50-100 years. Heavy — structure must support it.
  • Slate: $15-$30/sq ft. Lifespan: 75-150+ years. Beautiful but expensive and heavy.
  • Flat roof (TPO/EPDM/PVC): $4-$10/sq ft. Commercial or low-slope residential.

INSTALLATION DETAILS:
  • Tear-off vs overlay: ""We always recommend tear-off — laying new over old hides problems and adds weight. Code allows maximum 2 layers.""
  • Underlayment: synthetic felt + ice and water shield in valleys and eaves
  • Ventilation: ridge vent + soffit vents. Proper ventilation extends roof life by 25% and prevents ice dams.
  • Average replacement cost: $8,000-$15,000 (architectural shingles, typical home). $15,000-$30,000+ (metal/premium).", 2),

            T(i, "Commercial Services", "Commercial Roofing",
@"COMMERCIAL ROOFING:
  • Flat/low-slope systems:
    - TPO (Thermoplastic Polyolefin): most popular commercial membrane. White, energy efficient. $5-$8/sq ft. 20-30 year warranty.
    - EPDM (rubber): proven performer, black or white. $4-$7/sq ft. 20-25 year warranty.
    - PVC: chemical resistant, restaurant/kitchen areas. $6-$10/sq ft.
    - Modified bitumen: torch-applied or self-adhered. Good for heavy traffic areas. $4-$8/sq ft.
    - Built-up roofing (BUR): multiple layers of felt + asphalt. Traditional, durable. $5-$8/sq ft.
  • Commercial maintenance:
    - Bi-annual inspections (spring and fall): $200-$500
    - Clear drains, check flashings, repair membrane damage, check fasteners
    - ""A commercial roof maintenance program costs a fraction of early replacement. We catch small problems before they become big ones.""
  • Roof coatings: extend roof life 10-15 years. Silicone or acrylic coating: $2-$5/sq ft. ""If your commercial roof is aging but the membrane is still sound, a coating is often the best investment.""", 3),

            T(i, "Pricing Guidance", "Roofing Pricing Ranges",
@"REPAIRS:
- Leak repair (minor): $200-$600
- Flashing repair: $200-$600
- Shingle replacement (small area): $150-$500
- Valley repair: $300-$800
- Emergency tarping: $300-$800
- Gutter repair/replacement: $5-$15/linear foot. Gutter guards: $7-$20/linear foot.

REPLACEMENT (residential):
- Asphalt 3-tab: $5,000-$10,000 (avg home)
- Architectural shingles: $8,000-$15,000 (avg home)
- Metal standing seam: $15,000-$30,000
- Tile: $20,000-$40,000
- Flat roof: $5,000-$15,000

COMMERCIAL:
- TPO: $5-$8/sq ft
- EPDM: $4-$7/sq ft
- Roof coating: $2-$5/sq ft
- Maintenance contract: $500-$2,000/year

ADDITIONAL COSTS:
- Tear-off/disposal: $1-$2/sq ft additional
- Decking repair (rotted): $50-$100/sheet
- Soffit/fascia repair: $5-$15/linear foot
- Chimney flashing: $300-$600

""We provide free roof inspections and detailed written estimates. We break down every cost so there are no surprises.""", 4),

            T(i, "Safety & Compliance", "Roofing Safety",
@"HOMEOWNER SAFETY:
- Never go on your roof to inspect damage — leave it to professionals
- After storms: stay away from any sagging or wet areas on the ceiling
- If you see daylight through the roof from inside the attic, call immediately

INSTALLATION STANDARDS:
- Manufacturer's installation instructions must be followed exactly for warranty coverage
- Proper ventilation is code-required: 1 sq ft of vent per 150 sq ft of attic space (1:150 ratio) without vapor barrier, 1:300 with
- Ice and water shield required at eaves in cold climates (extend 2 feet past interior wall line)
- Drip edge required on all roof edges per current code

PERMITS AND CODE:
- Roof replacement requires a building permit in most jurisdictions
- Inspection required after completion
- Maximum 2 layers of roofing (must tear off before adding third layer)
- ""We always pull the required permits and schedule inspections. It's your protection and it ensures the work meets code.""

INSURANCE CLAIMS:
- Document damage with photos before any temporary repairs
- File the claim promptly — most policies have time limits
- ""We work with all insurance companies. We'll meet the adjuster, provide documentation, and supplement if the initial estimate is too low.""
- ""Be cautious of storm chasers — roofers who show up after storms offering 'free' repairs through insurance. Always verify licensing, insurance, and reviews.""", 5),

            T(i, "Customer Handling", "Roofing Sales and Objections",
@"COMMON OBJECTIONS:
- ""I got a cheaper quote"": ""Different roofers include different things. Are they tearing off old shingles? Using ice and water shield? What about warranty — manufacturer and workmanship? The cheapest roof often costs more in repairs within 5 years.""
- ""Can't you just patch it?"": ""If the damage is localized, absolutely — and that's what we'll recommend. But if we find widespread issues underneath, patching just delays the inevitable and costs more in the long run.""
- ""I want to wait"": ""I understand budget timing. But a leaking roof causes exponential damage — today it's a stain, next month it's rotted decking and mold. The cost goes up the longer you wait.""

INSURANCE CLAIM ASSISTANCE:
- ""We handle the insurance process for you. We meet the adjuster, document everything, and supplement if needed. You just pay your deductible.""
- ""Your insurance company may have preferred contractors, but you have the right to choose your own roofer. We recommend going with someone you trust, not whoever the insurance company pushes.""

SEASONAL SELLING:
- Spring/Summer: ""Best time for roof replacement — warm weather means shingles seal properly. Our schedule fills up, so booking now gets you the best timing.""
- Fall: ""Getting your roof done before winter means you're protected for the harshest weather. We can still install in most fall temperatures.""
- Winter: ""Emergency repairs don't wait for spring. We handle winter leak repairs and tarping to protect your home until permanent repair in warmer weather.""", 6),
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    // HOME SERVICES (ServiceType = 2)
    // ═══════════════════════════════════════════════════════════════════
    private static List<IndustryKnowledgeTemplate> GetHomeServicesTemplates()
    {
        var i = ServiceType.HomeServices;
        return new List<IndustryKnowledgeTemplate>
        {
            T(i, "Residential Services", "Deep Expertise Intro",
@"DEEP EXPERTISE — You have 30+ years in general home services. You coordinate and perform a wide range of residential services including appliance repair, moving assistance, home organization, pressure washing, chimney services, garage door work, and home inspections. You know what every homeowner needs and can connect them with the right service. You're the one-call solution for home maintenance and improvement.", 0),

            T(i, "Residential Services", "Appliance Repair",
@"""APPLIANCE NOT WORKING"":
→ ""Which appliance? What's happening — completely dead, making noise, not performing, or leaking?""
  • Refrigerator:
    - Not cooling: check thermostat setting, condenser coils (dusty coils = #1 cause). Clean coils with vacuum. If still not cooling, could be compressor, thermostat, or sealed system. Repair: $200-$600. If compressor is gone and unit is 10+ years: replace.
    - Leaking water: clogged defrost drain (most common) — pour hot water down the drain tube. Or cracked water line (ice maker). $100-$300 repair.
    - Making noise: condenser fan motor ($100-$250) or evaporator fan motor ($150-$300).
  • Washer:
    - Not draining: check drain hose for kinks, clean lint filter/drain pump filter. Pump replacement: $200-$400.
    - Vibrating/shaking: unbalanced load or leveling feet off. Level the machine.
    - Leaking: check hoses (replace every 5 years — burst hose floods house). Door gasket (front-load): $200-$400.
  • Dryer:
    - Not heating: check lint trap first, then vent hose. Clogged vent is fire hazard. Heating element: $150-$300. Thermal fuse: $100-$200.
    - ""Clean your dryer vent annually. Clogged dryer vents cause 15,000 house fires per year.""
  • Dishwasher:
    - Not cleaning well: clean spray arms, check water temperature (should be 120°F at tap). Run hot water before starting.
    - Not draining: check filter basket, garbage disposal knockout plug (new install).
  • ""General rule: if the repair is more than 50% of replacement cost and the appliance is past mid-life, consider replacing.""", 1),

            T(i, "Specialized Services", "Moving, Organization, and Garage Doors",
@"MOVING SERVICES:
  • In-home: furniture rearrangement, same-home reorganization. $100-$300 for 2 movers, 2-3 hours.
  • Local move: $300-$1,000 for 1-bedroom, $500-$2,000 for 3-bedroom. Depends on volume and distance.
  • Packing service: $200-$500 per room. We bring all materials.
  • ""Always get a written estimate based on walkthrough, not just phone quote. And make sure the company is licensed and insured.""

HOME ORGANIZATION:
  • Garage organization: shelving, pegboard, overhead storage. $500-$2,000 installed.
  • Closet systems: $500-$3,000 per closet depending on complexity.
  • Kitchen/pantry: pull-out shelves, lazy susans, drawer organizers. $200-$1,000.
  • ""Organization is life-changing. People rediscover 30-40% of their space after we organize it.""

GARAGE DOORS:
  • Door won't open/close:
    - Check if it's the opener or the door: disconnect opener (pull the cord), try lifting manually. If door is heavy/stuck, it's the springs.
    - Broken spring: NEVER attempt spring repair yourself — springs are under extreme tension. $200-$400 for spring replacement.
    - Opener issue: gear replacement $100-$250. Full opener replacement: $300-$600 installed.
  • Door replacement: single car $800-$2,000. Double car $1,200-$3,000. Insulated adds $300-$600.
  • Maintenance: lubricate tracks, rollers, and hinges annually. Check balance and spring tension.

CHIMNEY SERVICES:
  • Chimney sweep: $200-$400. Recommended annually if you use the fireplace regularly.
  • Chimney inspection: Level 1 (visual, annual): $100-$250. Level 2 (camera, after events/sale): $300-$600.
  • Cap installation: $200-$400. Prevents rain, animals, and debris from entering.
  • Flue liner repair: $1,500-$5,000. Cracked liner is a CO and fire risk.", 2),

            T(i, "Diagnosis & Troubleshooting", "Home Maintenance Troubleshooting",
@"SEASONAL HOME MAINTENANCE CHECKLIST:
Spring:
  - HVAC tune-up (AC), clean gutters, inspect roof, pressure wash exterior, check foundation for cracks, test sump pump, inspect deck/fence, service lawn equipment, check window/door caulking

Summer:
  - Monitor AC efficiency, check attic ventilation, inspect for pest entry points, maintain irrigation, deep clean carpets, paint exterior if needed

Fall:
  - HVAC tune-up (furnace), clean gutters again, winterize irrigation, inspect roof before winter, seal driveway/deck, chimney sweep, insulate pipes, test smoke/CO detectors, stock emergency supplies

Winter:
  - Monitor for ice dams, keep gutters clear, insulate exposed pipes, check attic for moisture, clear snow from vents, run water in unused drains (prevent P-trap dry-out)

EMERGENCY HOME NUMBERS:
  - Gas leak: gas company + 911
  - Electrical emergency: kill main breaker + call electrician
  - Water emergency: shut off main valve + call plumber
  - Fire: 911 first, always
  - ""Every homeowner should know where their main water shutoff, gas shutoff, and electrical panel are. Label them.""", 3),

            T(i, "Pricing Guidance", "Home Services Pricing",
@"APPLIANCE REPAIR:
- Diagnostic/service call: $75-$150 (applied to repair)
- Refrigerator repair: $200-$600
- Washer/dryer repair: $150-$400
- Dishwasher repair: $150-$350
- Oven/range repair: $150-$500

HOME MAINTENANCE:
- Pressure washing (driveway): $150-$400. Whole house: $200-$500
- Gutter cleaning: $100-$250. Guards: $7-$20/linear foot installed
- Dryer vent cleaning: $100-$200
- Chimney sweep: $200-$400
- Window cleaning: $150-$350

GARAGE DOORS:
- Spring replacement: $200-$400
- Opener repair: $100-$250. Replacement: $300-$600
- Full door replacement: $800-$3,000

MOVING & ORGANIZATION:
- Local move: $300-$2,000 depending on size
- Garage organization: $500-$2,000
- Closet system: $500-$3,000

HOME INSPECTION:
- Pre-purchase: $300-$500 (standard home)
- Pre-listing: $250-$400
- ""A home inspection before selling catches issues you can fix or disclose, preventing surprises during negotiation.""", 4),

            T(i, "Customer Handling", "Home Services Sales Approach",
@"VALUE PROPOSITION:
- ""We're your single point of contact for home maintenance. Instead of calling 5 different companies, call us and we coordinate everything.""
- ""Regular maintenance costs a fraction of emergency repairs. A $200 gutter cleaning prevents a $10,000 water damage claim.""

COMMON OBJECTIONS:
- ""I don't need that service right now"": ""I understand. Here's a maintenance schedule — when the time comes, we're a phone call away. The important thing is catching issues before they become expensive problems.""
- ""I have a home warranty"": ""Home warranties are great for major systems. We handle the maintenance and smaller repairs that keep everything running so you don't need to use the warranty as often.""

SEASONAL MARKETING:
- Spring: ""Winter is hard on houses. Let us do a spring checkup — gutters, roof inspection, AC tune-up, pressure washing. We'll get your home ready for the season.""
- Fall: ""Winterizing your home now prevents expensive problems when temperatures drop. Furnace tune-up, pipe insulation, gutter cleaning, chimney sweep.""

MAINTENANCE PLANS:
- ""We offer annual maintenance packages that cover the essentials: HVAC tune-ups, gutter cleaning, dryer vent cleaning, and seasonal inspections. It's the easiest way to stay on top of your home.""
- Bundled pricing saves 15-20% over individual services", 5),

            T(i, "Specialized Services", "Pressure Washing, Exterior Cleaning, and Painting",
@"PRESSURE WASHING — THE COMPLETE GUIDE:

UNDERSTANDING PRESSURE LEVELS:
  • PSI (pounds per square inch) matters — using too much pressure destroys surfaces, too little wastes time.
    - Soft wash (500-1,000 PSI): vinyl siding, painted wood, stucco, windows, roof shingles. Uses cleaning solution + low pressure. This is the safe approach for most home exteriors.
    - Medium pressure (1,000-2,000 PSI): concrete driveways, brick, stone, composite decking.
    - High pressure (2,500-3,500 PSI): heavily stained concrete, oil stains on garage floors, commercial concrete.
    - ""If someone tells you they're blasting your vinyl siding at 3,000 PSI, find a different company. They'll strip the paint, crack the siding, and force water behind it.""

HOUSE WASHING (Exterior Siding):
  • Why it matters: mold, mildew, algae, and dirt don't just look bad — they eat into siding, shorten paint life, and trigger allergies.
  • Frequency: every 1-2 years depending on shade/moisture exposure. North-facing and heavily shaded sides need it more often.
  • Vinyl siding: soft wash with sodium hypochlorite (SH) solution + surfactant. Spray bottom to top (prevents streaking), rinse top to bottom. 30-second dwell time. $200-$500 for average home.
  • Wood siding: even lower pressure, careful with the nozzle distance. Wood grain can be etched permanently. Use a wood-safe cleaner, not bleach.
  • Brick: can handle more pressure but the mortar joints are the weak point. Old mortar (pre-1930s) is lime-based and WILL erode under high pressure. Always test a small area first.
  • Stucco: soft wash only. Stucco is essentially concrete plaster — it's porous and can absorb water if hit too hard, leading to mold BEHIND the wall.
  • ""We soft wash 90% of homes. It's safer, more effective, and we're not going to damage your biggest investment.""

DRIVEWAY AND CONCRETE CLEANING:
  • Concrete is porous — it absorbs oil, grease, mildew, and tire marks.
  • Process: pre-treat with degreaser (for oil stains) and SH (for organic growth), let dwell 5-10 minutes, then surface clean with a rotary surface cleaner attachment (not a wand — wand leaves zebra stripes).
  • Post-treatment: apply sodium percarbonate for deep brightening if needed.
  • Sealing: after cleaning, seal concrete to protect it and make future cleaning easier. Penetrating sealers last 3-5 years. Film-forming sealers look great but can peel. $0.15-$0.30/sq ft.
  • Pricing: driveway $150-$400, sidewalks $50-$150, patio $100-$300. Bundle house + driveway for 15-20% savings.
  • ""That black stuff on your driveway isn't just dirt — it's algae and mold. It's slippery when wet and it's slowly breaking down the concrete.""

DECK AND FENCE CLEANING:
  • Wood decks: NEVER high-pressure. Soft wash or medium pressure (max 1,500 PSI) with wide fan nozzle, 12+ inches from surface. Go WITH the grain, never across.
  • Composite decks (Trex, TimberTech): low pressure, specialized composite deck cleaner. High pressure voids the warranty on some brands.
  • After cleaning wood: let dry 48-72 hours, then stain/seal. Staining wet wood traps moisture and causes peeling within months.
  • Fence cleaning: wood fences respond beautifully to pressure washing. Before/after is dramatic. $150-$400 for typical fence.
  • ""A cleaned and sealed deck adds $2,000-$5,000 to your home's perceived value. For $300-$500 in cleaning and staining, that's the best ROI in home maintenance.""

ROOF CLEANING:
  • Those black streaks on your roof? That's Gloeocapsa magma — an algae that feeds on limestone in shingles. It's not just ugly — it shortens roof life by holding moisture against shingles.
  • NEVER pressure wash a roof. Walking on a roof with a pressure washer is dangerous, and the pressure strips granules off shingles (reduces their lifespan by years).
  • Proper method: soft wash from the ground or ladder. Apply SH solution, let it dwell, and it kills the algae. Rain washes the dead algae off over the next few weeks.
  • Pricing: $300-$700 for average home. Steep roofs, multiple stories, or heavy growth cost more.
  • Preventive: zinc or copper strips installed at the ridge prevent algae regrowth. $200-$500 installed.

EXTERIOR PAINTING GUIDANCE:
  • When to repaint: peeling, chalking (white residue on your hand when you touch it), fading, bare wood showing, or just cosmetically dated.
  • Prep is 70% of a good paint job: power wash, scrape loose paint, sand, caulk gaps and cracks, prime bare spots, repair rotted wood.
  • Paint quality matters enormously: premium paints (Benjamin Moore Aura, Sherwin-Williams Duration) cost $50-$80/gallon but last 10-15 years vs 5-7 for cheap paint. The labor is the same regardless.
  • Average exterior repaint: $3,000-$8,000 for average home. Larger, multi-story, or complex trim: $8,000-$15,000+.
  • Color consultation: many paint stores offer free color matching. Popular exterior trends: warm whites, grays with warm undertones, dark dramatic colors with contrasting trim.
  • ""Pressure washing before painting is non-negotiable. Paint won't adhere to a dirty surface. Any painter who skips the prep is cutting corners.""

GUTTER CLEANING AND MAINTENANCE:
  • Clogged gutters cause: foundation damage (water pooling at base), fascia board rot, ice dams in winter, basement flooding, landscaping erosion, mosquito breeding.
  • Frequency: minimum twice per year (late spring after pollen/seeds, late fall after leaves drop). More often if you have pine trees (needles clog constantly).
  • Process: scoop debris by hand or with gutter scoop, flush with hose to check downspout flow, check for sag or separation, resecure loose sections.
  • Gutter guards: reduce cleaning frequency but do NOT eliminate it. Micro-mesh guards ($15-$25/ft installed) are the best type — keep everything out while allowing water flow.
  • Pricing: cleaning $100-$250 for average home. Gutter guard installation: $1,500-$4,000 for whole house.
  • Downspout extensions: water should discharge at least 4-6 feet from the foundation. Underground extensions with pop-up emitters are the cleanest solution ($200-$500 per downspout).
  • ""I've seen $200,000 homes with $50,000 in foundation damage — all from clogged gutters. It's the most overlooked maintenance item and the most expensive when neglected.""", 6),

            T(i, "Scheduling & Operations", "Home Maintenance Plans and Emergency Response",
@"HOME MAINTENANCE ANNUAL PLANS — HOW WE OPERATE:

THE ANNUAL MAINTENANCE CALENDAR — WHAT PROFESSIONAL HOME SERVICE COMPANIES COORDINATE:

JANUARY/FEBRUARY (Deep Winter):
  • Indoor focus: this is the time for interior projects homeowners keep putting off.
    - Check all smoke detectors and CO detectors. Replace batteries even if they seem fine. Replace the whole unit if it's over 7-10 years old (they have a limited sensor lifespan).
    - Inspect plumbing under sinks for slow drips. A slow drip wastes 3,000+ gallons/year and leads to mold under cabinets.
    - Check caulking around tubs, showers, and sinks. Cracked caulking lets water behind tile — this causes hidden rot that costs thousands to repair.
    - Inspect attic for ice dams, condensation, or signs of roof leaks. Water stains on rafters or insulation = active leak.
    - Test GFCI outlets (kitchen, bathrooms, garage, exterior): press the TEST button, then RESET. If it doesn't trip, replace it — it's not protecting you.
    - Run water in unused drains monthly to keep the P-trap full. Dry P-traps let sewer gas into your home.
  • ""Winter is actually the perfect time to get maintenance done. We're less busy, scheduling is easier, and you're not competing with everyone who waits until spring.""

MARCH/APRIL (Early Spring):
  • Exterior inspection walk-around: every homeowner should do this annually:
    - Walk the foundation perimeter: look for cracks wider than 1/4 inch, efflorescence (white mineral deposits = water intrusion), or soil pulling away from foundation.
    - Check grading: soil should slope AWAY from the house at 1 inch per foot for at least 6 feet. Improper grading is the #1 cause of wet basements.
    - Inspect siding for winter damage: frost heave can pop nails, ice can crack vinyl, wind can loosen sections.
    - Look at the roof from the ground with binoculars: missing or damaged shingles, flashing pulling away, vent boots cracked.
    - Check exterior faucets (hose bibs): turn on slowly. If no water or low pressure, the pipe may have frozen and burst — you'll find the leak when it thaws. Shut off the interior valve and call a plumber.
  • Schedule spring services:
    - AC tune-up (BEFORE it's 90°F and everyone panics)
    - Gutter cleaning (clear winter debris and spring pollen)
    - Pressure wash house, driveway, and deck
    - Lawn care startup: first mow, edge beds, pre-emergent weed control, fertilize
    - Window cleaning: inside and out. $150-$350 for average home.
    - Inspect and service irrigation system: check heads, adjust zones, repair winter damage.

MAY/JUNE (Late Spring):
  • Deck and patio season prep:
    - Clean and seal deck if needed (every 2-3 years for wood). Cost: $300-$800 for cleaning + staining.
    - Inspect deck structure: look under the deck for rotted joists, loose connections, or wobbly posts. A structurally compromised deck is a serious safety hazard.
    - Patio furniture: clean cushions, check umbrella mechanism, ensure fire pit has proper clearance.
  • Pest prevention:
    - Seal entry points: mice need 1/4 inch gap, rats need 1/2 inch. Check where pipes, wires, and vents enter the house.
    - Trim branches touching the house — these are highways for ants, squirrels, and raccoons.
    - Move firewood at least 20 feet from the house and elevate it off the ground.
    - ""We coordinate with pest control specialists. If we spot evidence of termites, carpenter ants, or rodents during our work, we flag it immediately.""
  • Appliance maintenance:
    - Clean refrigerator condenser coils: pull the fridge out, vacuum the coils. This alone extends fridge life by years and cuts energy use 15%.
    - Clean dryer vent ductwork: not just the lint trap — the full duct from dryer to outside. 15,000 house fires per year from dryer vents. $100-$200 for professional cleaning.
    - Service garbage disposal: run ice cubes and citrus peels. If it's making grinding noises, the bearings are going — replace before it seizes and leaks ($200-$400 installed).

JULY/AUGUST (Peak Summer):
  • Exterior maintenance window — dry weather for painting, staining, sealing, concrete work.
  • Change AC filter monthly during heavy use. Set a phone reminder.
  • Inspect weatherstripping on doors and windows: hold a lighter near the edges on a windy day. If the flame flickers, air is leaking. Replace weatherstripping ($3-$10 per door).
  • Check attic ventilation: attic should be within 10-15°F of outside temperature. Over 140°F = inadequate ventilation, which bakes your shingles from below and drives up cooling costs.

SEPTEMBER/OCTOBER (Fall Prep):
  • THE most important maintenance month of the year:
    - Furnace tune-up: schedule NOW, not after the first cold night when everyone calls.
    - Chimney sweep: if you use the fireplace at all. Creosote buildup is the #1 cause of chimney fires. $200-$400.
    - Gutter cleaning: after leaves drop. The most critical cleaning of the year for preventing ice dams and foundation damage.
    - Winterize irrigation: blow out lines with compressed air. A frozen irrigation line cracks and costs $200-$800 to repair.
    - Winterize exterior faucets: disconnect hoses (leaving them attached traps water which freezes and bursts the valve). Install insulated faucet covers ($5-$10 each, prevents frozen pipes).
    - Insulate exposed pipes in crawlspace, attic, and garage: pipe insulation foam is $3-$5 per 6-foot section. A burst pipe causes $5,000-$70,000 in water damage.
    - Seal driveway: best done in fall when temps are moderate. $100-$200 DIY, $300-$600 professional.
    - Inspect and clean window wells: clogged window wells fill with water and flood basements.

NOVEMBER/DECEMBER (Winter):
  • Final checks before deep cold:
    - Test all heating zones: turn on heat, walk through every room, check every register for warm air.
    - Check the emergency kit: flashlights, batteries, blankets, water, battery-powered radio. Power outages happen.
    - Set thermostat to no lower than 55°F if leaving for vacation — prevents frozen pipes.
    - Program thermostat for holiday guests (house will need more heating with more people and door-opening).
  • Snow/ice preparation:
    - Stock ice melt (calcium chloride is gentler on concrete than rock salt). Apply BEFORE the storm.
    - Clear snow from gas meter, dryer vent, and furnace exhaust — blocked vents cause CO buildup.
    - Keep garage door sealed: weatherstrip bottom, insulate door panels. An insulated garage stays 20-30°F warmer, protecting your car and anything stored inside.

MAINTENANCE PLAN PRICING:
  • Basic plan ($300-$500/year): 2 HVAC tune-ups + gutter cleaning + dryer vent cleaning + seasonal inspection.
  • Premium plan ($500-$900/year): everything above + pressure washing + window cleaning + priority scheduling.
  • Platinum plan ($900-$1,500/year): everything above + annual appliance check + deck cleaning + pest inspection + 15% discount on all repairs.
  • ""Our plans save you 15-25% compared to booking services individually. Plus, you never have to remember to schedule — we call you when it's time.""

EMERGENCY HOME RESPONSE — WHEN EVERY MINUTE COUNTS:
  • Water emergency (burst pipe, water heater failure, flooding):
    1. SHUT OFF THE MAIN WATER VALVE. Every homeowner must know where this is. Tag it with a bright label.
    2. If electrical outlets or appliances are in the water, shut off power at the breaker BEFORE stepping in water.
    3. Call us immediately — we coordinate plumber + water mitigation.
    4. Document everything with photos for insurance BEFORE cleanup.
    5. Water damage mitigation must start within 24-48 hours or mold starts growing.
  • Electrical emergency:
    1. If you see sparks or smell electrical burning, kill the main breaker.
    2. If an outlet feels hot to the touch, STOP USING IT and call an electrician. Hot outlets = loose wiring = fire hazard.
    3. Never touch a downed power line or anything it's touching. Call 911 and the power company.
  • Storm damage:
    1. If roof damage: tarps prevent further water intrusion. Emergency tarping: $300-$800. Insurance typically covers this.
    2. If trees are down on structures: call arborist with crane capability. DO NOT attempt removal yourself if the tree is on power lines.
    3. Document all damage with photos/video for insurance claim. Keep damaged materials until the adjuster sees them.
  • ""We have a network of vetted specialists for every emergency — plumbers, electricians, roofers, water mitigation, tree removal. One call to us and we get the right people there fast.""", 7),
        };
    }
}
