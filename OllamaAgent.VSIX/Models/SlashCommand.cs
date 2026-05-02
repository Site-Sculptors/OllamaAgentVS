using System.Collections.Generic;

namespace OllamaAgent.VSIX.Models
{
    public class SlashCommand
    {
        public string Command { get; set; }
        public string Description { get; set; }
        public string SystemInstruction { get; set; }
        public bool RequiresSelection { get; set; }
        public static List<SlashCommand> All => new List<SlashCommand>
        {
            new SlashCommand
            {
                Command = "/explain",
                Description = "Explain the selected code or active file in plain English.",
                SystemInstruction = "Explain the selected code or active file in plain English.",
                RequiresSelection = false
            },
            new SlashCommand
            {
                Command = "/fix",
                Description = "Identify bugs or errors in the selected code and suggest a corrected version.",
                SystemInstruction = "Identify bugs or errors in the selected code and suggest a corrected version.",
                RequiresSelection = false
            },
            new SlashCommand
            {
                Command = "/doc",
                Description = "Generate XML doc comments for the selected method or class.",
                SystemInstruction = "Generate XML doc comments (for C#) for the selected method or class.",
                RequiresSelection = true
            },
            new SlashCommand
            {
                Command = "/tests",
                Description = "Generate unit tests for the selected code using the project's test framework.",
                SystemInstruction = "Generate unit tests for the selected code using the project's test framework.",
                RequiresSelection = true
            }
        };
    }
}
