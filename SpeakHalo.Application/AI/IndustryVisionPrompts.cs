using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Application.AI;

/// <summary>
/// Cleaning-industry-specific prompts for Gemini Vision analysis.
/// Covers stain identification, surface damage, mold detection, space estimation,
/// before/after assessment, and detailed cleaning service quoting from photos.
/// </summary>
public static class IndustryVisionPrompts
{
    public static string GetSystemPrompt(ServiceType industry)
    {
        return industry switch
        {
            _ => GetCleaningSystemPrompt()
        };
    }

    public static string GetAnalysisPrompt(string analysisType, string? customerDescription = null)
    {
        var basePrompt = analysisType switch
        {
            "stain_identification" => GetStainIdentificationPrompt(customerDescription),
            "surface_assessment" => GetSurfaceAssessmentPrompt(customerDescription),
            "mold_detection" => GetMoldDetectionPrompt(customerDescription),
            "space_estimation" => GetSpaceEstimationPrompt(customerDescription),
            "before_after" => GetBeforeAfterPrompt(customerDescription),
            "quote_generation" => GetQuoteGenerationPrompt(customerDescription),
            "damage_assessment" => GetDamageAssessmentPrompt(customerDescription),
            "equipment_identification" => GetEquipmentIdentificationPrompt(),
            "triage" => GetTriagePrompt(customerDescription),
            _ => GetGeneralAnalysisPrompt(customerDescription)
        };

        return basePrompt;
    }

    private static string GetCleaningSystemPrompt()
    {
        return @"You are a professional cleaning industry specialist with 20+ years of experience in residential, commercial, and specialized cleaning. Your expertise includes:

STAIN EXPERTISE:
- Stain identification: type (organic, chemical, biological, mineral), age (fresh vs set-in), severity
- Stain-to-surface matching: what treatments work on what surfaces without damage
- Success probability: realistic assessment of whether the stain can be fully removed
- Product recommendations: specific products and methods for each stain type

SURFACE EXPERTISE:
- Material identification: hardwood species, tile types (ceramic, porcelain, natural stone), carpet fibers, upholstery fabrics
- Condition assessment: wear level, damage type (cosmetic vs structural), maintenance history indicators
- Cleaning method selection: what products and techniques are safe for each material
- Warning flags: surfaces that need special care (marble = no acid, hardwood = no steam, silk = dry clean only)

SPACE & CONDITION ASSESSMENT:
- Room size estimation from photos (approximate square footage)
- Condition grading: well-maintained, moderate, neglected, severe
- Time estimation: how long cleaning will take based on condition
- Crew sizing: how many cleaners needed

DAMAGE & SAFETY:
- Mold vs mildew identification and severity
- Water damage assessment (cosmetic staining vs structural)
- Biohazard recognition (when to refer to specialists)
- Safety concerns: lead paint indicators, asbestos risk areas, chemical damage

PHOTO ANALYSIS APPROACH:
1. Identify all visible surfaces and materials
2. Categorize issues by type and severity
3. Assess what's cleanable vs what needs repair/replacement
4. Determine if specialized equipment or products are needed
5. Flag any safety concerns
6. Provide realistic expectations for the customer
7. Estimate scope of work for quoting

Always provide honest, practical assessments. Don't overpromise — if a stain is likely permanent, say so. If mold looks like it needs remediation, recommend a specialist.";
    }

    private static string GetStainIdentificationPrompt(string? customerDescription)
    {
        var sanitizedDescription = SanitizePromptInput(customerDescription);
        var customerInfo = !string.IsNullOrEmpty(sanitizedDescription)
            ? $"\n\nCustomer reported: {sanitizedDescription}"
            : "";

        return $@"Analyze this photo to identify stains and recommend treatment. Provide response in JSON format:

{{
  ""stains"": [
    {{
      ""type"": ""stain type (e.g., red wine, pet urine, grease, coffee, ink, blood, rust, mold, water, unknown)"",
      ""category"": ""organic/chemical/biological/mineral/dye"",
      ""age"": ""fresh/recent/set-in/old"",
      ""severity"": ""Light/Moderate/Heavy/Severe"",
      ""size"": ""approximate size in inches or description"",
      ""surface"": ""what the stain is on (carpet type, hardwood, tile, upholstery fabric, etc.)"",
      ""surfaceMaterial"": ""specific material if identifiable (nylon carpet, microfiber, oak hardwood, porcelain tile, etc.)"",
      ""removalLikelihood"": ""Very Likely/Likely/Possible/Unlikely/Cannot Remove"",
      ""removalMethod"": ""step-by-step treatment recommendation"",
      ""products"": [""specific product recommendations""],
      ""warnings"": ""any risks or cautions (e.g., test for colorfastness, may damage surface, etc.)"",
      ""diyAdvice"": ""what the customer can try at home before we arrive"",
      ""professionalNeeded"": true/false,
      ""estimatedTime"": ""time to treat in minutes""
    }}
  ],
  ""overallAssessment"": ""summary of all stains and recommended approach"",
  ""estimatedCost"": ""cost range for professional stain treatment"",
  ""confidenceScore"": 0-100
}}{customerInfo}

Respond ONLY with valid JSON. Be specific about products and methods. If the stain type is uncertain, provide your best assessment with confidence noted.";
    }

    private static string GetSurfaceAssessmentPrompt(string? customerDescription)
    {
        var sanitizedDescription = SanitizePromptInput(customerDescription);
        var customerInfo = !string.IsNullOrEmpty(sanitizedDescription)
            ? $"\n\nCustomer reported: {sanitizedDescription}"
            : "";

        return $@"Analyze this photo to assess the surface condition and recommend cleaning approach. Provide response in JSON format:

{{
  ""surfaces"": [
    {{
      ""type"": ""surface type (hardwood, tile, carpet, granite, marble, vinyl, laminate, stainless steel, glass, upholstery, etc.)"",
      ""specificMaterial"": ""specific material if identifiable (oak hardwood, porcelain tile, Berber carpet, Carrara marble, etc.)"",
      ""condition"": ""Excellent/Good/Fair/Poor/Damaged"",
      ""issues"": [""list of visible issues (scratches, stains, buildup, wear, discoloration, etc.)""],
      ""cleaningMethod"": ""recommended cleaning approach"",
      ""safeProducts"": [""products safe for this surface""],
      ""dangerousProducts"": [""products that would DAMAGE this surface (e.g., acid on marble, steam on hardwood)""],
      ""maintenanceTips"": ""ongoing care recommendations"",
      ""needsRepair"": true/false,
      ""repairDescription"": ""what repair is needed if applicable (refinishing, re-grouting, re-caulking, etc.)""
    }}
  ],
  ""overallCondition"": ""summary assessment"",
  ""recommendedService"": ""type of cleaning service recommended (regular clean, deep clean, specialized treatment)"",
  ""estimatedTime"": ""hours for cleaning"",
  ""estimatedCost"": ""cost range"",
  ""priorityIssues"": [""issues to address first""],
  ""confidenceScore"": 0-100
}}{customerInfo}

Respond ONLY with valid JSON. Pay special attention to surface material identification — wrong products on the wrong surface causes damage.";
    }

    private static string GetMoldDetectionPrompt(string? customerDescription)
    {
        var sanitizedDescription = SanitizePromptInput(customerDescription);
        var customerInfo = !string.IsNullOrEmpty(sanitizedDescription)
            ? $"\n\nCustomer reported: {sanitizedDescription}"
            : "";

        return $@"Analyze this photo for mold, mildew, or moisture-related issues. Provide response in JSON format:

{{
  ""findings"": [
    {{
      ""type"": ""mold/mildew/water damage/efflorescence/mineral deposit"",
      ""color"": ""black/green/white/orange/pink/gray"",
      ""location"": ""where on the surface (grout lines, caulking, wall, ceiling, windowsill, etc.)"",
      ""approximateArea"": ""estimated size (square inches or feet)"",
      ""severity"": ""Minor/Moderate/Significant/Severe"",
      ""surfaceOrStructural"": ""Surface only (cleanable) / Potentially structural (may need remediation)"",
      ""likelyCause"": ""poor ventilation, water leak, condensation, flooding, etc."",
      ""healthConcern"": ""None/Low/Moderate/High"",
      ""cleaningApproach"": ""recommended treatment if surface-level"",
      ""products"": [""recommended products""]
    }}
  ],
  ""canWeHandleThis"": true/false,
  ""reason"": ""why we can or cannot handle this (e.g., under 10 sq ft = we can clean, over 10 sq ft or structural = need certified mold remediation specialist)"",
  ""referralNeeded"": true/false,
  ""referralType"": ""mold remediation specialist / plumber (for leak) / HVAC (for ventilation) / none"",
  ""preventionAdvice"": ""how to prevent recurrence (ventilation, dehumidifier, fix leak, etc.)"",
  ""urgency"": ""Can wait/Should address soon/Urgent/Emergency"",
  ""healthWarning"": ""any health warnings for occupants"",
  ""estimatedCost"": ""cost if we can handle it"",
  ""confidenceScore"": 0-100
}}{customerInfo}

Respond ONLY with valid JSON. IMPORTANT: If mold area exceeds 10 sq ft or appears to be behind walls/in structure, ALWAYS recommend certified mold remediation specialist. Do not overstate our capabilities.";
    }

    private static string GetSpaceEstimationPrompt(string? customerDescription)
    {
        var sanitizedDescription = SanitizePromptInput(customerDescription);
        var customerInfo = !string.IsNullOrEmpty(sanitizedDescription)
            ? $"\n\nCustomer described: {sanitizedDescription}"
            : "";

        return $@"Analyze this photo to estimate the space size and cleaning requirements for quoting purposes. Provide response in JSON format:

{{
  ""room"": {{
    ""type"": ""room type (kitchen, bathroom, bedroom, living room, office, etc.)"",
    ""estimatedSquareFeet"": approximate number,
    ""estimatedDimensions"": ""estimated L x W in feet"",
    ""ceilingHeight"": ""standard (8ft) / high (9-10ft) / vaulted / unknown"",
    ""condition"": ""Well-maintained/Moderate/Needs attention/Neglected/Severe""
  }},
  ""surfaces"": {{
    ""flooring"": ""type (hardwood, tile, carpet, vinyl, etc.)"",
    ""countertops"": ""type if visible (granite, laminate, marble, etc.)"",
    ""walls"": ""condition (clean, scuffed, stained, needs washing)"",
    ""windows"": number if visible,
    ""fixtures"": [""notable fixtures or features""]
  }},
  ""cleaningScope"": {{
    ""serviceType"": ""regular clean / deep clean / move-out / post-construction"",
    ""estimatedHours"": number,
    ""crewSize"": number,
    ""specialEquipmentNeeded"": [""any special equipment""],
    ""challenges"": [""anything that adds time or complexity""]
  }},
  ""issues"": [
    {{
      ""description"": ""visible issue"",
      ""severity"": ""Low/Medium/High"",
      ""additionalTime"": ""extra time needed in minutes""
    }}
  ],
  ""estimatedCost"": {{
    ""regularClean"": ""range"",
    ""deepClean"": ""range"",
    ""addOns"": [""available add-on services with prices""]
  }},
  ""confidenceScore"": 0-100
}}{customerInfo}

Respond ONLY with valid JSON. Size estimates are approximate — note confidence level. Focus on information useful for generating a cleaning quote.";
    }

    private static string GetBeforeAfterPrompt(string? customerDescription)
    {
        var sanitizedDescription = SanitizePromptInput(customerDescription);
        var customerInfo = !string.IsNullOrEmpty(sanitizedDescription)
            ? $"\n\nContext: {sanitizedDescription}"
            : "";

        return $@"Analyze this photo to assess the current state and describe the expected transformation after professional cleaning. Provide response in JSON format:

{{
  ""currentState"": {{
    ""overallCondition"": ""Poor/Fair/Moderate/Good"",
    ""visibleIssues"": [""list all visible cleaning issues""],
    ""worstAreas"": [""areas needing the most work""],
    ""conditionScore"": 1-10
  }},
  ""afterCleaning"": {{
    ""expectedImprovement"": ""description of how it will look after professional cleaning"",
    ""cannotFix"": [""issues that cleaning alone won't resolve (damage, wear, etc.)""],
    ""improvementScore"": 1-10,
    ""improvementPercentage"": ""estimated visual improvement percentage""
  }},
  ""serviceRequired"": {{
    ""type"": ""regular clean / deep clean / specialized treatment"",
    ""estimatedHours"": number,
    ""methods"": [""cleaning methods and techniques to be used""],
    ""products"": [""key products needed""],
    ""specialEquipment"": [""any special equipment needed""]
  }},
  ""customerTalkingPoints"": [""key points to highlight for the customer about the transformation""],
  ""estimatedCost"": ""cost range"",
  ""confidenceScore"": 0-100
}}{customerInfo}

Respond ONLY with valid JSON. Be realistic about what professional cleaning can achieve — don't promise perfection if there's permanent damage.";
    }

    private static string GetQuoteGenerationPrompt(string? customerDescription)
    {
        var sanitizedDescription = SanitizePromptInput(customerDescription);
        var customerInfo = !string.IsNullOrEmpty(sanitizedDescription)
            ? $"\n\nCustomer reported: {sanitizedDescription}"
            : "";

        return $@"Analyze this photo to generate a detailed cleaning service quote. Provide response in JSON format:

{{
  ""space"": {{
    ""type"": ""room/area type"",
    ""estimatedSize"": ""approximate square footage"",
    ""surfaceTypes"": [""all surface types visible""],
    ""condition"": ""current condition assessment""
  }},
  ""services"": [
    {{
      ""service"": ""service name"",
      ""description"": ""what's included"",
      ""estimatedTime"": ""hours"",
      ""crewSize"": number,
      ""productsNeeded"": [""specific products""],
      ""equipmentNeeded"": [""specific equipment""],
      ""priceRange"": ""low-high estimate"",
      ""priority"": ""Essential/Recommended/Optional""
    }}
  ],
  ""addOns"": [
    {{
      ""service"": ""add-on service name"",
      ""description"": ""what it includes"",
      ""price"": ""cost"",
      ""recommended"": true/false,
      ""reason"": ""why recommended or optional""
    }}
  ],
  ""totalEstimate"": {{
    ""essentialOnly"": ""price range for essential services"",
    ""withRecommended"": ""price range including recommended add-ons"",
    ""withEverything"": ""price range for all services and add-ons""
  }},
  ""recurringDiscount"": ""estimated recurring service rate (15-20% off)"",
  ""notes"": [""any important notes for the customer""],
  ""confidenceScore"": 0-100
}}{customerInfo}

Respond ONLY with valid JSON. Prices should reflect market rates. Be transparent about what's essential vs optional.";
    }

    private static string GetDamageAssessmentPrompt(string? customerDescription)
    {
        var sanitizedDescription = SanitizePromptInput(customerDescription);
        var customerInfo = !string.IsNullOrEmpty(sanitizedDescription)
            ? $"\n\nCustomer reported: {sanitizedDescription}"
            : "";

        return $@"Analyze this photo to assess damage and determine what's cleanable vs what needs repair or replacement. Provide response in JSON format:

{{
  ""damage"": [
    {{
      ""type"": ""damage type (stain, scratch, crack, chip, burn, water damage, mold, wear, etc.)"",
      ""surface"": ""affected surface"",
      ""severity"": ""Cosmetic/Minor/Moderate/Significant/Severe"",
      ""cleanable"": true/false,
      ""cleaningMethod"": ""how to clean if cleanable"",
      ""repairNeeded"": true/false,
      ""repairType"": ""type of repair if needed (refinish, re-grout, replace, re-caulk, patch, etc.)"",
      ""repairSpecialist"": ""who to call for repair (flooring company, handyman, plumber, etc.) or null if we can handle"",
      ""estimatedRepairCost"": ""cost if applicable""
    }}
  ],
  ""ourScope"": {{
    ""whatWeCanDo"": [""list of things we can clean or address""],
    ""whatWeCannotDo"": [""list of things beyond cleaning (structural repair, replacement, etc.)""],
    ""estimatedCleaningTime"": ""hours"",
    ""estimatedCleaningCost"": ""cost range""
  }},
  ""recommendations"": [
    {{
      ""action"": ""recommended action"",
      ""priority"": ""Immediate/Soon/When convenient"",
      ""reason"": ""why this is recommended""
    }}
  ],
  ""safetyWarnings"": [""any safety concerns (mold, structural, lead paint, asbestos, etc.)""],
  ""confidenceScore"": 0-100
}}{customerInfo}

Respond ONLY with valid JSON. Be honest about the limits of what cleaning can achieve. Distinguish clearly between cleanable issues and damage that needs repair.";
    }

    private static string GetEquipmentIdentificationPrompt()
    {
        return @"Identify cleaning equipment or the surface/material in this photo:

1. What is it? (surface material, stain type, equipment, fixture, etc.)
2. Specific material/brand identification if possible
3. Current condition assessment
4. Recommended cleaning approach
5. Products to use (and products to AVOID)
6. Special considerations

Provide JSON response. Focus on information relevant to professional cleaning service delivery.";
    }

    private static string GetTriagePrompt(string? customerDescription)
    {
        var sanitizedDescription = SanitizePromptInput(customerDescription);
        var customerInfo = !string.IsNullOrEmpty(sanitizedDescription)
            ? $"\n\nCustomer description: {sanitizedDescription}"
            : "";

        return $@"Perform visual triage to help the cleaning team prepare for this job:

1. What surfaces need cleaning and their materials?
2. What products should the team bring (including specialty items)?
3. What equipment is needed beyond standard cleaning kit?
4. Is this a standard clean, deep clean, or specialized service?
5. Estimated time on-site with recommended crew size?
6. Any safety equipment or precautions required?
7. Are there issues we should flag to the customer before starting?

Provide JSON response with specific product and equipment recommendations.{customerInfo}

Goal: Help the cleaning team arrive fully prepared to deliver excellent results on the first visit.";
    }

    private static string GetGeneralAnalysisPrompt(string? customerDescription)
    {
        var customerInfo = !string.IsNullOrEmpty(customerDescription)
            ? $"\n\nCustomer notes: {customerDescription}"
            : "";

        return $@"Analyze this photo from a professional cleaning perspective and provide:

1. What surfaces and materials are visible
2. Current cleanliness/condition assessment
3. Any visible stains, damage, or issues
4. Recommended cleaning service type
5. Estimated scope of work (time, crew, equipment)
6. Any concerns or special considerations

Format as JSON.{customerInfo}

Provide a practical assessment useful for quoting and preparing for the cleaning job.";
    }

    private static string SanitizePromptInput(string? input)
    {
        if (string.IsNullOrEmpty(input)) return "";

        var sanitized = input
            .Replace("\n", " ")
            .Replace("\r", " ")
            .Replace("\t", " ")
            .Replace("```", " ");

        var injectionPatterns = new[]
        {
            "[SYSTEM", "[IGNORE", "[OVERRIDE", "SYSTEM:", "INSTRUCTIONS:",
            "PROMPT:", "ROLE:", "ASSISTANT:", "USER:", "<<", ">>",
            "FORGET ABOVE", "IGNORE ABOVE", "DISREGARD", "NEW INSTRUCTIONS",
            "IGNORE ALL", "OVERRIDE ALL"
        };

        foreach (var pattern in injectionPatterns)
        {
            var idx = sanitized.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            while (idx >= 0)
            {
                sanitized = sanitized.Remove(idx, pattern.Length).Insert(idx, new string(' ', pattern.Length));
                idx = sanitized.IndexOf(pattern, idx + pattern.Length, StringComparison.OrdinalIgnoreCase);
            }
        }

        while (sanitized.Contains("  "))
            sanitized = sanitized.Replace("  ", " ");

        return sanitized.Length > 500 ? sanitized[..500].Trim() + "..." : sanitized.Trim();
    }
}
