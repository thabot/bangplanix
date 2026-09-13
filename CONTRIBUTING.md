# Contributing to Bangplanix

Thank you for contributing to Bangplanix! To maintain our high standards of microsecond performance, memory efficiency, and rock-solid enterprise stability, please follow these contribution guidelines.

## Development Workflow

1. **Prerequisites:**
   - .NET 10 SDK (or latest LTS)
   - Git
   - Docker (optional, for container tests)

2. **Code Standards:**
   - Follow the `.editorconfig` style rules.
   - Use Zero-Allocation patterns (`ReadOnlySpan<T>`, `Memory<T>`, `ArrayPool<T>`, `SearchValues<T>`) for all hot path parsing and rendering code.
   - Ensure all public APIs have XML documentation comments.
   - Ensure `Nullable` reference types are properly handled with zero compiler warnings.

3. **Testing Requirements:**
   - 100% test pass rate required (`dotnet test`).
   - Add unit tests for every new feature or bug fix.
   - Add visual snapshot tests for visual rendering elements.

4. **Contributor License Agreement (CLA):**
   - All external contributors must sign the [CLA.md](./CLA.md) before pull requests can be merged.
