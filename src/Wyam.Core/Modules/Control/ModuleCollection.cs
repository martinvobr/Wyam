﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wyam.Common.Documents;
using Wyam.Common.Modules;
using Wyam.Common.Pipelines;

namespace Wyam.Core.Modules.Control
{
    /// <summary>
    /// Executes child modules enabling better reuse. 
    /// </summary>
    /// <remarks>
    /// All child modules will be executed as if they were inline. This enables you to specify
    /// a sequence of modules outside of a pipeline and then reuse that sequence of modules
    /// in multiple pipelines. Note that this module is also handly for wrapping a single module
    /// that has a complex configuration if you expect to use it in multiple places.
    /// </remarks>
    /// <example>
    /// <code>
    /// Modules common = Modules(ModuleA(), ModuleB(), ModuleC());
    /// 
    /// Piplines.Add("A",
    ///     ModuleX(),
    ///     ModuleY(),
    ///     common,
    ///     ModuleZ()
    /// );
    /// 
    /// Piplines.Add("B",
    ///     ModuleX(),
    ///     common,
    ///     ModuleZ()
    /// );
    /// </code>
    /// </example>
    /// <category>Control</category>
    public class ModuleCollection : IModule
    {
        private readonly IModule[] _modules;

        /// <summary>
        /// Creates the Modules module with the specified child modules.
        /// </summary>
        /// <param name="modules">The child modules.</param>
        public ModuleCollection(params IModule[] modules)
        {
            _modules = modules;
        }

        public IEnumerable<IDocument> Execute(IReadOnlyList<IDocument> inputs, IExecutionContext context)
        {
            return context.Execute(_modules, inputs);
        }
    }
}
