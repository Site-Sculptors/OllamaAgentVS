# OllamaAgent.VSIX — Chat Thread Persistence

**Project:** OllamaAgent.VSIX — a Visual Studio extension using Ollama as a chat agent instead of Copilot.

**Task:** Implement chat thread persistence.

---

## Existing Models — Do Not Change

```csharp
public class ChatThread
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string SolutionPath { get; set; }
    public string ModelName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public bool IsAutoNamed { get; set; } = true;
    public ObservableCollection<ChatMessage> Messages { get; set; } = new();
}

public class ChatMessage
{
    public ChatRole Role { get; set; }  // enum — User, Assistant, or similar
    public string Message { get; set; } // property is Message not Content
    public string Display => $"{Role}: {Message}";
}
```

---

## Storage

- Default path: `%AppData%\OllamaAgent\chats\`
- User-overridable via `Properties.Settings.Default.ChatsDirectory` (to be added to settings)
- If `ChatsDirectory` setting is blank, fall back to the default path
- One JSON file per thread: `{thread.Id}.json`
- Use `System.Text.Json` for serialization
- `ObservableCollection<ChatMessage>` must be converted to/from `List<ChatMessage>` for the DTO — do not serialize `ObservableCollection` directly
- `ChatRole` is an enum — use `JsonStringEnumConverter` so files store `"User"`/`"Assistant"` not `0`/`1`
- `Display` is a computed property — mark it `[JsonIgnore]` or exclude it from the DTO

---

## Create IChatThreadStore and ChatThreadStore

```csharp
public interface IChatThreadStore
{
    Task<List<ChatThread>> LoadThreadsForSolutionAsync(string solutionPath);
    Task<List<ChatThread>> LoadGlobalThreadsAsync();
    Task SaveThreadAsync(ChatThread thread);
    Task DeleteThreadAsync(string threadId);
    string GetStorageDirectory();
}
```

**ChatThreadStore implementation notes:**
- `GetStorageDirectory()` reads `Properties.Settings.Default.ChatsDirectory` at call time (not cached at construction) so path changes in Options take effect immediately without restart
- `LoadThreadsForSolutionAsync(solutionPath)` loads all JSON files from the directory, deserializes them, and returns only those where `thread.SolutionPath == solutionPath`
- `LoadGlobalThreadsAsync()` returns threads where `SolutionPath` is null or empty
- `SaveThreadAsync` serializes the thread to `{storageDir}\{thread.Id}.json`, creating the directory if it doesn't exist
- `DeleteThreadAsync` deletes the file `{storageDir}\{threadId}.json` if it exists
- Wrap all file I/O in try/catch and log failures via `System.Diagnostics.Debug.WriteLine`

---

## Register in OllamaAgentVSIXPackage.InitializeAsync

Register alongside the existing services (`IOllamaAgentService`, `IOllamaModelService`, `IOllamaChatService`, `IModelStore`, `ChatViewModel`, `OllamaOptionsViewModel`):

```csharp
this.AddService(typeof(IChatThreadStore), (container, ct, serviceType) =>
{
    return Task.FromResult<object>(new ChatThreadStore());
}, promote: true);
```

---

## Auto-Naming

- **Trigger:** after the first assistant response is received on a thread where `IsAutoNamed == true` and `Name` is null/empty
- Fire a silent background call to Ollama using the same endpoint (`OllamaEndpoint`) and model (`thread.ModelName`)
- **Prompt:**
```
Summarize this conversation in 5 words or fewer as a chat title. Reply with only the title, no punctuation, no quotes.
User: {first user message}
Assistant: {first assistant response}
```
- On success: set `thread.Name` to the trimmed result, keep `IsAutoNamed = true`, call `SaveThreadAsync`
- On failure or empty result: fall back to the first 40 characters of the user's first message
- Once a user manually renames a thread, set `IsAutoNamed = false` — never auto-rename that thread again

---

## Solution Association

```csharp
var dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
var solutionPath = dte?.Solution?.FullName; // null if no solution open
```

- Assign to `thread.SolutionPath` at creation time
- On chat window load, call `LoadThreadsForSolutionAsync(solutionPath)` if a solution is open, otherwise `LoadGlobalThreadsAsync()`
- Subscribe to solution open/close events via `IVsSolutionEvents` and reload the thread list when the active solution changes

---

## ChatViewModel Changes

- Inject `IChatThreadStore` via constructor (registered as a singleton in the package)
- Add `ObservableCollection<ChatThread> Threads` property
- Add `ChatThread ActiveThread` property — the currently selected/active thread
- On init, load threads for the current solution and populate `Threads`
- **New thread:** create a `ChatThread` with current `SolutionPath` and `SelectedModel.Name`, add to `Threads`, call `SaveThreadAsync` immediately (saves an empty shell so it persists even if no messages are sent)
- **On message added:** update `ActiveThread.LastActivityAt = DateTime.UtcNow`, call `SaveThreadAsync`
- **On thread switch:** set `ActiveThread`, load its `Messages` into the chat display
- Sort `Threads` by `LastActivityAt` descending

---

## Options Page — Add ChatsDirectory

In `ViewModelBase`, add this property following the exact same pattern as the existing `ModelsDirectory` property:

```csharp
private string _chatsDirectory;
public string ChatsDirectory
{
    get => _chatsDirectory;
    set
    {
        if (_chatsDirectory != value)
        {
            _chatsDirectory = value;
            OnPropertyChanged();
            Settings.Default.ChatsDirectory = value;
            Settings.Default.Save();
        }
    }
}
```

- Load it in `LoadSettings()` the same way `ModelsDirectory` is loaded
- Add `SelectChatDirectoryCommand` following the same pattern as the existing `SelectModelsDirectoryCommand` — opens a `FolderBrowserDialog`, sets `ChatsDirectory`
- Add `ChatsDirectory` to `Properties.Settings.Default` (User scope, string, default empty)

In `OllamaOptionsControl.xaml`, add this block after the Models Directory section and before the Model combobox:

```xml
<TextBlock
    Margin="0,8,0,0"
    VerticalAlignment="Center"
    Text="Chats Directory:" />
<StackPanel VerticalAlignment="Center" Orientation="Horizontal">
    <TextBox
        Width="Auto"
        MinWidth="200"
        VerticalAlignment="Center"
        IsReadOnly="True"
        Text="{Binding ChatsDirectory}" />
    <Button
        Margin="8,0,0,0"
        VerticalAlignment="Center"
        Command="{Binding SelectChatDirectoryCommand}"
        Content="Browse..."
        IsEnabled="{Binding AgentEnabled}" />
</StackPanel>
```

---

## Architecture Constraints — Read Carefully

- `OllamaAgentVSIXPackage` is an `AsyncPackage` — all services are registered in `InitializeAsync` via `AddService`
- All UI updates must marshal to the main thread via `await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()`
- Use `async/await` throughout — no blocking `.Result` or `.Wait()` calls
- `ChatRole` is an enum — always use `JsonStringEnumConverter` when serializing
- `OllamaAgentOptionsPage` does not need changes — it resolves the ViewModel lazily and calls `LoadSettings()` on activate, which will automatically pick up `ChatsDirectory`
- `ViewModelBase` is the base for both `ChatViewModel` and `OllamaOptionsViewModel` — put shared properties (`ChatsDirectory`, `SelectChatDirectoryCommand`) in `ViewModelBase`, not in the subclasses
