using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace DotLiquid
{
    /// <summary>
    /// Rendering parameters
    /// </summary>
    public class RenderParameters
    {
        /// <summary>
        /// If you provide a Context object, you do not need to set any other parameters.
        /// </summary>
        public Context Context { get; set; }

        /// <summary>
        /// Hash of local variables used during rendering
        /// </summary>
        public Hash LocalVariables { get; set; }

        /// <summary>
        /// Filters used during rendering
        /// </summary>
        public IEnumerable<Type> Filters { get; set; }

        /// <summary>
        /// Hash of user-defined, internally-available variables
        /// </summary>
        public Hash Registers { get; set; }

        /// <summary>
        /// Gets or sets a value that controls whether errors are thrown as exceptions.
        /// </summary>
        [Obsolete("Use ErrorsOutputMode instead")]
        public bool RethrowErrors
        {
            get { return (ErrorsOutputMode == ErrorsOutputMode.Rethrow); }
            set { ErrorsOutputMode = (value ? ErrorsOutputMode.Rethrow : ErrorsOutputMode.Display); }
        }

        /// <summary>
        /// Errors output mode
        /// </summary>
        public ErrorsOutputMode ErrorsOutputMode { get; set; } = ErrorsOutputMode.Display;

        /// <summary>
        /// Liquid syntax flag used for backward compatibility
        /// </summary>
        public SyntaxCompatibility SyntaxCompatibilityLevel { get; set; }

        /// <summary>
        /// Maximum number of iterations for the For tag
        /// </summary>
        public int MaxIterations { get; set; } = 0;
        public IFormatProvider FormatProvider { get; }

        public RenderParameters(IFormatProvider formatProvider)
        {
            FormatProvider = formatProvider ?? throw new ArgumentNullException( nameof(formatProvider) );
            SyntaxCompatibilityLevel = Template.DefaultSyntaxCompatibilityLevel;
        }

        /// <summary>
        /// Rendering timeout in ms
        /// </summary>
        public int Timeout { get; set; } = 0;

        internal void Evaluate(Template template, out Context context, out Hash registers, out IEnumerable<Type> filters)
        {
            if (Context != null)
            {
                context = Context;
                registers = null;
                filters = null;
                context.RestartTimeout();
                return;
            }

            List<Hash> environments = new List<Hash>();
            if (LocalVariables != null)
            {
                environments.Add(LocalVariables);
            }
            CancellationToken token = CancellationToken.None;
            if (Timeout != 0)
            {
                CancellationTokenSource source = new CancellationTokenSource(Timeout);
                token = source.Token;
            }
            if (template.IsThreadSafe)
            {
                context = new Context(environments, new Hash(), new Hash(), ErrorsOutputMode, MaxIterations, FormatProvider, token)
                {
                    SyntaxCompatibilityLevel = this.SyntaxCompatibilityLevel
                };
            }
            else
            {
                environments.Add(template.Assigns);
                context = new Context(environments, template.InstanceAssigns, template.Registers, ErrorsOutputMode, MaxIterations, FormatProvider, token)
                {
                    SyntaxCompatibilityLevel = this.SyntaxCompatibilityLevel
                };
            }
            registers = Registers;
            filters = Filters;
        }

        /// <summary>
        /// Creates a RenderParameters from a context
        /// </summary>
        /// <param name="context"></param>
        /// <param name="formatProvider"></param>
        /// <returns></returns>
        public static RenderParameters FromContext(Context context, IFormatProvider formatProvider)
        {
            if (context == null)
                throw new ArgumentNullException( nameof(context) );
            return new RenderParameters(formatProvider) { Context = context };
        }
    }
}
