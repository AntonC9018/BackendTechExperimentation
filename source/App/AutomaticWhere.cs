// using System.Linq.Expressions;
// using HotChocolate;
// using HotChocolate.Data.Filters;
// using HotChocolate.Data.Filters.Expressions;
// using HotChocolate.Language;
// using HotChocolate.Language.Visitors;
// using HotChocolate.Resolvers;
// using HotChocolate.Types;
//
// namespace efcore_transactions;
//
// public class OnlyOwnProjectsVisitor : TypedDocumentVisitor<CustomContext>
// {
//     private readonly IReadOnlySet<IType> _typesFromWhichCanReachTargetType;
//
//     public OnlyOwnProjectsVisitor(IReadOnlySet<IType> typesFromWhichCanReachTargetType)
//     {
//         _typesFromWhichCanReachTargetType = typesFromWhichCanReachTargetType;
//     }
//
//     protected override ISyntaxVisitorAction Enter(FieldNode node, CustomContext customContext)
//     {
//         IMiddlewareContext context;
//         context.GetSelections()
//         // Put the type of the field on top of the stack.
//         var action = base.Enter(node, customContext);
//         if (action != Continue)
//             return action;
//
//         var fieldType = customContext.Types[^1];
//         var fieldRuntimeType = fieldType.ToRuntimeType();
//         if (fieldRuntimeType == customContext.TargetType)
//         {
//             // We're at the root type.
//             // The root type is the target type.
//             // Apply root level filter.
//             if (customContext.Types.Count == 1)
//             {
//                 customContext.OutExpressions.Add();
//             }
//             var parentType = () customContext.Types[^2];
//             switch (fieldType)
//             {
//                 case 
//             }
//             // save path
//             fieldType.
//         }
//
//         if (_typesFromWhichCanReachTargetType.Contains(fieldType))
//             return Continue;
//
//         return Skip;
//     }
// }
//
// public class CustomContext : QueryableFilterContext
// {
//     public System.Type TargetType { get; }
//
//     public CustomContext(IFilterInputType initialType, bool inMemory) : base(initialType, inMemory)
//     {
//     }
// }
//
// public class TypedDocumentVisitor : SyntaxWalker<QueryableFilterContext>
// {
//     protected override ISyntaxVisitorAction Enter(
//         OperationDefinitionNode node,
//         CustomContext context)
//     {
//         if (context.Schema.GetOperationType(node.Operation) is { } type)
//         {
//             context.Types.Push(type);
//             context.Variables.Clear();
//             return Continue;
//         }
//
//         return Skip;
//     }
//
//     protected override ISyntaxVisitorAction Enter(
//         VariableDefinitionNode node,
//         CustomContext context)
//     {
//         context.Variables[node.Variable.Name.Value] = node;
//         return Continue;
//     }
//
//     protected override ISyntaxVisitorAction Enter(
//         InlineFragmentNode node,
//         CustomContext context)
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
//
//         context.UnexpectedErrorsDetected = true;
//         return Skip;
//     }
//
//     protected override ISyntaxVisitorAction Enter(
//         FragmentDefinitionNode node,
//         CustomContext context)
//     {
//         if (context.Schema.TryGetType<INamedOutputType>(
//             node.TypeCondition.Name.Value,
//             out var namedOutputType))
//         {
//             context.Types.Push(namedOutputType);
//             return Continue;
//         }
//
//         context.UnexpectedErrorsDetected = true;
//         return Skip;
//     }
//
//     protected override ISyntaxVisitorAction Leave(
//        OperationDefinitionNode node,
//        CustomContext context)
//     {
//         context.Types.Pop();
//         context.Variables.Clear();
//         return Continue;
//     }
//
//     protected override ISyntaxVisitorAction Leave(
//         InlineFragmentNode node,
//         CustomContext context)
//     {
//         if (node.TypeCondition is { })
//         {
//             context.Types.Pop();
//         }
//
//         return Continue;
//     }
//
//     protected override ISyntaxVisitorAction Leave(
//         FragmentDefinitionNode node,
//         CustomContext context)
//     {
//         context.Types.Pop();
//         return Continue;
//     }
//     
//     protected override CustomContext OnAfterEnter(
//         ISyntaxNode node,
//         ISyntaxNode? parent,
//         CustomContext context,
//         ISyntaxVisitorAction action)
//     {
//         if (action.IsContinue())
//         {
//             context.Path.Push(node);
//         }
//         return context;
//     }
//
//     protected override CustomContext OnBeforeLeave(
//         ISyntaxNode node,
//         ISyntaxNode? parent,
//         CustomContext context)
//     {
//         if (node.Kind == SyntaxKind.OperationDefinition)
//         {
//             context.VisitedFragments.Clear();
//         }
//         context.Path.Pop();
//         return context;
//     }
//
//     protected override ISyntaxVisitorAction VisitChildren(
//         DocumentNode node,
//         CustomContext context)
//     {
//         for (var i = 0; i < node.Definitions.Count; i++)
//         {
//             if (node.Definitions[i].Kind != SyntaxKind.FragmentDefinition &&
//                 Visit(node.Definitions[i], node, context).IsBreak())
//             {
//                 return Break;
//             }
//         }
//
//         return DefaultAction;
//     }
//
//     protected override ISyntaxVisitorAction VisitChildren(
//         FragmentSpreadNode node,
//         CustomContext context)
//     {
//         if (base.VisitChildren(node, context).IsBreak())
//         {
//             return Break;
//         }
//
//         if (context.Fragments.TryGetValue(
//                 node.Name.Value,
//                 out var fragment) &&
//             context.VisitedFragments.Add(fragment.Name.Value))
//         {
//             if (Visit(fragment, node, context).IsBreak())
//             {
//                 return Break;
//             }
//         }
//
//         return DefaultAction;
//     }
//     
//     
//     protected abstract ISyntaxVisitorAction OnFieldEnter(
//         TContext context,
//         IFilterField field,
//         ObjectFieldNode node);
//
//     protected abstract ISyntaxVisitorAction OnFieldLeave(
//         TContext context,
//         IFilterField field,
//         ObjectFieldNode node);
//
//     protected abstract bool TryCombineOperations(
//         TContext context,
//         Queue<T> operations,
//         FilterCombinator combinator,
//         [NotNullWhen(true)] out T? combined);
//
//     protected override ISyntaxVisitorAction Leave(
//         ObjectValueNode node,
//         TContext context)
//     {
//         var operations = context.PopLevel();
//
//         if (TryCombineOperations(context, operations, FilterCombinator.And, out var combined))
//         {
//             context.GetLevel().Enqueue(combined);
//         }
//
//         return Continue;
//     }
//     protected override ISyntaxVisitorAction Enter(
//         ObjectValueNode node,
//         TContext context)
//     {
//         context.PushLevel(new Queue<T>());
//
//         return Continue;
//     }
//
//     protected override ISyntaxVisitorAction Enter(
//         ObjectFieldNode node,
//         TContext context)
//     {
//         base.Enter(node, context);
//
//         if (context.Operations.Peek() is IFilterField field and not IOrField and not IAndField)
//         {
//             return OnFieldEnter(context, field, node);
//         }
//
//         return Continue;
//     }
//
//     protected override ISyntaxVisitorAction Leave(
//         ObjectFieldNode node,
//         TContext context)
//     {
//         var result = Continue;
//
//         if (context.Operations.Peek() is IFilterField field)
//         {
//             result = OnFieldLeave(context, field, node);
//         }
//
//         base.Leave(node, context);
//
//         return result;
//     }
//
//     protected override ISyntaxVisitorAction Enter(
//         ListValueNode node,
//         TContext context)
//     {
//         context.PushLevel(new Queue<T>());
//         return Continue;
//     }
//
//     protected override ISyntaxVisitorAction Leave(
//         ListValueNode node,
//         TContext context)
//     {
//         var combinator =
//             context.Operations.Peek() is OrField
//                 ? FilterCombinator.Or
//                 : FilterCombinator.And;
//
//         var operations = context.PopLevel();
//
//         if (TryCombineOperations(context, operations, combinator, out var combined))
//         {
//             context.GetLevel().Enqueue(combined);
//         }
//
//         return Continue;
//     }
//     
//     protected override ISyntaxVisitorAction Leave(
//         ObjectFieldNode node,
//         TContext context)
//     {
//         context.Operations.Pop();
//         context.Types.Pop();
//         return Continue;
//     }
//
//     protected override ISyntaxVisitorAction Enter(
//         ObjectFieldNode node,
//         TContext context)
//     {
//         if (context.Types.Peek().NamedType() is InputObjectType inputType)
//         {
//             if (inputType.Fields.TryGetField(node.Name.Value,
//                     out IInputField? field))
//             {
//                 context.Operations.Push(field);
//                 context.Types.Push(field.Type);
//                 return Continue;
//             }
//
//             throw new InvalidOperationException(DataResources.FilterVisitor_InvalidField);
//         }
//         else
//         {
//             throw new InvalidOperationException(DataResources.FilterVisitor_InvalidType);
//         }
//     }
// }
