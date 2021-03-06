## Directives

> [Specification](https://facebook.github.io/graphql/June2018/#sec-Type-System.Directives)

Directives are created as instance of `DirectiveType`. When adding to target instance of `DirectiveInstance` is used. It contains a reference back to the original type.

Following directives are provided as static properties of `DirectiveType`

[{tanka.graphql.type.DirectiveType.Skip}]

[{tanka.graphql.type.DirectiveType.Include}]

[{tanka.graphql.type.DirectiveType.Deprecated}]



### Create custom directive

Create simple `DirectiveType` and apply instance of it to a field and modify resolver logic to execute custom logic if it has the directive present.

This example will require a role from user when trying to resolve a field value
[{tanka.graphql.tests.type.DirectiveTypeFacts.Authorize_field_directive_sdl}]

DirectiveVisitor applies the actual middleware to the resolver chain
[{tanka.graphql.tests.type.DirectiveTypeFacts.AuthorizeVisitor}]




