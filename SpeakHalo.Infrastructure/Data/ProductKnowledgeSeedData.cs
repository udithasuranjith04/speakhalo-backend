using SpeakHalo.Domain.Entities;

namespace SpeakHalo.Infrastructure.Data;

/// <summary>
/// Comprehensive SpeakHalo product knowledge for AI training
/// This data trains the AI assistant to answer questions about features, pricing, and workflows
/// </summary>
public static class ProductKnowledgeSeedData
{
    public static List<ProductKnowledge> GetSeedData()
    {
        return new List<ProductKnowledge>
        {
            // ===== CORE FEATURES =====
            new ProductKnowledge
            {
                Id = Guid.NewGuid().ToString(),
                Topic = "Core Features",
                Category = "Features",
                Question = "What is SpeakHalo?",
                Answer = "SpeakHalo is an AI-powered platform that helps cleaning businesses automate customer communications 24/7. It includes AI voice receptionists, WhatsApp agents, photo analysis for stain identification and quoting, smart scheduling, and team dispatch tools.",
                ShortAnswer = "AI platform for cleaning business automation with voice, WhatsApp, and photo analysis.",
                Keywords = new List<string> { "speakhalo", "ai", "automation", "cleaning", "platform" },
                Importance = 100,
                IsCommonQuestion = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },

            new ProductKnowledge
            {
                Id = Guid.NewGuid().ToString(),
                Topic = "Voice AI",
                Category = "Features",
                Question = "How does the AI voice receptionist work?",
                Answer = "The AI voice receptionist answers incoming calls automatically, understands natural speech, books appointments, provides service quotes, and transfers calls when needed. It works 24/7 and can handle multiple calls simultaneously. The AI uses your business information, pricing, and availability to provide accurate responses.",
                ShortAnswer = "Answers calls 24/7, books appointments, and provides quotes automatically.",
                Keywords = new List<string> { "voice", "calls", "receptionist", "phone", "answering" },
                AlternativeQuestions = new List<string>
                {
                    "Can SpeakHalo answer my phone calls?",
                    "How does call handling work?",
                    "What is the AI receptionist?"
                },
                Importance = 95,
                IsCommonQuestion = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },

            new ProductKnowledge
            {
                Id = Guid.NewGuid().ToString(),
                Topic = "WhatsApp AI",
                Category = "Features",
                Question = "How does WhatsApp automation work?",
                Answer = "The WhatsApp AI agent automatically responds to customer messages, analyzes photos they send, generates instant quotes from images, and can schedule appointments. It maintains conversation context and escalates to humans when needed. Customers can send photos of their issues and receive detailed quotes within minutes.",
                ShortAnswer = "Automated WhatsApp responses, photo analysis, and instant quotes from customer images.",
                Keywords = new List<string> { "whatsapp", "messaging", "photos", "chat", "instant" },
                AlternativeQuestions = new List<string>
                {
                    "Can it handle WhatsApp messages?",
                    "Does it work with WhatsApp?",
                    "How do I get WhatsApp automation?"
                },
                Importance = 90,
                IsCommonQuestion = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },

            new ProductKnowledge
            {
                Id = Guid.NewGuid().ToString(),
                Topic = "Photo-to-Quote",
                Category = "Features",
                Question = "How does photo-to-quote work?",
                Answer = "Customers send photos of stains, surfaces, or spaces via WhatsApp. Our Gemini Vision AI analyzes the images to identify stain types, surface materials, damage severity, and estimate cleaning scope. The system provides confidence scores: above 80% gets an automatic quote, 50-80% gets flagged for review, below 50% requests more photos. It covers residential, commercial, and specialized cleaning.",
                ShortAnswer = "AI analyzes customer photos to automatically generate detailed service quotes.",
                Keywords = new List<string> { "photo", "quote", "vision", "analysis", "instant", "gemini" },
                Workflow = "1. Customer sends photo via WhatsApp\n2. AI analyzes image (equipment, issue, severity)\n3. System generates quote with parts and labor\n4. Quote sent to customer (or flagged for review if confidence is low)",
                Importance = 95,
                IsCommonQuestion = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },

            // ===== PRICING =====
            new ProductKnowledge
            {
                Id = Guid.NewGuid().ToString(),
                Topic = "Pricing",
                Category = "Pricing",
                Question = "How much does SpeakHalo cost?",
                Answer = "SpeakHalo pricing is customized for each business. Your administrator sets your account limits including AI minutes, WhatsApp messages, SMS, and photo quotes. Contact your administrator or hello@speakhalo.com for pricing details.",
                ShortAnswer = "Pricing is customized per business. Contact your administrator for details.",
                Keywords = new List<string> { "pricing", "cost", "plans", "subscription", "fees" },
                Importance = 100,
                IsCommonQuestion = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },

            // ===== SETUP & ONBOARDING =====
            new ProductKnowledge
            {
                Id = Guid.NewGuid().ToString(),
                Topic = "Getting Started",
                Category = "Setup",
                Question = "How do I get started with SpeakHalo?",
                Answer = "Getting started is simple:\n\n1. **Account Setup**: Your administrator creates your account and configures your AI agent\n2. **Login**: Check your email for a login link — enter your email and use the code sent to you\n3. **Review Settings**: Check your business profile, AI agent greeting, and phone number\n4. **Test**: Make test calls and send test messages\n5. **Go Live**: Your AI receptionist starts answering calls automatically\n\nYour administrator handles onboarding and configuration for you.",
                ShortAnswer = "Your admin creates your account, you log in via email code, review settings, test, and go live.",
                Keywords = new List<string> { "setup", "onboarding", "getting started", "begin", "start" },
                Workflow = "Sign Up → Profile Setup → Phone Setup → WhatsApp Connect → Testing → Go Live",
                Importance = 90,
                IsCommonQuestion = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },

            // ===== INDUSTRIES =====
            new ProductKnowledge
            {
                Id = Guid.NewGuid().ToString(),
                Topic = "Supported Services",
                Category = "Features",
                Question = "What cleaning services does SpeakHalo support?",
                Answer = "SpeakHalo specializes in cleaning businesses:\n\n- **Residential Cleaning**: Regular house cleaning, deep cleaning, spring cleaning\n- **Move-In/Move-Out**: Turnover cleans for tenants and property managers\n- **Commercial Cleaning**: Office, retail, medical, restaurant, gym cleaning\n- **Carpet & Upholstery**: Steam cleaning, stain removal, fabric care\n- **Window & Pressure Washing**: Interior/exterior windows, driveways, decks, siding\n- **Specialized**: Post-construction cleanup, tile & grout, hoarding, biohazard\n\nThe AI is trained with deep cleaning industry expertise including stain removal, surface care, chemical safety, and scheduling.",
                ShortAnswer = "Residential, commercial, and specialized cleaning services.",
                Keywords = new List<string> { "services", "cleaning", "residential", "commercial", "specialized" },
                Importance = 80,
                IsCommonQuestion = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },

            // ===== TECHNICAL =====
            new ProductKnowledge
            {
                Id = Guid.NewGuid().ToString(),
                Topic = "AI Accuracy",
                Category = "Technical",
                Question = "How accurate is the AI?",
                Answer = "Our AI achieves high accuracy through:\n\n- **Gemini Vision**: Latest AI model for image analysis\n- **Industry-Specific Training**: Specialized knowledge for each trade\n- **Confidence Scoring**: 0-100% confidence on all responses\n- **Human Review**: Low-confidence items flagged for review\n- **Continuous Learning**: System improves from every interaction\n\nFor photo analysis, we achieve 80%+ confidence on most images. Voice AI handles 90%+ of calls successfully without human intervention.",
                ShortAnswer = "90%+ accuracy for voice, 80%+ for photo analysis, with confidence scoring and human backup.",
                Keywords = new List<string> { "accuracy", "reliability", "confidence", "ai", "quality" },
                Importance = 75,
                IsCommonQuestion = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },

            new ProductKnowledge
            {
                Id = Guid.NewGuid().ToString(),
                Topic = "Integration",
                Category = "Technical",
                Question = "Does SpeakHalo integrate with other tools?",
                Answer = "Yes! SpeakHalo integrates with:\n\n- **Calendar**: Google Calendar, Outlook for scheduling\n- **CRM**: Basic customer management included, Salesforce/HubSpot via API\n- **Accounting**: QuickBooks, Xero for invoicing (Business)\n- **Dispatch**: ServiceTitan, Jobber, Housecall Pro (Business)\n- **API**: Full REST API for custom integrations\n\nWebhooks available for real-time notifications. Zapier integration coming soon.",
                ShortAnswer = "Integrates with Google Calendar, CRM systems, accounting software, and dispatch tools via API.",
                Keywords = new List<string> { "integration", "api", "calendar", "crm", "webhook", "zapier" },
                Importance = 70,
                IsCommonQuestion = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },

            // ===== TROUBLESHOOTING =====
            new ProductKnowledge
            {
                Id = Guid.NewGuid().ToString(),
                Topic = "Troubleshooting",
                Category = "Support",
                Question = "What if the AI makes a mistake?",
                Answer = "We have multiple safeguards:\n\n1. **Confidence Thresholds**: Low-confidence responses go to review\n2. **Human Escalation**: AI transfers to humans when unsure\n3. **Review Dashboard**: Monitor all AI interactions\n4. **Training Feedback**: Mark incorrect responses to improve AI\n5. **Override Capability**: Manual correction for all quotes and appointments\n\nYou have full control and can override any AI decision. All interactions are logged for quality assurance.",
                ShortAnswer = "AI flags uncertain responses, escalates to humans, and you can override everything.",
                Keywords = new List<string> { "error", "mistake", "wrong", "problem", "fix" },
                Importance = 85,
                IsCommonQuestion = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },

            new ProductKnowledge
            {
                Id = Guid.NewGuid().ToString(),
                Topic = "Support",
                Category = "Support",
                Question = "How do I get support?",
                Answer = "Multiple support channels:\n\n- **In-App Chat**: Live chat in dashboard (all plans)\n- **Email**: support@speakhalo.com (24-hour response)\n- **Phone**: Priority phone support (Premium plan)\n- **Documentation**: Comprehensive help center\n- **Training**: Video tutorials and webinars\n\nPremium plan customers get priority support.",
                ShortAnswer = "Live chat, email support, phone support (higher plans), and comprehensive documentation.",
                Keywords = new List<string> { "support", "help", "assistance", "contact", "customer service" },
                Importance = 90,
                IsCommonQuestion = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };
    }
}
