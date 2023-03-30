// using HotChocolate.Language;
// using HotChocolate.Language.Visitors;
//
// namespace efcore_transactions;
//
// public class OnlyOwnProjectsVisitor : TypedDocumentVisitor<Context>
// {
//     protected override ISyntaxVisitorAction Enter(FieldNode node, Context context)
//     {
//         // Put the type of the field on top of the stack.
//         var action = base.Enter(node, context);
//         if (action != Continue)
//             return action;
//
//         var fieldType = context.Types[^1];
//         var fieldRuntimeType = fieldType.ToRuntimeType();
//         if (fieldRuntimeType != context.TargetType)
//             return action;
//
//         return Continue;
//     }
// }
//
// public class Context : ITypeVisitContext
// {
//     public System.Type TargetType { get; }
//     public ISchema Schema { get; }
//     public IList<IType> Types { get; set; } = new List<IType>();
//     
//     public Context(ISchema schema, System.Type targetType)
//     {
//         Schema = schema;
//         TargetType = targetType;
//     }
// }
//
// public class ITypeVisitContext : ISyntaxVisitorContext
// {
//     /// <summary>
//     /// Gets the schema on which the validation is executed.
//     /// </summary>
//     ISchema Schema { get; }
//     
//     /// <summary>
//     /// The current visitation path of types.
//     /// </summary>
//     IList<IType> Types { get; }
// }
//
// public class TypedDocumentVisitor<TContext> : SyntaxWalker<TContext>
//     where TContext : ITypeVisitContext
// {
//     protected TypedDocumentVisitor(SyntaxVisitorOptions options = default)
//         : base(options)
//     {
//     }
//
//     protected override ISyntaxVisitorAction Enter(
//         OperationDefinitionNode node,
//         TContext context)
//     {
//         if (context.Schema.GetOperationType(node.Operation) is { } type)
//         {
//             context.Types.Push(type);
//             // context.Variables.Clear();
//             return Continue;
//         }
//
//         return Skip;
//     }
//
//     protected override ISyntaxVisitorAction Enter(
//         VariableDefinitionNode node,
//         TContext context)
//     {
//         // context.Variables[node.Variable.Name.Value] = node;
//         return Continue;
//     }
//
//     protected override ISyntaxVisitorAction Enter(
//         InlineFragmentNode node,
//         TContext context)
//     {
//         if (node.TypeCondition is null)
//         {
//             return Continue;
//         }
//
//         if (context.Schema.TryGetType<INamedOutputType>(
//             node.TypeCondition.Name.Value,
//             out var type))
//         {
//             context.Types.Push(type);
//             return Continue;
//         }
//         return Skip;
//     }
//
//     protected override ISyntaxVisitorAction Enter(
//         FragmentDefinitionNode node,
//         TContext context)
//     {
//         if (context.Schema.TryGetType<INamedOutputType>(
//             node.TypeCondition.Name.Value,
//             out var namedOutputType))
//         {
//             context.Types.Push(namedOutputType);
//             return Continue;
//         }
//         return Skip;
//     }
//
//     protected override ISyntaxVisitorAction Leave(
//        OperationDefinitionNode node,
//        TContext context)
//     {
//         context.Types.Pop();
//         // context.Variables.Clear();
//         return Continue;
//     }
//
//     protected override ISyntaxVisitorAction Leave(
//         InlineFragmentNode node,
//         TContext context)
//     {
//         if (node.TypeCondition is not null)
//         {
//             context.Types.Pop();
//         }
//
//         return Continue;
//     }
//
//     protected override ISyntaxVisitorAction Leave(
//         FragmentDefinitionNode node,
//         TContext context)
//     {
//         context.Types.Pop();
//         return Continue;
//     }
// }
