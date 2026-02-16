---
applyTo: **/*.cs, **/*.razor
description: C# coding style and best practices for the project.
---

- Always use explicit access modifiers (public, private, protected, internal) for classes and class members.
- Use C# 14 extension keyword for extension methods.
- Do not use expression-bodied members for simple property getters and methods.
- Always use string interpolation ($"") instead of String.Format or concatenation for building strings.
- Prefer using 'is not null' over '!= null' for null checks. Same for 'is null' over '== null'.
- Use 'nameof()' operator instead of hardcoding member names as strings.
- Always use 'using' statements for IDisposable objects to ensure proper resource management. Use 'await using' for async disposables.
- Prefer 'async' and 'await' keywords for asynchronous programming instead of blocking calls.
- Use pattern matching (switch expressions, property patterns) for cleaner and more readable code. However, avoid overusing it in simple scenarios where traditional constructs are clearer.
- Always prefer 'foreach' loops over 'for' loops when iterating over collections unless you need the index.
- Use 'var' only when the type is evident from the right side of the assignment.
- Always prefer 'expression == false' over '!(expression)' for better readability.
- Use 'record' types for immutable data structures instead of classes when appropriate.
- Always use block bodies for 'if', 'else', 'for', 'while', and 'do' statements, even if they contain only a single statement, to improve readability and reduce errors when adding new statements later.
- Use 'readonly' modifier for fields that are assigned only in the constructor to indicate immutability.
- Use 'const' for compile-time constants and 'readonly' for runtime constants.
- Prefer string.IsNullOrWhiteSpace() over string.IsNullOrEmpty() when checking for empty strings to also consider strings that are only whitespace as empty.
- Prefer string.Empty over "".
- Prefer [] for collection types initialization.
