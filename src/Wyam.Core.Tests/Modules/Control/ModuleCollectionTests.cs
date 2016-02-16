﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Wyam.Testing;

namespace Wyam.Core.Tests.Modules.Control
{
    [TestFixture]
    [Parallelizable(ParallelScope.Self | ParallelScope.Children)]
    public class ModuleCollectionTests : BaseFixture
    {
        public class ExecuteMethodTests : ModuleCollectionTests
        {
            [Test]
            public void ChildModulesAreExecuted()
            {
                // Given
                Engine engine = new Engine();
                engine.CleanOutputPathOnExecute = false;
                CountModule a = new CountModule("A")
                {
                    AdditionalOutputs = 1
                };
                CountModule b = new CountModule("B")
                {
                    AdditionalOutputs = 2
                };
                CountModule c = new CountModule("C")
                {
                    AdditionalOutputs = 3
                };
                engine.Pipelines.Add(a, new Core.Modules.Control.ModuleCollection(b, c));

                // When
                engine.Execute();

                // Then
                Assert.AreEqual(1, a.ExecuteCount);
                Assert.AreEqual(1, b.ExecuteCount);
                Assert.AreEqual(1, c.ExecuteCount);
                Assert.AreEqual(1, a.InputCount);
                Assert.AreEqual(2, b.InputCount);
                Assert.AreEqual(6, c.InputCount);
                Assert.AreEqual(2, a.OutputCount);
                Assert.AreEqual(6, b.OutputCount);
                Assert.AreEqual(24, c.OutputCount);
            }

        }
    }
}
