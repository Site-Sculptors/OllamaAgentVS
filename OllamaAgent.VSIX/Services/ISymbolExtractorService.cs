namespace OllamaAgent.VSIX.Services
{
    public interface ISymbolExtractorService
    {
        /// <summary>
        /// Extracts a compact symbol summary (classes, methods) from the given source code.
        /// </summary>
        /// <param name="code">The source code to analyze.</param>
        /// <param name="language">The language of the code (e.g., "CSharp").</param>
        /// <returns>A compact symbol summary string.</returns>
        Task<string> ExtractSymbolSummaryAsync(string code, string language);
    }
}
