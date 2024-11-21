using System.Diagnostics.CodeAnalysis;
using Lombok.NET.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Lombok.NET.Extensions
{
	internal static class SyntaxNodeExtensions
	{
		private static readonly IDictionary<AccessTypes, SyntaxKind> SyntaxKindsByAccessType = new Dictionary<AccessTypes, SyntaxKind>(4)
		{
			[AccessTypes.Private] = SyntaxKind.PrivateKeyword,
			[AccessTypes.Protected] = SyntaxKind.ProtectedKeyword,
			[AccessTypes.Internal] = SyntaxKind.InternalKeyword,
			[AccessTypes.Public] = SyntaxKind.PublicKeyword
		};

		internal static readonly SyntaxTriviaList NullableTrivia = TriviaList(
			Trivia(
				NullableDirectiveTrivia(
					Token(SyntaxKind.EnableKeyword),
					true
				)
			)
		);

		private static readonly SyntaxTrivia AutoGeneratedComment = Comment("// <auto-generated/>");

		/// <summary>
		/// Traverses a syntax node upwards until it reaches a <code>BaseNamespaceDeclarationSyntax</code>.
		/// </summary>
		/// <param name="node">The syntax node to traverse.</param>
		/// <returns>The namespace this syntax node is in. <code>null</code> if a namespace cannot be found.</returns>
		public static NameSyntax? GetNamespace(this SyntaxNode node)
		{
			var parent = node.Parent;
			while (parent != null)
			{
				if (parent is BaseNamespaceDeclarationSyntax ns)
				{
					return ns.Name;
				}

				parent = parent.Parent;
			}

			return null;
		}

		/// <summary>
		/// Gets the using directives from a SyntaxNode. Traverses the tree upwards until it finds using directives.
		/// </summary>
		/// <param name="node">The staring point.</param>
		/// <returns>A list of using directives.</returns>
		public static SyntaxList<UsingDirectiveSyntax> GetUsings(this SyntaxNode node)
		{
			var parent = node.Parent;
			while (parent is not null)
			{
				if (parent is BaseNamespaceDeclarationSyntax ns && ns.Usings.Any())
				{
					return ns.Usings;
				}

				if (parent is CompilationUnitSyntax compilationUnit && compilationUnit.Usings.Any())
				{
					return compilationUnit.Usings;
				}

				parent = parent.Parent;
			}

			return default;
		}

		/// <summary>
		/// Gets the accessibility modifier for a type declaration.
		/// </summary>
		/// <param name="typeDeclaration">The type declaration's accessibility modifier to find.</param>
		/// <returns>The types accessibility modifier.</returns>
		public static SyntaxKind GetAccessibilityModifier(this BaseTypeDeclarationSyntax typeDeclaration)
		{
			if (typeDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword))
			{
				return SyntaxKind.PublicKeyword;
			}

			return SyntaxKind.InternalKeyword;
		}

		/// <summary>
		/// Constructs a new partial type from the original type's name, accessibility and type arguments.
		/// </summary>
		/// <param name="typeDeclaration">The type to clone.</param>
		/// <returns>A new partial type with a few of the original types traits.</returns>
		public static TypeDeclarationSyntax CreateNewPartialType(this TypeDeclarationSyntax typeDeclaration)
		{
			return typeDeclaration switch
			{
				ClassDeclarationSyntax => typeDeclaration.CreateNewPartialClass(),
				StructDeclarationSyntax => typeDeclaration.CreateNewPartialStruct(),
				InterfaceDeclarationSyntax => typeDeclaration.CreateNewPartialInterface(),
				_ => typeDeclaration
			};
		}

		/// <summary>
		/// Constructs a new partial class from the original type's name, accessibility and type arguments.
		/// </summary>
		/// <param name="type">The type to clone.</param>
		/// <returns>A new partial class with a few of the original types traits.</returns>
		public static ClassDeclarationSyntax CreateNewPartialClass(this TypeDeclarationSyntax type)
		{
			var declaration = ClassDeclaration(type.Identifier.Text)
				.WithModifiers(
					TokenList(
						Token(type.GetAccessibilityModifier()),
						Token(SyntaxKind.PartialKeyword)
					)
				).WithTypeParameterList(type.TypeParameterList);
			if (type.ShouldEmitNrtTrivia())
			{
				declaration = declaration.WithLeadingTrivia(NullableTrivia);
			}

			return declaration;
		}

		/// <summary>
		/// Constructs a new partial struct from the original type's name, accessibility and type arguments.
		/// </summary>
		/// <param name="type">The type to clone.</param>
		/// <returns>A new partial struct with a few of the original types traits.</returns>
		public static StructDeclarationSyntax CreateNewPartialStruct(this TypeDeclarationSyntax type)
		{
			var declaration = StructDeclaration(type.Identifier.Text)
				.WithModifiers(
					TokenList(
						Token(type.GetAccessibilityModifier()),
						Token(SyntaxKind.PartialKeyword)
					)
				).WithTypeParameterList(type.TypeParameterList);
			if (type.ShouldEmitNrtTrivia())
			{
				declaration = declaration.WithLeadingTrivia(NullableTrivia);
			}

			return declaration;
		}

		/// <summary>
		/// Constructs a new partial interface from the original type's name, accessibility and type arguments.
		/// </summary>
		/// <param name="type">The type to clone.</param>
		/// <returns>A new partial interface with a few of the original types traits.</returns>
		public static InterfaceDeclarationSyntax CreateNewPartialInterface(this TypeDeclarationSyntax type)
		{
			var declaration = InterfaceDeclaration(type.Identifier.Text)
				.WithModifiers(
					TokenList(
						Token(type.GetAccessibilityModifier()),
						Token(SyntaxKind.PartialKeyword)
					)
				).WithTypeParameterList(type.TypeParameterList);
			if (type.ShouldEmitNrtTrivia())
			{
				declaration = declaration.WithLeadingTrivia(NullableTrivia);
			}

			return declaration;
		}

		public static CompilationUnitSyntax CreateNewNamespace(this NameSyntax @namespace, MemberDeclarationSyntax innerMember)
		{
			return CreateNewNamespace(@namespace, default, innerMember);
		}

		public static CompilationUnitSyntax CreateNewNamespace(this NameSyntax @namespace, SyntaxList<UsingDirectiveSyntax> usings, MemberDeclarationSyntax innerMember)
		{
			var newNamespace = FileScopedNamespaceDeclaration(@namespace)
				.WithMembers(
					SingletonList(innerMember)
				);
			if (usings.Any())
			{
				var newUsing = usings[0].WithUsingKeyword(
					Token(
						TriviaList(
							AutoGeneratedComment
						),
						SyntaxKind.UsingKeyword,
						TriviaList()
					)
				);
				usings = usings.Replace(usings[0], newUsing);
			}
			else
			{
				newNamespace = newNamespace.WithNamespaceKeyword(
					Token(
						TriviaList(
							AutoGeneratedComment
						),
						SyntaxKind.NamespaceKeyword,
						TriviaList()
					)
				);
			}

			return CompilationUnit()
				.WithUsings(usings)
				.WithMembers(
					SingletonList<MemberDeclarationSyntax>(newNamespace)
				);
		}

		/// <summary>
		/// Checks if a TypeSyntax represents void.
		/// </summary>
		/// <param name="typeSyntax">The TypeSyntax to check.</param>
		/// <returns>True, if the type represents void.</returns>
		public static bool IsVoid(this TypeSyntax typeSyntax)
		{
			return typeSyntax is PredefinedTypeSyntax predefinedType && predefinedType.Keyword.IsKind(SyntaxKind.VoidKeyword);
		}

		/// <summary>
		/// Checks if a type is declared as a nested type.
		/// </summary>
		/// <param name="typeDeclaration">The type to check.</param>
		/// <returns>True, if the type is declared within another type.</returns>
		public static bool IsNestedType(this BaseTypeDeclarationSyntax typeDeclaration)
		{
			return typeDeclaration.Parent is TypeDeclarationSyntax;
		}

		/// <summary>
		/// Determines if the type is eligible for code generation.
		/// </summary>
		/// <param name="typeDeclaration">The type to check for.</param>
		/// <param name="namespace">The type's namespace. Will be set in this method.</param>
		/// <param name="diagnostic">A diagnostic to be emitted if the type is not valid.</param>
		/// <returns>True, if code can be generated for this type.</returns>
		public static bool TryValidateType(this TypeDeclarationSyntax typeDeclaration, [NotNullWhen(true)] out NameSyntax? @namespace, [NotNullWhen(false)] out Diagnostic? diagnostic)
		{
			@namespace = null;
			diagnostic = null;
			if (!typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
			{
				diagnostic = Diagnostic.Create(DiagnosticDescriptors.TypeMustBePartial, typeDeclaration.Identifier.GetLocation(), typeDeclaration.Identifier.Text);

				return false;
			}
			
			if (typeDeclaration.Modifiers.Any(token => token.Text == "file"))
			{
				diagnostic = Diagnostic.Create(DiagnosticDescriptors.TypeCannotBeFileLocal, typeDeclaration.Identifier.GetLocation(), typeDeclaration.Identifier.Text);

				return false;
			}

			if (typeDeclaration.IsNestedType())
			{
				diagnostic = Diagnostic.Create(DiagnosticDescriptors.TypeMustBeNonNested, typeDeclaration.Identifier.GetLocation(), typeDeclaration.Identifier.Text);

				return false;
			}

			@namespace = typeDeclaration.GetNamespace();
			if (@namespace is null)
			{
				diagnostic = Diagnostic.Create(DiagnosticDescriptors.TypeMustHaveNamespace, typeDeclaration.Identifier.GetLocation(), typeDeclaration.Identifier.Text);

				return false;
			}

			return true;
		}

		/// <summary>
		/// Removes all the members which do not have the desired access modifier.
		/// </summary>
		/// <param name="members">The members to filter</param>
		/// <param name="accessType">The access modifer to look out for.</param>
		/// <typeparam name="T">The type of the members (<code>PropertyDeclarationSyntax</code>/<code>FieldDeclarationSyntax</code>).</typeparam>
		/// <returns>The members which have the desired access modifier.</returns>
		/// <exception cref="ArgumentOutOfRangeException">If an access modifier is supplied which is not supported.</exception>
		public static IEnumerable<T> Where<T>(this IEnumerable<T> members, AccessTypes accessType)
			where T : MemberDeclarationSyntax
		{
			var predicateBuilder = PredicateBuilder.False<T>();
			foreach (AccessTypes t in typeof(AccessTypes).GetEnumValues())
			{
				if (accessType.HasFlag(t))
				{
					predicateBuilder = predicateBuilder.Or(m => m.Modifiers.Any(SyntaxKindsByAccessType[t]));
				}
			}

			return members.Where(predicateBuilder.Compile());
		}

		/// <summary>
		/// Creates a unique name for a type which can be used as the hint name in Source Generator output.
		/// </summary>
		/// <param name="type">The type to get the name for</param>
		/// <param name="namespace">The namespace which will be prepended to the type using underscores.</param>
		/// <returns>A unique name for the type inside a generator context.</returns>
		public static string GetHintName(this BaseTypeDeclarationSyntax type, NameSyntax @namespace)
		{
			return string.Concat(@namespace.ToString().Replace('.', '_'), '_', type.Identifier.Text);
		}

		/// <summary>
		/// Determines if the <code>#nullable enable</code> preprocessor directive should be emitted in generated code.
		/// </summary>
		/// <param name="node">The node to determine the nullability context in.</param>
		/// <returns><code>true</code> if the preprocessor directive should be emitted, <code>false</code> otherwise.</returns>
		public static bool ShouldEmitNrtTrivia(this SyntaxNode node)
		{
			return node.SyntaxTree.Options is CSharpParseOptions opt && (int)opt.LanguageVersion >= (int)LanguageVersion.CSharp8;
		}

		public static SyntaxTriviaList GetLeadingTriviaFromMultipleLocations(this FieldDeclarationSyntax field)
		{
			var typeTrivia = field.Declaration.Type.GetCommentTrivia();
			if (typeTrivia.Any())
			{
				return typeTrivia;
			}

			var modifierTrivia = field.Modifiers.FirstOrDefault().GetCommentTrivia();
			if (modifierTrivia.Any())
			{
				return modifierTrivia;
			}

			var attributeList = field.AttributeLists[0];

			return attributeList.OpenBracketToken.GetCommentTrivia();
		}

		public static SyntaxTriviaList GetCommentTrivia(this SyntaxToken token)
		{
			if (token.LeadingTrivia.Any(SyntaxKind.SingleLineCommentTrivia)
			    || token.LeadingTrivia.Any(SyntaxKind.MultiLineCommentTrivia)
			    || token.LeadingTrivia.Any(SyntaxKind.MultiLineDocumentationCommentTrivia)
			    || token.LeadingTrivia.Any(SyntaxKind.SingleLineDocumentationCommentTrivia))
			{
				return token.LeadingTrivia;
			}

			return default;
		}

		public static SyntaxTriviaList GetCommentTrivia(this TypeSyntax type)
		{
			if (type is PredefinedTypeSyntax predefinedType)
			{
				return predefinedType.Keyword.GetCommentTrivia();
			}

			if (type is IdentifierNameSyntax identifier)
			{
				return identifier.Identifier.GetCommentTrivia();
			}

			return default;
		}
	}
}

namespace System.Diagnostics.CodeAnalysis
{
	/// <summary>Specifies that when a method returns <see cref="ReturnValue"/>, the parameter will not be null even if the corresponding type allows it.</summary>
	[AttributeUsage(AttributeTargets.Parameter)]
	internal sealed class NotNullWhenAttribute : Attribute
	{
		/// <summary>Gets the return value condition.</summary>
		public bool ReturnValue { get; }

		/// <summary>Initializes the attribute with the specified return value condition.</summary>
		/// <param name="returnValue">
		/// The return value condition. If the method returns this value, the associated parameter will not be null.
		/// </param>
		public NotNullWhenAttribute(bool returnValue)
		{
			ReturnValue = returnValue;
		}
	}

	/// <summary>Specifies that the method or property will ensure that the listed field and property members have not-null values when returning with the specified return value condition.</summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	internal sealed class MemberNotNullWhenAttribute : Attribute
	{
		/// <summary>Initializes the attribute with the specified return value condition and a field or property member.</summary>
		/// <param name="returnValue">
		/// The return value condition. If the method returns this value, the associated parameter will not be null.
		/// </param>
		/// <param name="member">
		/// The field or property member that is promised to be not-null.
		/// </param>
		public MemberNotNullWhenAttribute(bool returnValue, string member)
		{
			ReturnValue = returnValue;
			Members = new[] { member };
		}

		/// <summary>Initializes the attribute with the specified return value condition and list of field and property members.</summary>
		/// <param name="returnValue">
		/// The return value condition. If the method returns this value, the associated parameter will not be null.
		/// </param>
		/// <param name="members">
		/// The list of field and property members that are promised to be not-null.
		/// </param>
		public MemberNotNullWhenAttribute(bool returnValue, params string[] members)
		{
			ReturnValue = returnValue;
			Members = members;
		}

		/// <summary>Gets the return value condition.</summary>
		public bool ReturnValue { get; }

		/// <summary>Gets field or property member names.</summary>
		public string[] Members { get; }
	}
}