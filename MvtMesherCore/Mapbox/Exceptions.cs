using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace MvtMesherCore.Mapbox;

public class PbfValidationFailure : DataException
{
    public readonly PbfValidation Issue;

    public PbfValidationFailure(PbfValidation issue, string message) : base(message)
    {
        Issue = issue;
    }
    
    public PbfValidationFailure(PbfValidation issue, string message, Exception innerException)
        : base(message, innerException)
    {
        Issue = issue;
    }
    
    public static PbfValidationFailure FromTags(IReadOnlyCollection<PbfTag> tags, [System.Runtime.CompilerServices.CallerMemberName] string callerName = "")
    {
        if (tags.Count < 1)
            throw new ArgumentException("Expected at least one tag");
        
        var sb = new StringBuilder(tags.Count.ToString());
        sb.Append(" unexpected tags found in ");
        sb.Append(callerName);
        sb.AppendJoin(", ", tags);
        return new PbfValidationFailure(PbfValidation.Tags, sb.ToString());
    }
}