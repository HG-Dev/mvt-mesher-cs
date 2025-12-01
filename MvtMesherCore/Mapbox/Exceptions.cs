using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace MvtMesherCore.Mapbox;

/// <summary>
/// Exception for PBF validation failures.
/// </summary>
public class PbfValidationFailure : DataException
{
    /// <summary>
    /// The validation issue encountered.
    /// </summary>
    public readonly PbfValidation Issue;

    /// <summary>
    /// Creates a new PbfValidationFailure exception.
    /// </summary>
    public PbfValidationFailure(PbfValidation issue, string message) : base(message)
    {
        Issue = issue;
    }

    /// <summary>
    /// Creates a new PbfValidationFailure exception with an inner exception.
    /// </summary>    
    public PbfValidationFailure(PbfValidation issue, string message, Exception innerException)
        : base(message, innerException)
    {
        Issue = issue;
    }
    
    /// <summary>
    /// Creates a Tags-type PbfValidationFailure from a collection of unexpected tags.
    /// </summary>
    /// <param name="tags">Unexpected tags to be included in the exception message.</param>
    /// <param name="callerName">Context in which the unexpected tags were found.</param>
    /// <exception cref="ArgumentException">Thrown when no tags are given</exception>
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