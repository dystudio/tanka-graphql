﻿using System.Collections.Generic;

namespace tanka.graphql.type
{
    public sealed class Fields : Dictionary<string, IField>
    {
        public void Add(string key, IGraphQLType type, Args arguments = null, Meta meta = null, object defaultValue = null)
        {
            Add(key, new Field(type, arguments, meta, defaultValue));
        }
    }
}