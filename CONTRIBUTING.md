# Contributing to Ultima Valheim (UOV)

Thank you for your interest in contributing to UOV! This document provides guidelines and information for contributors.

## Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/YOUR-USERNAME/UOV.git
   cd UOV
   ```
3. **Set up your development environment**:
   - Install Visual Studio 2019 or later
   - Install Valheim
   - Install BepInEx and Jotunn
   - Set `VALHEIM_INSTALL` environment variable

## Development Workflow

1. **Create a branch** for your feature or bugfix:
   ```bash
   git checkout -b feature/my-new-feature
   ```

2. **Make your changes**:
   - Follow the existing code style
   - Add XML documentation comments to public APIs
   - Test in both single-player and multiplayer

3. **Build and test**:
   ```bash
   dotnet build
   ```

4. **Commit your changes**:
   ```bash
   git add .
   git commit -m "Add feature: description of your changes"
   ```

5. **Push to your fork**:
   ```bash
   git push origin feature/my-new-feature
   ```

6. **Create a Pull Request** on GitHub

## Code Style Guidelines

- Use C# naming conventions (PascalCase for public members, camelCase for private)
- Add XML documentation comments to all public APIs
- Keep methods focused and single-purpose
- Use meaningful variable and method names
- Follow the Core + Sidecar architecture pattern

### Example:

```csharp
/// <summary>
/// Calculates skill XP based on action difficulty.
/// </summary>
/// <param name="baseDifficulty">Base difficulty of the action</param>
/// <param name="skillLevel">Current skill level</param>
/// <returns>XP amount to award</returns>
public float CalculateSkillXP(float baseDifficulty, int skillLevel)
{
    return baseDifficulty * (1.0f - (skillLevel / 100.0f));
}
```

## Creating a New Sidecar Module

1. Create a new project in the solution
2. Implement the `ICoreModule` interface
3. Add the project reference to `UltimaValheimCore`
4. Document your module's features in a README
5. Add example usage and configuration options

See `ExampleSidecar` for a complete reference implementation.

## Pull Request Guidelines

- **One feature per PR**: Keep PRs focused on a single feature or bugfix
- **Write clear descriptions**: Explain what changes you made and why
- **Reference issues**: If fixing a bug, reference the issue number
- **Update documentation**: Update relevant docs if you change APIs
- **Test multiplayer**: Always test in multiplayer mode if relevant

## Testing

Before submitting a PR, please test:

- ✅ Single-player mode
- ✅ Multiplayer (host)
- ✅ Multiplayer (client)
- ✅ Module loads without errors
- ✅ Configuration works as expected
- ✅ Persistence (save/load) works correctly
- ✅ No conflicts with other modules

## Reporting Bugs

When reporting bugs, please include:

- UOV version
- Valheim version
- BepInEx version
- Other installed mods
- Steps to reproduce
- Expected vs actual behavior
- Log files (BepInEx/LogOutput.log)

## Suggesting Features

We welcome feature suggestions! Please:

- Check if the feature already exists or is planned
- Explain the use case and benefits
- Consider if it fits the Core + Sidecar architecture
- Be open to discussion and feedback

## Module Development Best Practices

1. **Use CoreAPI for everything**: Don't access game internals directly
2. **Subscribe to events**: Use the EventBus for inter-module communication
3. **Never assume other modules exist**: Check with `CoreLifecycle.IsModuleRegistered()`
4. **Save data in OnSave()**: Always persist data in the save callback
5. **Clean up in OnShutdown()**: Release resources properly
6. **Log important operations**: Use `CoreAPI.Log` for debugging
7. **Handle errors gracefully**: Wrap risky operations in try-catch
8. **Test edge cases**: Empty worlds, first-time players, etc.

## Code of Conduct

- Be respectful and constructive
- Help others learn and grow
- Focus on the code, not the person
- Welcome newcomers and answer questions patiently

## Questions?

- **Discord**: Join our community server
- **GitHub Issues**: For bugs and feature requests
- **Discussions**: For general questions and ideas

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

---

Thank you for contributing to UOV! Together we're building something amazing for the Valheim community. 🎮⚔️
