﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GraphQLParser.AST;
using tanka.graphql.type.converters;

namespace tanka.graphql.type
{
    public class EnumType : INamedType, IValueConverter, IDescribable, IHasDirectives
    {
        private readonly EnumValues _values = new EnumValues();
        private readonly DirectiveList _directives;

        public EnumType(
            string name,
            EnumValues values,
            string description = null,
            IEnumerable<DirectiveInstance> directives = null)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));

            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            _directives = new DirectiveList(directives);

            foreach (var enumValue in values)
            {
                var value = enumValue.Value ?? new EnumValue(string.Empty);
                _values[enumValue.Key.ToUpperInvariant()] = value;
            }
        }

        public IEnumerable<KeyValuePair<string, EnumValue>> Values => _values;

        public string Description { get; }

        public IEnumerable<DirectiveInstance> Directives => _directives;

        public DirectiveInstance GetDirective(string name)
        {
            return _directives.GetDirective(name);
        }

        public bool HasDirective(string name)
        {
            return _directives.HasDirective(name);
        }

        public string Name { get; }

        public object Serialize(object value)
        {
            var enumValue = _values.SingleOrDefault(v => v.Key == value?
                                                             .ToString()
                                                             .ToUpperInvariant());
            return enumValue.Key;
        }

        public object ParseValue(object input)
        {
            if (input == null)
                return null;

            var stringInput = Convert.ToString(input, CultureInfo.InvariantCulture)
                ?.ToUpperInvariant();

            if (stringInput == null || !_values.ContainsKey(stringInput)) return null;

            var value = _values.SingleOrDefault(v => v.Key == stringInput);
            return value.Key;
        }

        public object ParseLiteral(GraphQLScalarValue input)
        {
            var value = _values.SingleOrDefault(v => v.Key == input.Value?.ToUpperInvariant());
            return value.Key;
        }

        public override string ToString()
        {
            return Name;
        }

        public bool Contains(string value)
        {
            return _values.ContainsKey(value);
        }
    }
}