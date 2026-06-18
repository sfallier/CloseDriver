# CloseDriver Coding Guidelines

## No LINQ — Ever

`System.Linq` is **banned** in this project. This is enforced at compile time via `<Using Remove="System.Linq" />` in every `.csproj`.

**Why:** LINQ extension methods generate hidden allocations (enumerator objects, closure captures, intermediate collections) on every call path. In a Bluetooth/serial application that processes high-frequency data callbacks, these allocations cause GC pressure, heap fragmentation, and unpredictable latency spikes — none of which are worth saving a line of code.

**Replace with explicit constructs:**

| LINQ | Replace with |
|---|---|
| `collection.FirstOrDefault(x => ...)` | `for`/`foreach` loop with early `break` |
| `collection.Where(...).Select(...)` | `foreach` loop building results manually |
| `items.Any(x => ...)` | `for` loop returning `bool` |
| `string.Join(",", items.Select(...))` | `StringBuilder` with manual loop |
| `array.Take(n).Reverse().Select(...)` | Direct index arithmetic |
| Query syntax (`from x in y select ...`) | Same — not allowed |

**Note:** `string.Join(separator, array)` without LINQ transforms is fine.

## No `async void` on High-Frequency System Event Callbacks

**Rule:** Never attach an `async void` handler to a system event that fires from a non-Task-aware source (BLE `ValueChanged`, WinRT timers, serial port `DataReceived`, etc.).

**Why this is dangerous:**  
An `async void` handler suspends at its first `await` and immediately returns to its caller — the platform considers the callback complete and is free to fire it again. Any resource shared between invocations (`StreamWriter`, `NetworkStream`, mutable state) is now accessed concurrently, causing `InvalidOperationException`, data corruption, or silent data loss.

**The rule in one sentence:**  
> If an event can fire again before the previous invocation has fully returned, do not use `async`/`await` inside its handler.

**Allowed patterns:**

| Situation | Pattern |
|---|---|
| I/O on every callback | Enqueue into a `Channel<T>`; one consumer `Task` drains it sequentially |
| Marshal to UI thread | `_syncContext.Post(...)` — synchronous enqueue, no `await` in callback |
| One-shot / user-gesture events (button click) | `async void` is acceptable — these cannot re-enter |
| Fire-and-forget with no shared state | `Task.Run(...)`, consciously accepting swallowed exceptions |

**Counter-example that triggered this rule:**

```csharp
// BAD — async void on a BLE notification; second notification fires at the await → crash
private async void OnDataReceived(object sender, byte[] data)
{
    await _writer.WriteLineAsync(...);
}

// GOOD — synchronous enqueue; Channel consumer owns the StreamWriter exclusively
private void OnDataReceived(object sender, byte[] data)
{
    _channel.Writer.TryWrite(FormatLine(data));
}
```
