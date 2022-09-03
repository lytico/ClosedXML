using System.Collections.Generic;

namespace ClosedXML.Excel.CalcEngine
{
    /// <summary>
    /// Base class for all AST nodes. All AST nodes must be immutable.
    /// </summary>
    internal abstract class AstNode
    {
        /// <summary>
        /// Method to accept a vistor (=call a method of visitor with correct type of the node).
        /// </summary>
        public abstract TResult Accept<TContext, TResult>(TContext context, IFormulaVisitor<TContext, TResult> visitor);
    }

    /// <summary>
    /// A base class for all AST nodes that can be evaluated to produce a value.
    /// </summary>
    internal abstract class ValueNode : AstNode
    {
    }

    /// <summary>
    /// AST node that contains a number, text or a bool.
    /// </summary>
    internal class ScalarNode : ValueNode
    {
        public ScalarNode(AnyValue value)
        {
            Value = value;
        }

        public AnyValue Value { get; }

        public override TResult Accept<TContext, TResult>(TContext context, IFormulaVisitor<TContext, TResult> visitor) => visitor.Visit(context, this);
    }

    internal enum UnaryOp
    {
        Add,
        Subtract,
        Percentage,
        SpillRange,
        ImplicitIntersection
    }

    /// <summary>
    /// Unary expression, e.g. +123
    /// </summary>
    internal class UnaryNode : ValueNode
    {
        public UnaryNode(UnaryOp operation, ValueNode expr)
        {
            Operation = operation;
            Expression = expr;
        }

        public UnaryOp Operation { get; }

        public ValueNode Expression { get; }

        public override TResult Accept<TContext, TResult>(TContext context, IFormulaVisitor<TContext, TResult> visitor) => visitor.Visit(context, this);
    }

    internal enum BinaryOp
    {
        // Text operators
        Concat,
        // Arithmetic
        Add,
        Sub,
        Mult,
        Div,
        Exp,
        // Comparison operators
        Lt,
        Lte,
        Eq,
        Neq,
        Gte,
        Gt,
        // References operators
        Range,
        Union,
        Intersection
    }

    /// <summary>
    /// Binary expression, e.g. 1+2
    /// </summary>
    internal class BinaryNode : ValueNode
    {
        public BinaryNode(BinaryOp operation, ValueNode exprLeft, ValueNode exprRight)
        {
            Operation = operation;
            LeftExpression = exprLeft;
            RightExpression = exprRight;
        }

        public BinaryOp Operation { get; }

        public ValueNode LeftExpression { get; }

        public ValueNode RightExpression { get; }

        public override TResult Accept<TContext, TResult>(TContext context, IFormulaVisitor<TContext, TResult> visitor) => visitor.Visit(context, this);
    }

    /// <summary>
    /// A function call, e.g. <c>SIN(0.5)</c>.
    /// </summary>
    internal class FunctionNode : ValueNode
    {
        public FunctionNode(string name, List<ValueNode> parms) : this(null, name, parms)
        {
        }

        public FunctionNode(PrefixNode prefix, string name, List<ValueNode> parms)
        {
            Prefix = prefix;
            Name = name;
            Parameters = parms;
        }

        public PrefixNode Prefix { get; }

        /// <summary>
        /// Name of the function.
        /// </summary>
        public string Name { get; }

        public List<ValueNode> Parameters { get; }

        public override TResult Accept<TContext, TResult>(TContext context, IFormulaVisitor<TContext, TResult> visitor) => visitor.Visit(context, this);
    }

    /// <summary>
    /// Expression that represents an omitted parameter.
    /// </summary>
    internal class EmptyArgumentNode : ValueNode
    {
        public override TResult Accept<TContext, TResult>(TContext context, IFormulaVisitor<TContext, TResult> visitor) => visitor.Visit(context, this);
    }

    // TODO: Merge with ScalarNode
    internal class ErrorNode : ValueNode
    {
        internal ErrorNode(Error error)
        {
            Error = error;
        }

        public Error Error { get; }

        public override TResult Accept<TContext, TResult>(TContext context, IFormulaVisitor<TContext, TResult> visitor) => visitor.Visit(context, this);
    }

    /// <summary>
    /// An placeholder node for AST nodes that are not yet supported in ClosedXML.
    /// </summary>
    internal class NotSupportedNode : ValueNode
    {
        public NotSupportedNode(string featureName)
        {
            FeatureName = featureName;
        }

        public string FeatureName { get; }

        public override TResult Accept<TContext, TResult>(TContext context, IFormulaVisitor<TContext, TResult> visitor) => visitor.Visit(context, this);
    }

    /// <summary>
    /// AST node for an reference to an external file in a formula.
    /// </summary>
    internal class FileNode : AstNode
    {
        /// <summary>
        /// If the file is references indirectly, numeric identifier of a file.
        /// </summary>
        public int? Numeric { get; }

        /// <summary>
        /// If a file is referenced directly, a path to the file on the disc/UNC/web link, .
        /// </summary>
        public string Path { get; }

        public FileNode(string path)
        {
            Path = path;
        }

        public FileNode(int numeric)
        {
            Numeric = numeric;
        }

        public override TResult Accept<TContext, TResult>(TContext context, IFormulaVisitor<TContext, TResult> visitor) => visitor.Visit(context, this);
    }

    /// <summary>
    /// AST node for prefix of a reference in a formula. Prefix is a specification where to look for a reference.
    /// <list type="bullet">
    /// <item>Prefix specifies a <c>Sheet</c> - used for references in the local workbook.</item>
    /// <item>Prefix specifies a <c>FirstSheet</c> and a <c>LastSheet</c> - 3D reference, references uses all sheets between first and last.</item>
    /// <item>Prefix specifies a <c>File</c>, no sheet is specified - used for named ranges in external file.</item>
    /// <item>Prefix specifies a <c>File</c> and a <c>Sheet</c> - references looks for its address in the sheet of the file.</item>
    /// </list>
    /// </summary>
    internal class PrefixNode : AstNode
    {
        public PrefixNode(FileNode file, string sheet, string firstSheet, string lastSheet)
        {
            File = file;
            Sheet = sheet;
            FirstSheet = firstSheet;
            LastSheet = lastSheet;
        }

        /// <summary>
        /// If prefix references data from another file, can be empty.
        /// </summary>
        public FileNode File { get; }

        /// <summary>
        /// Name of the sheet, without ! or escaped quotes. Can be empty in some cases (e.g. reference to a named range in an another file).
        /// </summary>
        public string Sheet { get; }

        /// <summary>
        /// If the prefix is for 3D reference, name of first sheet. Empty otherwise.
        /// </summary>
        public string FirstSheet { get; }

        /// <summary>
        /// If the prefix is for 3D reference, name of the last sheet. Empty otherwise.
        /// </summary>
        public string LastSheet { get; }

        public override TResult Accept<TContext, TResult>(TContext context, IFormulaVisitor<TContext, TResult> visitor) => visitor.Visit(context, this);
    }

    /// <summary>
    /// AST node for a reference of an area in some sheet.
    /// </summary>
    internal class ReferenceNode : ValueNode
    {
        public ReferenceNode(PrefixNode prefix, ReferenceItemType type, string address)
        {
            Prefix = prefix;
            Type = type;
            Address = address;
        }

        /// <summary>
        /// An optional prefix for reference item.
        /// </summary>
        public PrefixNode Prefix { get; }

        public ReferenceItemType Type { get; }

        /// <summary>
        /// An address of a reference that corresponds to <see cref="Type"/> or a name of named range.
        /// </summary>
        public string Address { get; }

        public override TResult Accept<TContext, TResult>(TContext context, IFormulaVisitor<TContext, TResult> visitor) => visitor.Visit(context, this);
    }

    internal enum ReferenceItemType { Cell, NamedRange, VRange, HRange }

    // TODO: The AST node doesn't have any stuff from StructuredReference term because structured reference is not yet suported and
    // the SR grammar has changed in not-yet-released (after 1.5.2) version of XLParser
    internal class StructuredReferenceNode : ValueNode
    {
        public StructuredReferenceNode(PrefixNode prefix)
        {
            Prefix = prefix;
        }

        /// <summary>
        /// Can be empty if no prefix available.
        /// </summary>
        public PrefixNode Prefix { get; }

        public override TResult Accept<TContext, TResult>(TContext context, IFormulaVisitor<TContext, TResult> visitor) => visitor.Visit(context, this);
    }
}