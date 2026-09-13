using System.Collections.Concurrent;
using System.Reflection;
using Bangplanix.Expressions.Functions;
using Bangplanix.Expressions.Security;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Bangplanix.Expressions.Compiler;

public sealed class RoslynExpressionCompiler : IExpressionEvaluator
{
    private static readonly ConcurrentDictionary<string, Func<object?, object?>> CompiledCache = new(StringComparer.Ordinal);
    private static readonly List<MetadataReference> DefaultReferences = InitializeReferences();

    private static List<MetadataReference> InitializeReferences()
    {
        var refs = new List<MetadataReference>();
        var assemblies = new HashSet<Assembly>
        {
            typeof(object).Assembly, // System.Private.CoreLib
            typeof(ReportFunctions).Assembly, // Bangplanix.Expressions
            typeof(System.Runtime.CompilerServices.DynamicAttribute).Assembly,
            typeof(System.Linq.Expressions.Expression).Assembly,
            typeof(Microsoft.CSharp.RuntimeBinder.Binder).Assembly,
            Assembly.Load("System.Runtime")
        };

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!asm.IsDynamic && !string.IsNullOrEmpty(asm.Location))
            {
                assemblies.Add(asm);
            }
        }

        foreach (var asm in assemblies)
        {
            try
            {
                if (!string.IsNullOrEmpty(asm.Location) && File.Exists(asm.Location))
                {
                    refs.Add(MetadataReference.CreateFromFile(asm.Location));
                }
            }
            catch
            {
                // Continue
            }
        }

        return refs;
    }

    public bool ValidateExpressionSafety(string expression, out string? securityViolationReason)
    {
        return AstSecurityValidator.IsExpressionSafe(expression, out securityViolationReason);
    }

    public TResult? Evaluate<TResult>(string expression, object? context = null)
    {
        var val = Evaluate(expression, context);
        if (val is null) return default;
        if (val is TResult result) return result;
        return (TResult)Convert.ChangeType(val, typeof(TResult), System.Globalization.CultureInfo.InvariantCulture);
    }

    public object? Evaluate(string expression, object? context = null)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return null;
        }

        var trimmed = expression.Trim();
        if (trimmed.StartsWith('='))
        {
            trimmed = trimmed[1..].Trim();
        }

        if (!ValidateExpressionSafety(trimmed, out var violationReason))
        {
            throw new InvalidOperationException($"Security sandbox violation in expression '{expression}': {violationReason}");
        }

        if (CompiledCache.TryGetValue(trimmed, out var compiledFunc))
        {
            return compiledFunc(context);
        }

        var compiled = CompileExpression(trimmed);
        CompiledCache[trimmed] = compiled;

        return compiled(context);
    }

    private static Func<object?, object?> CompileExpression(string expression)
    {
        var className = $"DynamicExpr_{Guid.NewGuid():N}";
        var code = $@"
using System;
using System.Collections.Generic;
using Bangplanix.Expressions.Functions;
using Bangplanix.Expressions.Polyfills;
using static Bangplanix.Expressions.Functions.ReportFunctions;
using static Bangplanix.Expressions.Polyfills.LegacyExpressionPolyfills;

namespace Bangplanix.Expressions.Dynamic
{{
    public static class {className}
    {{
        public static object? Evaluate(object? ctx)
        {{
            return {expression};
        }}
    }}
}}";

        var syntaxTree = CSharpSyntaxTree.ParseText(code);

        var compilation = CSharpCompilation.Create(
            $"Bangplanix_Dynamic_{Guid.NewGuid():N}",
            [syntaxTree],
            DefaultReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release));

        using var peStream = new MemoryStream();
        var emitResult = compilation.Emit(peStream);

        if (!emitResult.Success)
        {
            var errors = string.Join("; ", emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.GetMessage()));
            throw new InvalidOperationException($"Failed to compile dynamic expression '{expression}': {errors}");
        }

        peStream.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(peStream.ToArray());
        var type = assembly.GetType($"Bangplanix.Expressions.Dynamic.{className}");
        var method = type?.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static);

        if (method is null)
        {
            throw new InvalidOperationException("Compiled method could not be found.");
        }

        return ctx => method.Invoke(null, [ctx]);
    }
}
