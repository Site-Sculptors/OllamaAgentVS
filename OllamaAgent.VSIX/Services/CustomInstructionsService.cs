using System;
using System.IO;
using System.Threading.Tasks;

namespace OllamaAgent.VSIX.Services
{
    public class CustomInstructionsService
    {
        private string _instructions;
        private string _instructionsPath;
        public bool IsActive => !string.IsNullOrEmpty(_instructions);
        public string Instructions => _instructions;

        public async Task LoadAsync(string solutionDir)
        {
            if (string.IsNullOrEmpty(solutionDir))
            {
                _instructions = null;
                _instructionsPath = null;
                return;
            }
            var githubDir = Path.Combine(solutionDir, ".github");
            var ollamaPath = Path.Combine(githubDir, "ollama-instructions.md");
            var copilotPath = Path.Combine(githubDir, "copilot-instructions.md");
            if (File.Exists(ollamaPath))
            {
                _instructions = File.ReadAllText(ollamaPath);
                _instructionsPath = ollamaPath;
            }
            else if (File.Exists(copilotPath))
            {
                _instructions = File.ReadAllText(copilotPath);
                _instructionsPath = copilotPath;
            }
            else
            {
                _instructions = null;
                _instructionsPath = null;
            }
        }
    }
}
