using System;

namespace ScrollerMapper.Writers
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field 
    )]
    internal class CommentsAttribute : Attribute {
        public string Comment { get; }

        public CommentsAttribute(string comment)
        {
            Comment = comment;
        }
    }
}