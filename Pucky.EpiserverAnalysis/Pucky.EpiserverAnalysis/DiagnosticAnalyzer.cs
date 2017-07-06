using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Pucky.EpiserverAnalysis
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PuckyEpiserverAnalysisAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "PuckyEpiserverAnalysis";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Property);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var property = (IPropertySymbol)context.Symbol;
            if (!property.IsVirtual)
                return;

            var type = property.Type;
            if (type.SpecialType != SpecialType.System_String)
                return;

            var attributes = property.GetAttributes();
            if (attributes.Length < 1)
                return;

            if (!HasDisplayAttribute(attributes))
                return;

            if (HasCultureSpecificAttribute(attributes))
                return;

            if (!NameSuggestsATextField(property))
                return;
           
            var diagnostic = Diagnostic.Create(Rule, property.Locations[0], property.Name);

            context.ReportDiagnostic(diagnostic);
        }

        static bool HasDisplayAttribute(ImmutableArray<AttributeData> attributes)
            => attributes.Any(i => i.AttributeClass.ToString() == "System.ComponentModel.DataAnnotations.DisplayAttribute");

        static bool HasCultureSpecificAttribute(ImmutableArray<AttributeData> attributes)
            => attributes.Any(i => i.AttributeClass.ToString() == "EPiServer.DataAnnotations.CultureSpecificAttribute");

        static bool NameSuggestsATextField(IPropertySymbol property)
        {
            var name = property.Name.ToLowerInvariant();
            foreach (var nameFragment in PossibleTextFieldNameFragments)
                if (name.Contains(nameFragment))
                    return true;

            return false;
        }

        static string[] PossibleTextFieldNameFragments =>
            new[] {
                "text",
                "description",
                "header",
                "heading",
                "title",
                "summary",
                "body",
                "headline",
                "label"
            };
    }
}
