﻿using System.Collections.Generic;

namespace tanka.graphql.type
{
    public sealed class InputFields : Dictionary<string, InputObjectField>
    {
        public void Add(string name, EnumType type, object defaultValue = null, Meta meta = null)
        {
            Add(name, new InputObjectField(type, meta, defaultValue));
        }

        public void Add(string name, InputObjectType type, object defaultValue = null, Meta meta = null)
        {
            Add(name, new InputObjectField(type, meta, defaultValue));
        }

        public void Add(string name, ScalarType type, object defaultValue = null, Meta meta = null)
        {
            Add(name, new InputObjectField(type, meta, defaultValue));
        }

        public void Add(string name, List type, object defaultValue = null, Meta meta = null)
        {
            Add(name, new InputObjectField(type, meta, defaultValue));
        }

        public void Add(string name, NonNull type, object defaultValue = null, Meta meta = null)
        {
            Add(name, new InputObjectField(type, meta, defaultValue));
        }
    }
}