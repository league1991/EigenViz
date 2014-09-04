// Copyright (c) 2013 Sergiu Dotenco
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Caustic.Visualizers {
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [ProvideService(typeof(IMatVisualizerService), ServiceName = "EigenMatVisualizerService")]
    [InstalledProductRegistration("Eigen Matrix Visualizer", "Eigen Matrix Visualizer", "1.0")]
    [Guid("DD87C361-AE43-4048-B93C-64AAE7551CA2")]

    public sealed class MatVisualizerPackage : Package {
        /// <summary>
        ///     Initialization of the package; register vector visualizer service
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            IServiceContainer serviceContainer = this;

            serviceContainer.AddService(typeof(IMatVisualizerService), new MatVisualizerService(),
                true);
        }
    }
}