// C# 15 union types.
//
// `union Name(A, B, C);` declares a type whose value is exactly one of the listed "case types".
// - Each case type converts implicitly to the union (no wrapper call needed).
// - `switch` over a union is checked for exhaustiveness against the case types, so no `_` arm.
// - Patterns like `Dog d` look INSIDE the union (at its Value), not at the union itself.
//
// Storage: a declared union is generated as a struct with a single `object? Value` field.
// Value-type cases (e.g. an `int` case) are BOXED into that object. To avoid boxing you can
// hand-write a "custom union" ([Union] attribute + HasValue/TryGetValue(out T) members).
// `default(Pet)` holds a null Value, which is why the null pattern matters.

namespace Showcase.Language;

public record Cat(string Name);
public record Dog(string Name, string Breed);
public record Bird(string Name, bool CanTalk);

public union Pet(Cat, Dog, Bird);

// A Result-style union.
public record User(int Id, string Name);
public record Success(User Value);
public record NotFound(int Id);
public record ValidationError(string Message);

public union UserResult(Success, NotFound, ValidationError);

// A union with a value-type case, to show boxing.
public union IntOrString(int, string);

public static class UnionsDemo
{
    public static string Describe(Pet pet) => pet switch
    {
        Cat c => $"{c.Name} the cat says meow",
        Dog { Breed: "Labrador" } d => $"{d.Name} the labrador fetches",
        Dog d => $"{d.Name} the {d.Breed} barks",
        Bird b when b.CanTalk => $"{b.Name} the bird says hello",
        Bird b => $"{b.Name} the bird chirps",
        null => "no pet (default union, Value is null)",
        // No `_` arm: the compiler knows Cat/Dog/Bird/null cover every possibility.
    };

    public static int KindWithoutNullArm(Pet pet) => pet switch { Cat => 1, Dog => 2, Bird => 3 };

    public static UserResult FindUser(int id) => id switch
    {
        <= 0 => new ValidationError("Id must be positive"), // implicit conversion ValidationError -> UserResult
        1 => new Success(new User(1, "Ada")),               // implicit conversion Success -> UserResult
        _ => new NotFound(id),                              // implicit conversion NotFound -> UserResult
    };

    public static string Render(UserResult result) => result switch
    {
        Success s => $"200 OK: {s.Value.Name}",
        NotFound n => $"404: user {n.Id} not found",
        ValidationError v => $"400: {v.Message}",
        null => "empty result",
    };

    public static void Run()
    {
        Console.WriteLine("== Union types ==");

        Pet[] pets = [new Cat("Tom"), new Dog("Rex", "Labrador"), new Dog("Fido", "Beagle"), new Bird("Polly", true), default];
        foreach (var pet in pets)
        {
            Console.WriteLine($"  {Describe(pet)}");
        }

        foreach (var id in new[] { 1, 42, -5 })
        {
            Console.WriteLine($"  FindUser({id}) -> {Render(FindUser(id))}");
        }

        Pet empty = default;
        Console.WriteLine($"  default(Pet) is null: {empty is null}; Value is null: {empty.Value is null}");

        IntOrString number = 42;
        Console.WriteLine($"  IntOrString(42).Value runtime type: {number.Value?.GetType().Name} (stored as object? -> boxed)");
        var fields = typeof(Pet).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Console.WriteLine($"  Pet is a {(typeof(Pet).IsValueType ? "struct" : "class")} with fields: {string.Join(", ", fields.Select(f => $"{f.FieldType.Name} {f.Name}"))}");

        // A switch with no null arm compiles WITHOUT a warning when the input is a parameter
        // (its contents are assumed non-null), but default(Pet) then throws at runtime.
        // Writing the same switch directly on `empty` gives warning CS8655, because flow analysis
        // knows that local may hold null.
        try
        {
            _ = KindWithoutNullArm(empty);
        }
        catch (System.Runtime.CompilerServices.SwitchExpressionException)
        {
            Console.WriteLine("  switch without a null arm on default(Pet) -> SwitchExpressionException");
        }
        Console.WriteLine();
    }
}
