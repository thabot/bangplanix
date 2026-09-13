using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bangplanix.Expressions.Security;

public static class AstSecurityValidator
{
    private static readonly HashSet<string> BannedRootNamespaces = new(StringComparer.OrdinalIgnoreCase)
    {
        "System.IO",
        "System.Diagnostics",
        "System.Reflection",
        "System.Net",
        "System.Threading",
        "System.Runtime",
        "System.Security",
        "System.AppDomain",
        "System.Environment",
        "System.GC",
        "Microsoft.Win32"
    };

    private static readonly HashSet<string> BannedTypeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "File", "Directory", "Path", "FileStream", "StreamReader", "StreamWriter",
        "Process", "ProcessStartInfo",
        "Assembly", "Type", "MethodInfo", "FieldInfo", "PropertyInfo", "Activator",
        "HttpClient", "WebClient", "Socket", "TcpClient", "UdpClient", "Dns",
        "Thread", "ThreadPool", "Task",
        "Marshal", "Unsafe", "MemoryMarshal"
    };

    public static bool IsExpressionSafe(string expression, out string? violationReason)
    {
        violationReason = null;

        if (string.IsNullOrWhiteSpace(expression))
        {
            return true;
        }

        // Wrap as expression statement or return lambda
        var codeToParse = expression.Trim();
        if (codeToParse.StartsWith('='))
        {
            codeToParse = codeToParse[1..].Trim();
        }

        var sourceCode = $"public class Sandbox {{ public object? Eval() => {codeToParse}; }}";
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = syntaxTree.GetRoot();

        var walker = new SecuritySyntaxWalker();
        walker.Visit(root);

        if (walker.ViolationFound)
        {
            violationReason = walker.ViolationReason;
            return false;
        }

        return true;
    }

    private sealed class SecuritySyntaxWalker : CSharpSyntaxWalker
    {
        public bool ViolationFound { get; private set; }
        public string? ViolationReason { get; private set; }

        public override void VisitUnsafeStatement(UnsafeStatementSyntax node)
        {
            SetViolation("Unsafe statements and pointer manipulation are strictly prohibited.");
        }

        public override void VisitPointerType(PointerTypeSyntax node)
        {
            SetViolation("Pointer types are strictly prohibited.");
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            var identifier = node.Identifier.Text;
            if (BannedTypeNames.Contains(identifier))
            {
                SetViolation($"Access to banned system type '{identifier}' is blocked by sandbox.");
                return;
            }

            base.VisitIdentifierName(node);
        }

        public override void VisitQualifiedName(QualifiedNameSyntax node)
        {
            var fullName = node.ToString();
            foreach (var banned in BannedRootNamespaces)
            {
                if (fullName.StartsWith(banned, StringComparison.OrdinalIgnoreCase))
                {
                    SetViolation($"Access to banned namespace '{banned}' is blocked by sandbox.");
                    return;
                }
            }

            base.VisitQualifiedName(node);
        }

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            var fullExpression = node.ToString();
            foreach (var banned in BannedRootNamespaces)
            {
                if (fullExpression.StartsWith(banned, StringComparison.OrdinalIgnoreCase))
                {
                    SetViolation($"Access to banned namespace '{banned}' is blocked by sandbox.");
                    return;
                }
            }

            var memberName = node.Name.Identifier.Text;
            if (memberName.Equals("GetType", StringComparison.OrdinalIgnoreCase) && node.Expression is not null)
            {
                SetViolation("Dynamic reflection via 'GetType()' is blocked.");
                return;
            }

            base.VisitMemberAccessExpression(node);
        }

        private void SetViolation(string reason)
        {
            if (!ViolationFound)
            {
                ViolationFound = true;
                ViolationReason = reason;
            }
        }
    }
}
