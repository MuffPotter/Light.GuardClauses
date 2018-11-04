﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Light.Undefine;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Light.GuardClauses.SourceCodeTransformation
{
    public sealed class SourceFileMerger
    {
        private readonly SourceFileMergeOptions _options;

        public SourceFileMerger(SourceFileMergeOptions options) =>
            _options = options ?? throw new ArgumentNullException(nameof(options));

        public async Task CreateSingleSourceFileAsync()
        {
            // Prepare the target syntax
            var stringBuilder = new StringBuilder().AppendLine($@"/*
License information for Light.GuardClauses

The MIT License (MIT)
Copyright (c) 2016 - 2018 Kenny Pflug mailto:kenny.pflug@live.de

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the ""Software""), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
#if !NET35_CF
using System.Linq.Expressions;
#endif
using System.Reflection;
#if (NETSTANDARD2_0 || NETSTANDARD1_0 || NET45 || SILVERLIGHT)
using System.Runtime.CompilerServices;
#endif
#if (NETSTANDARD2_0 || NET45 || NET40 || NET35)
using System.Runtime.Serialization;
#endif
using System.Text;
using System.Text.RegularExpressions;
{(_options.IncludeJetBrainsAnnotationsUsing ? "using JetBrains.Annotations;" + Environment.NewLine : string.Empty)}using {_options.BaseNamespace}.Exceptions;
using {_options.BaseNamespace}.FrameworkExtensions;

// ReSharper disable StaticMemberInGenericType

namespace {_options.BaseNamespace}
{{
}}

namespace {_options.BaseNamespace}.Exceptions
{{
}}

namespace {_options.BaseNamespace}.FrameworkExtensions
{{
}}");
            if (_options.IncludeJetBrainsAnnotations)
            {
                stringBuilder.AppendLine().AppendLine(@"/* 
License information for JetBrains.Annotations

MIT License
Copyright (c) 2016 JetBrains http://www.jetbrains.com

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the ""Software""), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. */

namespace JetBrains.Annotations
{
}");
            }

            var targetSyntaxTree = CSharpSyntaxTree.ParseText(stringBuilder.ToString());

            var targetRoot = (CompilationUnitSyntax) targetSyntaxTree.GetRoot();

            var namespaces = targetRoot.Members
                                       .OfType<NamespaceDeclarationSyntax>()
                                       .ToList();
            var defaultNamespace = namespaces.First(@namespace => @namespace.Name.ToString() == "Light.GuardClauses");
            var exceptionsNamespace = namespaces.First(@namespace => @namespace.Name.ToString() == "Light.GuardClauses.Exceptions");
            var extensionsNamespace = namespaces.First(@namespace => @namespace.Name.ToString() == "Light.GuardClauses.FrameworkExtensions");
            var jetBrainsNamespace = namespaces.FirstOrDefault(@namespace => @namespace.Name.ToString() == "JetBrains.Annotations");
            var replacedNodes = new Dictionary<NamespaceDeclarationSyntax, NamespaceDeclarationSyntax>
            {
                [defaultNamespace] = defaultNamespace,
                [exceptionsNamespace] = exceptionsNamespace,
                [extensionsNamespace] = extensionsNamespace,
            };
            if (_options.IncludeJetBrainsAnnotations)
                replacedNodes.Add(jetBrainsNamespace, jetBrainsNamespace);

            var allSourceFiles = new DirectoryInfo(_options.SourceFolder).GetFiles("*.cs", SearchOption.AllDirectories)
                                                                         .Where(f => !f.FullName.Contains("obj") &&
                                                                                     !f.FullName.Contains("bin"))
                                                                         .ToDictionary(f => f.Name);

            // Start with Check.CommonAssertions before all other files to prepare the Check class
            var currentFile = allSourceFiles["Check.CommonAssertions.cs"];

            var sourceSyntaxTree = CSharpSyntaxTree.ParseText(await currentFile.ReadContentAsync());
            var checkClassDeclaration = (ClassDeclarationSyntax)sourceSyntaxTree.GetRoot()
                                                                                .DescendantNodes()
                                                                                .First(node => node.Kind() == SyntaxKind.ClassDeclaration);
            checkClassDeclaration = checkClassDeclaration.WithModifiers(checkClassDeclaration.Modifiers.Remove(checkClassDeclaration.Modifiers.First(token => token.Kind() == SyntaxKind.PartialKeyword)));

            // Process all other files
            foreach (var fileName in allSourceFiles.Keys)
            {
                if (fileName == "Check.CommonAssertions.cs")
                    continue;

                currentFile = allSourceFiles[fileName];
                sourceSyntaxTree = CSharpSyntaxTree.ParseText(await currentFile.ReadContentAsync());
                var originalNamespace = defaultNamespace;
                if (currentFile.Directory?.Name == "FrameworkExtensions")
                    originalNamespace = extensionsNamespace;
                else if (currentFile.Directory?.Name == "Exceptions")
                    originalNamespace = exceptionsNamespace;
                else if (_options.IncludeJetBrainsAnnotations && currentFile.Name == "ReSharperAnnotations.cs")
                    originalNamespace = jetBrainsNamespace;

                // If the file contains assertions, add it to the existing Check class declaration
                if (originalNamespace == defaultNamespace && currentFile.Name.StartsWith("Check."))
                {
                    var classDeclaration = (ClassDeclarationSyntax)sourceSyntaxTree.GetRoot()
                                                                                   .DescendantNodes()
                                                                                   .First(node => node.Kind() == SyntaxKind.ClassDeclaration);
                    checkClassDeclaration = checkClassDeclaration.WithMembers(checkClassDeclaration.Members.AddRange(classDeclaration.Members));
                    continue;
                }

                // Else just get the members of the first namespace and add them to the corresponding one
                var sourceCompilationUnit = (CompilationUnitSyntax)sourceSyntaxTree.GetRoot();
                var membersToAdd = ((NamespaceDeclarationSyntax)sourceCompilationUnit.Members[0]).Members;

                // The ExpressionExtensions.cs file needs to be adjusted as it cannot be compiled for .NET 3.5 Compact Framework
                if (currentFile.Name == "ExpressionExtensions.cs")
                {
                    var expressionExtensionsClass = (ClassDeclarationSyntax)membersToAdd[0];
                    expressionExtensionsClass =
                        expressionExtensionsClass
                           .WithLeadingTrivia(
                                TriviaList(
                                    Trivia(
                                        IfDirectiveTrivia(
                                                PrefixUnaryExpression(
                                                    SyntaxKind.LogicalNotExpression,
                                                    IdentifierName("NET35_CF")),
                                                true,
                                                true,
                                                true)))
                                   .AddRange(
                                        expressionExtensionsClass.GetLeadingTrivia()))
                           .WithTrailingTrivia(
                                TriviaList(
                                    Trivia(
                                        EndIfDirectiveTrivia(true))))
                           .NormalizeWhitespace();

                    membersToAdd = List<MemberDeclarationSyntax>(new[] { expressionExtensionsClass });
                }

                var currentlyEditedNamespace = replacedNodes[originalNamespace];
                replacedNodes[originalNamespace] = currentlyEditedNamespace
                   .WithMembers(
                        currentlyEditedNamespace.Members.AddRange(membersToAdd));
            }

            // After the Check class declaration is finished, insert it into the default namespace
            var currentDefaultNamespace = replacedNodes[defaultNamespace];
            replacedNodes[defaultNamespace] = currentDefaultNamespace.WithMembers(currentDefaultNamespace.Members.Insert(0, checkClassDeclaration));

            // Update the target compilation unit
            targetRoot = targetRoot.ReplaceNodes(replacedNodes.Keys, (originalNode, _) => replacedNodes[originalNode]).NormalizeWhitespace();

            // Make types internal if necessary
            if (_options.ChangePublicTypesToInternalTypes)
            {
                var changedTypeDeclarations = new Dictionary<BaseTypeDeclarationSyntax, BaseTypeDeclarationSyntax>();

                foreach (var typeDeclaration in targetRoot.DescendantNodes().Where(node => node.Kind() == SyntaxKind.ClassDeclaration ||
                                                                                           node.Kind() == SyntaxKind.StructDeclaration)
                                                                            .Cast<BaseTypeDeclarationSyntax>())
                {
                    var publicModifier = typeDeclaration.Modifiers[0];
                    var adjustedModifiers =
                        typeDeclaration
                        .Modifiers
                        .RemoveAt(0)
                        .Insert(0, Token(SyntaxKind.InternalKeyword).WithTriviaFrom(publicModifier));

                    if (typeDeclaration is ClassDeclarationSyntax classDeclaration)
                        changedTypeDeclarations[classDeclaration] = classDeclaration.WithModifiers(adjustedModifiers);

                    else if (typeDeclaration is StructDeclarationSyntax structDeclaration)
                        changedTypeDeclarations[structDeclaration] = structDeclaration.WithModifiers(adjustedModifiers);
                }

                targetRoot = targetRoot.ReplaceNodes(changedTypeDeclarations.Keys, (originalNode, _) => changedTypeDeclarations[originalNode]);
            }

            // Remove preprocessor directives if necessary
            var targetFileContent = targetRoot.ToFullString();
            if (_options.RemovePreprocessorDirectives)
                targetFileContent = new UndefineTransformation().Undefine(targetFileContent, _options.DefinedPreprocessorSymbols).ToString();

            targetFileContent = CleanupStep.Cleanup(targetFileContent, _options.RemoveContractAnnotations).ToString();

            // Write the target file 
            await File.WriteAllTextAsync(_options.TargetFile, targetFileContent);
        }
    }
}