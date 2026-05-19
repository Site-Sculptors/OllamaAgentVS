# OllamaAgentVS: Semantic Codebase Search & Deep Context Awareness Plan

This plan outlines the steps to add semantic codebase search and deep context awareness to the OllamaAgentVS extension. Each item is a checkbox for tracking progress.

## Goals
- [ ] Enable the agent to search and understand the entire codebase, not just the current file
- [ ] Provide symbol, reference, and semantic search capabilities
- [ ] Integrate codebase Q&A into the chat UI

## Implementation Steps

### 1. Workspace Indexing
- [ ] Enumerate all solution/project files
- [ ] Parse files for symbols (classes, methods, properties, etc.)
- [ ] Build an in-memory index for fast lookup
- [ ] Builds after this step

### 2. Symbol & Reference Search
- [ ] Implement symbol search (by name/type)
- [ ] Implement reference search (find usages)
- [ ] Expose APIs for symbol and reference queries
- [ ] Builds after this step

### 3. Semantic & Full-Text Search
- [ ] Implement full-text search across all files
- [ ] Add basic semantic search (e.g., using Roslyn for C#)
- [ ] Expose APIs for semantic queries
- [ ] Builds after this step

### 4. Agent Context Integration
- [ ] Allow agent to retrieve code snippets and summaries from any file
- [ ] Provide context window for agent prompts (surrounding code, related symbols)
- [ ] Summarize large files or results for LLM input
- [ ] Builds after this step

### 5. UI Integration
- [ ] Add codebase Q&A option to chat window
- [ ] Display search results and context in chat or a dedicated panel
- [ ] Allow user to select codebase context for agent queries
- [ ] Builds after this step

### 6. Testing & Validation
- [ ] Unit tests for indexing and search APIs
- [ ] Integration tests for agent Q&A
- [ ] User testing for UI/UX
- [ ] Builds after this step

---

**Progress:**
- [ ] Workspace Indexing
- [ ] Symbol & Reference Search
- [ ] Semantic & Full-Text Search
- [ ] Agent Context Integration
- [ ] UI Integration
- [ ] Testing & Validation

---

*Update this file as you complete each step.*
