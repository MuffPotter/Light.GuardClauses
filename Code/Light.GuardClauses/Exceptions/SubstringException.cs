﻿using System;
using System.Runtime.Serialization;

namespace Light.GuardClauses.Exceptions
{
    /// <summary>
    /// This exception indicates that a string is in an invalid state.
    /// </summary>
    [Serializable]
    public class SubstringException : StringException
    {
        /// <summary>
        /// Creates a new instance of <see cref="SubstringException" />.
        /// </summary>
        /// <param name="parameterName">The name of the parameter (optional).</param>
        /// <param name="message">The message of the exception (optional).</param>
        public SubstringException(string? parameterName = null, string? message = null) : base(parameterName, message) { }

        /// <inheritdoc />
        protected SubstringException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
