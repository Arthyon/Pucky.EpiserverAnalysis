# Pucky.EpiserverAnalysis

Roslyn-analyzer to remind me to add CultureSpecific-attribute to Episerver-properties.


Will match properties that:
- Is virtual
- Is a string
- Has Display-attribute
- Does not have CultureSpecific-attribute
- The Name suggests a text-field (whitelist of various namings for textfields in `DiagnosticAnalyzer`)
