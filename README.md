# Net11Showcase

Small, runnable demos of new **C# 15** and **.NET 11** features. Every demo was built and run on
**.NET SDK 11.0.100** (Windows 11). All output below was copied from real runs.

```
dotnet --list-sdks   ->  9.0.312, 10.0.201, 11.0.100
```

| Project | What it shows |
|---|---|
| [src/Showcase.Language](src/Showcase.Language) | C# 15: unions, closed hierarchies, collection expression arguments, labeled break/continue |
| [src/Showcase.Runtime](src/Showcase.Runtime) | Runtime async: live stack traces plus a quick time/allocation comparison |
| [src/Showcase.Runtime.Classic](src/Showcase.Runtime.Classic) | Helper: the same async code compiled *without* runtime async, used as the baseline |
| [src/Showcase.Api](src/Showcase.Api) | ASP.NET Core 11: async validation (with localization), native OpenTelemetry tags, OpenAPI 3.2, plus a small EF Core 11 section |
| [tests/Showcase.Tests](tests/Showcase.Tests) | xUnit tests for the language features |

Shared settings live in [Directory.Build.props](Directory.Build.props): `net11.0`, `Nullable`,
`ImplicitUsings`, `LangVersion=default` (C# 15 on net11.0). [global.json](global.json) pins SDK 11.0.100.

```powershell
dotnet build      # 0 warnings, 0 errors
dotnet test       # Passed! - Failed: 0, Passed: 21, Skipped: 0, Total: 21
```

> **Opt-in flags:** none of the C# 15 features shown here needed `LangVersion=preview` or a feature flag.
> Runtime async *does* need an opt-in for user code (see section 5).

---

## 1. Union types (C# 15)

**What it is.** `public union Pet(Cat, Dog, Bird);` declares a type whose value is exactly one of the listed
*case types*. Each case type converts implicitly to the union. A `switch` over a union is checked for
exhaustiveness against the case types, so you don't need a `_` arm. Type patterns such as `Dog d` test the
union's *contents* (its `Value`), not the union itself.

**Where.** [src/Showcase.Language/Unions.cs](src/Showcase.Language/Unions.cs). It contains a `Pet(Cat, Dog, Bird)`
union, a Result-style `UserResult(Success, NotFound, ValidationError)` union, and `IntOrString(int, string)`.
Tests: [tests/Showcase.Tests/UnionTests.cs](tests/Showcase.Tests/UnionTests.cs).

**Run.** `dotnet run --project src/Showcase.Language -- unions`

```
== Union types ==
  Tom the cat says meow
  Rex the labrador fetches
  Fido the Beagle barks
  Polly the bird says hello
  no pet (default union, Value is null)
  FindUser(1) -> 200 OK: Ada
  FindUser(42) -> 404: user 42 not found
  FindUser(-5) -> 400: Id must be positive
  default(Pet) is null: True; Value is null: True
  IntOrString(42).Value runtime type: Int32 (stored as object? -> boxed)
  Pet is a struct with fields: Object <Value>k__BackingField
  switch without a null arm on default(Pet) -> SwitchExpressionException
```

**Storage and boxing (verified on 11.0.100).** I checked the generated type with reflection:

- `Pet` is a **struct** marked `[System.Runtime.CompilerServices.Union]` that implements `IUnion`.
- It has exactly one field, `object <Value>k__BackingField`, and one public constructor per case type.
- **Value-type cases are boxed.** `IntOrString` stores `42` as a boxed `Int32` in that `object` field.
  This matches the docs. To avoid boxing, hand-write a *custom union* (`[Union]` plus
  `HasValue`/`TryGetValue(out T)` members). That isn't shown here.

**The null pattern.**

- `default(Pet)` holds a null `Value`, and `pet is null` is `true` for it.
- `Describe` includes a `null =>` arm, which is the docs' recommendation over `_`. A discard arm would also
  silently accept any case added later.
- Without a null arm, a switch on a `Pet` **parameter** compiles without a warning, because the contents are
  assumed non-null. It then throws `SwitchExpressionException` at runtime for `default(Pet)`.
- The same switch on a **local** that flow analysis knows may be default gives warning CS8655
  ("the pattern 'null' is not covered").

## 2. Closed hierarchies (C# 15)

**What it is.** `public closed record PaymentEvent(...)` means every *direct* subclass must be declared in the
same assembly. The compiler therefore knows the full list, and a switch that handles all of them is
exhaustive without `_`. A closed class is implicitly abstract.

**Where.** [src/Showcase.Language/ClosedHierarchies.cs](src/Showcase.Language/ClosedHierarchies.cs).
Tests: [tests/Showcase.Tests/ClosedHierarchyTests.cs](tests/Showcase.Tests/ClosedHierarchyTests.cs).

**Run.** `dotnet run --project src/Showcase.Language -- closed`

```
== Closed hierarchies ==
  PaymentAuthorized  -> authorized 19.99
  PaymentCaptured    -> captured 19.99
  PaymentRefunded    -> refunded 5.00 (damaged item)
  PaymentFailed      -> failed: card expired
  typeof(PaymentEvent).IsAbstract = True
```

**The compile error, verified.** I tried to derive from `PaymentEvent` in another assembly (a scratch project
referencing Showcase.Language). The attempt is left commented out in the test project:

```
error CS9382: 'PaymentDisputed': cannot use a closed type 'PaymentEvent' from another assembly as a base type.
```

**Closed hierarchy vs union.**

| | Closed hierarchy | Union |
|---|---|---|
| Relationship | Cases *inherit* from the base and share its members | Cases are existing, unrelated types (even `int`, `string`, or types you don't own) |
| Where cases live | Same assembly as the base | Anywhere; listed in the union declaration |
| Runtime shape | Ordinary class instances | A wrapper struct holding `object? Value` |
| Good for | Your own domain model, e.g. event or state types | Ad-hoc "one of these" results and return types |

## 3. Collection expression arguments (C# 15)

**What it is.** `with(...)` as the *first* element of a collection expression passes arguments to the target's
constructor (or to its `CollectionBuilder` method). Typical uses are a capacity or a comparer. It isn't
allowed for arrays or spans.

**Where.** [src/Showcase.Language/CollectionExpressionArguments.cs](src/Showcase.Language/CollectionExpressionArguments.cs).
Tests: [tests/Showcase.Tests/CollectionExpressionArgumentTests.cs](tests/Showcase.Tests/CollectionExpressionArgumentTests.cs).
`InMemoryUserDirectory` in the API also uses it.

```csharp
List<string> names = [with(capacity: values.Length * 2), .. values];
HashSet<string> tags = [with(StringComparer.OrdinalIgnoreCase), .. input];
Dictionary<string, int> ages = [with(StringComparer.OrdinalIgnoreCase)];
```

**Run.** `dotnet run --project src/Showcase.Language -- collections`

```
== Collection expression arguments ==
  List: Count=3, Capacity=6
  HashSet (OrdinalIgnoreCase): Count=2 -> [Hello, World]
  tags.Contains("WORLD") = True
  HashSet (default comparer): Count=3
  Dictionary with comparer: ages["ADA"] = 36
  Squares: [1, 4, 9, 16, 25], Capacity=5
```

## 4. Labeled `break` / `continue` (C# 15)

**What it is.** Label a loop (`outer: for (...)`) and use `break outer;` or `continue outer;` from a nested
loop. This replaces the `bool found` flag variable or a `goto`.

**Where.** [src/Showcase.Language/LabeledBreakContinue.cs](src/Showcase.Language/LabeledBreakContinue.cs). It has
a flag-variable version next to each labeled version: `FindFirstPair` uses `break outer`, and
`RowSumsSkippingNegativeRows` uses `continue rows`.
Tests: [tests/Showcase.Tests/LabeledBreakContinueTests.cs](tests/Showcase.Tests/LabeledBreakContinueTests.cs).
The tests check that both versions return the same results.

**Run.** `dotnet run --project src/Showcase.Language -- labels`

```
== Labeled break / continue ==
  First adjacent pair summing to 21 (flag):  (2, 1)
  First adjacent pair summing to 21 (label): (2, 1)
  Pair summing to 100 (label):               none
  Row sums, skip rows with negatives (flag):  [10, 42]
  Row sums, skip rows with negatives (label): [10, 42]
```

## 5. Runtime async (.NET 11)

**What it is.** With runtime async, the C# compiler no longer generates a state machine class for each
`async` method. It marks the method with `MethodImplAttributes.Async` (`0x2000`), and the runtime handles
suspension and resumption itself.

**Opt-in: what I actually found on SDK 11.0.100.**

I built a one-method probe with clean builds and checked `MethodImplementationFlags` and whether an
`[AsyncStateMachine]` attribute was emitted:

| Project setting | Async flag 0x2000 | State machine emitted |
|---|---|---|
| *(nothing)* | no | yes, so runtime async is **off by default for user code** |
| `<Features>runtime-async=on</Features>` | **yes** | **no** |
| `<UseRuntimeAsync>true</UseRuntimeAsync>` | no | yes, so this has no effect on user code |
| `<UseRuntimeAsync>false</UseRuntimeAsync>` | no | yes |

- Only [Showcase.Runtime.csproj](src/Showcase.Runtime/Showcase.Runtime.csproj) sets
  `<Features>$(Features);runtime-async=on</Features>`.
- `EnablePreviewFeatures` isn't needed.
- ⚠ Passing `-p:Features=...` on the command line doesn't trigger a recompile in an incremental build.
  Do a clean build or use `--no-incremental` when you toggle it.

**Where.**

- [src/Showcase.Runtime/NestedChain.cs](src/Showcase.Runtime/NestedChain.cs) is compiled twice: once into
  Showcase.Runtime with runtime async on, and once into
  [Showcase.Runtime.Classic](src/Showcase.Runtime.Classic/Showcase.Runtime.Classic.csproj) as a linked file
  without it. A `#if` picks the namespace.
- [src/Showcase.Runtime/Program.cs](src/Showcase.Runtime/Program.cs) runs both versions in the same process.

**Run.** `dotnet run --project src/Showcase.Runtime -c Release`

```
== Runtime async ==
Classic.NestedChain uses runtime async: False
RtAsync.NestedChain uses runtime async: True

--- Live stack inside InnerAsync: Classic (compiler state machines) (11 frames)
   at Showcase.Runtime.Classic.NestedChain.InnerAsync()
   at System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start[TStateMachine](TStateMachine& stateMachine)
   at Showcase.Runtime.Classic.NestedChain.InnerAsync()
   at Showcase.Runtime.Classic.NestedChain.MiddleAsync()
   at System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start[TStateMachine](TStateMachine& stateMachine)
   at Showcase.Runtime.Classic.NestedChain.MiddleAsync()
   at Showcase.Runtime.Classic.NestedChain.OuterAsync()
   at System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start[TStateMachine](TStateMachine& stateMachine)
   at Showcase.Runtime.Classic.NestedChain.OuterAsync()
   at Program.<Main>$(String[] args)
   at Program.<Main>(String[] args)

--- Live stack inside InnerAsync: Runtime async (5 frames)
   at Showcase.Runtime.RuntimeAsync.NestedChain.InnerAsync()
   at Showcase.Runtime.RuntimeAsync.NestedChain.MiddleAsync()
   at Showcase.Runtime.RuntimeAsync.NestedChain.OuterAsync()
   at Program.<Main>$(String[] args)
   at Program.<Main>(String[] args)

Chain depth 20. Median of 5 runs each. Allocations = all threads (GC.GetTotalAllocatedBytes).
scenario                               mode                 time     alloc/op
synchronous completion (no suspension) classic            49.7ms        864 B
                                       runtime async      58.1ms         72 B
suspends at bottom (Task.Yield)        classic           106.5ms       2298 B
                                       runtime async      56.7ms       1472 B
```

A second run of the same build gave these timing rows:

```
synchronous completion (no suspension) classic            44.9ms        864 B
                                       runtime async      32.1ms         72 B
suspends at bottom (Task.Yield)        classic           114.1ms       2238 B
                                       runtime async      51.7ms       1472 B
```

**What these numbers do and don't prove.**

- **Stack traces: a solid result.** The live stack drops from 11 frames to 5. The `AsyncMethodBuilderCore.Start`
  frames and the duplicated method frames are gone. This is deterministic and is what profilers, debuggers
  and `new StackTrace()` see. Exception stack traces were already cleaned up before .NET 11, so they look
  about the same either way.
- **Allocations: reasonably solid for this exact code.** They were the same on both runs.
  - In the synchronous path, classic async allocates a `Task<int>` at most levels. Small results come from a
    cache. Runtime async allocated only the final task the caller awaited (72 B per call versus 864 B).
  - When the chain really suspends, both modes allocate, but runtime async allocated about a third less here.
- **Timings: NOT conclusive.**
  - The synchronous case swung from "runtime async 17% slower" to "runtime async 28% faster" between two
    runs.
  - The suspending case was faster with runtime async both times, but this is a Stopwatch loop on a laptop,
    with no pinning, no BenchmarkDotNet, and no statistics beyond a median of five.
  - Treat it as a demo of the mechanics, not as a performance claim. Use BenchmarkDotNet if you need real
    numbers.

## 6. ASP.NET Core 11: async validation (+ localization)

**What it is.**

- .NET 11 adds `AsyncValidationAttribute` and `IAsyncValidatableObject`.
- With `builder.Services.AddValidation()`, minimal APIs run these validators automatically, sync and async
  together. They return a 400 `ValidationProblem` before your handler runs.
- Calling `AddLocalization()` before `AddValidation()` turns on localized messages.

**Where.**

- [src/Showcase.Api/Validation/UserRegistration.cs](src/Showcase.Api/Validation/UserRegistration.cs) contains
  the request record and `UniqueUsernameAttribute : AsyncValidationAttribute`.
- [src/Showcase.Api/Validation/IUserDirectory.cs](src/Showcase.Api/Validation/IUserDirectory.cs) is a fake
  user store with a 20 ms simulated delay. It is case-insensitive and pre-seeded with `admin` and `alice`.
- Messages live in [src/Showcase.Api/Resources/ValidationMessages.resx](src/Showcase.Api/Resources/ValidationMessages.resx)
  and `ValidationMessages.de.resx`, wired up through `options.LocalizerProvider` in
  [Program.cs](src/Showcase.Api/Program.cs).
- The culture comes from `Accept-Language` through `UseRequestLocalization`, with `en` and `de` supported.

**Run.** `dotnet run --project src/Showcase.Api --urls http://localhost:5080`, then use
[src/Showcase.Api/Showcase.Api.http](src/Showcase.Api/Showcase.Api.http) or curl. Real responses:

```
POST /users {"username":"bob","email":"bob@example.com"}
-> 201 {"username":"bob","email":"bob@example.com"}

POST /users {"username":"alice",...}
-> 400 {"title":"One or more validation errors occurred.","status":400,"errors":{"Username":["This Username is already taken. Please choose another one."]}}

POST /users  Accept-Language: de  {"username":"Admin",...}
-> 400 {...,"errors":{"Username":["Dieser Wert für Username ist bereits vergeben. Bitte wählen Sie einen anderen."]}}

POST /users  Accept-Language: de  {"username":"ab","email":"not-an-email"}
-> 400 {...,"errors":{"Username":["Das Feld Username muss zwischen 3 und 20 Zeichen lang sein."],"Email":["Das Feld Email enthält keine gültige E-Mail-Adresse."]}}

POST /users {"username":"ab"}
-> 400 {...,"errors":{"Username":["The field Username must be a string with a minimum length of 3 and a maximum length of 20."],"Email":["The Email field is required."]}}
```

**Localization notes from what actually worked.**

- **Custom messages.** An explicit `ErrorMessage` (here `"UsernameTaken"`) is used as the resource key.
- **Built-in attributes.** These are looked up by `{AttributeType}_Error`. The keys
  `RequiredAttribute_Error`, `StringLengthAttribute_Error` and `EmailAddressAttribute_Error` all worked
  with the full attribute class name.
- **Placeholders.** `{0}` in the message is the member's display name (`Username`), not the submitted value.
  My first resource string used `'{0}'` expecting the value and printed `'Username'`.
- **Missing keys.** A key that is missing from the resx falls back to the default English text. That
  happened for `StringLength` until I added the German entry.

## 7. ASP.NET Core 11: native OpenTelemetry HTTP server tags

**What it is.** ASP.NET Core 11 puts the OpenTelemetry semantic-convention tags directly on its HTTP server
`Activity` (source `Microsoft.AspNetCore`). You no longer need `OpenTelemetry.Instrumentation.AspNetCore` for
them.

**Where.** [src/Showcase.Api/Telemetry/SpanRecorder.cs](src/Showcase.Api/Telemetry/SpanRecorder.cs). It is a plain
`System.Diagnostics.ActivityListener`, and the project references **no OpenTelemetry packages at all**.
Each finished span is printed to the console, and the last 20 are served at `GET /debug/last-spans`.

**Real console output for one request:**

```
[span] POST /users (Server, 61.5 ms)
         client.address = ::1
         network.peer.address = ::1
         network.peer.port = 64314
         server.address = localhost
         server.port = 5080
         http.request.method = POST
         user_agent.original = curl/8.16.0
         url.scheme = http
         url.path = /users
         http.response.status_code = 201
         network.protocol.version = 1.1
         http.route = /users
```

`http.request.method`, `http.route` and `http.response.status_code` are all present. The display name already
follows the `{method} {route}` convention, for example `GET /users/{name}`.

## 8. ASP.NET Core 11: OpenAPI 3.2

**Where.** `builder.Services.AddOpenApi()` and `app.MapOpenApi()` in [Program.cs](src/Showcase.Api/Program.cs),
using the `Microsoft.AspNetCore.OpenApi` 11.0.0 package.

**Run.** `GET http://localhost:5080/openapi/v1.json`

```json
{
  "openapi": "3.2.0",
  "info": { "title": "Showcase.Api | v1", "version": "1.0.0" },
  ...
```

With no options set, the document reports **`"openapi": "3.2.0"`**. To pin 3.1, set
`options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1`.

## 9. EF Core 11 on SQLite (optional section)

**Where.**

- [src/Showcase.Api/Data/ShopContext.cs](src/Showcase.Api/Data/ShopContext.cs) defines Customers and Orders.
  Orders can have no customer ("guest" orders).
- [src/Showcase.Api/Data/EfEndpoints.cs](src/Showcase.Api/Data/EfEndpoints.cs) maps `GET /ef/report`.
- [src/Showcase.Api/Migrations](src/Showcase.Api/Migrations) holds the migration.
- SQL is logged through the normal logger: `"Microsoft.EntityFrameworkCore.Database.Command": "Information"`
  in [appsettings.json](src/Showcase.Api/appsettings.json).

**One-step migration with `--add`.** The `dotnet-ef` 11.0.0 local tool is in [dotnet-tools.json](dotnet-tools.json):

```powershell
dotnet tool restore
dotnet ef database update InitialCreate --add --project src/Showcase.Api
```

Real output, trimmed:

```
Creating and applying migration 'InitialCreate'.
...
Applying migration '20261118093643_InitialCreate'.
      CREATE TABLE "Customers" ( ... );
      CREATE TABLE "Orders" ( ... );
...
Migration '20261118093643_InitialCreate' was successfully created and applied.
```

The migration files were written to `src/Showcase.Api/Migrations` as usual. The endpoint also calls
`Database.MigrateAsync()`, so a fresh clone works without running the CLI.

**Run.** `GET http://localhost:5080/ef/report`

```json
{"fullJoin":[{"customer":"Ada","orderId":2},{"customer":"Ada","orderId":3},{"customer":"Grace","orderId":4},
             {"customer":"Linus","orderId":null},{"customer":null,"orderId":1}],
 "biggestOrder":{"id":3,"total":99.5},"customerWithMostOrders":"Ada","customerWithFewestOrders":"Linus"}
```

Generated SQL, copied from the console log:

```sql
-- FullJoin
SELECT "c"."Name" AS "Customer", "o"."Id" AS "OrderId"
FROM "Customers" AS "c"
FULL JOIN "Orders" AS "o" ON "c"."Id" = "o"."CustomerId"

-- MaxByAsync(o => o.Total)
SELECT "o"."Id", "o"."CustomerId", "o"."Total"
FROM "Orders" AS "o"
ORDER BY "o"."Total" DESC
LIMIT 1

-- MaxByAsync(c => c.Orders.Count())   (MinByAsync is identical without DESC)
SELECT "c"."Id", "c"."Name"
FROM "Customers" AS "c"
ORDER BY (
    SELECT COUNT(*)
    FROM "Orders" AS "o"
    WHERE "c"."Id" = "o"."CustomerId") DESC
LIMIT 1
```

**SQLite notes.**

- `FULL JOIN` worked. SQLite has supported it since 3.39, and the bundled `e_sqlite3` is newer than that.
- I stored `decimal Total` as `REAL` with `HasConversion<double>()` **up front**. That is the usual SQLite advice,
  because SQLite has no decimal type and EF Core's SQLite provider has historically refused to `ORDER BY` decimals.
  I did not test `MaxBy` on a plain decimal column without it.
- I cast the key to `(int?)c.Id` so both key selectors have the same type as the nullable `CustomerId`. I did
  not try the version without the cast.

---

## Where the docs and SDK 11.0.100 disagreed (or were unclear)

- **Runtime async opt-in.**
  - The runtime "what's new" page says to *enable* it with `<Features>runtime-async=on</Features>`. A note on
    the same page says to *opt out* with `<UseRuntimeAsync>false</UseRuntimeAsync>`, which implies it is on
    by default.
  - On 11.0.100 it is **off** by default for user code, and `UseRuntimeAsync=true` did not turn it on. Only
    `Features=runtime-async=on` did.
- **OpenAPI default version.** One part of the ASP.NET Core 11 release notes describes 3.2 as something you
  opt into with `OpenApiVersion = OpenApi3_2`. Another says 3.2 is the default. The SDK produced **3.2.0
  by default**.
- **Native OTel tag list.** The release notes I read listed `http.request.method`, `url.path`,
  `http.response.status_code` and `server.address` "such as" examples, and didn't mention `http.route`. In
  practice `http.route` **is** emitted.
- **Status labels.**
  - The EF Core 11 "what's new" page still says "currently in development", but the 11.0.0 packages worked
    as documented.
  - The ASP.NET Core 11 release notes still have some preview wording, for example "Preview 5/6" in the
    validation section.
- **Not a doc issue, but a tooling quirk.** `dotnet sln add src/Showcase.Runtime/...` failed with "Solution
  folder 'src' already contains a project with the filename 'Showcase.Runtime.Classic.csproj'". It seems to
  try to add the referenced project a second time, and it rolls back the whole command. I wrote
  [Net11Showcase.slnx](Net11Showcase.slnx) by hand.

Everything else (union syntax and behavior, `closed`, `with(...)`, labeled jumps, `AsyncValidationAttribute`,
validation localization, `FullJoin`, `MaxBy`/`MinBy`, `database update --add`) compiled and behaved as
documented, with no preview flags.
