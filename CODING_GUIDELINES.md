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
