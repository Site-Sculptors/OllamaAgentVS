# OllamaAgentVS: Semantic Codebase Search & Deep Context Awareness Plan

This plan outlines the steps to add semantic codebase search and deep context awareness to the OllamaAgentVS extension. Each item is a checkbox for tracking progress.

## Goals
- [ ] Enable the agent to search and understand the entire codebase, not just the current file
- [ ] Provide symbol, reference, and semantic search capabilities
- [ ] Integrate codebase Q&A into the chat UI

## Implementation Steps

### 1. Workspace Indexing
- [x] Enumerate all solution/project files
 [x] Parse files for symbols (classes, methods, properties, etc.)
 [x] Build an in-memory index for fast lookup
 [x] Builds after this step

### 2. Symbol & Reference Search
 [x] Implement symbol search (by name/type)
 [x] Implement reference search (find usages)
 [x] Expose APIs for symbol and reference queries
 [x] Builds after this step

### 3. Semantic & Full-Text Search
 [x] Implement full-text search across all files
 [x] Add basic semantic search (e.g., using Roslyn for C#)
 [x] Expose APIs for semantic queries
 [x] Builds after this step

### 4. Agent Context Integration
 [x] Allow agent to retrieve code snippets and summaries from any file
 [x] Provide context window for agent prompts (surrounding code, related symbols)
 [x] Summarize large files or results for LLM input
 [x] Builds after this step

### 5. UI Integration
 [x] Add codebase Q&A option to chat window
 [x] Display search results and context in chat or a dedicated panel
 [x] Allow user to select codebase context for agent queries
 [x] Builds after this step

### 6. Testing & Validation
 [x] Unit tests for indexing and search APIs
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
