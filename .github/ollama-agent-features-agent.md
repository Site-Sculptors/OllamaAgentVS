# OllamaAgent VSIX — Feature Implementation Checklist

Ordered from easiest/highest-impact to hardest. Give this file to Copilot one section at a time.

---

## Phase 1 — Context Basics (Low effort, massive UX improvement)

- [x] **Inject active file into every message**
- Get the active document via `DTE.ActiveDocument` or `ITextEditorFactoryService`
- Prepend file name, language, and full contents to the system prompt on every request
- If file is too large (>500 lines), include only the visible portion

- [x] **Inject current selection into every message**
- If the user has text selected in the editor, include it as a highlighted block in the prompt
- Label it clearly: `// Selected code:` so the model knows it's the focus

- [x] **Slash command: `/explain`**
- Detect if user message starts with `/explain`
- Prepend system instruction: explain the selected code or active file in plain English
- Falls back to active file if nothing is selected

- [x] **Slash command: `/fix`**
- Detect `/fix` prefix
- Prepend system instruction: identify bugs or errors in the selected code and suggest a corrected version
- Inject selection or active file as context

- [x] **Slash command: `/doc`**
- Detect `/doc` prefix
- Prepend system instruction: generate XML doc comments (for C#) for the selected method or class
- Inject selection as context

- [x] **Slash command: `/tests`**
- Detect `/tests` prefix
- Prepend system instruction: generate unit tests for the selected code using the project's test framework
- Inject selection as context

- [x] **Slash command autocomplete popup**
- When user types `/` in the chat input, show a popup list of available slash commands
- Keyboard-navigable, pressing Enter or Tab completes the command

---

## Phase 2 — Explicit Context Attachment (Medium effort, Copilot parity)

- [x] **`#filename` token parsing**
- Detect `#word` tokens in the user's message before sending
- Search open documents and solution files for a matching filename
- Attach the matched file's contents to the prompt
- Show the resolved filename as a tag/chip in the chat UI so the user sees what was attached

- [x] **Paperclip / attach file button in chat UI**
- Add a button next to the send button
- Opens a file picker scoped to the current solution
- Attaches chosen file's contents to the next message

- [x] **`ollama-instructions.md` custom instructions**
- On solution open, check for `.github/ollama-instructions.md` or `.github/copilot-instructions.md` at the solution root
- If found, silently prepend its contents to every system prompt
- Show a small indicator in the chat window that custom instructions are active

- [x] **Streaming responses**
  - Switch from awaiting the full Ollama response to consuming the NDJSON stream from `/api/chat`
  - Append tokens to the chat message as they arrive
  - Show a blinking cursor or typing indicator while streaming
  - Add a Stop button that aborts the HTTP request mid-stream

- [x] **"Ask Ollama" right-click context menu**
  - Register a command in the editor context menu: `Ask Ollama about this`
  - Opens the chat window (if not already open) and pre-fills it with the selection and a `/explain` prompt

---

## Phase 3 — Solution Awareness (Medium-high effort)

- [x] **`@solution` token — solution file tree**
  - Detect `@solution` in the user's message
  - Walk the solution hierarchy via DTE/IVsSolution and build a compact file tree string
  - Include project names, folder structure, and file names (not contents)
  - Append the tree to the prompt so the model knows the project layout

- [x] **`@solution` with file contents (selective)**
  - When `@solution` is used, also attach contents of small/relevant files (e.g. `.csproj`, `Program.cs`, interfaces)
  - Cap total context size to avoid overwhelming the model's context window
  - Prioritize files related to keywords in the user's message

- [x] **Active file symbol list**
  - Use Roslyn or the VS language service to extract class/method names from the active file
  - Append a compact symbol summary to the system prompt (e.g. `// Classes: ChatViewModel, ViewModelBase // Methods: LoadAsync, SendMessage`)
  - Helps the model understand structure without sending the entire file

- [x] **Error list context (`/fix` enhancement)**
  - When `/fix` is used with no selection, check the VS Error List for errors in the active file
  - Attach the error messages and line numbers as additional context
  - Mirrors Copilot's "Fix using Copilot" smart action behavior

- [x] **Output window context (`#output` token)**
  - Detect `#output` in the user's message
  - Capture the current content of the Build or Debug output pane
  - Attach it to the prompt — useful for "why did my build fail?" queries

---

## Phase 4 — RAG / Semantic Search (High effort)

- [ ] **Embedding index of the solution**
  - Use Ollama's embedding endpoint (`/api/embeddings`) with a model like `nomic-embed-text`
  - Walk all `.cs` files in the solution and generate embeddings per file (or per class/method chunk)
  - Store vectors locally (SQLite with a float array column, or a simple binary file)
  - Rebuild index on solution open and on file save

- [ ] **Semantic retrieval on each message**
  - Embed the user's message using the same embedding model
  - Compute cosine similarity against the stored index
  - Retrieve the top 3–5 most relevant chunks
  - Inject them into the prompt as `// Relevant code from solution:`

- [ ] **Incremental index updates**
  - Subscribe to VS file-save events
  - Re-embed only the changed file rather than rebuilding the whole index
  - Show an "Indexing…" status indicator in the chat window

- [ ] **Index management UI in Options page**
  - Add a section to `OllamaAgentOptionsPage` for embedding settings
  - Select which embedding model to use (dropdown populated from `ModelStore`)
  - Button to manually trigger a full re-index
  - Show index status: file count, last indexed time, index size on disk

- [ ] **Streaming responses**
  - Switch from awaiting the full Ollama response to consuming the NDJSON stream from `/api/chat`
  - Append tokens to the chat message as they arrive
  - Show a blinking cursor or typing indicator while streaming
  - Add a Stop button that aborts the HTTP request mid-stream

- [ ] **"Ask Ollama" right-click context menu**
  - Register a command in the editor context menu: `Ask Ollama about this`
  - Opens the chat window (if not already open) and pre-fills it with the selection and a `/explain` prompt

- [ ] **`@solution` token — solution file tree**
  - Detect `@solution` in the user's message
  - Walk the solution hierarchy via DTE/IVsSolution and build a compact file tree string
  - Include project names, folder structure, and file names (not contents)
  - Append the tree to the prompt so the model knows the project layout

- [ ] **`@solution` with file contents (selective)**
  - When `@solution` is used, also attach contents of small/relevant files (e.g. `.csproj`, `Program.cs`, interfaces)
  - Cap total context size to avoid overwhelming the model's context window
  - Prioritize files related to keywords in the user's message

- [ ] **Active file symbol list**
  - Use Roslyn or the VS language service to extract class/method names from the active file
  - Append a compact symbol summary to the system prompt (e.g. `// Classes: ChatViewModel, ViewModelBase // Methods: LoadAsync, SendMessage`)
  - Helps the model understand structure without sending the entire file

- [ ] **Error list context (`/fix` enhancement)**
  - When `/fix` is used with no selection, check the VS Error List for errors in the active file
  - Attach the error messages and line numbers as additional context
  - Mirrors Copilot's "Fix using Copilot" smart action behavior

- [ ] **Output window context (`#output` token)**
  - Detect `#output` in the user's message
  - Capture the current content of the Build or Debug output pane
  - Attach it to the prompt — useful for "why did my build fail?" queries

- [ ] **RAG/semantic search (embeddings, retrieval, context injection)**
  - Use Ollama's embedding endpoint (`/api/embeddings`) with a model like `nomic-embed-text`
  - Walk all `.cs` files in the solution and generate embeddings per file (or per class/method chunk)
  - Store vectors locally (SQLite with a float array column, or a simple binary file)
  - Rebuild index on solution open and on file save

- [ ] **Index management UI**
  - Add a section to `OllamaAgentOptionsPage` for embedding settings
  - Select which embedding model to use (dropdown populated from `ModelStore`)
  - Button to manually trigger a full re-index
  - Show index status: file count, last indexed time, index size on disk
